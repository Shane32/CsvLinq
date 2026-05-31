using System;

namespace Shane32.CsvLinq;

/// <summary>
/// Defines CSV formatting and parsing options.
/// </summary>
public sealed class CsvOptions
{
    /// <summary>
    /// Gets or sets the line ending used when writing CSV records.
    /// </summary>
    public string LineEnding { get; internal set; } = Environment.NewLine;

    /// <summary>
    /// Gets or sets how line endings inside CSV field values are handled.
    /// </summary>
    public CsvLineEndingHandling LineEndingsInStrings { get; internal set; } = CsvLineEndingHandling.Allow;

    /// <summary>
    /// Gets or sets the replacement text used when replacing line endings inside CSV field values.
    /// </summary>
    public string LineEndingReplacement { get; internal set; } = " ";

    /// <summary>
    /// Gets or sets a value indicating whether written CSV data ends with a final line ending.
    /// </summary>
    public bool EndsWithNewLine { get; internal set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether CSV data includes a header row.
    /// </summary>
    public bool HasHeaderRow { get; internal set; } = true;
}
