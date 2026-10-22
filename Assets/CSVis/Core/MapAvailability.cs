using UnityEngine;

namespace CSVis.Core
{
    /// <summary>Checks the things a Mapbox map needs before we try to build one.</summary>
    public static class MapAvailability
    {
        private const string ConfigResource = "Mapbox/MapboxConfiguration";

        public static bool IsAvailable(out string reason)
        {
            reason = null;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                reason = "Maps need an internet connection. Connect to Wi-Fi and try again.";
                return false;
            }

            var config = Resources.Load<TextAsset>(ConfigResource);
            if (config == null || !config.text.Contains("\"AccessToken\"") || !config.text.Contains("pk."))
            {
                reason = "Maps are not configured: add a Mapbox access token to Assets/Resources/Mapbox/MapboxConfiguration.txt.";
                return false;
            }

            return true;
        }
    }
}
