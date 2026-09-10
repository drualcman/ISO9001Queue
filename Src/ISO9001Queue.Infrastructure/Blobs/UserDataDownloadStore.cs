using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace ISO9001Queue.Infrastructure.Blobs;

internal sealed class UserDataDownloadStore(
    IOptions<UserDataBlobOptions> options,
    ILogger<UserDataDownloadStore> logger) : IUserDataDownloadStore
{
    /// <summary>
    /// Días que vive el enlace. Es el mismo plazo que el contenedor de respaldos borra sus blobs, así
    /// que el enlace nunca sobrevive al fichero ni el fichero al enlace. Si cambia la regla del
    /// contenedor, este número cambia con ella.
    /// </summary>
    private const int LinkDays = 7;

    public async Task<UserDataDownload> PublishAsync(string fileName, byte[] content,
        CancellationToken cancellationToken = default)
    {
        UserDataBlobOptions settings = options.Value;
        BlobClient blob = await UploadAsync(settings, fileName, content, cancellationToken);

        // Sin clave de cuenta no se puede firmar un enlace, y el contenedor es privado: mejor decirlo
        // que dejar al usuario con una URL que le va a dar 404.
        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException(
                "The storage connection string has no account key, so no download link can be signed for the data export.");

        BlobSasBuilder permissions = new BlobSasBuilder
        {
            BlobContainerName = settings.Container,
            BlobName = fileName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddDays(LinkDays)
        };
        permissions.SetPermissions(BlobSasPermissions.Read);

        logger.LogInformation(
            "Data export published for download: {Container}/{BlobName}, {Bytes} bytes, {Days} day(s)",
            settings.Container, fileName, content.LongLength, LinkDays);

        return new UserDataDownload(blob.GenerateSasUri(permissions).ToString(), LinkDays);
    }

    private static async Task<BlobClient> UploadAsync(UserDataBlobOptions settings, string fileName,
        byte[] content, CancellationToken cancellationToken)
    {
        BlobContainerClient container = new BlobServiceClient(ConnectionStringOf(settings))
            .GetBlobContainerClient(settings.Container);
        // Privado: lo que se guarda aquí son datos personales, y el único acceso es el enlace firmado.
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        BlobClient blob = container.GetBlobClient(fileName);
        BlobHttpHeaders headers = new BlobHttpHeaders { ContentType = "application/zip" };
        using MemoryStream stream = new MemoryStream(content);
        await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken);

        return blob;
    }

    private static string ConnectionStringOf(UserDataBlobOptions settings) =>
        string.IsNullOrWhiteSpace(settings.ConnectionString)
            ? throw new InvalidOperationException(
                "No blob connection string configured for data exports (UserDataBlobOptions:ConnectionString or the host \"Blob\" setting).")
            : settings.ConnectionString;
}
