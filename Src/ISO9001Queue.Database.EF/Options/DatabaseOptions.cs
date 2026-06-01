namespace ISO9001Queue.Database.EF.Options;

public class DatabaseOptions
{
    public const string SectionKey = nameof(DatabaseOptions);
    public string ConnectionString { get; set; } = string.Empty;
}
