using System;
using System.Collections.Generic;
using CSVis.Core;
using CSVis.ExampleLoader;
using CSVis.ExampleLoader.Helper;
using UnityEngine;

namespace CSVis.IO
{
    /// <summary>
    /// Turns a user supplied CSV into a plot. The layout is inferred from the number of columns:
    /// <list type="bullet">
    /// <item>2 columns: 2D static plot (X, Y)</item>
    /// <item>3 columns: 3D static plot (X, Y, Z)</item>
    /// <item>1 + 3n columns: dynamic plot with n animated points (time, then X, Y, Z for each point)</item>
    /// <item>6 columns: geographic map (latitude, longitude, height, R, G, B)</item>
    /// </list>
    /// Every failure is reported through the returned <see cref="PlotResult"/>; nothing here throws on bad input.
    /// </summary>
    public class CustomFilePlotLoader : PlotLoader
    {
        public const int MaxStaticPoints = 5000;
        public const int MaxDynamicPoints = 50;
        public const int MaxDynamicRows = 3000;
        public const int MaxMapLocations = 300;

        private const int StaticPlot2DColumns = 2;
        private const int StaticPlot3DColumns = 3;
        private const int GeoMapColumns = 6;

        private const int GeoMapLatitude = 0;
        private const int GeoMapLongitude = 1;
        private const int GeoMapValue = 2;
        private const int GeoMapR = 3;
        private const int GeoMapG = 4;
        private const int GeoMapB = 5;

        private const float GeoSpawnScale = 0.01f;
        private const float GeoHeightScaleMax = 0.2f;
        private const float GeoHeightScaleMin = 0f;

        public const string SupportedLayouts =
            "Supported layouts: 2 columns (X, Y), 3 columns (X, Y, Z), time + X, Y, Z per point (1+3n columns), " +
            "or 6 columns for a map (latitude, longitude, height, R, G, B).";

        /// <summary>What kind of plot <see cref="PlotCustomFile"/> would build for this table, or null if unsupported.</summary>
        public static string DescribeLayout(int columns)
        {
            if (columns == StaticPlot2DColumns) return "2D plot";
            if (columns == StaticPlot3DColumns) return "3D plot";
            if (columns == GeoMapColumns) return "map";
            if (columns > 3 && (columns - 1) % 3 == 0) return "animated plot";
            return null;
        }

