using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CSVis.EditorTools
{
    /// <summary>
    /// Unity opens a brand-new project on an empty "Untitled" scene. Load the app's scene instead
    /// so the project doesn't look empty the first time it is opened.
    /// </summary>
    [InitializeOnLoad]
    public static class OpenMenuSceneOnLoad
    {
        private const string MenuScenePath = "Assets/Scenes/MenuScene.unity";

        static OpenMenuSceneOnLoad()
        {
            EditorApplication.delayCall += OpenIfEmpty;
        }

        private static void OpenIfEmpty()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode)
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) && !scene.isDirty && scene.rootCount <= 2)
            {
                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            }
        }
    }
}
