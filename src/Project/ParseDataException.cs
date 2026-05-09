using System;

namespace Shane32.CsvLinq;

/// <summary>
/// Represents an error caused by a field value that could not be parsed.
/// </summary>
public class ParseDataException : InvalidDataException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParseDataException" /> class.
    /// </summary>
    /// <param name="rowNumber">The one-based row number containing the field.</param>
    /// <param name="columnNumber">The one-based column number containing the field.</param>
    /// <param name="columnName">The configured column name.</param>
    /// <param name="innerException">The exception that caused the parse failure.</param>
    public ParseDataException(int rowNumber, int columnNumber, string columnName, Exception innerException)
        : base($"Could not parse row {rowNumber}, column {columnNumber} ('{columnName}')", innerException)
    {
    }
}
