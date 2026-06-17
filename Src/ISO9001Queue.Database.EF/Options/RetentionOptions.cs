namespace ISO9001Queue.Database.EF.Options;

public class RetentionOptions
{
    public const string SectionKey = nameof(RetentionOptions);

    /// <summary>Maximum number of characters kept in the debug <c>Data</c> column when writing.</summary>
    public int MaxDataLength { get; set; } = 4000;

    /// <summary>Days the debug <c>Data</c> blob is kept before being cleared (set to empty).</summary>
    public int DataRetentionDays { get; set; } = 7;

    /// <summary>Months a full record is kept before the row is deleted.</summary>
    public int RecordRetentionMonths { get; set; } = 36;
}
