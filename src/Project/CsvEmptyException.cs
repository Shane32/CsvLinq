namespace Shane32.CsvLinq;

/// <summary>
/// Represents an error caused by CSV data that does not contain a header row.
/// </summary>
public class CsvEmptyException : InvalidDataException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvEmptyException" /> class.
    /// </summary>
    public CsvEmptyException() : base("The CSV file does not contain a header row")
    {
    }
}
