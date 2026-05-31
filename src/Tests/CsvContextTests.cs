using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shane32.CsvLinq;
using CsvInvalidDataException = Shane32.CsvLinq.InvalidDataException;

namespace Shane32.CsvLinq.Tests;

[TestClass]
public class CsvContextTests
{
    [TestMethod]
    public void LoadMatchesAlternateHeadersAndDefaultTypes()
    {
        var guid = Guid.Parse("f1dc7e7d-d63e-4279-8dfd-cecb6e26cda8");
        var csv =
            "Full Name,Quantity,Amount,SoldOn,Active,Token,Website" + Environment.NewLine +
            "Widgets,52,45.99,2020-07-01T00:00:00.0000000,Y," + guid + ",http://localhost/test" + Environment.NewLine;

        var rows = new SampleContext().Load(new StringReader(csv));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Widgets", rows[0].Name);
        Assert.AreEqual(52, rows[0].Quantity);
        Assert.AreEqual(45.99m, rows[0].Amount);
        Assert.AreEqual(new DateTime(2020, 7, 1), rows[0].SoldOn);
        Assert.AreEqual(true, rows[0].Active);
        Assert.AreEqual(guid, rows[0].Token);
        Assert.AreEqual(new Uri("http://localhost/test"), rows[0].Website);
    }

    [TestMethod]
    public void LoadSkipsUnknownColumnsAndEmptyRowsWhenConfigured()
    {
        var csv =
            "Name,Ignored,Quantity" + Environment.NewLine +
            "Widgets,x,52" + Environment.NewLine +
            ",," + Environment.NewLine +
            "Bolts,y,22" + Environment.NewLine;

        var rows = new SampleContext().Load(new StringReader(csv));

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("Widgets", rows[0].Name);
        Assert.AreEqual("Bolts", rows[1].Name);
    }

    [TestMethod]
    public void ModelConstructorBypassesOnModelCreating()
    {
        var modelBuilder = new CsvModelBuilder<SampleRow>();
        modelBuilder.Column(x => x.Name);
        modelBuilder.Column(x => x.Quantity);
        var context = new ModelConstructorContext(modelBuilder.Build());

        var rows = context.Load(new StringReader("Name,Quantity" + Environment.NewLine + "Widgets,52" + Environment.NewLine));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Widgets", rows[0].Name);
        Assert.AreEqual(52, rows[0].Quantity);
    }

    [TestMethod]
    public void DefaultConstructorLazilyInitializesModel()
    {
        var context = new LazyModelInitializationContext();

        Assert.AreEqual(0, context.OnModelCreatingCallCount);

        _ = context.Model;
        _ = context.Model;

        Assert.AreEqual(1, context.OnModelCreatingCallCount);
    }

    [TestMethod]
    public void ModelExposesConfiguredOptions()
    {
        var options = new NoFinalNewLineContext().Model.Options;

        Assert.IsNotNull(options);
        Assert.IsFalse(options.EndsWithNewLine);
        Assert.IsTrue(options.HasHeaderRow);
    }

    [TestMethod]
    public void BuildCopiesOptions()
    {
        var builder = new CsvModelBuilder<SampleRow>();
        builder.LineEnding("\r\n");
        builder.EndsWithNewLine(false);
        var model = builder.Build();

        builder.LineEnding("\n");
        builder.EndsWithNewLine(true);

        Assert.AreEqual("\r\n", model.Options.LineEnding);
        Assert.IsFalse(model.Options.EndsWithNewLine);
    }

    [TestMethod]
    public void MissingRequiredColumnThrows()
    {
        var csv = "Name" + Environment.NewLine + "Widgets" + Environment.NewLine;

        Assert.ThrowsException<ColumnMissingException>(() => new SampleContext().Load(ToStream(csv)));
    }

    [TestMethod]
    public void MissingRequiredDataThrows()
    {
        var csv = "Name,Quantity" + Environment.NewLine + "Widgets," + Environment.NewLine;

        Assert.ThrowsException<ColumnDataMissingException>(() => new SampleContext().Load(ToStream(csv)));
    }

