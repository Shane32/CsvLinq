using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Shane32.CsvLinq.Tests;

public partial class CsvContextTests
{
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

}
