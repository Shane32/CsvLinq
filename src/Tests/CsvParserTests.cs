using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CsvInvalidDataException = Shane32.CsvLinq.InvalidDataException;

namespace Shane32.CsvLinq.Tests;

[TestClass]
public class CsvParserTests
{
    [TestMethod]
    public void ParseReturnsEmptyForNullOrEmptyText()
    {
        var options = NewOptions();

        var fromNull = CsvParser.Parse(null!, options);
        var fromEmpty = CsvParser.Parse(string.Empty, options);

        Assert.AreEqual(0, fromNull.Count);
        Assert.AreEqual(0, fromEmpty.Count);
    }

    [TestMethod]
    public void ParseParsesSimpleRecordsAndRowNumbers()
    {
        var records = CsvParser.Parse("a,b\nc,d", NewOptions());

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual(1, records[0].RowNumber);
        AssertFields(records[0], "a", "b");
        Assert.AreEqual(2, records[1].RowNumber);
        AssertFields(records[1], "c", "d");
    }

    [TestMethod]
    public void ParseDoesNotAddFinalRecordWhenTextEndsWithNewLine()
    {
        var records = CsvParser.Parse("a,b\r\n", NewOptions());

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "a", "b");
    }

    [TestMethod]
    public void ParseHandlesEscapedQuotesAndQuotedCommas()
    {
        var records = CsvParser.Parse("\"a\"\"b\",\"c,d\"", NewOptions());

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "a\"b", "c,d");
    }

    [TestMethod]
    public void ParseAllowsRawQuotesWithinUnquotedField()
    {
        var records = CsvParser.Parse("ab\"cd", NewOptions());

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "ab\"cd");
    }

    [TestMethod]
    public void ParseThrowsWhenInputEndsInsideQuotedField()
    {
        Assert.ThrowsException<CsvInvalidDataException>(() => CsvParser.Parse("\"abc", NewOptions()));
    }

    [TestMethod]
    public void ParseAllowsLineEndingsInQuotedField()
    {
        var records = CsvParser.Parse("\"a\r\nb\",x", NewOptions(CsvLineEndingHandling.Allow));

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "a\r\nb", "x");
    }

    [TestMethod]
    public void ParseReplacesLineEndingsInQuotedField()
    {
        var records = CsvParser.Parse("\"a\r\nb\nc\",x", NewOptions(CsvLineEndingHandling.Replace, "|"));

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "a|b|c", "x");
    }

    [TestMethod]
    public void ParseRejectsLineEndingsInQuotedField()
    {
        Assert.ThrowsException<CsvInvalidDataException>(() =>
            CsvParser.Parse("\"a\nb\"", NewOptions(CsvLineEndingHandling.Reject)));
    }

    [TestMethod]
    public void ParseThrowsForUnsupportedLineEndingHandling()
    {
        var options = NewOptions((CsvLineEndingHandling)999);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => CsvParser.Parse("\"a\nb\"", options));
    }

    [TestMethod]
    public async Task ParseAsyncThrowsWhenReaderIsNullAsync()
    {
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => CsvParser.ParseAsync(null!, NewOptions()));
    }

    [TestMethod]
    public async Task ParseAsyncReturnsEmptyWhenReaderHasNoCharactersAsync()
    {
        var records = await CsvParser.ParseAsync(new StringReader(string.Empty), NewOptions());

        Assert.AreEqual(0, records.Count);
    }

    [TestMethod]
    public async Task ParseAsyncParsesSimpleRecordsAndRowNumbersAsync()
    {
        var records = await CsvParser.ParseAsync(new StringReader("a,b\nc,d"), NewOptions());

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual(1, records[0].RowNumber);
        AssertFields(records[0], "a", "b");
        Assert.AreEqual(2, records[1].RowNumber);
        AssertFields(records[1], "c", "d");
    }

    [TestMethod]
    public async Task ParseAsyncHonorsCancellationTokenAsync()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try {
            await CsvParser.ParseAsync(new StringReader("a,b"), NewOptions(), cts.Token);
            Assert.Fail("Expected cancellation exception.");
        } catch (OperationCanceledException) {
        }
    }

    [TestMethod]
    public async Task ParseAsyncHandlesQuoteAtEndOfBufferAndCompletesFieldAsync()
    {
        using var reader = new ChunkedTextReader("\"abc\"", 1);

        var records = await CsvParser.ParseAsync(reader, NewOptions());

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "abc");
    }

    [TestMethod]
    public async Task ParseAsyncHandlesEscapedQuoteAcrossReadBoundariesAsync()
    {
        using var reader = new ChunkedTextReader("\"a\"\"b\"", 1);

        var records = await CsvParser.ParseAsync(reader, NewOptions());

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "a\"b");
    }

    [TestMethod]
    public async Task ParseAsyncHandlesQuotedCarriageReturnFollowedByLineFeedAcrossReadBoundariesAsync()
    {
        using var reader = new ChunkedTextReader("\"a\r\nb\"", 1);

        var records = await CsvParser.ParseAsync(reader, NewOptions(CsvLineEndingHandling.Allow));

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "a\r\nb");
    }

    [TestMethod]
    public async Task ParseAsyncHandlesQuotedCarriageReturnWithoutLineFeedAcrossReadBoundariesAsync()
    {
        using var reader = new ChunkedTextReader("\"a\rb\"", 1);

        var records = await CsvParser.ParseAsync(reader, NewOptions(CsvLineEndingHandling.Allow));

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "a\rb");
    }

    [TestMethod]
    public async Task ParseAsyncHandlesRecordCarriageReturnFollowedByLineFeedAcrossReadBoundariesAsync()
    {
        using var reader = new ChunkedTextReader("a\r\nb", 1);

        var records = await CsvParser.ParseAsync(reader, NewOptions());

        Assert.AreEqual(2, records.Count);
        AssertFields(records[0], "a");
        AssertFields(records[1], "b");
    }

    [TestMethod]
    public async Task ParseAsyncHandlesRecordCarriageReturnWithoutLineFeedAcrossReadBoundariesAsync()
    {
        using var reader = new ChunkedTextReader("a\rb", 1);

        var records = await CsvParser.ParseAsync(reader, NewOptions());

        Assert.AreEqual(2, records.Count);
        AssertFields(records[0], "a");
        AssertFields(records[1], "b");
    }

    [TestMethod]
    public async Task ParseAsyncReplacesQuotedLineEndingsAsync()
    {
        using var reader = new ChunkedTextReader("\"a\r\nb\nc\"", 2);

        var records = await CsvParser.ParseAsync(reader, NewOptions(CsvLineEndingHandling.Replace, "_"));

        Assert.AreEqual(1, records.Count);
        AssertFields(records[0], "a_b_c");
    }

    [TestMethod]
    public async Task ParseAsyncRejectsQuotedLineEndingsAsync()
    {
        await Assert.ThrowsExceptionAsync<CsvInvalidDataException>(() =>
            CsvParser.ParseAsync(new StringReader("\"a\nb\""), NewOptions(CsvLineEndingHandling.Reject)));
    }

    [TestMethod]
    public async Task ParseAsyncThrowsForUnsupportedLineEndingHandlingAsync()
    {
        var options = NewOptions((CsvLineEndingHandling)999);

        await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
            CsvParser.ParseAsync(new StringReader("\"a\nb\""), options));
    }

    [TestMethod]
    public async Task ParseAsyncThrowsWhenInputEndsInsideQuotedFieldAsync()
    {
        await Assert.ThrowsExceptionAsync<CsvInvalidDataException>(() =>
            CsvParser.ParseAsync(new StringReader("\"abc"), NewOptions()));
    }

    private static CsvOptions NewOptions(CsvLineEndingHandling handling = CsvLineEndingHandling.Allow, string replacement = " ")
    {
        return new CsvOptions {
            LineEndingsInStrings = handling,
            LineEndingReplacement = replacement,
        };
    }

    private static void AssertFields(CsvRecord record, string value)
    {
        Assert.AreEqual(1, record.Fields.Count);
        Assert.AreEqual(value, record.Fields[0]);
    }

    private static void AssertFields(CsvRecord record, string value1, string value2)
    {
        Assert.AreEqual(2, record.Fields.Count);
        Assert.AreEqual(value1, record.Fields[0]);
        Assert.AreEqual(value2, record.Fields[1]);
    }

    private sealed class ChunkedTextReader : TextReader
    {
        private readonly string _text;
        private readonly int _chunkSize;
        private int _position;

        public ChunkedTextReader(string text, int chunkSize)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _chunkSize = chunkSize;
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            var read = Read(buffer, index, count);
            return Task.FromResult(read);
        }

#if NET7_0_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_position >= _text.Length)
                return new ValueTask<int>(0);

            var charsToRead = Math.Min(Math.Min(_chunkSize, buffer.Length), _text.Length - _position);
            _text.AsSpan(_position, charsToRead).CopyTo(buffer.Span);
            _position += charsToRead;
            return new ValueTask<int>(charsToRead);
        }
#endif

        public override int Read(char[] buffer, int index, int count)
        {
            if (_position >= _text.Length)
                return 0;

            var charsToRead = Math.Min(Math.Min(_chunkSize, count), _text.Length - _position);
            _text.CopyTo(_position, buffer, index, charsToRead);
            _position += charsToRead;
            return charsToRead;
        }
    }
}