    [TestMethod]
    public void StringNullabilityOverrideControlsBlankFields()
    {
        var csv = "Id,NullableName,NonNullableName" + Environment.NewLine + "1,," + Environment.NewLine;

        var rows = new StringNullabilityOverrideContext().Load(ToStream(csv));

        Assert.IsNull(rows[0].NullableName);
        Assert.AreEqual(string.Empty, rows[0].NonNullableName);
    }

#if NET6_0_OR_GREATER
    [TestMethod]
    public void StringNullabilityIsInferredFromNullableAnnotations()
    {
        var csv = "Id,RequiredName,OptionalName" + Environment.NewLine + "1,," + Environment.NewLine;

        var rows = new InferredStringNullabilityContext().Load(ToStream(csv));

        Assert.AreEqual(string.Empty, rows[0].RequiredName);
        Assert.IsNull(rows[0].OptionalName);
    }
#else
    [TestMethod]
    public void StringNullabilityDefaultsToNullableWhenReflectionIsUnavailable()
    {
        var csv = "Id,Name" + Environment.NewLine + "1," + Environment.NewLine;

        var rows = new UnknownStringNullabilityContext().Load(ToStream(csv));

        Assert.IsNull(rows[0].Name);
    }
#endif

    [TestMethod]
    public void DuplicateMappedHeaderThrows()
    {
        var csv = "Name,Full Name,Quantity" + Environment.NewLine + "Widgets,Widgets,52" + Environment.NewLine;

        Assert.ThrowsException<DuplicateColumnException>(() => new SampleContext().Load(ToStream(csv)));
    }

    [TestMethod]
    public void SaveDoesNotQuoteFieldsThatDontNeedEncoding()
    {
        var context = new SampleContext();
        var rows = new List<SampleRow> {
            new SampleRow { Name = "Widgets", Quantity = 1 },
            new SampleRow { Name = "plain text", Quantity = 2 },
            new SampleRow { Name = "123", Quantity = 3 },
        };

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        // Fields that don't need encoding should not be quoted
        Assert.IsFalse(csv.Contains("\"Widgets\""), "Simple field should not be quoted");
        Assert.IsFalse(csv.Contains("\"plain text\""), "Field with internal space should not be quoted");
        Assert.IsFalse(csv.Contains("\"123\""), "Numeric string should not be quoted");
        // Verify the plain values do appear
        StringAssert.Contains(csv, "Widgets");
        StringAssert.Contains(csv, "plain text");
        StringAssert.Contains(csv, "123");
    }

