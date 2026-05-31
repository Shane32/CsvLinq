using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CsvInvalidDataException = Shane32.CsvLinq.InvalidDataException;

namespace Shane32.CsvLinq.Tests;

public partial class CsvContextTests
{
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


}
