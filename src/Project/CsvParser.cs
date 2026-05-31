using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    internal static async Task<List<CsvRecord>> ParseAsync(TextReader reader, CsvOptions options, CancellationToken cancellationToken = default)
    {
        if (reader == null)
            throw new ArgumentNullException(nameof(reader));

        var records = new List<CsvRecord>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var quotedField = false;
        var rowNumber = 1;
        var recordStartRow = 1;
        var lastWasNewLine = false;
        var pendingQuoteInQuotedField = false;
        var pendingQuotedCarriageReturn = false;
        var pendingRecordCarriageReturn = false;
        var hasAnyCharacters = false;
        var buffer = new char[4096];

        while (true) {
#if !NET7_0_OR_GREATER
            cancellationToken.ThrowIfCancellationRequested();
#endif
            var charsRead = await ReadAsync(reader, buffer, cancellationToken).ConfigureAwait(false);
            if (charsRead == 0)
                break;

            for (var i = 0; i < charsRead; i++) {
                var c = buffer[i];
                hasAnyCharacters = true;
                lastWasNewLine = false;

                if (inQuotes && pendingQuotedCarriageReturn) {
                    if (c == '\n') {
                        AppendLineEndingInField(options, field, '\r', true);
                        rowNumber++;
                        pendingQuotedCarriageReturn = false;
                        continue;
                    }
                    AppendLineEndingInField(options, field, '\r', false);
                    rowNumber++;
                    pendingQuotedCarriageReturn = false;
                }

                if (inQuotes && pendingQuoteInQuotedField) {
                    if (c == '"') {
                        field.Append('"');
                        pendingQuoteInQuotedField = false;
                        continue;
                    }
                    inQuotes = false;
                    pendingQuoteInQuotedField = false;
                }

                if (pendingRecordCarriageReturn) {
                    if (c == '\n') {
                        pendingRecordCarriageReturn = false;
                        continue;
                    }
                    pendingRecordCarriageReturn = false;
                }

                if (inQuotes) {
                    if (c == '"') {
                        pendingQuoteInQuotedField = true;
                    } else if (c == '\r') {
                        pendingQuotedCarriageReturn = true;
                    } else if (c == '\n') {
                        AppendLineEndingInField(options, field, c, false);
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
                } else if (c == '\r') {
                    fields.Add(field.ToString());
                    records.Add(new CsvRecord(fields.ToArray(), recordStartRow));
                    fields = new List<string>();
                    field.Length = 0;
                    quotedField = false;
                    rowNumber++;
                    recordStartRow = rowNumber;
                    lastWasNewLine = true;
                    pendingRecordCarriageReturn = true;
                } else if (c == '\n') {
                    fields.Add(field.ToString());
                    records.Add(new CsvRecord(fields.ToArray(), recordStartRow));
                    fields = new List<string>();
                    field.Length = 0;
                    quotedField = false;
                    rowNumber++;
                    recordStartRow = rowNumber;
                    lastWasNewLine = true;
                } else {
                    field.Append(c);
                }
            }
        }

        if (!hasAnyCharacters)
            return records;

        if (pendingQuotedCarriageReturn) {
            AppendLineEndingInField(options, field, '\r', false);
            rowNumber++;
        }

        if (pendingQuoteInQuotedField)
            inQuotes = false;

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

    private static void AppendLineEndingInField(CsvOptions options, StringBuilder field, char current, bool carriageReturnFollowedByLineFeed)
    {
        switch (options.LineEndingsInStrings) {
            case CsvLineEndingHandling.Allow:
                if (carriageReturnFollowedByLineFeed) {
                    field.Append("\r\n");
                } else {
                    field.Append(current);
                }
                return;
            case CsvLineEndingHandling.Replace:
                field.Append(options.LineEndingReplacement);
                return;
            case CsvLineEndingHandling.Reject:
                throw new CsvInvalidDataException("Line endings are not allowed inside CSV fields");
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.LineEndingsInStrings, "Unsupported line ending handling.");
        }
    }

    private static Task<int> ReadAsync(TextReader reader, char[] buffer, CancellationToken cancellationToken)
    {
#if NET7_0_OR_GREATER
        return reader.ReadAsync(buffer.AsMemory(), cancellationToken).AsTask();
#else
        return reader.ReadAsync(buffer, 0, buffer.Length);
#endif
    }
}
