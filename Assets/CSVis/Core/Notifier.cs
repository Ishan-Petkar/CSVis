using System;
using System.Collections.Generic;
using UnityEngine;

namespace CSVis.Core
{
    public enum NotificationLevel
    {
        Info,
        Success,
        Warning,
        Error,
    }

    public struct Notification
    {
        public NotificationLevel Level;
        public string Message;
        public float Duration;
    }

    /// <summary>
    /// App-wide user feedback. Anything can call this; a view (see CSVis.UI.ToastView) renders it in the
    /// headset. Without a view the messages still go to the log, so it is safe to call from tests and the editor.
    /// </summary>
    public static class Notifier
    {
        private const int HistorySize = 30;

        private static readonly List<Notification> History = new List<Notification>();

        /// <summary>Raised on the main thread for every notification.</summary>
        public static event Action<Notification> Posted;

        public static IReadOnlyList<Notification> Recent => History;

        public static Notification? Last => History.Count > 0 ? History[History.Count - 1] : (Notification?)null;

        public static void Info(string message) => Post(NotificationLevel.Info, message, 3.5f);

        public static void Success(string message) => Post(NotificationLevel.Success, message, 3f);

        public static void Warning(string message) => Post(NotificationLevel.Warning, message, 5f);

        public static void Error(string message) => Post(NotificationLevel.Error, message, 7f);

        public static void Clear()
        {
            History.Clear();
        }

        private static void Post(NotificationLevel level, string message, float duration)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var notification = new Notification { Level = level, Message = message, Duration = duration };

            History.Add(notification);
            if (History.Count > HistorySize)
            {
                History.RemoveAt(0);
            }

            switch (level)
            {
                case NotificationLevel.Error:
                    Debug.LogWarning("[CSVis] " + message);
                    break;
                case NotificationLevel.Warning:
                    Debug.LogWarning("[CSVis] " + message);
                    break;
                default:
                    Debug.Log("[CSVis] " + message);
                    break;
            }

            try
            {
                Posted?.Invoke(notification);
            }
            catch (Exception e)
            {
                // A broken view must never take the app down
                Debug.LogWarning("[CSVis] Notification view failed: " + e.Message);
            }
        }
    }
}