    [TestMethod]
    public void SaveQuotesFieldsContainingCommas()
    {
        var context = new SampleContext();
        var rows = new List<SampleRow> { new SampleRow { Name = "Widgets, Inc.", Quantity = 1 } };

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "\"Widgets, Inc.\"");
    }

    [TestMethod]
    public void SaveQuotesAndEscapesFieldsContainingDoubleQuotes()
    {
        var context = new SampleContext();
        var rows = new List<SampleRow> { new SampleRow { Name = "say \"hello\"", Quantity = 1 } };

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "\"say \"\"hello\"\"\"");
    }

    [TestMethod]
    public void SaveQuotesFieldsContainingNewlines()
    {
        var context = new SampleContext();
        var rows = new List<SampleRow> {
            new SampleRow { Name = "Widgets", Quantity = 1, Notes = "line1\r\nline2" }
        };

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "\"line1\r\nline2\"");
    }

    [TestMethod]
    public void SaveQuotesFieldsContainingLfNewlines()
    {
        var context = new SampleContext();
        var rows = new List<SampleRow> {
            new SampleRow { Name = "Widgets", Quantity = 1, Notes = "line1\nline2" }
        };

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "\"line1\nline2\"");
    }

    [TestMethod]
    public void SaveQuotesFieldsWithLeadingOrTrailingSpaces()
    {
        var context = new SampleContext();
        var rows = new List<SampleRow> {
            new SampleRow { Name = " leading", Quantity = 1 },
            new SampleRow { Name = "trailing ", Quantity = 2 },
        };

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "\" leading\"");
        StringAssert.Contains(csv, "\"trailing \"");
    }

    [TestMethod]
    public void SaveAndLoadRoundTripsFieldsWithCommas()
    {
        var context = new SampleContext();
        var original = new SampleRow { Name = "A, B, C", Quantity = 5 };

        var stream = new MemoryStream();
        context.Save(stream, new[] { original });
        stream.Position = 0;
        var rows = context.Load(stream);

        Assert.AreEqual(original.Name, rows[0].Name);
    }

    [TestMethod]
    public void SaveAndLoadRoundTripsFieldsWithDoubleQuotes()
    {
        var context = new SampleContext();
        var original = new SampleRow { Name = "He said \"hello\"", Quantity = 5 };

        var stream = new MemoryStream();
        context.Save(stream, new[] { original });
        stream.Position = 0;
        var rows = context.Load(stream);

        Assert.AreEqual(original.Name, rows[0].Name);
    }

    [TestMethod]
    public void SaveAndLoadRoundTripsFieldsWithEmbeddedNewlines()
    {
        var context = new SampleContext();
        var original = new SampleRow { Name = "Widgets", Quantity = 1, Notes = "first\r\nsecond" };

        var stream = new MemoryStream();
        context.Save(stream, new[] { original });
        stream.Position = 0;
        var rows = context.Load(stream);

        Assert.AreEqual(original.Notes, rows[0].Notes);
    }

    [TestMethod]
    public void SaveEscapesAndRoundTripsCsvFields()
    {
        var context = new SampleContext();
        var rows = new List<SampleRow> {
            new SampleRow {
                Name = "A, \"quoted\" value",
                Quantity = 3,
                Notes = "first line" + Environment.NewLine + "second line"
            }
        };

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "\"A, \"\"quoted\"\" value\"");
        stream.Position = 0;
        var result = context.Load(stream);
        Assert.AreEqual(rows[0].Name, result[0].Name);
        Assert.AreEqual(rows[0].Notes, result[0].Notes);
    }

    [TestMethod]
    public void SaveCanOmitFinalNewLine()
    {
        var context = new NoFinalNewLineContext();
        var stream = new MemoryStream();

        context.Save(stream, new[] { new SampleRow { Name = "Widgets", Quantity = 1 } });
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        Assert.IsFalse(csv.EndsWith(Environment.NewLine, StringComparison.Ordinal));
    }

    [TestMethod]
    public void SaveCanOmitHeaderRow()
    {
        var context = new HeaderlessContext();
        var stream = new MemoryStream();

        context.Save(stream, new[] { new HeaderlessRow { Name = "Widgets", Quantity = 2 } });
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        Assert.AreEqual("Widgets,2" + Environment.NewLine, csv);
    }

    [TestMethod]
    public void LoadCanReadWithoutHeaderRow()
    {
        var rows = new HeaderlessContext().Load(ToStream(
            "Widgets,2" + Environment.NewLine +
            "Bolts,5" + Environment.NewLine));

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("Widgets", rows[0].Name);
        Assert.AreEqual(2, rows[0].Quantity);
        Assert.AreEqual("Bolts", rows[1].Name);
        Assert.AreEqual(5, rows[1].Quantity);
    }

    [TestMethod]
    public void LineEndingsInFieldsCanBeReplaced()
    {
        var context = new ReplaceLineEndingsContext();
        var rows = new[] {
            new SampleRow {
                Name = "Widgets",
                Quantity = 1,
                Notes = "first" + Environment.NewLine + "second"
            }
        };

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "first second");
    }

    [TestMethod]
    public void LineEndingsInFieldsCanBeRejected()
    {
        var context = new RejectLineEndingsContext();
        var rows = new[] {
            new SampleRow {
                Name = "Widgets",
                Quantity = 1,
                Notes = "first" + Environment.NewLine + "second"
            }
        };

        Assert.ThrowsException<CsvInvalidDataException>(() => context.Save(new MemoryStream(), rows));
    }

    [TestMethod]
    public void ColumnSerializerOverridesDefault()
    {
        var rows = new CustomColumnContext().Load(ToStream("Name,Quantity" + Environment.NewLine + "Widgets,Q-42" + Environment.NewLine));

        Assert.AreEqual(42, rows[0].Quantity);
    }

    [TestMethod]
    public void TypeFormatterOverridesAllColumnsOfType()
    {
        var context = new TypeFormatterContext();
        var rows = context.Load(ToStream("Name,Quantity,Amount" + Environment.NewLine + "Widgets,7,$12.50" + Environment.NewLine));

        Assert.AreEqual(12.50m, rows[0].Amount);

        var stream = new MemoryStream();
        context.Save(stream, rows);
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();
        StringAssert.Contains(csv, "$12.50");
    }

    [TestMethod]
    public async Task LoadAndSaveUsesFilesAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        var context = new SampleContext();
        try {
            await context.SaveAsync(path, new[] { new SampleRow { Name = "Widgets", Quantity = 2 } });
            var rows = await context.LoadAsync(path);
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("Widgets", rows[0].Name);
            Assert.AreEqual(2, rows[0].Quantity);
        } finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void DefaultFileEncodingIsUtf8WithoutBom()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        var context = new SampleContext();
        try {
            context.Save(path, new[] { new SampleRow { Name = "Widgets", Quantity = 2 } });
            var bytes = File.ReadAllBytes(path);
            // UTF-8 BOM is EF BB BF; assert the file does not start with a BOM
            Assert.IsTrue(bytes.Length >= 3, "File should contain data");
            Assert.IsFalse(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "File should not have UTF-8 BOM");
            // Verify it is valid UTF-8
            var text = Encoding.UTF8.GetString(bytes);
            StringAssert.Contains(text, "Widgets");
        } finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void DefaultStreamEncodingIsUtf8WithoutBom()
    {
        var context = new SampleContext();
        var stream = new MemoryStream();
        context.Save(stream, new[] { new SampleRow { Name = "Widgets", Quantity = 2 } });
        var bytes = stream.ToArray();
        // UTF-8 BOM is EF BB BF; assert the stream does not start with a BOM
        Assert.IsTrue(bytes.Length >= 3, "Stream should contain data");
        Assert.IsFalse(bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "Stream should not have UTF-8 BOM");
    }

    [TestMethod]
    public void SaveAndLoadFileWithCustomEncoding()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        var context = new SampleContext();
        var encoding = Encoding.GetEncoding("iso-8859-1");
        try {
            context.Save(path, new[] { new SampleRow { Name = "caf\u00e9", Quantity = 3 } }, encoding);
            var rows = context.Load(path, encoding);
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("caf\u00e9", rows[0].Name);
        } finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void SaveAndLoadStreamWithCustomEncoding()
    {
        var context = new SampleContext();
        var encoding = Encoding.GetEncoding("iso-8859-1");
        var stream = new MemoryStream();
        context.Save(stream, new[] { new SampleRow { Name = "caf\u00e9", Quantity = 3 } }, encoding);
        stream.Position = 0;
        var rows = context.Load(stream, encoding);
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("caf\u00e9", rows[0].Name);
    }

    [TestMethod]
    public async Task SaveAndLoadFileWithCustomEncodingAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        var context = new SampleContext();
        var encoding = Encoding.GetEncoding("iso-8859-1");
        try {
            await context.SaveAsync(path, new[] { new SampleRow { Name = "caf\u00e9", Quantity = 3 } }, encoding);
            var rows = await context.LoadAsync(path, encoding);
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("caf\u00e9", rows[0].Name);
        } finally {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public async Task SaveAndLoadStreamWithCustomEncodingAsync()
    {
        var context = new SampleContext();
        var encoding = Encoding.GetEncoding("iso-8859-1");
        var stream = new MemoryStream();
        await context.SaveAsync(stream, new[] { new SampleRow { Name = "caf\u00e9", Quantity = 3 } }, encoding);
        stream.Position = 0;
        var rows = await context.LoadAsync(stream, encoding);
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("caf\u00e9", rows[0].Name);
    }

    [TestMethod]
    public async Task LoadAndSaveUsesTextReaderAndTextWriterAsync()
    {
        var context = new SampleContext();
        var writer = new StringWriter();

        await context.SaveAsync(writer, new[] { new SampleRow { Name = "Widgets", Quantity = 2 } });
        var rows = await context.LoadAsync(new StringReader(writer.ToString()));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual("Widgets", rows[0].Name);
        Assert.AreEqual(2, rows[0].Quantity);
    }

    [TestMethod]
    public async Task AsyncMethodsHonorAlreadyCanceledTokensAsync()
    {
        var context = new SampleContext();
        var cancellationToken = new CancellationToken(true);

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => context.LoadAsync(new StringReader("Name,Quantity"), cancellationToken));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => context.SaveAsync(new StringWriter(), new[] { new SampleRow { Name = "Widgets", Quantity = 2 } }, cancellationToken));
    }

    [TestMethod]
    public async Task AsyncMethodsUseOnReadFileAsyncAndOnWriteFileAsync()
    {
        var context = new AsyncHookContext();
        var csv = "Name,Quantity" + Environment.NewLine + "Widgets,2" + Environment.NewLine;

        var rows = await context.LoadAsync(new StringReader(csv));
        await context.SaveAsync(new StringWriter(), rows);

        Assert.IsTrue(context.OnReadFileAsyncCalled);
        Assert.IsFalse(context.OnReadFileCalled);
        Assert.IsTrue(context.OnWriteFileAsyncCalled);
        Assert.IsFalse(context.OnWriteFileCalled);
    }

    [TestMethod]
    public async Task LoadAsyncUsesBufferedParserReadsAsync()
    {
        var csv = new StringBuilder();
        csv.Append("Name,Quantity");
        csv.Append(Environment.NewLine);
        for (var i = 0; i < 2500; i++) {
            csv.Append("Item");
            csv.Append(i);
            csv.Append(',');
            csv.Append(i);
            csv.Append(Environment.NewLine);
        }

        var reader = new CountingTextReader(csv.ToString());
        var rows = await new SampleContext().LoadAsync(reader);

        Assert.AreEqual(2500, rows.Count);
        Assert.IsTrue(reader.ReadAsyncCallCount < csv.Length / 10, "Expected buffered reads rather than one read per character.");
    }

    [TestMethod]
    public void NativeTypesSaveUsesDefaultIsoFormats()
    {
        var context = new NativeTypesContext();
        var row = new NativeTypesRow {
            Token = Guid.Parse("f1dc7e7d-d63e-4279-8dfd-cecb6e26cda8"),
            HappenedAt = new DateTime(2024, 5, 8, 13, 45, 12, 345, DateTimeKind.Utc),
            HappenedAtOffset = new DateTimeOffset(2024, 5, 8, 13, 45, 12, 345, TimeSpan.FromHours(-7)),
#if NET6_0_OR_GREATER
            AvailableOn = new DateOnly(2024, 5, 9),
#endif
        };

        var stream = new MemoryStream();
        context.Save(stream, new[] { row });
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "f1dc7e7d-d63e-4279-8dfd-cecb6e26cda8");
        StringAssert.Contains(csv, ",2024-05-08T13:45:12.345,2024-05-08T13:45:12.3450000-07:00");
