using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StructureBuild;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StructureBuild.Editor
{
    /// <summary>
    /// Desktop-only regression for the two-stage boot flow. It checks the
    /// running title first, then invokes START and proves that the Single-mode
    /// scene transition unloads TitleRoot before gameplay begins. The two
    /// camera captures are authored desktop/Game-View evidence, not emulator
    /// evidence.
    /// </summary>
    public static class StructureBuildTitleFlowSmokeTest
    {
        private const string TitleScenePath = "Assets/Scenes/StructureBuildTitle.unity";
        private const string GameplayScenePath = "Assets/Scenes/StructureBuild.unity";
        private const string StateKey = "StructureBuild.TitleFlowSmokeState";
        // Kept separate from the original XR packaging evidence so HUD layout
        // regressions can be reviewed without overwriting prior captures.
        private const string VisualDirectory = "VisualQA/UiLayout-20260821-Editor";

        private static int frame;
        private static bool waitingForGameplay;

        [MenuItem("Structure Build/Run Title To Gameplay Flow Smoke Test")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before running the title-flow smoke test.");

            StructureBuildTitleSceneInstaller.ValidateTitleScene();
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Require(scenes.Length >= 2 && scenes[0].path == TitleScenePath && scenes[1].path == GameplayScenePath,
                "Build Settings must launch the isolated title scene before StructureBuild gameplay.");
            var expectedTitleGuid = AssetDatabase.AssetPathToGUID(TitleScenePath);
            Require(scenes[0].guid.ToString() == expectedTitleGuid,
                $"Title Build Settings GUID mismatch. entry={scenes[0].guid}; asset={expectedTitleGuid}.");

            Directory.CreateDirectory(VisualDirectory);
            EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            SessionState.SetString(StateKey, "requested");
            frame = 0;
            waitingForGameplay = false;
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void InstallHook()
        {
            EditorApplication.playModeStateChanged -= HandlePlayMode;
            EditorApplication.playModeStateChanged += HandlePlayMode;
        }

        private static void HandlePlayMode(PlayModeStateChange state)
        {
            var requested = SessionState.GetString(StateKey, string.Empty);
            if (state == PlayModeStateChange.EnteredPlayMode && requested == "requested")
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode || (requested != "passed" && requested != "failed")) return;
            var passed = requested == "passed";
            SessionState.EraseString(StateKey);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void Tick()
        {
            try
            {
                frame++;
                if (!waitingForGameplay)
                {
                    if (frame < 3) return;
                    var title = UnityEngine.Object.FindAnyObjectByType<StructureTitleController>();
                    var cover = FindInActiveScene("TitleCover");
                    Require(SceneManager.GetActiveScene().path == TitleScenePath && title != null && cover != null && cover.activeInHierarchy,
                        "Running title stage is missing TitleRoot, TitleCover, or its isolated scene.");
                    Require(UnityEngine.Object.FindAnyObjectByType<StructureGameController>() == null &&
                            FindInActiveScene("PuzzleTable_Editable") == null && FindInActiveScene("GameplayPresentation_v2") == null,
                        "Gameplay was visible or active during the title stage.");
                    var titleAnchor = UnityEngine.Object.FindAnyObjectByType<DesignPlayerStart>();
                    var titleCamera = FindInActiveScene("TitleDesktopCamera")?.GetComponent<Camera>();
                    Require(titleAnchor != null && titleCamera != null &&
                            Vector3.Distance(titleCamera.transform.position, titleAnchor.DesignedEyePosition) < 0.02f,
                        "Title desktop camera is not derived from its title design start.");
                    Capture(titleCamera, Path.Combine(VisualDirectory, "01-title-isolated-desktop.png"));
                    Debug.Log($"STRUCTURE_TITLE_RUNTIME_ISOLATION_OK: title eye={titleAnchor.DesignedEyePosition:F2}; TitleCover is the only visible stage and no gameplay root is loaded.");
                    waitingForGameplay = true;
                    title.BeginGame();
                    return;
                }

                if (SceneManager.GetActiveScene().path != GameplayScenePath || frame < 7) return;
                var game = UnityEngine.Object.FindAnyObjectByType<StructureGameController>();
                var desktopCamera = FindInActiveScene("DesktopCamera")?.GetComponent<Camera>();
                var gameplayAnchor = UnityEngine.Object.FindAnyObjectByType<DesignPlayerStart>();
                Require(game != null && desktopCamera != null && gameplayAnchor != null,
                    "Gameplay scene failed to initialize after START.");
                Require(FindInActiveScene("TitleRoot") == null && FindInActiveScene("TitleCover") == null &&
                        UnityEngine.Object.FindObjectsByType<StructureTitleController>(FindObjectsInactive.Include).Length == 0,
                    "TitleRoot or title controller remained after loading gameplay in Single mode.");
                Require(FindInActiveScene("PuzzleTable_Editable") != null && FindInActiveScene("PicoWorldHUD") != null &&
                        FindInActiveScene("GameplayInstructions_Pico") != null,
                    "The physical tabletop or world-space gameplay UI is missing after START.");
                gameplayAnchor.ApplyDesktopPose(desktopCamera.GetComponent<DesktopCameraOrbit>());
                Require(Vector3.Distance(desktopCamera.transform.position, gameplayAnchor.DesignedEyePosition) < 0.02f,
                    "Gameplay desktop camera is not derived from its gameplay design start.");
                Capture(desktopCamera, Path.Combine(VisualDirectory, "02-gameplay-worldspace-desktop.png"));
                Debug.Log($"STRUCTURE_TITLE_FLOW_OK: START unloaded TitleRoot; physical gameplay table loaded at eye={gameplayAnchor.DesignedEyePosition:F2}; title and gameplay desktop starts use their own authoritative stage anchors.");
                PassAndExit();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SessionState.SetString(StateKey, "failed");
                StopAndExit();
            }
        }

        private static GameObject FindInActiveScene(string name)
        {
            var active = SceneManager.GetActiveScene();
            return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .FirstOrDefault(item => item.name == name && item.gameObject.scene == active)?.gameObject;
        }

        private static void Capture(Camera camera, string path)
        {
            const int width = 1920;
            const int height = 1080;
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            // Unity 6000.5 on this macOS runner can crash in the native
            // billboard-particle path when Camera.Render is called from a
            // headless editor smoke test. The particle systems are only the
            // decorative star backdrop, so temporarily suppress their
            // renderers for this deterministic HUD/layout capture. We also
            // suppress meteor trail lines for the same editor-only crash
            // path. They are restored immediately and remain enabled in the
            // actual player.
            var particleRenderers = UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsInactive.Exclude);
            var rendererStates = new List<(ParticleSystemRenderer renderer, bool enabled)>();
            var trailRenderers = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude);
            var trailStates = new List<(LineRenderer renderer, bool enabled)>();
            try
            {
                foreach (var particleRenderer in particleRenderers)
                {
                    if (particleRenderer == null) continue;
                    rendererStates.Add((particleRenderer, particleRenderer.enabled));
                    particleRenderer.enabled = false;
                }
                foreach (var trailRenderer in trailRenderers)
                {
                    if (trailRenderer == null) continue;
                    trailStates.Add((trailRenderer, trailRenderer.enabled));
                    trailRenderer.enabled = false;
                }
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                foreach (var state in rendererStates)
                    if (state.renderer != null) state.renderer.enabled = state.enabled;
                foreach (var state in trailStates)
                    if (state.renderer != null) state.renderer.enabled = state.enabled;
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(image);
            }
        }

        private static void PassAndExit()
        {
            SessionState.SetString(StateKey, "passed");
            StopAndExit();
        }

        private static void StopAndExit()
        {
            EditorApplication.update -= Tick;
            EditorApplication.ExitPlaymode();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