        public PlotResult PlotCustomFile(string fileName, string csv)
        {
            var title = TitleFromFileName(fileName);

            var table = CsvTable.Parse(csv, out var error);
            if (table == null)
            {
                return PlotResult.Fail(error);
            }

            var columns = table.ColumnCount;
            if (columns < 2)
            {
                return PlotResult.Fail("The file has only one column. CSVis needs at least two numeric columns. " + SupportedLayouts);
            }

            try
            {
                if (columns == StaticPlot2DColumns)
                {
                    return PlotStatic(title, table, 2);
                }

                if (columns == StaticPlot3DColumns)
                {
                    return PlotStatic(title, table, 3);
                }

                if (columns == GeoMapColumns)
                {
                    return PlotGeographicMap(title, table);
                }

                if ((columns - 1) % 3 == 0)
                {
                    return PlotDynamic(title, table);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return PlotResult.Fail("Could not build a plot from this file (" + e.GetType().Name + ").");
            }

            return PlotResult.Fail("The file has " + columns + " columns, which CSVis can't plot. " + SupportedLayouts);
        }

        private PlotResult PlotStatic(string title, CsvTable table, int dimensions)
        {
            var indices = new int[dimensions];
            for (var i = 0; i < dimensions; i++)
            {
                indices[i] = i;
            }

            var skipped = table.ReadAlignedFloatColumns(indices, out var columns, MaxStaticPoints);
            var points = columns[0].Count;
            if (points == 0)
            {
                return PlotResult.Fail("No rows contained valid numbers in all " + dimensions + " columns.");
            }

            StaticPlotHelper.StaticPlotData data;
            if (dimensions == 2)
            {
                data = new StaticPlotHelper.StaticPlotData(title, columns[0], columns[1], table.Headers[0], table.Headers[1]);
            }
            else
            {
                data = new StaticPlotHelper.StaticPlotData(
                    title, columns[0], columns[1], columns[2], table.Headers[0], table.Headers[1], table.Headers[2]);
            }

            var plotter = StaticPlotHelper.GetStaticPlotter(data);
            var plot = Instantiate(plotter);
            SetSpawn(plot.PointHolder, title);

            return Describe(title, points, skipped, table.Rows.Count, MaxStaticPoints);
        }

        private PlotResult PlotDynamic(string title, CsvTable table)
        {
            var pointCount = (table.ColumnCount - 1) / 3;
            if (pointCount > MaxDynamicPoints)
            {
                return PlotResult.Fail("The file describes " + pointCount + " moving points. The limit is " + MaxDynamicPoints + ".");
            }

            var times = table.ReadStringColumn(0, MaxDynamicRows);
            var pointColumns = new List<DynamicPlotHelper.DynamicPlotData.PointColumns>();
            var skipped = 0;
            var emptyPoints = 0;
            var longest = 0;

            for (var p = 0; p < pointCount; p++)
            {
                var offset = 1 + p * 3;
                skipped += table.ReadAlignedFloatColumns(new[] { offset, offset + 1, offset + 2 }, out var xyz, MaxDynamicRows);
                if (xyz[0].Count == 0)
                {
                    // e.g. a point that is only present for part of the recording and blank otherwise
                    emptyPoints++;
                    continue;
                }

                longest = Math.Max(longest, xyz[0].Count);
                pointColumns.Add(new DynamicPlotHelper.DynamicPlotData.PointColumns(xyz[0], xyz[1], xyz[2]));
            }

            if (pointColumns.Count == 0)
            {
                return PlotResult.Fail("None of the " + pointCount + " points had any valid X, Y, Z numbers.");
            }

            // The time label is indexed by row, so it can never be longer than the longest point
            if (times.Count > longest)
            {
                times.RemoveRange(longest, times.Count - longest);
            }

            var firstXyz = 1;
            var data = new DynamicPlotHelper.DynamicPlotData(
                title, pointColumns, times,
                table.Headers[firstXyz], table.Headers[firstXyz + 1], table.Headers[firstXyz + 2]);

            var plotter = DynamicPlotHelper.GetDynamicPlotter(data);
            var plot = Instantiate(plotter);
            SetSpawn(plot.PointHolder, title);

            var result = Describe(title, pointColumns.Count, skipped, table.Rows.Count, MaxDynamicRows);
            if (emptyPoints > 0)
            {
                result.Message += " " + emptyPoints + " empty point" + (emptyPoints == 1 ? " was" : "s were") + " ignored.";
            }

            return result;
        }

        private PlotResult PlotGeographicMap(string title, CsvTable table)
        {
            if (!MapAvailability.IsAvailable(out var reason))
            {
                return PlotResult.Fail(reason);
            }

            var indices = new[] { GeoMapLatitude, GeoMapLongitude, GeoMapValue, GeoMapR, GeoMapG, GeoMapB };
            var skipped = table.ReadAlignedFloatColumns(indices, out var columns, MaxMapLocations);
            var count = columns[GeoMapLatitude].Count;
            if (count == 0)
            {
                return PlotResult.Fail("No rows had valid numbers in all 6 columns (latitude, longitude, height, R, G, B).");
            }

            float minLat = float.MaxValue, maxLat = float.MinValue, minLon = float.MaxValue, maxLon = float.MinValue;
            for (var i = 0; i < count; i++)
            {
                var lat = columns[GeoMapLatitude][i];
                var lon = columns[GeoMapLongitude][i];
                if (lat < -85f || lat > 85f || lon < -180f || lon > 180f)
                {
                    return PlotResult.Fail(
                        "Row " + (i + 1) + " has coordinates out of range. The first column must be latitude (-85 to 85) " +
                        "and the second longitude (-180 to 180).");
                }

                minLat = Math.Min(minLat, lat);
                maxLat = Math.Max(maxLat, lat);
                minLon = Math.Min(minLon, lon);
                maxLon = Math.Max(maxLon, lon);
            }

            // Colours are 0-1, but spreadsheets usually export 0-255
            var colorScale = 1f;
            for (var i = 0; i < count; i++)
            {
                if (columns[GeoMapR][i] > 1f || columns[GeoMapG][i] > 1f || columns[GeoMapB][i] > 1f)
                {
                    colorScale = 1f / 255f;
                    break;
                }
            }

            var colors = new List<Color>(count);
            var locations = new List<string>(count);
            for (var i = 0; i < count; i++)
            {
                colors.Add(new Color(
                    Mathf.Clamp01(columns[GeoMapR][i] * colorScale),
                    Mathf.Clamp01(columns[GeoMapG][i] * colorScale),
                    Mathf.Clamp01(columns[GeoMapB][i] * colorScale)));

                // Mapbox expects "latitude, longitude"
                locations.Add(columns[GeoMapLatitude][i].ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)
                              + ", " + columns[GeoMapLongitude][i].ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture));
            }

