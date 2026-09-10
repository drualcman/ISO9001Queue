using System.IO.Compression;

namespace ISO9001Queue.Infrastructure.Email;

/// <summary>
/// Packs the export into the file that travels to the user: a zip with the JSON inside.
/// </summary>
/// <remarks>
/// El JSON en crudo de una cuenta activa —cada operación deja su registro de auditoría— llega a
/// decenas de MB, y ese adjunto viaja en Base64 dentro de una petición JSON a la API de correo: o lo
/// rechaza o se pasa del tiempo de espera, y el usuario se queda sin nada. Un registro de auditoría
/// comprime muchísimo (es texto repetitivo), así que el zip resuelve el caso normal; lo que aun así no
/// cabe se entrega como enlace de descarga.
/// </remarks>
internal static class UserDataArchiveBuilder
{
    public static byte[] Compress(byte[] jsonData, string entryName)
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
