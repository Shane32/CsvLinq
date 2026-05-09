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
    public void DuplicateMappedHeaderThrows()
    {
        var csv = "Name,Full Name,Quantity" + Environment.NewLine + "Widgets,Widgets,52" + Environment.NewLine;

        Assert.ThrowsException<DuplicateColumnException>(() => new SampleContext().Load(ToStream(csv)));
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
    public async Task AsyncMethodsHonorAlreadyCanceledTokens()
    {
        var context = new SampleContext();
        var cancellationToken = new CancellationToken(true);

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => context.LoadAsync(new StringReader("Name,Quantity"), cancellationToken));
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => context.SaveAsync(new StringWriter(), new[] { new SampleRow { Name = "Widgets", Quantity = 2 } }, cancellationToken));
    }

#if NET7_0_OR_GREATER
    [TestMethod]
    public async Task AsyncMethodsPassCancellationTokensToTextReaderAndTextWriterOnNet7OrLater()
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

    private sealed class CustomColumnContext : CsvContext<SampleRow>
    {
        protected override void OnModelCreating(CsvModelBuilder<SampleRow> modelBuilder)
        {
            modelBuilder.Column(x => x.Name);
            modelBuilder.Column(x => x.Quantity)
                .Deserialize(value => int.Parse(value.Substring(2), CultureInfo.InvariantCulture))
                .Serialize(value => "Q-" + value);
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
}
