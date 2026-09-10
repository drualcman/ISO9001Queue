using System.IO.Compression;

namespace ISO9001Queue.Infrastructure.Email;

/// <summary>
/// Turns the export into the file that travels in the email: a zip with the JSON inside.
/// </summary>
/// <remarks>
/// El JSON en crudo se manda dentro de un zip porque el de una cuenta activa —cada operación deja su
/// registro de auditoría— llega a decenas de MB, y ese adjunto viaja en Base64 dentro de una petición
/// JSON a la API de correo: o lo rechaza o se pasa del tiempo de espera, y el usuario se queda sin
/// nada. Un registro de auditoría comprime muchísimo (es texto repetitivo), así que el zip resuelve el
/// caso normal; y si aun así no cabe, se dice con el tamaño exacto en vez de pelearse cinco veces con
/// la API.
/// </remarks>
internal static class UserDataAttachmentBuilder
{
    public static EmailAttachment Build(byte[] jsonData, string timestamp, long maxBytes, ILogger logger)
    {
        byte[] compressed = Compress(jsonData, $"quality-data-{timestamp}.json");
        logger.LogInformation(
            "Quality data export packed: {RawBytes} bytes of JSON -> {ZipBytes} bytes zipped",
            jsonData.LongLength, compressed.LongLength);

        if (compressed.LongLength > maxBytes)
            throw new InvalidOperationException(
                $"The quality data export is {compressed.LongLength} bytes zipped, over the {maxBytes} bytes " +
                "an email can carry. It has to be delivered as a download link instead.");

        return new EmailAttachment($"quality-data-{timestamp}.zip", compressed);
    }

    private static byte[] Compress(byte[] jsonData, string entryName)
    {
        using MemoryStream output = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using Stream entryStream = entry.Open();
            entryStream.Write(jsonData, 0, jsonData.Length);
        }

        return output.ToArray();
    }
}
