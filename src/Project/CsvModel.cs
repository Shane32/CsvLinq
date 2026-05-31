using System;
using System.Collections.Generic;

namespace Shane32.CsvLinq;

/// <summary>
/// Describes the CSV mapping for a row model.
/// </summary>
/// <typeparam name="TModel">The row model type.</typeparam>
public sealed class CsvModel<TModel>
    where TModel : class, new()
{
    private readonly Dictionary<string, CsvColumnModel> _columnLookup;
    private readonly Dictionary<Type, ICsvFormatter> _formatters;

    internal CsvModel(
        CsvColumnModel[] columns,
        Dictionary<string, CsvColumnModel> columnLookup,
        Dictionary<Type, ICsvFormatter> formatters,
        CsvOptions options,
        bool skipEmptyRows)
    {
        Columns = columns;
        _columnLookup = new Dictionary<string, CsvColumnModel>(columnLookup, StringComparer.OrdinalIgnoreCase);
        _formatters = new Dictionary<Type, ICsvFormatter>(formatters);
        Options = options;
        SkipEmptyRows = skipEmptyRows;
    }

    /// <summary>
    /// Gets the configured CSV columns.
    /// </summary>
    public IReadOnlyList<CsvColumnModel> Columns { get; }

    /// <summary>
    /// Gets CSV formatting and parsing options.
    /// </summary>
    public CsvOptions Options { get; }

    /// <summary>
    /// Gets a value indicating whether empty rows are skipped when loading CSV data.
    /// </summary>
    public bool SkipEmptyRows { get; }

    /// <summary>
    /// Tries to find a configured column by CSV header name.
    /// </summary>
    /// <param name="headerName">The CSV header name.</param>
    /// <param name="column">When this method returns, contains the matching column when found.</param>
    /// <returns><see langword="true" /> when a matching column is found; otherwise, <see langword="false" />.</returns>
    public bool TryGetColumn(string headerName, out CsvColumnModel column)
        => _columnLookup.TryGetValue((headerName ?? string.Empty).Trim(), out column);

    internal bool TryGetFormatter(Type type, out ICsvFormatter formatter)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        return _formatters.TryGetValue(effectiveType, out formatter);
    }
}
