using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace StructureBuild.Editor
{
    [InitializeOnLoad]
    internal static class StructureBuildEditorStartup
    {
        private const string MainScenePath = "Assets/Scenes/StructureBuild.unity";

        static StructureBuildEditorStartup()
        {
            EditorApplication.delayCall += OpenMainSceneWhenEditorIsEmpty;
        }

        private static void OpenMainSceneWhenEditorIsEmpty()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            bool hasEditableScene = activeScene.IsValid()
                && (!string.IsNullOrEmpty(activeScene.path) || activeScene.rootCount > 0);

            if (!hasEditableScene)
            {
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            }
        }
    }
}
