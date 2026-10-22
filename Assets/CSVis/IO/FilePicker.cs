using System;
using System.IO;
using CSVis.Core;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Windows.Storage;
using Windows.Storage.Pickers;
#endif

namespace CSVis.IO
{
    /// <summary>
    /// Opens the platform file picker for a CSV and hands the text back on Unity's main thread.
    /// HoloLens (UWP) uses the system picker, the editor uses the native open dialog.
    /// Always reports problems to the user instead of throwing.
    /// </summary>
    public static class FilePicker
    {
        private static bool pickerOpen;

        public static bool IsSupported
        {
            get
            {
#if UNITY_EDITOR || ENABLE_WINMD_SUPPORT
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>Opens the picker and plots the chosen file.</summary>
        public static void PickAndPlot()
        {
            PickCsv((name, text) => PlotCommands.PlotCsv(name, text));
        }

        public static void PickCsv(Action<string, string> onPicked)
        {
            if (pickerOpen)
            {
                Notifier.Info("The file picker is already open.");
                return;
            }

            if (!IsSupported)
            {
                Notifier.Warning("Opening files is only available on the HoloLens and in the Unity editor.");
                return;
            }

#if UNITY_EDITOR
            PickInEditor(onPicked);
#elif ENABLE_WINMD_SUPPORT
            PickOnUwp(onPicked);
#endif
        }

#if UNITY_EDITOR
        private static void PickInEditor(Action<string, string> onPicked)
        {
            var path = UnityEditor.EditorUtility.OpenFilePanel("Open a CSV file", string.Empty, "csv,txt");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                onPicked(Path.GetFileName(path), File.ReadAllText(path));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Notifier.Error("Could not read that file: " + e.Message);
            }
        }
#endif

#if ENABLE_WINMD_SUPPORT
        private static void PickOnUwp(Action<string, string> onPicked)
        {
            pickerOpen = true;

            // The picker must run on the UWP UI thread; results come back to Unity's app thread
            UnityEngine.WSA.Application.InvokeOnUIThread(async () =>
            {
                string name = null;
                string text = null;
                string error = null;

                try
                {
                    var picker = new FileOpenPicker
                    {
                        SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                        ViewMode = PickerViewMode.List,
                    };
                    picker.FileTypeFilter.Add(".csv");
                    picker.FileTypeFilter.Add(".txt");

                    StorageFile file = await picker.PickSingleFileAsync();
                    if (file != null)
                    {
                        name = file.Name;
                        text = await FileIO.ReadTextAsync(file);
                    }
                }
                catch (Exception e)
                {
                    error = e.Message;
                }

                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    pickerOpen = false;

                    if (error != null)
                    {
                        Notifier.Error("Could not open that file: " + error);
                    }
                    else if (text != null)
                    {
                        onPicked(name, text);
                    }
                }, false);
            }, false);
        }
#endif
    }
}
