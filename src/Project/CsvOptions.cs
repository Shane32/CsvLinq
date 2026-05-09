using System;

namespace Shane32.CsvLinq;

/// <summary>
/// Defines CSV formatting and parsing options.
/// </summary>
internal sealed class CsvOptions
{
    /// <summary>
    /// Gets or sets the line ending used when writing CSV records.
    /// </summary>
    public string LineEnding { get; set; } = Environment.NewLine;

    /// <summary>
    /// Gets or sets how line endings inside CSV field values are handled.
    /// </summary>
    public CsvLineEndingHandling LineEndingsInStrings { get; set; } = CsvLineEndingHandling.Allow;

    /// <summary>
    /// Gets or sets the replacement text used when replacing line endings inside CSV field values.
    /// </summary>
    public string LineEndingReplacement { get; set; } = " ";

    /// <summary>
    /// Gets or sets a value indicating whether written CSV data ends with a final line ending.
    /// </summary>
    public bool EndsWithNewLine { get; set; } = true;
}
