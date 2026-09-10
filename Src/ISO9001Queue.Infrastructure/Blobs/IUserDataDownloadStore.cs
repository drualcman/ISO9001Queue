namespace ISO9001Queue.Infrastructure.Blobs;

/// <summary>
/// Leaves a data export that does not fit in an email where the user can download it.
/// </summary>
public interface IUserDataDownloadStore
{
    /// <summary>
    /// Uploads the file to the export container and returns the read-only link and the days it works.
    /// Nothing deletes it here: the container removes its own blobs when they age out.
    /// </summary>
    Task<UserDataDownload> PublishAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
}
