using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shane32.CsvLinq;

namespace Shane32.CsvLinq.Tests;

[TestClass]
public partial class CsvContextTests
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

    private static MemoryStream ToStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
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
