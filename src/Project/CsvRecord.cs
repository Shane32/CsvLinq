using System.Collections.Generic;

namespace Shane32.CsvLinq;

internal sealed class CsvRecord
{
    internal CsvRecord(IReadOnlyList<string> fields, int rowNumber)
    {
        Fields = fields;
        RowNumber = rowNumber;
    }

    internal IReadOnlyList<string> Fields { get; }

    internal int RowNumber { get; }
}
