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

Columns are required by default.  Use `.Optional()` when a header may be missing or a field may be blank.  Header
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
| `DateTime` | ISO 8601 date/time (no offset), read as `DateTimeKind.Unspecified` | `2024-05-08T13:45:12.3450000` |
| `DateTimeOffset` | ISO 8601 round-trip with offset | `2024-05-08T13:45:12.3450000-07:00` |
| `DateOnly` (`net6.0+`) | ISO 8601 date | `2024-05-08` |
| `TimeOnly` (`net6.0+`) | ISO 8601 time | `13:45:12.3450000` |
| `TimeSpan` | constant (`c`) | `1.02:03:04.5000000` |
| `Guid` | dashed (`D`) | `f1dc7e7d-d63e-4279-8dfd-cecb6e26cda8` |
| `Uri` | URI string | `https://example.com/items/42` |
| `bool` | `true`/`false`, `1`/`0`, `Y`/`N`, `Yes`/`No` | `Y` |
| `enum` | enum name (case-insensitive on read) | `Pending` |
| numeric primitives (`int`, `decimal`, etc.) | invariant culture | `45.99` |

Common alternative: use a custom formatter for date-only `DateTime` values.

```csharp
builder.Format<DateTime>(
    value => DateTime.SpecifyKind(
        DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeKind.Unspecified),
    value => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
```

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

CSV options include delimiter, output line ending, final newline behavior, encoding, and how line endings inside
quoted fields are handled:

```csharp
builder.Configure(options => {
    options.LineEnding = "\r\n";
    options.EndsWithNewLine = true;
    options.LineEndingsInStrings = CsvLineEndingHandling.Replace;
    options.LineEndingReplacement = " ";
});
```

For deeper customization, override `OnReadFile`, `OnReadRow`, `OnWriteFile`, `OnWriteRow`, `DefaultDeserialize`, or
`DefaultSerialize` in your derived context.

## Credits

Glory to Jehovah, Lord of Lords and King of Kings, creator of Heaven and Earth, who through his Son Jesus Christ,
has reedemed me to become a child of God. -Shane32
