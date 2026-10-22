using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CSVis.IO
{
    /// <summary>
    /// Tolerant, allocation-light CSV reader for user supplied files.
    /// Handles a UTF-8 BOM, CRLF/LF/CR line endings, quoted fields (with escaped quotes and embedded
    /// delimiters/newlines), comma / semicolon / tab delimiters, trailing empty columns and headerless files.
    /// Never throws: problems are reported through <see cref="Parse"/>'s error output.
    /// </summary>
    public sealed class CsvTable
    {
        public const int MaxColumns = 512;

        /// <summary>Column headers. Synthesised ("Column 1", ...) when the file has no header row.</summary>
        public string[] Headers { get; private set; }

        /// <summary>Data rows, each padded/truncated to <see cref="ColumnCount"/> fields.</summary>
        public List<string[]> Rows { get; private set; }

        public int ColumnCount => Headers.Length;

        public bool HadHeader { get; private set; }

        public char Delimiter { get; private set; }

        private CsvTable() { }

        /// <summary>
        /// Parses CSV text. Returns null and sets <paramref name="error"/> if nothing usable was found.
        /// </summary>
        public static CsvTable Parse(string text, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "The file is empty.";
                return null;
            }

            if (text[0] == '﻿')
            {
                text = text.Substring(1);
            }

            if (text.IndexOf('\0') >= 0)
            {
                error = "This looks like a binary file, not a CSV text file.";
                return null;
            }

            var delimiter = DetectDelimiter(text);
            var records = ReadRecords(text, delimiter);
            if (records.Count == 0)
            {
                error = "No rows were found in the file.";
                return null;
            }

            // Drop blank lines and trim trailing empty columns shared by every row
            records.RemoveAll(IsBlankRecord);
            if (records.Count == 0)
            {
                error = "No rows were found in the file.";
                return null;
            }

            var columns = 0;
            foreach (var record in records)
            {
                columns = Math.Max(columns, LastNonEmpty(record) + 1);
            }

            if (columns == 0)
            {
                error = "The file contains no data.";
                return null;
            }

            if (columns > MaxColumns)
            {
                error = "The file has too many columns (" + columns + "). The limit is " + MaxColumns + ".";
                return null;
            }

            var normalized = new List<string[]>(records.Count);
            foreach (var record in records)
            {
                var row = new string[columns];
                for (var i = 0; i < columns; i++)
                {
                    row[i] = i < record.Count ? record[i].Trim() : string.Empty;
                }
                normalized.Add(row);
            }

            var table = new CsvTable { Delimiter = delimiter, Rows = normalized };

            if (LooksLikeHeader(normalized[0]))
            {
                table.HadHeader = true;
                table.Headers = normalized[0];
                normalized.RemoveAt(0);
            }
            else
            {
                table.Headers = new string[columns];
                for (var i = 0; i < columns; i++)
                {
                    table.Headers[i] = "Column " + (i + 1);
                }
            }

            // Header cells can be blank (e.g. ",1,,,2,,,3"); give them a stable name
            for (var i = 0; i < table.Headers.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(table.Headers[i]))
                {
                    table.Headers[i] = "Column " + (i + 1);
                }
            }

            if (normalized.Count == 0)
            {
                error = "The file has a header but no data rows.";
                return null;
            }

            return table;
        }

        /// <summary>
        /// Reads a column as floats. Rows whose cell is not a finite number are skipped.
        /// </summary>
        /// <returns>The number of rows that were skipped.</returns>
        public int ReadFloatColumn(int column, List<float> values, int maxRows = int.MaxValue)
        {
            var skipped = 0;
            for (var r = 0; r < Rows.Count && values.Count < maxRows; r++)
            {
                if (TryParseNumber(Rows[r][column], Delimiter, out var value))
                {
                    values.Add(value);
                }
                else
                {
                    skipped++;
                }
            }

            return skipped;
        }

        /// <summary>
        /// Reads several numeric columns, keeping only rows where all of them are valid numbers so the columns stay aligned.
        /// </summary>
        /// <returns>The number of rows that were skipped.</returns>
        public int ReadAlignedFloatColumns(int[] columns, out List<float>[] result, int maxRows = int.MaxValue)
        {
            result = new List<float>[columns.Length];
            for (var c = 0; c < columns.Length; c++)
            {
                result[c] = new List<float>();
            }

            var skipped = 0;
            var buffer = new float[columns.Length];
            foreach (var row in Rows)
            {
                if (result[0].Count >= maxRows)
                {
                    break;
                }

                var ok = true;
                for (var c = 0; c < columns.Length && ok; c++)
                {
                    ok = TryParseNumber(row[columns[c]], Delimiter, out buffer[c]);
                }

                if (!ok)
                {
                    skipped++;
                    continue;
                }

                for (var c = 0; c < columns.Length; c++)
                {
                    result[c].Add(buffer[c]);
                }
            }

            return skipped;
        }

        public List<string> ReadStringColumn(int column, int maxRows = int.MaxValue)
        {
            var values = new List<string>(Math.Min(Rows.Count, maxRows));
            for (var r = 0; r < Rows.Count && values.Count < maxRows; r++)
            {
                values.Add(Rows[r][column]);
            }

            return values;
        }

        public static bool TryParseNumber(string text, char delimiter, out float value)
        {
            value = 0f;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;

            // European files ("1,5" with ';' as the delimiter) use a decimal comma, so it must be tried before
            // the comma is read as a thousands separator ("1,001.20" has both and stays a thousands comma)
            if (delimiter != ',' && text.IndexOf(',') >= 0 && text.IndexOf('.') < 0)
            {
                if (!float.TryParse(text.Replace(',', '.'), styles, CultureInfo.InvariantCulture, out value))
                {
                    return false;
                }
            }
            else if (!float.TryParse(text, styles, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool LooksLikeHeader(string[] firstRow)
        {
            // A header row is any row that is not entirely numeric. Blank cells count as non-numeric,
            // which is what we want for headers such as ",1,,,2,,,3".
            foreach (var cell in firstRow)
            {
                if (!TryParseNumber(cell, ',', out _))
                {
                    return true;
                }
            }

            return false;
        }

        private static char DetectDelimiter(string text)
        {
            // Look at the first few non-empty lines, ignoring anything inside quotes. On a tie the rarer
            // delimiters win: "1,5;2,25" is far more likely semicolon-separated than comma-separated.
            char[] candidates = { '\t', ';', ',' };
            var bestCount = 0;
            var best = ',';
            foreach (var candidate in candidates)
            {
                var count = 0;
                var inQuotes = false;
                var lines = 0;
                for (var i = 0; i < text.Length && lines < 5; i++)
                {
                    var ch = text[i];
                    if (ch == '"')
                    {
                        inQuotes = !inQuotes;
                    }
                    else if (!inQuotes && ch == candidate)
                    {
                        count++;
                    }
                    else if (!inQuotes && ch == '\n')
                    {
                        lines++;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = candidate;
                }
            }

            return best;
        }

        private static List<List<string>> ReadRecords(string text, char delimiter)
        {
            var records = new List<List<string>>();
            var record = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;
            var length = text.Length;

            for (var i = 0; i < length; i++)
            {
                var ch = text[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(ch);
                    }

                    continue;
                }

                if (ch == '"' && field.Length == 0)
                {
                    inQuotes = true;
                }
                else if (ch == delimiter)
                {
                    record.Add(field.ToString());
                    field.Length = 0;
                }
                else if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && i + 1 < length && text[i + 1] == '\n')
                    {
                        i++;
                    }

                    record.Add(field.ToString());
                    field.Length = 0;
                    records.Add(record);
                    record = new List<string>();
                }
                else
                {
                    field.Append(ch);
                }
            }

            if (field.Length > 0 || record.Count > 0)
            {
                record.Add(field.ToString());
                records.Add(record);
            }

            return records;
        }

        private static bool IsBlankRecord(List<string> record)
        {
            return LastNonEmpty(record) < 0;
        }

        private static int LastNonEmpty(List<string> record)
        {
            for (var i = record.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(record[i]))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
