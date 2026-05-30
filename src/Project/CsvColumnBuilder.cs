using System;

namespace Shane32.CsvLinq;

/// <summary>
/// Configures a CSV column mapped to a model member.
/// </summary>
/// <typeparam name="TModel">The row model type.</typeparam>
/// <typeparam name="TValue">The column value type.</typeparam>
public sealed class CsvColumnBuilder<TModel, TValue>
    where TModel : class, new()
{
    private readonly CsvModelBuilder<TModel> _modelBuilder;
    private readonly CsvColumnModel _column;

    internal CsvColumnBuilder(CsvModelBuilder<TModel> modelBuilder, CsvColumnModel column)
    {
        _modelBuilder = modelBuilder;
        _column = column;
    }

    /// <summary>
    /// Adds an alternate header name that can map to this column when loading CSV data.
    /// </summary>
    /// <param name="name">The alternate header name.</param>
    /// <returns>The column builder.</returns>
    public CsvColumnBuilder<TModel, TValue> AlternateName(string name)
    {
        _column.AlternateNames.Add(name?.Trim() ?? throw new ArgumentNullException(nameof(name)));
        _modelBuilder.AddHeader(name, _column);
        return this;
    }

    /// <summary>
    /// Marks the column as optional when loading CSV data.
    /// </summary>
    /// <returns>The column builder.</returns>
    public CsvColumnBuilder<TModel, TValue> Optional()
    {
        _column.Optional = true;
        return this;
    }

    /// <summary>
    /// Marks the column as required when loading CSV data.
    /// </summary>
    /// <returns>The column builder.</returns>
    public CsvColumnBuilder<TModel, TValue> Required()
    {
        _column.Optional = false;
        return this;
    }

    /// <summary>
    /// Configures whether blank field values deserialize as <see langword="null" /> for string columns.
    /// </summary>
    /// <param name="nullable">A value indicating whether blank string field values deserialize as <see langword="null" />.</param>
    /// <returns>The column builder.</returns>
    public CsvColumnBuilder<TModel, TValue> Nullable(bool nullable = true)
    {
        if (_column.Type != typeof(string))
            throw new InvalidOperationException("Nullability can only be configured for string columns.");
        _column.StringValueNullable = nullable;
        return this;
    }

    /// <summary>
    /// Configures a deserializer for this column.
    /// </summary>
    /// <param name="deserialize">The function used to deserialize field values.</param>
    /// <returns>The column builder.</returns>
    public CsvColumnBuilder<TModel, TValue> Deserialize(Func<string, TValue> deserialize)
    {
        _column.Deserializer = deserialize == null ? (Func<string, object>)null : value => deserialize(value);
        return this;
    }

    /// <summary>
    /// Configures a serializer for this column.
    /// </summary>
    /// <param name="serialize">The function used to serialize values.</param>
    /// <returns>The column builder.</returns>
    public CsvColumnBuilder<TModel, TValue> Serialize(Func<TValue, string> serialize)
    {
        _column.Serializer = serialize == null ? (Func<object, string>)null : value => serialize((TValue)value);
        return this;
    }
}
