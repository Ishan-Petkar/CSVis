using System;
using System.IO;
using UnityEngine;

namespace CSVis.Core
{
    /// <summary>
    /// Last line of defence. Persists errors to a log file (retrievable from the HoloLens Device Portal under
    /// the app's LocalState folder) and tells the user once, without flooding them, when something unexpected happens.
    /// </summary>
    public static class ErrorReporter
    {
        private const string LogFileName = "csvis.log";
        private const long MaxLogBytes = 512 * 1024;
        private const float ToastCooldownSeconds = 8f;

        private static bool initialized;
        private static string logPath;
        private static float lastToastTime = -100f;
        private static string lastMessage;

        public static string LogPath => logPath;

        public static int ErrorCount { get; private set; }

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            try
            {
                logPath = Path.Combine(Application.persistentDataPath, LogFileName);
                RotateIfLarge();
                Write("---- CSVis " + Application.version + " started " + DateTime.Now.ToString("u")
                      + " on " + SystemInfo.deviceModel + " (" + Application.platform + ") ----");
            }
            catch (Exception e)
            {
                logPath = null;
                Debug.LogWarning("[CSVis] File logging unavailable: " + e.Message);
            }

            Application.logMessageReceived += OnLog;
        }

        public static void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            Application.logMessageReceived -= OnLog;
            initialized = false;
        }

        /// <summary>Runs an action and converts any exception into a friendly notification.</summary>
        /// <returns>true when the action completed without throwing.</returns>
        public static bool Guard(string what, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Notifier.Error("Could not " + what + ". " + Friendly(e));
                return false;
            }
        }

        private static string Friendly(Exception e)
        {
            if (e is OutOfMemoryException)
            {
                return "The device ran out of memory. Clear some plots and try again.";
            }

            if (e is FormatException || e is InvalidCastException)
            {
                return "The data was not in the expected format.";
            }

            if (e is IOException || e is UnauthorizedAccessException)
            {
                return "The file could not be read.";
            }

            return "See the log for details.";
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error)
            {
                return;
            }

            ErrorCount++;
            Write(type + ": " + condition + "\n" + stackTrace);

            // Exceptions in per-frame code can fire hundreds of times a second: tell the user once
            var now = Time.realtimeSinceStartup;
            if (now - lastToastTime < ToastCooldownSeconds || condition == lastMessage)
            {
                return;
            }

            lastToastTime = now;
            lastMessage = condition;
            Notifier.Error("Something went wrong, but your plots are safe. Details were saved to the log.");
        }

        private static void RotateIfLarge()
        {
            if (logPath == null || !File.Exists(logPath))
            {
                return;
            }

            if (new FileInfo(logPath).Length > MaxLogBytes)
            {
                var old = logPath + ".old";
                if (File.Exists(old))
                {
                    File.Delete(old);
                }

                File.Move(logPath, old);
            }
        }

        private static void Write(string line)
        {
            if (logPath == null)
            {
                return;
            }

            try
            {
                File.AppendAllText(logPath, line + "\n");
            }
            catch (Exception)
            {
                // Logging must never throw. Disable file logging if the disk is unavailable.
                logPath = null;
            }
        }
    }
}
