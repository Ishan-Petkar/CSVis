using System.Collections.Generic;
using System.IO;
using System.Linq;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace CSVis.EditorTools
{
    /// <summary>
    /// One-shot migration/setup of the project for Unity 6 + MRTK3 + OpenXR on HoloLens 2.
    /// Safe to run repeatedly. Run from the menu, or headless:
    ///   Unity -batchmode -quit -projectPath . -executeMethod CSVis.EditorTools.CSVisProjectSetup.Run
    /// </summary>
    public static class CSVisProjectSetup
    {
        private const string MenuScenePath = "Assets/Scenes/MenuScene.unity";
        private const string PlotContainerPath = "Assets/Resources/CSVis/PlotContainer.prefab";
        private const string BoundsVisualsPath = "Assets/Resources/CSVis/BoundsVisuals.prefab";

        private const string XrRigPath = "Packages/org.mixedrealitytoolkit.input/Assets/Prefabs/MRTK XR Rig.prefab";
        private const string InputSimulatorPath = "Packages/org.mixedrealitytoolkit.input/Simulation/Prefabs/MRTKInputSimulator.prefab";
        private const string BoundsVisualsSourcePath = "Packages/org.mixedrealitytoolkit.spatialmanipulation/BoundsControl/Prefabs/BoundingBoxWithTraditionalHandles.prefab";
        private const string DefaultProfilePath = "Packages/org.mixedrealitytoolkit.core/Configuration/Default Profiles/MRTKProfile.asset";

        private const string HoloLensFeatureSetId = "com.microsoft.openxr.featureset.hololens";

        // OpenXR features MRTK3 wants on HoloLens 2 (matched on feature class name)
        private static readonly string[] HoloLensFeatureTypes =
        {
            "MixedRealityFeaturePlugin",
            "HandTrackingFeaturePlugin",
            "MotionControllerFeaturePlugin",
            "HandInteractionProfile",
            "MicrosoftHandInteraction",
            "EyeGazeInteraction",
            "HandTracking",
        };

        [MenuItem("CSVis/Setup for HoloLens 2")]
        public static void Run()
        {
            CSVisUIAssetsBuilder.Build();
            RebuildPlotContainer();
            CopyBoundsVisuals();
            SetupMenuScene();
            ConfigureMrtkProfile();
            ConfigureXr();
            ConfigurePlayer();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CSVis] HoloLens 2 setup complete");
        }

        /// <summary>
        /// PlotContainer is the prefab every plot is built inside: a PlotHolder (tagged plotContainer) that
        /// the plotters fill and the user grabs. Its toolbar is created at runtime by PlotToolbar.
        /// </summary>
        private static void RebuildPlotContainer()
        {
            var container = new GameObject("PlotContainer");

            var holder = new GameObject("PlotHolder");
            holder.tag = "plotContainer";
            holder.transform.SetParent(container.transform, false);

            PrefabUtility.SaveAsPrefabAsset(container, PlotContainerPath);
            Object.DestroyImmediate(container);
        }

        private static void CopyBoundsVisuals()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BoundsVisualsPath) == null)
            {
                AssetDatabase.CopyAsset(BoundsVisualsSourcePath, BoundsVisualsPath);
            }
        }

        /// <summary>
        /// The scene is just the MRTK3 XR rig, the editor input simulator, a light and the CSVis bootstrap;
        /// every menu is built in code at runtime.
        /// </summary>
        private static void SetupMenuScene()
        {
            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "MixedRealityPlayspace" || root.name == "MixedRealityToolkit" || root.name == "Menu")
                {
                    Object.DestroyImmediate(root);
                }
            }

            var roots = scene.GetRootGameObjects();
            if (!roots.Any(r => r.name.StartsWith("MRTK XR Rig")))
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(XrRigPath), scene);
            }
            if (!roots.Any(r => r.name.StartsWith("MRTKInputSimulator")))
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(InputSimulatorPath), scene);
            }
            if (!roots.Any(r => r.name == "CSVis"))
            {
                var app = new GameObject("CSVis");
                app.AddComponent<CSVis.Core.AppBootstrap>();
                SceneManager.MoveGameObjectToScene(app, scene);
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!PrefabUtility.IsPartOfPrefabInstance(t.gameObject) || PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                    }
                }
            }

            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MenuScenePath, true) };
        }

        private static void ConfigureMrtkProfile()
        {
            MRTKSettings.ProfileForBuildTarget(BuildTargetGroup.Standalone); // creates Assets/MRTK.Generated/MRTKSettings.asset
            var settings = AssetDatabase.LoadAssetAtPath<MRTKSettings>("Assets/MRTK.Generated/MRTKSettings.asset");
            var profile = AssetDatabase.LoadAssetAtPath<MRTKProfile>(DefaultProfilePath);
            if (settings == null || profile == null)
            {
                Debug.LogError("[CSVis] Could not configure the MRTK profile");
                return;
            }

            foreach (var group in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA })
            {
                if (settings.GetProfileForBuildTarget(group) == null)
                {
                    settings.SetProfileForBuildTarget(group, profile);
                }
            }
            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// OpenXR loader + Microsoft HoloLens feature set for UWP. The Mac editor uses no XR loader;
        /// MRTK's input simulator drives the scene instead.
        /// </summary>
        private static void ConfigureXr()
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget))
            {
                Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.WSA))
            {
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.WSA);
            }

            var wsa = perTarget.SettingsForBuildTarget(BuildTargetGroup.WSA);
            wsa.InitManagerOnStart = true;
            XRPackageMetadataStore.AssignLoader(wsa.AssignedSettings, typeof(OpenXRLoader).FullName, BuildTargetGroup.WSA);
            EditorUtility.SetDirty(perTarget);

            var featureSet = OpenXRFeatureSetManager.GetFeatureSetWithId(BuildTargetGroup.WSA, HoloLensFeatureSetId);
            if (featureSet != null)
            {
                featureSet.isEnabled = true;
                OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(BuildTargetGroup.WSA);
            }
            else
            {
                Debug.LogWarning("[CSVis] Microsoft HoloLens OpenXR feature set not found");
            }

            var openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.WSA);
            if (openXr == null)
            {
                Debug.LogWarning("[CSVis] No OpenXR settings for UWP yet - open Project Settings > XR Plug-in Management once on the Windows PC");
                return;
            }

            openXr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            openXr.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth16Bit;

            var enabled = new List<string>();
            foreach (var feature in openXr.GetFeatures())
            {
                var typeName = feature.GetType().Name;
                if (HoloLensFeatureTypes.Any(n => typeName.Contains(n)))
                {
                    feature.enabled = true;
                    enabled.Add(typeName);
                }
            }
            EditorUtility.SetDirty(openXr);
            Debug.Log("[CSVis] OpenXR (UWP) features enabled: " + string.Join(", ", enabled));
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Carleton University";
            PlayerSettings.productName = "CSVis";

            var uwp = NamedBuildTarget.WindowsStoreApps;
            PlayerSettings.SetScriptingBackend(uwp, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(uwp, ApiCompatibilityLevel.NET_Unity_4_8);

            PlayerSettings.WSA.packageName = "CSVis";
            PlayerSettings.WSA.applicationDescription = "CSVis - mixed reality data visualization";
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClient, true);      // Mapbox tiles
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.InternetClientServer, true);
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.SpatialPerception, true);   // spatial awareness
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.GazeInput, true);           // eye gaze
            PlayerSettings.WSA.SetCapability(PlayerSettings.WSACapability.Microphone, true);          // MRTK speech
            PlayerSettings.WSA.SetTargetDeviceFamily(PlayerSettings.WSATargetFamily.Holographic, true);

            EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.D3D;
            EditorUserBuildSettings.wsaArchitecture = "ARM64";

            // MRTK3 is built on the Input System; keep the legacy manager for the existing scripts
            var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            if (playerSettings != null)
            {
                var so = new SerializedObject(playerSettings);
                var inputHandler = so.FindProperty("activeInputHandler");
                if (inputHandler != null)
                {
                    inputHandler.intValue = 2; // Both
                    so.ApplyModifiedProperties();
                }
            }
        }
    }
}