#if NET6_0_OR_GREATER
        StringAssert.Contains(csv, "2024-05-09");
#endif
    }

    [TestMethod]
    public void NativeTypesLoadParsesDefaultFormatsAndDateTimeKindIsUnspecified()
    {
        var csv =
            "Token,HappenedAt,HappenedAtOffset" +
#if NET6_0_OR_GREATER
            ",AvailableOn" +
#endif
            Environment.NewLine +
            "f1dc7e7d-d63e-4279-8dfd-cecb6e26cda8,2024-05-08T13:45:12.3450000+02:00,2024-05-08T13:45:12.3450000-07:00" +
#if NET6_0_OR_GREATER
            ",2024-05-09" +
#endif
            Environment.NewLine;

        var rows = new NativeTypesContext().Load(new StringReader(csv));

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(Guid.Parse("f1dc7e7d-d63e-4279-8dfd-cecb6e26cda8"), rows[0].Token);
        Assert.AreEqual(DateTimeKind.Unspecified, rows[0].HappenedAt.Kind);
        Assert.AreEqual(new DateTime(2024, 5, 8, 13, 45, 12, 345, DateTimeKind.Unspecified), rows[0].HappenedAt);
        Assert.AreEqual(new DateTimeOffset(2024, 5, 8, 13, 45, 12, 345, TimeSpan.FromHours(-7)), rows[0].HappenedAtOffset);
#if NET6_0_OR_GREATER
        Assert.AreEqual(new DateOnly(2024, 5, 9), rows[0].AvailableOn);
#endif
    }

