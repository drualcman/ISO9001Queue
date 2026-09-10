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
    /// Biggest attachment that is worth sending. It is not the provider cap (~25 MB over Base64): it
    /// is a size the messaging API answers well within its timeout. A zip over this is left in blob
    /// storage and the email carries a download link instead.
    /// </summary>
    public long MaxAttachmentBytes { get; set; } = 3 * 1024 * 1024 + 333 * 1024 + 333;
}
