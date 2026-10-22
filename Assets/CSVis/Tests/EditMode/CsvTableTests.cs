using System.Collections.Generic;
using CSVis.IO;
using NUnit.Framework;

namespace CSVis.Tests
{
    public class CsvTableTests
    {
        private static CsvTable Parse(string text)
        {
            var table = CsvTable.Parse(text, out var error);
            Assert.IsNotNull(table, "Expected a table but got error: " + error);
            return table;
        }

        private static string Error(string text)
        {
            var table = CsvTable.Parse(text, out var error);
            Assert.IsNull(table);
            Assert.IsNotEmpty(error);
            return error;
        }

        [Test]
        public void ParsesHeaderAndRows()
        {
            var table = Parse("x,y\n1,2\n3,4\n");
            Assert.IsTrue(table.HadHeader);
            Assert.AreEqual(new[] { "x", "y" }, table.Headers);
            Assert.AreEqual(2, table.Rows.Count);
        }

        [Test]
        public void HeaderlessFileGetsSyntheticHeaders()
        {
            var table = Parse("1,2,3\n4,5,6\n");
            Assert.IsFalse(table.HadHeader);
            Assert.AreEqual("Column 1", table.Headers[0]);
            Assert.AreEqual(2, table.Rows.Count);
        }

        [Test]
        public void TrailingEmptyColumnsAreIgnored()
        {
            // Matches the bundled C2_1.csv: numbers followed by a trailing comma
            var table = Parse("-0.1, -12.0, -0.02, \n0.2, -12.0, -0.03, \n");
            Assert.AreEqual(3, table.ColumnCount);
            Assert.IsFalse(table.HadHeader);
            Assert.AreEqual(2, table.Rows.Count);
        }

        [Test]
        public void HandlesBomAndCrLf()
        {
            var table = Parse("﻿a,b\r\n1,2\r\n3,4\r\n");
            Assert.AreEqual("a", table.Headers[0]);
            Assert.AreEqual(2, table.Rows.Count);
        }

        [Test]
        public void HandlesQuotedFieldsWithThousandsAndDelimiters()
        {
            var table = Parse("name,pressure\nA,\"1,001.20\"\n\"B, Inc\",5\n");
            var values = new List<float>();
            Assert.AreEqual(0, table.ReadFloatColumn(1, values));
            Assert.AreEqual(1001.2f, values[0], 0.001f);
            Assert.AreEqual("B, Inc", table.Rows[1][0]);
        }

        [Test]
        public void DetectsSemicolonAndTabDelimiters()
        {
            Assert.AreEqual(';', Parse("a;b\n1;2\n").Delimiter);
            Assert.AreEqual('\t', Parse("a\tb\n1\t2\n").Delimiter);
        }

        [Test]
        public void DecimalCommaIsUnderstoodWithSemicolons()
        {
            var table = Parse("a;b\n1,5;2,25\n");
            var values = new List<float>();
            table.ReadFloatColumn(0, values);
            Assert.AreEqual(1.5f, values[0], 0.0001f);
        }

        [Test]
        public void InvalidNumbersAreSkippedAndCounted()
        {
            var table = Parse("x,y\n1,2\nabc,4\n5,\n7,8\n");
            var skipped = table.ReadAlignedFloatColumns(new[] { 0, 1 }, out var cols);
            Assert.AreEqual(2, skipped);
            Assert.AreEqual(2, cols[0].Count);
            Assert.AreEqual(cols[0].Count, cols[1].Count);
        }

        [Test]
        public void NaNAndInfinityAreRejected()
        {
            Assert.IsFalse(CsvTable.TryParseNumber("NaN", ',', out _));
            Assert.IsFalse(CsvTable.TryParseNumber("Infinity", ',', out _));
            Assert.IsFalse(CsvTable.TryParseNumber("1e999", ',', out _));
        }

        [Test]
        public void RowCapIsHonoured()
        {
            var table = Parse("x,y\n1,1\n2,2\n3,3\n4,4\n");
            var skipped = table.ReadAlignedFloatColumns(new[] { 0, 1 }, out var cols, 2);
            Assert.AreEqual(0, skipped);
            Assert.AreEqual(2, cols[0].Count);
        }

        [Test]
        public void BlankHeaderCellsGetNames()
        {
            var table = Parse(",1,,,2\n0,1,1,1,1\n");
            Assert.AreEqual("Column 1", table.Headers[0]);
            Assert.AreEqual("1", table.Headers[1]);
        }

        [Test]
        public void ErrorsAreReportedInsteadOfThrown()
        {
            StringAssert.Contains("empty", Error(""));
            StringAssert.Contains("empty", Error("   \n  \n"));
            StringAssert.Contains("binary", Error("a,b\0\n1,2\n"));
            StringAssert.Contains("no data rows", Error("x,y\n"));
        }

        [Test]
        public void TooManyColumnsIsRejected()
        {
            var header = string.Join(",", new string[CsvTable.MaxColumns + 5]).Replace(",", ",x");
            StringAssert.Contains("too many columns", Error(header + "\n1\n"));
        }

        [Test]
        public void MalformedQuotesDoNotCrash()
        {
            var table = CsvTable.Parse("a,b\n\"unterminated,1\n2,3\n", out _);
            // Either a table or a clean error is fine; it must not throw
            Assert.Pass();
        }

        [Test]
        public void LargeFileParsesQuickly()
        {
            var sb = new System.Text.StringBuilder("x,y,z\n");
            for (var i = 0; i < 100000; i++)
            {
                sb.Append(i).Append(',').Append(i * 2).Append(',').Append(i % 7).Append('\n');
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var table = Parse(sb.ToString());
            watch.Stop();
            Assert.AreEqual(100000, table.Rows.Count);
            Assert.Less(watch.ElapsedMilliseconds, 3000);
        }
    }
}
