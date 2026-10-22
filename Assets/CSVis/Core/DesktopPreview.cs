using UnityEngine;
using UnityEngine.XR;

namespace CSVis.Core
{
    /// <summary>
    /// When the app runs in a normal window (Unity editor, or a desktop build) instead of a headset, there is no
    /// see-through display to draw on. Give the window a proper backdrop, bring everything closer so it fills the
    /// view, and show the simulator controls. None of this runs on a HoloLens.
    /// </summary>
    public sealed class DesktopPreview : MonoBehaviour
    {
        private static readonly Color Backdrop = new Color(0.035f, 0.05f, 0.09f, 1f);

        public static bool IsActive { get; private set; }

        public static float MenuDistance => IsActive ? 0.55f : CSVis.UI.UITheme.MenuDistance;

        /// <summary>Extra yaw (degrees): the menu sits to the left of centre and plots to the right so they don't overlap.</summary>
        public static float MenuYaw => IsActive ? -14f : 0f;

        public static float PlotYaw => IsActive ? 14f : 0f;

        public static float PlotDistance => IsActive ? 1.0f : PlotManager.SpawnDistance;

        public static void Create()
        {
            IsActive = !XRSettings.isDeviceActive;
            if (!IsActive)
            {
                return;
            }

            new GameObject("CSVis Desktop Preview").AddComponent<DesktopPreview>();
        }

        private void Start()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Backdrop;
                camera.fieldOfView = 60f;
            }
        }

        private void OnGUI()
        {
            const string hint =
                "Desktop preview   |   Hold SPACE + move mouse: right hand   |   Left click: press / pinch   |   WASD: walk   |   Hold right mouse: look";
            var style = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = new Color(0.69f, 0.74f, 0.82f, 1f);
            GUI.Label(new Rect(0, Screen.height - 28, Screen.width, 24), hint, style);
        }

        private void OnDestroy()
        {
            IsActive = false;
        }
    }
}
