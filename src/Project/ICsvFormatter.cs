namespace Shane32.CsvLinq;

/// <summary>
/// Represents a CSV formatter for serialization and deserialization.
/// </summary>
public interface ICsvFormatter
{
    /// <summary>
    /// Converts CSV text to a model value.
    /// </summary>
    /// <param name="value">The CSV text value.</param>
    /// <returns>The deserialized model value.</returns>
    public object Deserialize(string value);

    /// <summary>
    /// Converts a model value to CSV text.
    /// </summary>
    /// <param name="value">The model value.</param>
    /// <returns>The serialized CSV text value.</returns>
    public string Serialize(object value);
}