#if NET7_0_OR_GREATER
    [TestMethod]
    public async Task AsyncMethodsPassCancellationTokensToTextReaderAndTextWriterOnNet7OrLaterAsync()
    {
        var context = new SampleContext();
        var cancellationToken = new CancellationTokenSource().Token;
        var reader = new CapturingTextReader("Name,Quantity" + Environment.NewLine + "Widgets,2" + Environment.NewLine);
        var writer = new CapturingTextWriter();

        var rows = await context.LoadAsync(reader, cancellationToken);
        await context.SaveAsync(writer, rows, cancellationToken);

        Assert.AreEqual(cancellationToken, reader.CancellationToken);
        Assert.AreEqual(cancellationToken, writer.WriteCancellationToken);
#if NET8_0_OR_GREATER
        Assert.AreEqual(cancellationToken, writer.FlushCancellationToken);
#endif
    }
#endif

#if NET6_0_OR_GREATER
    [TestMethod]
    public void DateOnlyAndTimeOnlyRoundTripWithDefaultFormatters()
    {
        var context = new SampleContext();
        var row = new SampleRow {
            Name = "Widgets",
            Quantity = 2,
            AvailableOn = new DateOnly(2024, 5, 8),
            StartsAt = new TimeOnly(13, 45, 12, 345)
        };

        var stream = new MemoryStream();
        context.Save(stream, new[] { row });
        stream.Position = 0;
        var csv = new StreamReader(stream).ReadToEnd();

        StringAssert.Contains(csv, "2024-05-08");
        StringAssert.Contains(csv, "13:45:12.3450000");

        stream.Position = 0;
        var rows = context.Load(stream);
        Assert.AreEqual(row.AvailableOn, rows[0].AvailableOn);
        Assert.AreEqual(row.StartsAt, rows[0].StartsAt);
    }
