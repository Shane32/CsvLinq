namespace Shane32.CsvLinq;

/// <summary>
/// Specifies how line endings inside CSV field values are handled.
/// </summary>
public enum CsvLineEndingHandling
{
    /// <summary>
    /// Allows line endings inside CSV field values.
    /// </summary>
    Allow,

    /// <summary>
    /// Replaces line endings inside CSV field values.
    /// </summary>
    Replace,

    /// <summary>
    /// Rejects line endings inside CSV field values.
    /// </summary>
    Reject
}
