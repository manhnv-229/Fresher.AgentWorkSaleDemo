using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Service;
using Demo.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace Demo.Infrastructure.Services;

/// <summary>
/// MinIO S3-compatible storage qua HTTP client trực tiếp.
/// Bao gồm AWS Signature V4 signing, upload/download/delete object, và per-request timeout handling.
/// </summary>
public sealed class MinioKnowledgeStorageService(
    HttpClient httpClient,
    IOptions<KnowledgeStorageOptions> options) : IKnowledgeStorageService
{
#region Declaration

    /// <summary>
    /// SHA256 hash của payload rỗng. Dùng cho các request không có body (GET, DELETE) theo yêu cầu của MinIO S3.
    /// </summary>
    private const string EmptyPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    /// <summary>
    /// Options cấu hình storage: endpoint, port, credentials, bucket, timeout, và SSL settings.
    /// </summary>
    private readonly KnowledgeStorageOptions storageOptions = options.Value;

#endregion

#region Property

    /// <summary>
    /// Tên bucket MinIO được cấu hình từ appsettings.
    /// </summary>
    public string BucketName => storageOptions.Bucket;

    /// <summary>
    /// Dung lượng upload tối đa (bytes) được cấu hình từ appsettings.
    /// </summary>
    public long MaxUploadBytes => storageOptions.MaxUploadBytes;

#endregion

#region Method

    /// <summary>
    /// Upload file lên MinIO bằng HTTP PUT. Tính toán content hash, ký AWS Signature V4, và trả về ETag + VersionId.
    /// </summary>
    public async Task<KnowledgeStorageUploadResult> UploadAsync(KnowledgeStorageUploadRequest request, CancellationToken cancellationToken)
    {
        var requestUri = BuildObjectUri(request.Bucket, request.ObjectKey);
        using var content = new StreamContent(request.Content);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ContentType);
        content.Headers.ContentLength = request.SizeBytes;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = content
        };
        httpRequest.Headers.TryAddWithoutValidation("x-amz-content-sha256", request.PayloadSha256);
        Sign(httpRequest, request.PayloadSha256);

        using var response = await SendAsync(httpRequest, "upload", requestUri, cancellationToken);

        return new KnowledgeStorageUploadResult(
            request.Bucket,
            request.ObjectKey,
            response.Headers.ETag?.Tag?.Trim('"'),
            response.Headers.TryGetValues("x-amz-version-id", out var values) ? values.FirstOrDefault() : null);
    }

    /// <summary>
    /// Download file từ MinIO. Trả về stream nội dung, content type, và dung lượng.
    /// </summary>
    public async Task<KnowledgeStorageDownloadResult> DownloadAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, BuildObjectUri(bucket, objectKey));
        httpRequest.Headers.TryAddWithoutValidation("x-amz-content-sha256", EmptyPayloadHash);
        Sign(httpRequest, EmptyPayloadHash);

        var response = await SendAsync(httpRequest, "download", httpRequest.RequestUri!, cancellationToken);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var sizeBytes = response.Content.Headers.ContentLength;
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new KnowledgeStorageDownloadResult(stream, contentType, sizeBytes);
    }

    /// <summary>
    /// Xóa object khỏi MinIO. Không throw exception nếu object không tồn tại (idempotent).
    /// </summary>
    public async Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, BuildObjectUri(bucket, objectKey));
        httpRequest.Headers.TryAddWithoutValidation("x-amz-content-sha256", EmptyPayloadHash);
        Sign(httpRequest, EmptyPayloadHash);

        using var _ = await SendAsync(httpRequest, "delete", httpRequest.RequestUri!, cancellationToken);
    }

    /// <summary>
    /// Xây dựng URI cho MinIO object. Hỗ trợ both path-style và virtual-hosted-style URLs.
    /// </summary>
    private Uri BuildObjectUri(string bucket, string objectKey)
    {
        var scheme = storageOptions.UseSsl ? "https" : "http";
        var escapedKey = string.Join("/", objectKey.Split('/').Select(Uri.EscapeDataString));
        var builder = new UriBuilder(scheme, storageOptions.Endpoint, storageOptions.Port)
        {
            Path = storageOptions.ForcePathStyle
                ? $"{bucket}/{escapedKey}"
                : escapedKey,
            Host = storageOptions.ForcePathStyle ? storageOptions.Endpoint : $"{bucket}.{storageOptions.Endpoint}"
        };

        return builder.Uri;
    }

    /// <summary>
    /// Ký AWS Signature V4 cho request. Bao gồm canonical request construction, string to sign, và signing key derivation.
    /// </summary>
    private void Sign(HttpRequestMessage request, string payloadHash)
    {
        var now = DateTime.UtcNow;
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var host = request.RequestUri!.IsDefaultPort
            ? request.RequestUri.Host
            : $"{request.RequestUri.Host}:{request.RequestUri.Port}";

        request.Headers.Host = host;
        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);

        var canonicalUri = request.RequestUri.AbsolutePath;
        var canonicalHeaders = new StringBuilder()
            .Append("host:").Append(host).Append('\n')
            .Append("x-amz-content-sha256:").Append(payloadHash).Append('\n')
            .Append("x-amz-date:").Append(amzDate).Append('\n')
            .ToString();
        const string signedHeaders = "host;x-amz-content-sha256;x-amz-date";
        var canonicalRequest = string.Join('\n',
            request.Method.Method,
            canonicalUri,
            request.RequestUri.Query.TrimStart('?'),
            canonicalHeaders,
            signedHeaders,
            payloadHash);

        var credentialScope = $"{dateStamp}/{storageOptions.Region}/s3/aws4_request";
        var stringToSign = string.Join('\n',
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            HashHex(canonicalRequest));

        var signingKey = GetSignatureKey(storageOptions.SecretKey, dateStamp, storageOptions.Region, "s3");
        var signature = ToHex(Hmac(signingKey, stringToSign));
        var authorization = $"AWS4-HMAC-SHA256 Credential={storageOptions.AccessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
    }

    /// <summary>
    /// Tính SHA256 hash và chuyển sang hex string.
    /// </summary>
    private static string HashHex(string value)
    {
        return ToHex(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    /// <summary>
    /// Tạo signing key từ secret key, date stamp, region, và service name theo AWS Signature V4 specification.
    /// </summary>
    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        var dateKey = Hmac(Encoding.UTF8.GetBytes($"AWS4{key}"), dateStamp);
        var dateRegionKey = Hmac(dateKey, regionName);
        var dateRegionServiceKey = Hmac(dateRegionKey, serviceName);
        return Hmac(dateRegionServiceKey, "aws4_request");
    }

    /// <summary>
    /// Tính HMAC-SHA256 cho dữ liệu input.
    /// </summary>
    private static byte[] Hmac(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    /// <summary>
    /// Chuyển byte array sang hex string.
    /// </summary>
    private static string ToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Gửi HTTP request với per-request timeout handling. Bắt lỗi timeout và HTTP error response, chuyển sang typed exceptions.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        string operation,
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        // Tạo per-request timeout CancellationTokenSource để kiểm soát timeout cho từng request riêng biệt
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(storageOptions.RequestTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var responseBody = response.Content is null
                ? string.Empty
                : await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();

            throw new InvalidOperationException(
                $"MinIO returned {(int)response.StatusCode} {response.StatusCode} for {operation} '{requestUri}'. Response: {TrimForMessage(responseBody)}");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Timeout do per-request CancellationTokenSource gây ra, không phải do cancellationToken bên ngoài
            throw new OperationCanceledException(
                $"Storage operation '{operation}' to '{requestUri}' timed out after {storageOptions.RequestTimeoutSeconds}s.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Could not reach MinIO for {operation} '{requestUri}'. {exception.Message}",
                exception);
        }
    }

    /// <summary>
    /// Cắt ngắn response body để hiển thị trong error message, tránh log quá dài.
    /// </summary>
    private static string TrimForMessage(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "(empty response)";
        }

        var normalized = value.Trim().Replace('\n', ' ').Replace('\r', ' ');
        return normalized.Length <= 500 ? normalized : normalized[..500];
    }

#endregion
}