#endif

    private static MemoryStream ToStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

#if NET7_0_OR_GREATER
    private sealed class CapturingTextReader : StringReader
    {
        public CapturingTextReader(string text)
            : base(text)
        {
        }

        public CancellationToken CancellationToken { get; private set; }

        public override Task<string> ReadToEndAsync(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return base.ReadToEndAsync(cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return base.ReadAsync(buffer, cancellationToken);
        }
    }

    private sealed class CapturingTextWriter : StringWriter
    {
        public CancellationToken WriteCancellationToken { get; private set; }

        public CancellationToken FlushCancellationToken { get; private set; }

        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            WriteCancellationToken = cancellationToken;
            return base.WriteAsync(buffer, cancellationToken);
        }

#if NET8_0_OR_GREATER
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            FlushCancellationToken = cancellationToken;
            return base.FlushAsync(cancellationToken);
        }
#endif
    }
#endif

    private sealed class CountingTextReader : TextReader
    {
        private readonly string _text;
        private int _index;

        public CountingTextReader(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public int ReadAsyncCallCount { get; private set; }

        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (index < 0 || index > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > buffer.Length - index)
                throw new ArgumentOutOfRangeException(nameof(count));

            var charsRemaining = _text.Length - _index;
            if (charsRemaining <= 0)
                return 0;

            var charsToCopy = Math.Min(count, charsRemaining);
            _text.CopyTo(_index, buffer, index, charsToCopy);
            _index += charsToCopy;
            return charsToCopy;
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            ReadAsyncCallCount++;
            return Task.FromResult(Read(buffer, index, count));
        }

#if NET7_0_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadAsyncCallCount++;

            var charsRemaining = _text.Length - _index;
            if (charsRemaining <= 0)
                return ValueTask.FromResult(0);

            var charsToCopy = Math.Min(buffer.Length, charsRemaining);
            _text.AsMemory(_index, charsToCopy).CopyTo(buffer);
            _index += charsToCopy;
            return ValueTask.FromResult(charsToCopy);
        }
