using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Shane32.CsvLinq;

/// <summary>
/// Builds the CSV mapping for a row model.
/// </summary>
/// <typeparam name="TModel">The row model type.</typeparam>
public sealed class CsvModelBuilder<TModel>
    where TModel : class, new()
{
    private readonly List<CsvColumnModel> _columns = new List<CsvColumnModel>();
    private readonly Dictionary<string, CsvColumnModel> _columnLookup = new Dictionary<string, CsvColumnModel>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, ICsvFormatter> _formatters = new Dictionary<Type, ICsvFormatter>();
    private bool _skipEmptyRows;

    private CsvOptions Options { get; } = new CsvOptions();

    /// <summary>
    /// Configures the line ending used when writing CSV records.
    /// </summary>
    /// <param name="lineEnding">The line ending text.</param>
    /// <returns>The model builder.</returns>
    public CsvModelBuilder<TModel> LineEnding(string lineEnding)
    {
        Options.LineEnding = lineEnding;
        return this;
    }

    /// <summary>
    /// Configures how line endings inside CSV field values are handled.
    /// </summary>
    /// <param name="handling">The line ending handling behavior.</param>
    /// <returns>The model builder.</returns>
    public CsvModelBuilder<TModel> LineEndingsInStrings(CsvLineEndingHandling handling)
    {
        Options.LineEndingsInStrings = handling;
        return this;
    }

    /// <summary>
    /// Configures the replacement text used when replacing line endings inside CSV field values.
    /// </summary>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>The model builder.</returns>
    public CsvModelBuilder<TModel> LineEndingReplacement(string replacement)
    {
        Options.LineEndingReplacement = replacement;
        return this;
    }

    /// <summary>
    /// Configures whether written CSV data ends with a final line ending.
    /// </summary>
    /// <param name="endsWithNewLine">A value indicating whether to write a final line ending.</param>
    /// <returns>The model builder.</returns>
    public CsvModelBuilder<TModel> EndsWithNewLine(bool endsWithNewLine)
    {
        Options.EndsWithNewLine = endsWithNewLine;
        return this;
    }

    /// <summary>
    /// Configures the model to read and write CSV data without a header row.
    /// </summary>
    /// <returns>The model builder.</returns>
    public CsvModelBuilder<TModel> OmitHeaderRow()
    {
        Options.HasHeaderRow = false;
        return this;
    }

    /// <summary>
    /// Configures the model to skip empty rows when loading CSV data.
    /// </summary>
    /// <returns>The model builder.</returns>
    public CsvModelBuilder<TModel> SkipEmptyRows()
    {
        _skipEmptyRows = true;
        return this;
    }

    /// <summary>
    /// Adds a column using the referenced field or property name as the CSV header name.
    /// </summary>
    /// <typeparam name="TValue">The column value type.</typeparam>
    /// <param name="memberAccessor">An expression that references a model field or property.</param>
    /// <returns>The column builder.</returns>
    public CsvColumnBuilder<TModel, TValue> Column<TValue>(Expression<Func<TModel, TValue>> memberAccessor)
    {
        if (memberAccessor == null)
            throw new ArgumentNullException(nameof(memberAccessor));
        if (memberAccessor.Body is MemberExpression memberExpression)
            return Column(memberAccessor, memberExpression.Member.Name);
        throw new ArgumentOutOfRangeException(nameof(memberAccessor), $"{nameof(memberAccessor)} must reference a field or property");
    }

    /// <summary>
    /// Adds a column using the specified CSV header name.
    /// </summary>
    /// <typeparam name="TValue">The column value type.</typeparam>
    /// <param name="memberAccessor">An expression that references a model field or property.</param>
    /// <param name="name">The CSV header name.</param>
    /// <returns>The column builder.</returns>
    public CsvColumnBuilder<TModel, TValue> Column<TValue>(Expression<Func<TModel, TValue>> memberAccessor, string name)
    {
        if (memberAccessor == null)
            throw new ArgumentNullException(nameof(memberAccessor));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        if (!(memberAccessor.Body is MemberExpression memberExpression))
            throw new ArgumentOutOfRangeException(nameof(memberAccessor), $"{nameof(memberAccessor)} must reference a field or property");

        var member = memberExpression.Member;
        if (member is PropertyInfo propertyInfo) {
            if (!propertyInfo.CanRead)
                throw new ArgumentOutOfRangeException(nameof(memberAccessor), "The property cannot be read");
            if (!propertyInfo.CanWrite)
                throw new ArgumentOutOfRangeException(nameof(memberAccessor), "The property cannot be written");
        } else if (!(member is FieldInfo)) {
            throw new ArgumentOutOfRangeException(nameof(memberAccessor), "The expression must reference a field or property");
        }

        if (_columns.Any(x => x.Member == member))
            throw new InvalidOperationException("This member has already been added as a column");

        var column = new CsvColumnModel(name.Trim(), typeof(TValue), member) {
            StringValueNullable = GetStringValueNullable(typeof(TValue), member)
        };
        AddHeader(column.Name, column);
        _columns.Add(column);
        return new CsvColumnBuilder<TModel, TValue>(this, column);
    }

    /// <summary>
    /// Configures a formatter used for all columns with the specified value type.
    /// </summary>
    /// <typeparam name="TValue">The value type handled by the formatter.</typeparam>
    /// <param name="deserialize">The function used to deserialize field values.</param>
    /// <param name="serialize">The function used to serialize values.</param>
    /// <returns>The model builder.</returns>
    public CsvModelBuilder<TModel> Format<TValue>(Func<string, TValue> deserialize, Func<TValue, string> serialize)
    {
        _formatters[typeof(TValue)] = new CsvFormatter<TValue>(deserialize, serialize);
        return this;
    }

    internal void AddHeader(string name, CsvColumnModel column)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));
        _columnLookup.Add(name.Trim(), column);
    }

    internal CsvModel<TModel> Build()
    {
        if (Options.LineEnding == null)
            throw new InvalidOperationException("LineEnding cannot be null");
        if (Options.LineEnding.Length == 0)
            throw new InvalidOperationException("LineEnding cannot be empty");
        if (Options.LineEndingReplacement == null)
            throw new InvalidOperationException("LineEndingReplacement cannot be null");
        return new CsvModel<TModel>(_columns.ToArray(), _columnLookup, _formatters, Options, _skipEmptyRows);
    }

    private static bool GetStringValueNullable(Type dataType, MemberInfo member)
    {
        if (dataType != typeof(string))
            return false;

#if NET6_0_OR_GREATER
        var context = new NullabilityInfoContext();
        var state = NullabilityState.Unknown;
        if (member is PropertyInfo propertyInfo)
            state = context.Create(propertyInfo).WriteState;
        else if (member is FieldInfo fieldInfo)
            state = context.Create(fieldInfo).WriteState;
        return state != NullabilityState.NotNull;
#else
        return true;
#endif
    }
}
