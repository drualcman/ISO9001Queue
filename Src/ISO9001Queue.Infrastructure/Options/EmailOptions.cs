namespace ISO9001Queue.Infrastructure.Options;

public class EmailOptions
{
    public const string SectionKey = nameof(EmailOptions);
    public string Url { get; set; } = "https://api.community-mall.com/messaging/";
    public int CompanyId { get; set; } = 5;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminName { get; set; } = "Admin";

    /// <summary>
    /// How long to wait for the messaging API. The default HttpClient timeout is 100 seconds, which a
    /// data export with a real attachment goes over: the request is cancelled, the queue message is
    /// retried and after five attempts it lands in the poison queue without a single email sent.
    /// </summary>
    public int TimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Biggest attachment that is worth sending. Providers cap at ~25 MB and that cap applies to the
    /// Base64 payload, which inflates raw bytes by ~37%; anything above this is rejected, so it is
    /// caught here — with the real size in the log — instead of failing five times against the API.
    /// </summary>
    public long MaxAttachmentBytes { get; set; } = 15 * 1024 * 1024;
}
