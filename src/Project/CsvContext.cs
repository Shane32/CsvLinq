using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvInvalidDataException = Shane32.CsvLinq.InvalidDataException;

namespace Shane32.CsvLinq;

/// <summary>
/// Provides the base CSV loading and saving behavior for a row model.
/// </summary>
/// <typeparam name="TModel">The row model type mapped to CSV records.</typeparam>
public abstract class CsvContext<TModel>
    where TModel : class, new()
{
    private static readonly Encoding _defaultEncoding = new UTF8Encoding(false);

    /// <summary>
    /// Initializes a new CSV context and builds its model configuration.
    /// </summary>
    protected CsvContext()
    {
        var builder = new CsvModelBuilder<TModel>();
        OnModelCreating(builder);
        Model = builder.Build();
    }

    /// <summary>
    /// Gets the model configuration used by this context.
    /// </summary>
    public CsvModel<TModel> Model { get; }

    /// <summary>
    /// Configures the columns, options, and formatters used by this context.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    protected abstract void OnModelCreating(CsvModelBuilder<TModel> modelBuilder);

    /// <summary>
    /// Loads CSV rows from a file.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <returns>The loaded row models.</returns>
    public virtual List<TModel> Load(string filename)
    {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            return Load(stream);
        }
    }

    /// <summary>
    /// Loads CSV rows from a file using the specified encoding.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="encoding">The character encoding to use when reading the file.</param>
    /// <returns>The loaded row models.</returns>
    public virtual List<TModel> Load(string filename, Encoding encoding)
    {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            return Load(stream, encoding);
        }
    }

    /// <summary>
    /// Loads CSV rows from a stream.
    /// </summary>
    /// <param name="stream">The stream containing CSV data.</param>
    /// <returns>The loaded row models.</returns>
    public virtual List<TModel> Load(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using (var reader = new StreamReader(stream, _defaultEncoding, true, 1024, true))
            return Load(reader);
    }

    /// <summary>
    /// Loads CSV rows from a stream using the specified encoding.
    /// </summary>
    /// <param name="stream">The stream containing CSV data.</param>
    /// <param name="encoding">The character encoding to use when reading the stream.</param>
    /// <returns>The loaded row models.</returns>
    public virtual List<TModel> Load(Stream stream, Encoding encoding)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using (var reader = new StreamReader(stream, encoding ?? _defaultEncoding, true, 1024, true))
            return Load(reader);
    }

    /// <summary>
    /// Loads CSV rows from a text reader.
    /// </summary>
    /// <param name="reader">The reader containing CSV text.</param>
    /// <returns>The loaded row models.</returns>
    public virtual List<TModel> Load(TextReader reader)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));
        return OnReadFile(reader);
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a file.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(string filename)
    {
        return await LoadAsync(filename, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a file.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(string filename, CancellationToken cancellationToken)
    {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        cancellationToken.ThrowIfCancellationRequested();
        using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) {
            return await LoadAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a file using the specified encoding.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="encoding">The character encoding to use when reading the file.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(string filename, Encoding encoding)
    {
        return await LoadAsync(filename, encoding, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a file using the specified encoding.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="encoding">The character encoding to use when reading the file.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(string filename, Encoding encoding, CancellationToken cancellationToken)
    {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        cancellationToken.ThrowIfCancellationRequested();
        using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) {
            return await LoadAsync(stream, encoding, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a stream.
    /// </summary>
    /// <param name="stream">The stream containing CSV data.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(Stream stream)
    {
        return await LoadAsync(stream, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a stream.
    /// </summary>
    /// <param name="stream">The stream containing CSV data.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using (var reader = new StreamReader(stream, _defaultEncoding, true, 1024, true))
            return await LoadAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a stream using the specified encoding.
    /// </summary>
    /// <param name="stream">The stream containing CSV data.</param>
    /// <param name="encoding">The character encoding to use when reading the stream.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(Stream stream, Encoding encoding)
    {
        return await LoadAsync(stream, encoding, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a stream using the specified encoding.
    /// </summary>
    /// <param name="stream">The stream containing CSV data.</param>
    /// <param name="encoding">The character encoding to use when reading the stream.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using (var reader = new StreamReader(stream, encoding ?? _defaultEncoding, true, 1024, true))
            return await LoadAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a text reader.
    /// </summary>
    /// <param name="reader">The reader containing CSV text.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(TextReader reader)
    {
        return await LoadAsync(reader, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously loads CSV rows from a text reader.
    /// </summary>
    /// <param name="reader">The reader containing CSV text.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that returns the loaded row models.</returns>
    public virtual async Task<List<TModel>> LoadAsync(TextReader reader, CancellationToken cancellationToken)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));
        cancellationToken.ThrowIfCancellationRequested();
#if NET7_0_OR_GREATER
        var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
#else
        var text = await reader.ReadToEndAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
#endif
        using (var stringReader = new StringReader(text))
            return OnReadFile(stringReader);
    }

    /// <summary>
    /// Saves row models to a CSV file.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="data">The row models to save.</param>
    public virtual void Save(string filename, IEnumerable<TModel> data)
    {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None)) {
            Save(stream, data);
        }
    }

    /// <summary>
    /// Saves row models to a CSV file using the specified encoding.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="encoding">The character encoding to use when writing the file.</param>
    public virtual void Save(string filename, IEnumerable<TModel> data, Encoding encoding)
    {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None)) {
            Save(stream, data, encoding);
        }
    }

    /// <summary>
    /// Saves row models to a stream as CSV data.
    /// </summary>
    /// <param name="stream">The stream to write CSV data to.</param>
    /// <param name="data">The row models to save.</param>
    public virtual void Save(Stream stream, IEnumerable<TModel> data)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using (var writer = new StreamWriter(stream, _defaultEncoding, 1024, true))
            Save(writer, data);
    }

    /// <summary>
    /// Saves row models to a stream as CSV data using the specified encoding.
    /// </summary>
    /// <param name="stream">The stream to write CSV data to.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="encoding">The character encoding to use when writing the stream.</param>
    public virtual void Save(Stream stream, IEnumerable<TModel> data, Encoding encoding)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using (var writer = new StreamWriter(stream, encoding ?? _defaultEncoding, 1024, true))
            Save(writer, data);
    }

    /// <summary>
    /// Saves row models to a text writer as CSV data.
    /// </summary>
    /// <param name="writer">The writer to receive CSV text.</param>
    /// <param name="data">The row models to save.</param>
    public virtual void Save(TextWriter writer, IEnumerable<TModel> data)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        OnWriteFile(writer, data);
    }

    /// <summary>
    /// Asynchronously saves row models to a CSV file.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="data">The row models to save.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(string filename, IEnumerable<TModel> data)
    {
        await SaveAsync(filename, data, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves row models to a CSV file.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(string filename, IEnumerable<TModel> data, CancellationToken cancellationToken)
    {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        cancellationToken.ThrowIfCancellationRequested();
        using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) {
            await SaveAsync(stream, data, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously saves row models to a CSV file using the specified encoding.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="encoding">The character encoding to use when writing the file.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(string filename, IEnumerable<TModel> data, Encoding encoding)
    {
        await SaveAsync(filename, data, encoding, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves row models to a CSV file using the specified encoding.
    /// </summary>
    /// <param name="filename">The path to the CSV file.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="encoding">The character encoding to use when writing the file.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(string filename, IEnumerable<TModel> data, Encoding encoding, CancellationToken cancellationToken)
    {
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        cancellationToken.ThrowIfCancellationRequested();
        using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) {
            await SaveAsync(stream, data, encoding, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asynchronously saves row models to a stream as CSV data.
    /// </summary>
    /// <param name="stream">The stream to write CSV data to.</param>
    /// <param name="data">The row models to save.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(Stream stream, IEnumerable<TModel> data)
    {
        await SaveAsync(stream, data, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves row models to a stream as CSV data.
    /// </summary>
    /// <param name="stream">The stream to write CSV data to.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(Stream stream, IEnumerable<TModel> data, CancellationToken cancellationToken)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using (var writer = new StreamWriter(stream, _defaultEncoding, 1024, true))
            await SaveAsync(writer, data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves row models to a stream as CSV data using the specified encoding.
    /// </summary>
    /// <param name="stream">The stream to write CSV data to.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="encoding">The character encoding to use when writing the stream.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(Stream stream, IEnumerable<TModel> data, Encoding encoding)
    {
        await SaveAsync(stream, data, encoding, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves row models to a stream as CSV data using the specified encoding.
    /// </summary>
    /// <param name="stream">The stream to write CSV data to.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="encoding">The character encoding to use when writing the stream.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(Stream stream, IEnumerable<TModel> data, Encoding encoding, CancellationToken cancellationToken)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        using (var writer = new StreamWriter(stream, encoding ?? _defaultEncoding, 1024, true))
            await SaveAsync(writer, data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves row models to a text writer as CSV data.
    /// </summary>
    /// <param name="writer">The writer to receive CSV text.</param>
    /// <param name="data">The row models to save.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(TextWriter writer, IEnumerable<TModel> data)
    {
        await SaveAsync(writer, data, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves row models to a text writer as CSV data.
    /// </summary>
    /// <param name="writer">The writer to receive CSV text.</param>
    /// <param name="data">The row models to save.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public virtual async Task SaveAsync(TextWriter writer, IEnumerable<TModel> data, CancellationToken cancellationToken)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        cancellationToken.ThrowIfCancellationRequested();
        var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        OnWriteFile(stringWriter, data);
        cancellationToken.ThrowIfCancellationRequested();
#if NET7_0_OR_GREATER
        await writer.WriteAsync(stringWriter.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
#if NET8_0_OR_GREATER
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
#else
        cancellationToken.ThrowIfCancellationRequested();
        await writer.FlushAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
#endif
#else
        await writer.WriteAsync(stringWriter.ToString()).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await writer.FlushAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
#endif
    }

    /// <summary>
    /// Reads row models from a text reader.
    /// </summary>
    /// <param name="reader">The reader containing CSV text.</param>
    /// <returns>The loaded row models.</returns>
    protected virtual List<TModel> OnReadFile(TextReader reader)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        var records = CsvParser.Parse(reader.ReadToEnd(), Model.Options);
        if (records.Count == 0) {
            if (!Model.Options.HasHeaderRow)
                return new List<TModel>();
            throw new CsvEmptyException();
        }

        var columnMapping = Model.Options.HasHeaderRow
            ? CreateColumnMapping(records[0].Fields)
            : Model.Columns.ToArray();
        var startRow = Model.Options.HasHeaderRow ? 1 : 0;
        var data = new List<TModel>(Math.Max(0, records.Count - startRow));
        for (var i = startRow; i < records.Count; i++) {
            var item = OnReadRow(records[i].Fields, records[i].RowNumber, columnMapping);
            if (item != null)
                data.Add(item);
        }
        return data;
    }

    /// <summary>
    /// Reads a single CSV record into a row model.
    /// </summary>
    /// <param name="fields">The field values for the record.</param>
    /// <param name="rowNumber">The one-based row number in the source CSV text.</param>
    /// <param name="columnMapping">The column mapping for the record fields.</param>
    /// <returns>The row model, or <see langword="null" /> when the row is skipped.</returns>
    protected virtual TModel OnReadRow(IReadOnlyList<string> fields, int rowNumber, CsvColumnModel[] columnMapping)
    {
        if (fields == null)
            throw new ArgumentNullException(nameof(fields));
        if (columnMapping == null)
            throw new ArgumentNullException(nameof(columnMapping));

        var hasData = fields.Any(x => !string.IsNullOrEmpty(x));
        if (!hasData) {
            if (Model.SkipEmptyRows)
                return default(TModel);
            if (columnMapping.Any(x => x != null && !x.Optional))
                throw new RowEmptyException(rowNumber);
        }

        var item = new TModel();
        for (var i = 0; i < columnMapping.Length; i++) {
            var column = columnMapping[i];
            if (column == null)
                continue;

            var valueMissing = i >= fields.Count;
            var value = valueMissing ? null : fields[i];
            if (string.IsNullOrEmpty(value)) {
                if (column.Type == typeof(string)) {
                    if (valueMissing) {
                        if (!column.Optional && !column.StringValueNullable)
                            throw new ColumnDataMissingException(column.Name, rowNumber);
                        SetValue(item, column.Member, null);
                        continue;
                    }

                    if (column.StringValueNullable && column.Deserializer == null && !Model.TryGetFormatter(column.Type, out _)) {
                        SetValue(item, column.Member, null);
                        continue;
                    }

                    SetValue(item, column.Member, DeserializeValue(value, column));
                    continue;
                }

                if (!column.Optional)
                    throw new ColumnDataMissingException(column.Name, rowNumber);
                continue;
            }

            object parsed;
            try {
                parsed = DeserializeValue(value, column);
            } catch (Exception ex) when (!(ex is CsvInvalidDataException)) {
                throw new ParseDataException(rowNumber, i + 1, column.Name, ex);
            }

            SetValue(item, column.Member, parsed);
        }
        return item;
    }

    /// <summary>
    /// Writes row models as CSV text.
    /// </summary>
    /// <param name="writer">The writer to receive CSV text.</param>
    /// <param name="data">The row models to write.</param>
    protected virtual void OnWriteFile(TextWriter writer, IEnumerable<TModel> data)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var records = new List<string>();
        if (Model.Options.HasHeaderRow)
            records.Add(FormatRecord(Model.Columns.Select(x => x.Name)));

        foreach (var item in data)
            records.Add(FormatRecord(OnWriteRow(item)));

        writer.Write(string.Join(Model.Options.LineEnding, records));
        if (Model.Options.EndsWithNewLine && records.Count > 0)
            writer.Write(Model.Options.LineEnding);
    }

    /// <summary>
    /// Writes a single row model as field values.
    /// </summary>
    /// <param name="data">The row model to write.</param>
    /// <returns>The serialized field values.</returns>
    protected virtual IEnumerable<string> OnWriteRow(TModel data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        foreach (var column in Model.Columns) {
            var value = GetValue(data, column.Member);
            yield return SerializeValue(value, column);
        }
    }

    /// <summary>
    /// Deserializes a field value using the built-in type conversion rules.
    /// </summary>
    /// <param name="value">The field value to deserialize.</param>
    /// <param name="dataType">The destination data type.</param>
    /// <returns>The deserialized value.</returns>
    protected virtual object DefaultDeserialize(string value, Type dataType)
    {
        if (dataType == null)
            throw new ArgumentNullException(nameof(dataType));

        var underlyingType = Nullable.GetUnderlyingType(dataType);
        if (underlyingType != null)
            return string.IsNullOrEmpty(value) ? null : DefaultDeserialize(value, underlyingType);

        if (dataType == typeof(string))
            return value;
        if (dataType == typeof(DateTime))
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (dataType == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
#if NET6_0_OR_GREATER
        if (dataType == typeof(DateOnly))
            return DateOnly.ParseExact(value, "O", CultureInfo.InvariantCulture);
        if (dataType == typeof(TimeOnly))
            return TimeOnly.ParseExact(value, "O", CultureInfo.InvariantCulture);
#endif
        if (dataType == typeof(TimeSpan))
            return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        if (dataType == typeof(Uri))
            return new Uri(value);
        if (dataType == typeof(Guid))
            return Guid.Parse(value);
        if (dataType == typeof(bool))
            return ParseBoolean(value);
        if (dataType.IsEnum)
            return Enum.Parse(dataType, value, true);

        return Convert.ChangeType(value, dataType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Serializes a value using the built-in type conversion rules.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="dataType">The source data type.</param>
    /// <returns>The serialized field value.</returns>
    protected virtual string DefaultSerialize(object value, Type dataType)
    {
        if (value == null)
            return null;

        var underlyingType = Nullable.GetUnderlyingType(dataType);
        if (underlyingType != null)
            dataType = underlyingType;

        if (dataType == typeof(DateTime))
            return ((DateTime)value).ToString("O", CultureInfo.InvariantCulture);
        if (dataType == typeof(DateTimeOffset))
            return ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture);
#if NET6_0_OR_GREATER
        if (dataType == typeof(DateOnly))
            return ((DateOnly)value).ToString("O", CultureInfo.InvariantCulture);
        if (dataType == typeof(TimeOnly))
            return ((TimeOnly)value).ToString("O", CultureInfo.InvariantCulture);
#endif
        if (dataType == typeof(TimeSpan))
            return ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture);
        if (dataType == typeof(Uri))
            return value.ToString();
        if (dataType == typeof(Guid))
            return ((Guid)value).ToString("D");
        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        return value.ToString();
    }

    private CsvColumnModel[] CreateColumnMapping(IReadOnlyList<string> header)
    {
        var mapping = new CsvColumnModel[header.Count];
        var mapped = new HashSet<CsvColumnModel>();

        for (var i = 0; i < header.Count; i++) {
            if (!Model.TryGetColumn(header[i], out var column))
                continue;
            if (!mapped.Add(column))
                throw new DuplicateColumnException(column.Name);
            mapping[i] = column;
        }

        foreach (var column in Model.Columns) {
            if (!column.Optional && !mapped.Contains(column))
                throw new ColumnMissingException(column.Name);
        }

        return mapping;
    }

    private object DeserializeValue(string value, CsvColumnModel column)
    {
        if (column.Deserializer != null)
            return column.Deserializer(value);
        if (Model.TryGetFormatter(column.Type, out var formatter))
            return formatter.Deserialize(value);
        return DefaultDeserialize(value, column.Type);
    }

    private string SerializeValue(object value, CsvColumnModel column)
    {
        if (value == null)
            return null;
        if (column.Serializer != null)
            return column.Serializer(value);
        if (Model.TryGetFormatter(column.Type, out var formatter))
            return formatter.Serialize(value);
        return DefaultSerialize(value, column.Type);
    }

    private string FormatRecord(IEnumerable<string> fields)
    {
        return string.Join(",", fields.Select(FormatField));
    }

    private string FormatField(string value)
    {
        value = ApplyLineEndingPolicy(value ?? string.Empty);
        var mustQuote =
            ContainsCharacter(value, ',') ||
            ContainsCharacter(value, '"') ||
            ContainsCharacter(value, '\r') ||
            ContainsCharacter(value, '\n') ||
            value.Length > 0 && (value[0] == ' ' || value[value.Length - 1] == ' ');

        if (!mustQuote)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private string ApplyLineEndingPolicy(string value)
    {
        if (!ContainsCharacter(value, '\r') && !ContainsCharacter(value, '\n'))
            return value;

        switch (Model.Options.LineEndingsInStrings) {
            case CsvLineEndingHandling.Allow:
                return value;
            case CsvLineEndingHandling.Replace:
                return value.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Model.Options.LineEndingReplacement);
            case CsvLineEndingHandling.Reject:
                throw new CsvInvalidDataException("Line endings are not allowed inside CSV fields");
            default:
                throw new InvalidOperationException("Unsupported line ending handling.");
        }
    }

    private static bool ContainsCharacter(string value, char character)
    {
#if NET6_0_OR_GREATER
        return value.Contains(character);
#else
        return value.IndexOf(character) >= 0;
#endif
    }

    private static object GetValue(TModel item, MemberInfo member)
    {
        if (member is FieldInfo fieldInfo)
            return fieldInfo.GetValue(item);
        if (member is PropertyInfo propertyInfo)
            return propertyInfo.GetValue(item, null);
        throw new InvalidOperationException("Column member is not a field or property");
    }

    private static void SetValue(TModel item, MemberInfo member, object value)
    {
        if (member is FieldInfo fieldInfo) {
            fieldInfo.SetValue(item, value);
            return;
        }
        if (member is PropertyInfo propertyInfo) {
            propertyInfo.SetValue(item, value, null);
            return;
        }
        throw new InvalidOperationException("Column member is not a field or property");
    }

    private static bool ParseBoolean(string value)
    {
        switch (value.Trim().ToLowerInvariant()) {
            case "true":
            case "t":
            case "yes":
            case "y":
            case "1":
                return true;
            case "false":
            case "f":
            case "no":
            case "n":
            case "0":
                return false;
            default:
                return bool.Parse(value);
        }
    }
}
