using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace StructureBuild.Editor
{
    public static class BuildCommands
    {
        [MenuItem("Structure Build/Build Android APK")]
        public static void BuildAndroid()
        {
            SetXRAutomatic(true);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            var report = Build(BuildTarget.Android, "Builds/结构构建-PICO.apk");
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Android build failed: " + report.summary.result);
        }

        // Unity's Android tooling rejects projects whose absolute path contains non-ASCII
        // characters. This staging entry point keeps the user-facing project path intact
        // while allowing an ASCII-path copy to produce the same APK.
        public static void BuildAndroidAsciiPath()
        {
            SetXRAutomatic(true);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            var report = Build(BuildTarget.Android, "Builds/StructureBuild-PICO.apk");
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Android build failed: " + report.summary.result);
        }

        [MenuItem("Structure Build/Build Desktop QA")]
        public static void BuildDesktop()
        {
            // This project stores its PICO loader settings under Android.
            // Do not flip those serialized Android settings off just because
            // a macOS QA build is requested: doing so made a later PICO APK
            // eligible to start as a non-XR window until another Android
            // build happened to repair it. The desktop bootstrap already
            // keeps the XR camera disabled outside Android.
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            var report = Build(BuildTarget.StandaloneOSX, "Builds/StructureBuild-QA.app");
            if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Desktop build failed: " + report.summary.result);
        }

        [MenuItem("Structure Build/Enable PICO XR for Android")]
        public static void EnablePicoXRForAndroid() => SetXRAutomatic(true);

        [MenuItem("Structure Build/Validate Campaign and Scene")]
        public static void ValidateCampaignAndScene()
        {
            for (var index = 0; index < CampaignData.Levels.Length; index++)
            {
                var level = CampaignData.Levels[index];
                var cells = new HashSet<Vector3Int>(level.referenceSolution.Select(cell => cell.ToVector3Int()));
                var result = ProjectionRules.Evaluate(cells, level);
                if (!result.complete)
                {
                    throw new System.Exception($"Level {index + 1} reference solution is invalid: front {result.front.matched}/{result.front.targetCount}, side {result.side.matched}/{result.side.targetCount}, extras {result.front.extras + result.side.extras}, budget {cells.Count}/{level.cubeLimit}");
                }
                Debug.Log($"Level {index + 1:00} OK: {level.displayName}, {cells.Count} cubes");
            }

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/StructureBuild.unity", OpenSceneMode.Single);
            var renderers = Object.FindObjectsByType<Renderer>();
            foreach (var renderer in renderers)
            {
                if (!renderer.name.EndsWith("_Editable")) continue;
                var bounds = renderer.bounds;
                Debug.Log($"Editable model {renderer.name}: size={bounds.size}, center={bounds.center}");
            }
            Debug.Log($"Scene validation OK: {scene.path}, roots={scene.rootCount}, renderers={renderers.Length}");
        }

        [MenuItem("Structure Build/Validate XR World-Space Package")]
        public static void ValidateXrWorldSpacePackage()
        {
            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            if (enabledScenes.Length < 2 ||
                enabledScenes[0].path != StructureBuildTitleSceneInstaller.TitleScenePath ||
                enabledScenes[1].path != "Assets/Scenes/StructureBuild.unity" ||
                enabledScenes[0].guid.ToString() != AssetDatabase.AssetPathToGUID(StructureBuildTitleSceneInstaller.TitleScenePath) ||
                enabledScenes[0].guid.ToString() == "00000000000000000000000000000000")
            {
                throw new System.Exception("Build Settings must contain the imported isolated title scene at index 0 and gameplay at index 1.");
            }
            StructureBuildTitleSceneInstaller.ValidateTitleScene();
            StructureBuildCurrentSceneInstaller.ValidateInstalledCurrentScene();
            Debug.Log("STRUCTURE_XR_PACKAGE_VALIDATE_OK: valid-GUID isolated title stage at build index 0 and physical world-space gameplay stage at index 1 both passed validation.");
        }

        private static BuildReport Build(BuildTarget target, string outputPath)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new System.Exception("No enabled scenes in Build Settings.");
            if (File.Exists(outputPath) || Directory.Exists(outputPath)) FileUtil.DeleteFileOrDirectory(outputPath);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.StrictMode
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Build result: {report.summary.result}, size: {report.summary.totalSize} bytes, output: {outputPath}");
            return report;
        }

        private static XRGeneralSettingsPerBuildTarget LoadXRSettings()
        {
            var guid = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget").FirstOrDefault();
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void SetXRAutomatic(bool enabled)
        {
            var manager = LoadXRSettings()?.SettingsForBuildTarget(BuildTargetGroup.Android)?.Manager;
            if (manager == null) return;
            manager.automaticLoading = enabled;
            manager.automaticRunning = enabled;
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }
    }
}
