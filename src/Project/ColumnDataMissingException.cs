namespace Shane32.CsvLinq;

/// <summary>
/// Represents an error caused by a missing required value in a CSV column.
/// </summary>
public class ColumnDataMissingException : InvalidDataException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnDataMissingException" /> class.
    /// </summary>
    /// <param name="columnName">The name of the column with missing data.</param>
    /// <param name="rowNumber">The one-based row number containing the missing data.</param>
    public ColumnDataMissingException(string columnName, int rowNumber) : base($"Missing required data in column '{columnName}' on row {rowNumber}")
    {
    }
}
