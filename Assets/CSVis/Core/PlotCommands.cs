using System;
using CSVis.ExampleLoader;
using CSVis.IO;
using UnityEngine;

namespace CSVis.Core
{
    /// <summary>
    /// The only entry points the UI uses to create plots. Each one checks limits and prerequisites, never throws,
    /// cleans up after itself on failure, and tells the user what happened.
    /// </summary>
    public static class PlotCommands
    {
        /// <summary>Files bigger than this would exhaust HoloLens 2 memory long before they finish plotting.</summary>
        public const int MaxFileCharacters = 15 * 1000 * 1000;

        public static bool RunExample<TLoader>(string displayName, bool needsInternet = false) where TLoader : PlotLoader
        {
            return RunExample(typeof(TLoader), displayName, needsInternet);
        }

        public static bool RunExample(Type loaderType, string displayName, bool needsInternet = false)
        {
            if (!PlotManager.CanAdd(out var reason))
            {
                Notifier.Warning(reason);
                return false;
            }

            if (needsInternet && !MapAvailability.IsAvailable(out reason))
            {
                Notifier.Warning(reason);
                return false;
            }

            var host = new GameObject("CSVis Loader");
            var ok = false;
            try
            {
                var loader = (PlotLoader)host.AddComponent(loaderType);
                loader.LoadPlot();
                ok = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PlotManager.RemoveUnregistered();
                Notifier.Error("Could not add " + displayName + ". " + (e is OutOfMemoryException
                    ? "The device ran out of memory."
                    : "Try again, or clear some plots first."));
            }
            finally
            {
                UnityEngine.Object.Destroy(host);
            }

            if (ok)
            {
                Notifier.Success(displayName + " added.");
            }

            return ok;
        }

        public static PlotResult PlotCsv(string fileName, string csvText)
        {
            PlotResult result;

            if (!PlotManager.CanAdd(out var reason))
            {
                result = PlotResult.Fail(reason);
            }
            else if (string.IsNullOrEmpty(csvText))
            {
                result = PlotResult.Fail("The file is empty.");
            }
            else if (csvText.Length > MaxFileCharacters)
            {
                result = PlotResult.Fail("That file is too large for the HoloLens (" + (csvText.Length / 1000000) + " MB). Try a file under 15 MB.");
            }
            else
            {
                var host = new GameObject("CSVis Loader");
                try
                {
                    result = host.AddComponent<CustomFilePlotLoader>().PlotCustomFile(fileName, csvText);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    result = PlotResult.Fail("Could not plot this file. " + (e is OutOfMemoryException ? "The device ran out of memory." : e.Message));
                }
                finally
                {
                    UnityEngine.Object.Destroy(host);
                }
            }

            if (!result.Success)
            {
                PlotManager.RemoveUnregistered();
                Notifier.Error(result.Message);
            }
            else if (result.SkippedRows > 0)
            {
                Notifier.Warning(result.Message);
            }
            else
            {
                Notifier.Success(result.Message);
            }

            return result;
        }
    }
}