#endif
    }

    public class SampleRow
    {
        public string Name { get; set; }

        public int Quantity { get; set; }

        public decimal? Amount { get; set; }

        public DateTime? SoldOn { get; set; }

        public bool? Active { get; set; }

        public Guid? Token { get; set; }

        public Uri Website { get; set; }

        public string Notes { get; set; }

#if NET6_0_OR_GREATER
        public DateOnly? AvailableOn { get; set; }

        public TimeOnly? StartsAt { get; set; }
#endif
    }

    private sealed class NativeTypesRow
    {
        public Guid Token { get; set; }

        public DateTime HappenedAt { get; set; }

        public DateTimeOffset HappenedAtOffset { get; set; }

#if NET6_0_OR_GREATER
        public DateOnly AvailableOn { get; set; }
#endif
    }

    private sealed class StringNullabilityOverrideRow
    {
        public int Id { get; set; }

        public string NullableName { get; set; } = "initial";

        public string NonNullableName { get; set; } = "initial";
    }

#if NET6_0_OR_GREATER
#nullable enable
    private sealed class InferredStringNullabilityRow
    {
        public int Id { get; set; }

        public string RequiredName { get; set; } = "initial";

        public string? OptionalName { get; set; } = "initial";
    }
#nullable restore
#else
    private sealed class UnknownStringNullabilityRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = "initial";
    }
#endif

    private class SampleContext : CsvContext<SampleRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
        {
            modelBuilder.SkipEmptyRows();
            modelBuilder.Column(x => x.Name).AlternateName("Full Name");
            modelBuilder.Column(x => x.Quantity);
            modelBuilder.Column(x => x.Amount).Optional();
            modelBuilder.Column(x => x.SoldOn).Optional();
            modelBuilder.Column(x => x.Active).Optional();
            modelBuilder.Column(x => x.Token).Optional();
            modelBuilder.Column(x => x.Website).Optional();
            modelBuilder.Column(x => x.Notes).Optional();
#if NET6_0_OR_GREATER
            modelBuilder.Column(x => x.AvailableOn).Optional();
            modelBuilder.Column(x => x.StartsAt).Optional();
#endif
        }
    }

    private sealed class NativeTypesContext : CsvContext<NativeTypesRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<NativeTypesRow> modelBuilder)
        {
            modelBuilder.Column(x => x.Token);
            modelBuilder.Column(x => x.HappenedAt);
            modelBuilder.Column(x => x.HappenedAtOffset);
#if NET6_0_OR_GREATER
            modelBuilder.Column(x => x.AvailableOn);
#endif
        }
    }

    private sealed class StringNullabilityOverrideContext : CsvContext<StringNullabilityOverrideRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<StringNullabilityOverrideRow> modelBuilder)
        {
            modelBuilder.Column(x => x.Id);
            modelBuilder.Column(x => x.NullableName).Nullable();
            modelBuilder.Column(x => x.NonNullableName).Nullable(false);
        }
    }

#if NET6_0_OR_GREATER
    private sealed class InferredStringNullabilityContext : CsvContext<InferredStringNullabilityRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<InferredStringNullabilityRow> modelBuilder)
        {
            modelBuilder.Column(x => x.Id);
            modelBuilder.Column(x => x.RequiredName);
            modelBuilder.Column(x => x.OptionalName);
        }
    }
