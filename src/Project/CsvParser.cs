using System;
using System.Collections.Generic;
using System.Text;
using CsvInvalidDataException = Shane32.CsvLinq.InvalidDataException;

namespace Shane32.CsvLinq;

internal static class CsvParser
{
    internal static List<CsvRecord> Parse(string text, CsvOptions options)
    {
        var records = new List<CsvRecord>();
        if (string.IsNullOrEmpty(text))
            return records;

        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var quotedField = false;
        var rowNumber = 1;
        var recordStartRow = 1;
        var lastWasNewLine = false;

        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            lastWasNewLine = false;

            if (inQuotes) {
                if (c == '"') {
                    if (i + 1 < text.Length && text[i + 1] == '"') {
                        field.Append('"');
                        i++;
                    } else {
                        inQuotes = false;
                    }
                } else if (c == '\r' || c == '\n') {
                    AppendLineEndingInField(text, options, field, ref i, c);
                    rowNumber++;
                } else {
                    field.Append(c);
                }
                continue;
            }

            if (c == '"' && field.Length == 0 && !quotedField) {
                inQuotes = true;
                quotedField = true;
            } else if (c == ',') {
                fields.Add(field.ToString());
                field.Length = 0;
                quotedField = false;
            } else if (c == '\r' || c == '\n') {
                fields.Add(field.ToString());
                records.Add(new CsvRecord(fields.ToArray(), recordStartRow));
                fields = new List<string>();
                field.Length = 0;
                quotedField = false;
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
                rowNumber++;
                recordStartRow = rowNumber;
                lastWasNewLine = true;
            } else {
                field.Append(c);
            }
        }

        if (inQuotes)
            throw new CsvInvalidDataException("The CSV file ended inside a quoted field");

        if (!lastWasNewLine) {
            fields.Add(field.ToString());
            records.Add(new CsvRecord(fields.ToArray(), recordStartRow));
        }

        return records;
    }

    private static void AppendLineEndingInField(string text, CsvOptions options, StringBuilder field, ref int index, char current)
    {
        switch (options.LineEndingsInStrings) {
            case CsvLineEndingHandling.Allow:
                if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n') {
                    field.Append("\r\n");
                    index++;
                } else {
                    field.Append(current);
                }
                return;
            case CsvLineEndingHandling.Replace:
                if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    index++;
                field.Append(options.LineEndingReplacement);
                return;
            case CsvLineEndingHandling.Reject:
                throw new CsvInvalidDataException("Line endings are not allowed inside CSV fields");
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.LineEndingsInStrings, "Unsupported line ending handling.");
        }
    }
}
