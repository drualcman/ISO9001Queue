namespace ISO9001Queue.Infrastructure.Options;

/// <summary>
/// Where a data export that is too big to travel attached is left for the user to download. The
/// connection string falls back to the host's "Blob" setting — the same account the queue triggers use.
/// </summary>
/// <remarks>
/// El contenedor es el de los respaldos, que ya borra solo sus blobs a los 7 días: por eso aquí no hay
/// ninguna limpieza propia. El enlace es una SAS de sólo lectura con ese mismo plazo, así que caduca a
/// la vez que desaparece el fichero.
/// </remarks>
public class UserDataBlobOptions
{
    public const string SectionKey = nameof(UserDataBlobOptions);

    /// <summary>Blob storage connection string. Empty => falls back to the host "Blob" app setting.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Private container for data exports. Never the public one used by the feedback summary.</summary>
    public string Container { get; set; } = "backups";
}
