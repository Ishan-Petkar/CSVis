using System;
using System.Collections.Generic;
using System.IO;
using CSVis.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace CSVis.EditorTools
{
    /// <summary>
    /// Catches the setup mistakes that would otherwise only show up on the headset (or as a silent blank app):
    /// Git LFS placeholders instead of real files, an expired signing certificate, a missing OpenXR loader,
    /// incomplete UI assets. Runs before every build and from the CSVis > Validate Project menu.
    /// </summary>
    public sealed class CSVisBuildValidator : IPreprocessBuildWithReport
    {
        private static readonly string[] BinaryExtensions = { ".dll", ".png", ".jpg", ".fbx", ".wav", ".mp3", ".ttf" };

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            Validate(errors, warnings);

            foreach (var warning in warnings)
            {
                Debug.LogWarning("[CSVis] " + warning);
            }

            if (errors.Count > 0)
            {
                throw new BuildFailedException("CSVis build blocked:\n - " + string.Join("\n - ", errors));
            }
        }

        [MenuItem("CSVis/Validate Project")]
        public static void ValidateFromMenu()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            Validate(errors, warnings);

            foreach (var warning in warnings)
            {
                Debug.LogWarning("[CSVis] " + warning);
            }

            foreach (var error in errors)
            {
                Debug.LogError("[CSVis] " + error);
            }

            if (errors.Count == 0)
            {
                Debug.Log("[CSVis] Project validation passed" + (warnings.Count > 0 ? " with " + warnings.Count + " warning(s)" : "."));
            }
        }

        public static void Validate(List<string> errors, List<string> warnings)
        {
            CheckLfsPointers(errors);
            CheckUiAssets(errors);
            CheckScenes(errors);
            CheckCertificate(errors, warnings);
            CheckXr(warnings);
            CheckMapbox(warnings);
        }

        private static void CheckLfsPointers(List<string> errors)
        {
            var pointers = new List<string>();
            var header = new byte[24];
            foreach (var path in Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories))
            {
                if (Array.IndexOf(BinaryExtensions, Path.GetExtension(path).ToLowerInvariant()) < 0)
                {
                    continue;
                }

                using (var stream = File.OpenRead(path))
                {
                    var read = stream.Read(header, 0, header.Length);
                    if (read >= 20 && System.Text.Encoding.ASCII.GetString(header, 0, 20) == "version https://git-")
                    {
                        pointers.Add(path);
                    }
                }
            }

            if (pointers.Count > 0)
            {
                errors.Add(pointers.Count + " files are Git LFS placeholders, not real files (e.g. " + pointers[0] +
                           "). Install Git LFS and run 'git lfs pull'.");
            }
        }

        private static void CheckUiAssets(List<string> errors)
        {
            var assets = AssetDatabase.LoadAssetAtPath<CSVisUIAssets>("Assets/Resources/CSVis/UIAssets.asset");
            if (assets == null)
            {
                errors.Add("UI assets are missing. Run the menu CSVis > Setup for HoloLens 2.");
            }
            else if (!assets.IsComplete(out var missing))
            {
                errors.Add("UI assets are incomplete (" + missing + "). Run the menu CSVis > Rebuild UI Assets.");
            }
        }

        private static void CheckScenes(List<string> errors)
        {
            var enabled = 0;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && File.Exists(scene.path))
                {
                    enabled++;
                }
            }

            if (enabled == 0)
            {
                errors.Add("No scene is enabled in the build. Run the menu CSVis > Setup for HoloLens 2.");
            }
        }

        private static void CheckCertificate(List<string> errors, List<string> warnings)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WSAPlayer)
            {
                return;
            }

            var path = PlayerSettings.WSA.certificatePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                errors.Add("No signing certificate. Player Settings > Publishing Settings > Certificate > Create...");
                return;
            }

            var notAfter = PlayerSettings.WSA.certificateNotAfter;
            if (notAfter.HasValue && notAfter.Value < DateTime.Now)
            {
                errors.Add("The UWP signing certificate expired on " + notAfter.Value.ToShortDateString() +
                           ". Player Settings > Publishing Settings > Certificate > Create... a new one.");
            }
        }

        private static void CheckXr(List<string> warnings)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WSAPlayer)
            {
                return;
            }

            var settings = XRGeneralSettingsForWsa();
            var hasOpenXr = false;
            if (settings != null && settings.Manager != null)
            {
                foreach (var loader in settings.Manager.activeLoaders)
                {
                    hasOpenXr |= loader is OpenXRLoader;
                }
            }

            if (!hasOpenXr)
            {
                warnings.Add("OpenXR is not enabled for Universal Windows Platform: the app would start without head tracking. " +
                             "Run CSVis > Setup for HoloLens 2 or tick OpenXR under Project Settings > XR Plug-in Management.");
            }
        }

        private static XRGeneralSettings XRGeneralSettingsForWsa()
        {
            return XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.WSA);
        }

        private static void CheckMapbox(List<string> warnings)
        {
            var config = Resources.Load<TextAsset>("Mapbox/MapboxConfiguration");
            if (config == null || !config.text.Contains("pk."))
            {
                warnings.Add("No Mapbox access token: the geographic map examples will be unavailable.");
            }
        }
    }
}
