namespace Shane32.CsvLinq;

/// <summary>
/// Represents an error caused by an empty row when required columns are configured.
/// </summary>
public class RowEmptyException : InvalidDataException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RowEmptyException" /> class.
    /// </summary>
    /// <param name="rowNumber">The one-based row number that is empty.</param>
    public RowEmptyException(int rowNumber) : base($"Empty row found on row {rowNumber} with required columns")
    {
    }
}
