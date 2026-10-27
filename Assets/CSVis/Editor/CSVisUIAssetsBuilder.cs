using System.IO;
using CSVis.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CSVis.EditorTools
{
    /// <summary>
    /// Creates Assets/Resources/CSVis/UIAssets.asset: the references the runtime UI needs to MRTK3 prefabs
    /// (which live in packages and can't be loaded by path at runtime), fonts and sprites.
    /// </summary>
    public static class CSVisUIAssetsBuilder
    {
        private const string AssetPath = "Assets/Resources/CSVis/UIAssets.asset";
        private const string IconFolder = "Assets/CSVis/Materials/Icons/png/";

        private const string ButtonPrefab = "Packages/org.mixedrealitytoolkit.uxcomponents/Button/Prefabs/Empty Button.prefab";
        private const string PanelPrefab = "Packages/org.mixedrealitytoolkit.uxcomponents/Button/Prefabs/UIPanelBackplate_R16.prefab";

        [MenuItem("CSVis/Rebuild UI Assets")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));

            var assets = AssetDatabase.LoadAssetAtPath<CSVisUIAssets>(AssetPath);
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<CSVisUIAssets>();
                AssetDatabase.CreateAsset(assets, AssetPath);
            }

            assets.buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefab);
            assets.panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefab);
            assets.fontRegular = FindFont("Selawik-Regular");
            assets.fontSemibold = FindFont("Selawik-Semibold");

            assets.logo = Sprite("logo_cube_csvis.png");
            assets.staticPlots = Sprite("logo_cube_static_plots.png");
            assets.dynamicPlots = Sprite("logo_cube_dynamic_plots.png");
            assets.geographicMaps = Sprite("logo_cube_geographic_maps.png");
            assets.sineWave = Sprite("SineWaveIcon.png");
            assets.iris = Sprite("Flower.png");
            assets.electron = Sprite("AtomIcon.png");
            assets.drone = Sprite("DroneIcon.png");
            assets.dodecahedron = Sprite("dodecahedron.png");
            assets.parliament = Sprite("parliament.png");

            if (!assets.IsComplete(out var missing))
            {
                Debug.LogError("[CSVis] UI assets are incomplete: " + missing);
            }

            EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
        }

        private static TMP_FontAsset FindFont(string namePrefix)
        {
            foreach (var guid in AssetDatabase.FindAssets(namePrefix + " t:TMP_FontAsset"))
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (font != null)
                {
                    return font;
                }
            }

            Debug.LogError("[CSVis] Font not found: " + namePrefix);
            return null;
        }

        private static Sprite Sprite(string file)
        {
            var path = IconFolder + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[CSVis] Icon not found: " + path + " (run 'git lfs pull' if it is a pointer file)");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || importer.maxTextureSize != 1024)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Trilinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
