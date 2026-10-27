using TMPro;
using UnityEngine;

namespace CSVis.UI
{
    /// <summary>
    /// References to the MRTK3 prefabs, fonts and sprites the code-built UI needs. Packages can't be loaded by path
    /// at runtime, so the editor menu CSVis > Setup for HoloLens 2 fills this asset in (Resources/CSVis/UIAssets).
    /// </summary>
    public sealed class CSVisUIAssets : ScriptableObject
    {
        public const string ResourcePath = "CSVis/UIAssets";

        [Header("MRTK3 prefabs")]
        public GameObject buttonPrefab;
        public GameObject panelPrefab;

        [Header("Fonts")]
        public TMP_FontAsset fontRegular;
        public TMP_FontAsset fontSemibold;

        [Header("Sprites")]
        public Sprite logo;
        public Sprite staticPlots;
        public Sprite dynamicPlots;
        public Sprite geographicMaps;
        public Sprite sineWave;
        public Sprite iris;
        public Sprite electron;
        public Sprite drone;
        public Sprite dodecahedron;
        public Sprite parliament;

        private static CSVisUIAssets cached;

        public static CSVisUIAssets Load()
        {
            if (cached == null)
            {
                cached = Resources.Load<CSVisUIAssets>(ResourcePath);
            }

            return cached;
        }

        /// <summary>True when everything the builder needs is assigned.</summary>
        public bool IsComplete(out string missing)
        {
            missing = null;
            if (buttonPrefab == null) missing = "buttonPrefab";
            else if (panelPrefab == null) missing = "panelPrefab";
            else if (fontRegular == null) missing = "fontRegular";
            else if (fontSemibold == null) missing = "fontSemibold";
            return missing == null;
        }
    }
}
