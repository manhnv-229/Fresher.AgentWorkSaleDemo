using System.Diagnostics;
using System.Text;

using Demo.Application.DTOs;
using Demo.Application.Interfaces.Service;
using Demo.Infrastructure.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Demo.Infrastructure.Services;

/// <summary>
/// Dùng LibreOffice headless để chuyển đổi file Office sang định dạng preview hỗ trợ bởi trình duyệt.
/// </summary>
public sealed class KnowledgePreviewConverter(
    IOptions<KnowledgePreviewOptions> options,
    ILogger<KnowledgePreviewConverter> logger) : IKnowledgePreviewConverter
{
    private readonly KnowledgePreviewOptions _options = options.Value;

    /// <summary>
    /// Chuyển file PPTX sang PDF bằng LibreOffice headless và trả về stream PDF đã buffer trong memory.
    /// </summary>
    public async Task<KnowledgePreviewResult> ConvertPptxToPdfAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "knowledge-preview", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var inputFilePath = Path.Combine(tempDirectory, "source.pptx");
            await using (var inputFile = File.Create(inputFilePath))
            {
                await content.CopyToAsync(inputFile, cancellationToken);
            }

            var outputFilePath = Path.Combine(tempDirectory, "source.pdf");
            await RunOfficeConversionAsync(inputFilePath, tempDirectory, cancellationToken);

            if (!File.Exists(outputFilePath))
            {
                throw new InvalidOperationException("Preview conversion did not produce a PDF output file.");
            }

            var outputBytes = await File.ReadAllBytesAsync(outputFilePath, cancellationToken);
            var previewStream = new MemoryStream(outputBytes);
            var previewFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.pdf";
            return new KnowledgePreviewResult(previewStream, previewFileName, "application/pdf", outputBytes.LongLength);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Không thể dọn dẹp thư mục tạm preview: {TempDirectory}", tempDirectory);
            }
        }
    }

    private async Task RunOfficeConversionAsync(
        string inputFilePath,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, _options.ConversionTimeoutSeconds)));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _options.OfficeCommandPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.StartInfo.ArgumentList.Add("--headless");
        process.StartInfo.ArgumentList.Add("--convert-to");
        process.StartInfo.ArgumentList.Add("pdf");
        process.StartInfo.ArgumentList.Add("--outdir");
        process.StartInfo.ArgumentList.Add(outputDirectory);
        process.StartInfo.ArgumentList.Add(inputFilePath);

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("Could not start the preview conversion process.");
            }

            await process.WaitForExitAsync(linkedCts.Token);
            var standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = await process.StandardError.ReadToEndAsync(cancellationToken);
            if (process.ExitCode != 0)
            {
                var details = BuildProcessDetails(standardOutput, standardError);
                throw new InvalidOperationException($"Preview conversion failed with exit code {process.ExitCode}.{details}");
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryTerminateProcess(process);
            throw new TimeoutException("Preview conversion timed out.");
        }
        catch (Exception)
        {
            TryTerminateProcess(process);
            throw;
        }
    }

    private static string BuildProcessDetails(string standardOutput, string standardError)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            builder.Append(" Output: ").Append(standardOutput.Trim());
        }

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            builder.Append(" Error: ").Append(standardError.Trim());
        }

        return builder.ToString();
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Bỏ qua lỗi cleanup process để giữ lỗi gốc của conversion.
        }
    }
}
