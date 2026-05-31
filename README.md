# Shane32.CsvLinq

CsvLinq maps a single CSV file to a strongly typed list of row models.  You define a context by creating
`CsvContext<TModel>` with a model configuration delegate; the context then loads CSV data from a file or stream into
`List<TModel>`, or saves a list back to a file or stream.

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

var csv = new CsvContext<InvoiceLine>(builder => {
    builder.Column(x => x.Date);
    builder.Column(x => x.Quantity, "Qty");
    builder.Column(x => x.Description)
        .AlternateName("Desc");
    builder.Column(x => x.Amount);
    builder.Column(x => x.Notes)
        .Optional();

    builder.SkipEmptyRows();
});
```

Read and write data directly:

```csharp
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
