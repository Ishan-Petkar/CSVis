using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CSVis.EditorTools
{
    /// <summary>
    /// On Retina Macs the Game view renders at half resolution by default ("Low Resolution Aspect Ratios"),
    /// which makes the UI look blurry. Turn that off whenever a Game view is open.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewSharpness
    {
        static GameViewSharpness()
        {
            EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += _ => Apply();
        }

        private static void Apply()
        {
            var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var property = gameViewType?.GetProperty("lowResolutionForAspectRatios",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            foreach (var view in Resources.FindObjectsOfTypeAll(gameViewType))
            {
                try
                {
                    property.SetValue(view, false);
                }
                catch (Exception)
                {
                    // Purely cosmetic; never let it interfere with the editor
                }
            }
        }
    }
}
