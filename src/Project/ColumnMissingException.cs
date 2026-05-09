namespace Shane32.CsvLinq;

/// <summary>
/// Represents an error caused by a missing required CSV column.
/// </summary>
public class ColumnMissingException : InvalidDataException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnMissingException" /> class.
    /// </summary>
    /// <param name="columnName">The name of the missing column.</param>
    public ColumnMissingException(string columnName) : base($"Missing required column '{columnName}'")
    {
    }
}
