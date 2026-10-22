using CSVis.UI;
using UnityEngine;

namespace CSVis.Core
{
    /// <summary>
    /// Entry point, placed once in the scene. Starts error reporting and builds the menu and toast UI.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class AppBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            ErrorReporter.Initialize();

            var assets = CSVisUIAssets.Load();
            if (assets == null)
            {
                Debug.LogError("CSVis UI assets are missing. In the Unity editor run the menu CSVis > Setup for HoloLens 2.");
                return;
            }

            if (!assets.IsComplete(out var missing))
            {
                Debug.LogError("CSVis UI assets are incomplete (" + missing + " is not assigned). Run CSVis > Setup for HoloLens 2.");
                return;
            }

            DesktopPreview.Create();
            ToastView.Create(assets);
            MenuController.Create(assets);
        }

        private void OnDestroy()
        {
            ErrorReporter.Shutdown();
        }
    }
}
