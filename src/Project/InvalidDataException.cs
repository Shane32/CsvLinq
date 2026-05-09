using System;

namespace Shane32.CsvLinq;

/// <summary>
/// Represents an error caused by invalid CSV data.
/// </summary>
public class InvalidDataException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidDataException" /> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public InvalidDataException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidDataException" /> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public InvalidDataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
