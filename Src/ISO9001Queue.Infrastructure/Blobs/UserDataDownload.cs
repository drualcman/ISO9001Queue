namespace ISO9001Queue.Infrastructure.Blobs;

/// <summary>A published export: where to download it from and how long that link works.</summary>
public record UserDataDownload(string Url, int Days);