            var centerLat = (minLat + maxLat) / 2f;
            var centerLon = (minLon + maxLon) / 2f;
            var zoom = ZoomForSpan(Math.Max(maxLat - minLat, maxLon - minLon));

            var geoHelper = gameObject.AddComponent<GeoMapPlotHelper>();
            var data = new GeoMapPlotHelper.GeoMapData(
                title,
                centerLat,
                centerLon,
                zoom,
                GeoMapPlotHelper.GeoDefaultPlotScale,
                locations,
                columns[GeoMapValue],
                colors,
                GeoSpawnScale,
                GeoHeightScaleMax,
                GeoHeightScaleMin,
                PlotHelper.GetPlotHolder());

            var plotter = geoHelper.GetMrMap(data);
            var map = Instantiate(plotter);
            SetSpawn(map.MapHolder, title);

            return Describe(title, count, skipped, table.Rows.Count, MaxMapLocations);
        }

        /// <summary>Picks a Mapbox zoom level that fits the data's geographic spread (in degrees).</summary>
        public static int ZoomForSpan(float spanDegrees)
        {
            if (spanDegrees < 0.003f) return 17;
            if (spanDegrees < 0.01f) return 16;
            if (spanDegrees < 0.03f) return 15;
            if (spanDegrees < 0.08f) return 14;
            if (spanDegrees < 0.2f) return 13;
            if (spanDegrees < 0.5f) return 12;
            if (spanDegrees < 1.5f) return 10;
            if (spanDegrees < 5f) return 8;
            return 6;
        }

        private static PlotResult Describe(string title, int plotted, int skipped, int totalRows, int cap)
        {
            var message = "Plotted " + plotted.ToString("N0") + (plotted == 1 ? " point" : " points") + " from " + title + ".";
            if (totalRows > cap)
            {
                message += " Only the first " + cap.ToString("N0") + " rows are shown.";
            }

            if (skipped > 0)
            {
                message += " " + skipped.ToString("N0") + (skipped == 1 ? " row was" : " rows were") + " skipped (not numbers).";
            }

            return PlotResult.Ok(message, plotted, skipped);
        }

        public static string TitleFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "CSV plot";
            }

            var name = System.IO.Path.GetFileNameWithoutExtension(fileName.Trim());
            return string.IsNullOrWhiteSpace(name) ? "CSV plot" : name;
        }

        public override void LoadPlot()
        {
            Debug.LogWarning("CustomFilePlotLoader plots a file: call PlotCustomFile(name, csv) instead.");
        }
    }
}
