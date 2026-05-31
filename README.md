# Shane32.CsvLinq

CsvLinq maps a single CSV file to a strongly typed list of row models.  You define a context by deriving from
`CsvContext<TModel>` and configuring the model once in `OnModelCreating`; the context then loads CSV data from a
file or stream into `List<TModel>`, or saves a list back to a file or stream.

The library targets `netstandard2.0` and has no runtime package dependencies.

## Basic usage

```csharp
public class InvoiceLine
{
    public DateTime Date { get; set; }
    public int Quantity { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string Notes { get; set; }
}

public class InvoiceLineCsv : CsvContext<InvoiceLine>
{
    protected override void OnModelCreating(CsvModelBuilder<InvoiceLine> builder)
    {
        builder.Column(x => x.Date);
        builder.Column(x => x.Quantity, "Qty");
        builder.Column(x => x.Description)
            .AlternateName("Desc");
        builder.Column(x => x.Amount);
        builder.Column(x => x.Notes)
            .Optional();

        builder.SkipEmptyRows();
    }
}
```

Read and write data directly:

```csharp
var csv = new InvoiceLineCsv();

List<InvoiceLine> rows = csv.Load("invoice.csv");

rows.Add(new InvoiceLine {
    Date = DateTime.Today,
    Quantity = 2,
    Description = "Widgets",
    Amount = 12.50m
});

csv.Save("invoice-out.csv", rows);
```

Async file and stream APIs are also available:

```csharp
var rows = await csv.LoadAsync("invoice.csv");
await csv.SaveAsync("invoice-out.csv", rows);
```

## Configuration

`CsvModelBuilder<TModel>` and `CsvColumnBuilder<TModel, TValue>` are fluent builders. Configure your mappings inside
`OnModelCreating`, then let `CsvContext<TModel>` handle load/save operations.

### `CsvModelBuilder<TModel>` methods

| Method | Description |
| --- | --- |
| `.LineEnding(string lineEnding)` | Sets the line ending used when writing CSV (`\r\n`, `\n`, etc.). |
| `.LineEndingsInStrings(CsvLineEndingHandling handling)` | Controls how line endings inside field values are handled when writing. |
| `.LineEndingReplacement(string replacement)` | Sets replacement text used when string line endings are replaced. |
| `.EndsWithNewLine(bool endsWithNewLine)` | Controls whether output ends with a trailing line ending. |
| `.OmitHeaderRow()` | Configures reading/writing CSV with no header row. |
| `.SkipEmptyRows()` | Skips completely empty rows when loading CSV. |
| `.Column<TValue>(Expression<Func<TModel, TValue>> memberAccessor)` | Maps a field/property using the member name as the column header. |
| `.Column<TValue>(Expression<Func<TModel, TValue>> memberAccessor, string name)` | Maps a field/property using an explicit column header name. |
| `.Format<TValue>(Func<string, TValue> deserialize, Func<TValue, string> serialize)` | Registers a type-wide formatter used by all mapped columns of that type. |

### `CsvColumnBuilder<TModel, TValue>` methods

| Method | Description |
| --- | --- |
| `.AlternateName(string name)` | Adds an additional accepted header name (read-time alias). |
| `.Optional()` | Marks the column as optional when loading. |
| `.Required()` | Marks the column as required when loading. |
| `.Nullable(bool nullable = true)` | For `string` columns, controls whether blank field values deserialize as `null`. |
| `.Deserialize(Func<string, TValue> deserialize)` | Sets a column-specific deserializer. |
| `.Serialize(Func<TValue, string> serialize)` | Sets a column-specific serializer. |

Columns are required by default. Use `.Optional()` when a header may be missing or a field may be blank. Header
matching is case-insensitive and trims surrounding whitespace, and alternate names can be defined for compatibility
with older exports.

For string columns, blank fields deserialize according to the mapped member's nullable-reference annotation on
supported target frameworks: `string?` receives `null`, while `string` receives `string.Empty`.  When nullability
metadata is not available, strings are treated as nullable.  Use `.Nullable()` or `.Nullable(false)` to override the
inferred behavior.

Default serializers/deserializers support these types and formats:

| Type | Default format | Sample |
| --- | --- | --- |
| `string` | raw text | `Widgets` |
| `DateTime` | ISO 8601 date/time (no offset), read as `DateTimeKind.Unspecified` (any input offset is ignored) | `2024-05-08T13:45:12.345` |
| `DateTimeOffset` | ISO 8601 round-trip with offset | `2024-05-08T13:45:12.3450000-07:00` |
| `DateOnly` (`net6.0+`) | ISO 8601 date | `2024-05-08` |
| `TimeOnly` (`net6.0+`) | ISO 8601 time | `13:45:12.3450000` |
| `TimeSpan` | constant (`c`) | `1.02:03:04.5000000` |
| `Guid` | dashed (`D`) | `f1dc7e7d-d63e-4279-8dfd-cecb6e26cda8` |
| `Uri` | URI string | `https://example.com/items/42` |
| `bool` | `true`/`false`, `1`/`0`, `Y`/`N`, `Yes`/`No` | `True` |
| `enum` | enum name (case-insensitive on read) | `Pending` |
| numeric primitives (`int`, `decimal`, etc.) | invariant culture | `45.99` |

Per-column serialization can be customized:

```csharp
builder.Column(x => x.Quantity)
    .Deserialize(value => int.Parse(value.Substring(2)))
    .Serialize(value => "Q-" + value);
```

Type-wide formatters apply to every configured column of that type:

```csharp
builder.Format<decimal>(
    value => decimal.Parse(value.TrimStart('$'), CultureInfo.InvariantCulture),
    value => "$" + value.ToString("0.00", CultureInfo.InvariantCulture));
```

CSV output options can be configured directly on the model builder:

```csharp
builder.LineEnding("\r\n")
    .EndsWithNewLine(true)
    .LineEndingsInStrings(CsvLineEndingHandling.Replace)
    .LineEndingReplacement(" ");
```

## Advanced customization

For advanced scenarios, `CsvContext<TModel>` exposes protected methods you can override:

| Method | Description |
| --- | --- |
| `OnModelCreating(CsvModelBuilder<TModel> modelBuilder)` | Builds the model mapping (required override). |
| `OnReadFile(TextReader reader)` | Customizes how an entire CSV document is read synchronously. |
| `OnReadFileAsync(TextReader reader, CancellationToken cancellationToken = default)` | Customizes how an entire CSV document is read asynchronously. |
| `OnReadRow(IReadOnlyList<string> fields, int rowNumber, CsvColumnModel[] columnMapping)` | Customizes conversion of one parsed record into a row model. |
| `OnWriteFile(TextWriter writer, IEnumerable<TModel> data)` | Customizes how a full CSV document is written synchronously. |
| `OnWriteFileAsync(TextWriter writer, IEnumerable<TModel> data, CancellationToken cancellationToken = default)` | Customizes how a full CSV document is written asynchronously. |
| `OnWriteRow(TModel data)` | Customizes conversion of one row model into CSV field values. |
| `DefaultDeserialize(string value, Type dataType)` | Overrides built-in type conversion for reading values. |
| `DefaultSerialize(object value, Type dataType)` | Overrides built-in type conversion for writing values. |

Most applications only need `OnModelCreating`; override the other methods when you need custom parsing/writing
pipelines or conversion behavior.

## Credits

Glory to Jehovah, Lord of Lords and King of Kings, creator of Heaven and Earth, who through his Son Jesus Christ,
has reedemed me to become a child of God. -Shane32