#else
    private sealed class UnknownStringNullabilityContext : CsvContext<UnknownStringNullabilityRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<UnknownStringNullabilityRow> modelBuilder)
        {
            modelBuilder.Column(x => x.Id);
            modelBuilder.Column(x => x.Name);
        }
    }
#endif

    private sealed class NoFinalNewLineContext : SampleContext
    {
        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.EndsWithNewLine(false);
        }
    }

    private sealed class ReplaceLineEndingsContext : SampleContext
    {
        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.LineEndingsInStrings(CsvLineEndingHandling.Replace);
        }
    }

    private sealed class RejectLineEndingsContext : SampleContext
    {
        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.LineEndingsInStrings(CsvLineEndingHandling.Reject);
        }
    }

    private sealed class AsyncHookContext : SampleContext
    {
        public bool OnReadFileCalled { get; private set; }

        public bool OnReadFileAsyncCalled { get; private set; }

        public bool OnWriteFileCalled { get; private set; }

        public bool OnWriteFileAsyncCalled { get; private set; }

        protected override List<SampleRow> OnReadFile(TextReader reader)
        {
            OnReadFileCalled = true;
            return base.OnReadFile(reader);
        }

        protected override async Task<List<SampleRow>> OnReadFileAsync(TextReader reader, CancellationToken cancellationToken = default)
        {
            OnReadFileAsyncCalled = true;
            return await base.OnReadFileAsync(reader, cancellationToken);
        }

        protected override void OnWriteFile(TextWriter writer, IEnumerable<SampleRow> data)
        {
            OnWriteFileCalled = true;
            base.OnWriteFile(writer, data);
        }

        protected override async Task OnWriteFileAsync(TextWriter writer, IEnumerable<SampleRow> data, CancellationToken cancellationToken = default)
        {
            OnWriteFileAsyncCalled = true;
            await base.OnWriteFileAsync(writer, data, cancellationToken);
        }
    }

    private sealed class CustomColumnContext : CsvContext<SampleRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
        {
            modelBuilder.Column(x => x.Name);
            modelBuilder.Column(x => x.Quantity)
                .Deserialize(DeserializeQuantity)
                .Serialize(value => "Q-" + value);
        }

        private static int DeserializeQuantity(string value)
        {
#if NET7_0_OR_GREATER
            return int.Parse(value.AsSpan(2), CultureInfo.InvariantCulture);
#else
            return int.Parse(value.Substring(2), CultureInfo.InvariantCulture);
#endif
        }
    }

    private sealed class ModelConstructorContext : CsvContext<SampleRow>
    {
        public ModelConstructorContext(CsvModel<SampleRow> model)
            : base(model)
        {
        }

        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
            => throw new AssertFailedException("OnModelCreating should not be called.");
    }

    private sealed class LazyModelInitializationContext : CsvContext<SampleRow>
    {
        public bool ConstructorCompleted { get; private set; }

        public int OnModelCreatingCallCount { get; private set; }

        public LazyModelInitializationContext()
        {
            ConstructorCompleted = true;
        }

        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
        {
            OnModelCreatingCallCount++;
            if (!ConstructorCompleted)
                throw new AssertFailedException("OnModelCreating should be called lazily.");
            modelBuilder.Column(x => x.Name);
        }
    }

    private sealed class TypeFormatterContext : CsvContext<SampleRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
        {
            modelBuilder.Format<decimal>(
                value => decimal.Parse(value.TrimStart('$'), System.Globalization.CultureInfo.InvariantCulture),
                value => "$" + value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            modelBuilder.Column(x => x.Name);
            modelBuilder.Column(x => x.Quantity);
            modelBuilder.Column(x => x.Amount).Optional();
        }
    }

    private sealed class HeaderlessRow
    {
        public string Name { get; set; } = "";

        public int Quantity { get; set; }
    }

    private sealed class HeaderlessContext : CsvContext<HeaderlessRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<HeaderlessRow> modelBuilder)
        {
            modelBuilder.OmitHeaderRow();
            modelBuilder.Column(x => x.Name);
            modelBuilder.Column(x => x.Quantity);
        }
    }
}
