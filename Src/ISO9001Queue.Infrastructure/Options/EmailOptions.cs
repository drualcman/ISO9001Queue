namespace ISO9001Queue.Infrastructure.Options;

public class EmailOptions
{
    public const string SectionKey = nameof(EmailOptions);
    public string Url { get; set; } = "https://api.community-mall.com/messaging/";
    public int CompanyId { get; set; } = 5;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminName { get; set; } = "Admin";
}
