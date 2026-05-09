namespace Shane32.CsvLinq;

/// <summary>
/// Represents an error caused by multiple CSV headers that map to the same configured column.
/// </summary>
public class DuplicateColumnException : InvalidDataException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateColumnException" /> class.
    /// </summary>
    /// <param name="columnName">The duplicated column name.</param>
    public DuplicateColumnException(string columnName) : base($"Duplicate column '{columnName}' detected")
    {
    }
}
