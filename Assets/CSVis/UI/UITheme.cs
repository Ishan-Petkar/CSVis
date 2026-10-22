using UnityEngine;

namespace CSVis.UI
{
    /// <summary>
    /// Design tokens. All lengths are millimetres: menu canvases are scaled by 0.001 so one canvas unit is one mm,
    /// which keeps sizes meaningful for HoloLens ergonomics (touch targets of at least 20 mm, text at about 0.6 degrees).
    /// </summary>
    public static class UITheme
    {
        // Colours (HoloLens is additive, so "dark" means translucent; keep surfaces mid-dark rather than black)
        public static readonly Color PanelFill = new Color(0.075f, 0.09f, 0.12f, 0.94f);
        public static readonly Color ToastFill = new Color(0.06f, 0.07f, 0.1f, 0.96f);
        public static readonly Color TextPrimary = new Color(0.96f, 0.97f, 0.99f, 1f);
        public static readonly Color TextSecondary = new Color(0.69f, 0.74f, 0.82f, 1f);
        public static readonly Color TextDisabled = new Color(0.45f, 0.49f, 0.56f, 1f);
        public static readonly Color Accent = new Color(0.30f, 0.79f, 0.94f, 1f);
        public static readonly Color Success = new Color(0.37f, 0.92f, 0.83f, 1f);
        public static readonly Color Warning = new Color(1.00f, 0.82f, 0.40f, 1f);
        public static readonly Color Danger = new Color(1.00f, 0.42f, 0.42f, 1f);
        public static readonly Color Divider = new Color(1f, 1f, 1f, 0.14f);

        // Layout
        public const float PanelWidth = 400f;
        public const float PanelPadding = 18f;
        public const float HeaderHeight = 60f;
        public const float CardHeight = 68f;
        public const float CardGap = 8f;
        public const float FooterHeight = 46f;
        public const float IconButton = 40f;
        public const float FooterButtonWidth = 112f;

        // Type (canvas units == mm)
        public const float TitleSize = 24f;
        public const float SubtitleSize = 13f;
        public const float CardTitleSize = 16f;
        public const float CardBodySize = 11.5f;
        public const float FooterSize = 13f;
        public const float ToastSize = 14f;

        // Where the menu appears relative to the user's eyes
        public const float MenuDistance = 0.85f;
        public const float MenuDropMeters = 0.06f;

        public static Color ForLevel(CSVis.Core.NotificationLevel level)
        {
            switch (level)
            {
                case CSVis.Core.NotificationLevel.Success: return Success;
                case CSVis.Core.NotificationLevel.Warning: return Warning;
                case CSVis.Core.NotificationLevel.Error: return Danger;
                default: return Accent;
            }
        }
    }
}
