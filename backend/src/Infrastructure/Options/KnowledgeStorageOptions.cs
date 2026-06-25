namespace Demo.Infrastructure.Options;

public sealed class KnowledgeStorageOptions
{
    public string Endpoint { get; init; } = "localhost";

    public int Port { get; init; } = 9000;

    public string AccessKey { get; init; } = "minioadmin";

    public string SecretKey { get; init; } = "minioadmin123";

    public string Bucket { get; init; } = "knowledge";

    public string Region { get; init; } = "us-east-1";

    public bool UseSsl { get; init; }

    public bool ForcePathStyle { get; init; } = true;

    public long MaxUploadBytes { get; init; } = 25 * 1024 * 1024;

    public int RequestTimeoutSeconds { get; init; } = 15;

    public int ConnectTimeoutSeconds { get; init; } = 5;
}
