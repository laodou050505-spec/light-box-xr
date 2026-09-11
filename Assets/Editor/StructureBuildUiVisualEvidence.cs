#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StructureBuild;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StructureBuild.Editor
{
    /// <summary>
    /// Produces deterministic editor-rendered evidence for Android-only PICO
    /// canvases. It never changes or saves the authored scene: the active XR
    /// camera pose and visibility are previewed in memory, captured, and then
    /// Play Mode is stopped.
    /// </summary>
    public static class StructureBuildUiVisualEvidence
    {
        private const string ScenePath = "Assets/Scenes/StructureBuild.unity";
        private const string StateKey = "StructureBuild.UiVisualEvidence";
        private const string OutputDirectory = "VisualQA/DesignedPanels-20260907/Final";
        private const string StartupLayoutKey = StateKey + ".StartupLayout";
        private const string StartupOutputDirectory = "VisualQA/StartupLayout-20260909/Preview";
        private static int frame;
        private static int stage;
        private static readonly List<CaptureRecord> captureRecords = new List<CaptureRecord>();
        private static bool IsStartupLayout => SessionState.GetBool(StartupLayoutKey, false);
        private static string ActiveOutputDirectory => IsStartupLayout ? StartupOutputDirectory : OutputDirectory;

        [MenuItem("Structure Build/Capture Designed PICO UI Evidence")]
        public static void Run()
        {
            BeginRun(false);
        }

        [MenuItem("Structure Build/Capture Startup Layout Reference Evidence")]
        public static void RunStartupLayout()
        {
            BeginRun(true);
        }

        private static void BeginRun(bool startupLayout)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before capturing PICO UI evidence.");
            SessionState.SetBool(StartupLayoutKey, startupLayout);
            Directory.CreateDirectory(ActiveOutputDirectory);
            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetString(StateKey, "requested");
            frame = 0;
            stage = 0;
            captureRecords.Clear();
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
            var status = SessionState.GetString(StateKey, string.Empty);
            if (state == PlayModeStateChange.EnteredPlayMode && status == "requested")
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
                return;
            }
            if (state != PlayModeStateChange.EnteredEditMode || (status != "passed" && status != "failed")) return;
            var passed = status == "passed";
            SessionState.EraseString(StateKey);
            SessionState.EraseBool(StartupLayoutKey);
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
        }

        private static void Tick()
        {
            try
            {
                frame++;
                if (frame < 4) return;
                var desktopCamera = GameObject.Find("DesktopCamera")?.GetComponent<Camera>();
                var designStart = UnityEngine.Object.FindAnyObjectByType<DesignPlayerStart>();
                var game = UnityEngine.Object.FindAnyObjectByType<StructureGameController>();
                var instructions = UnityEngine.Object.FindAnyObjectByType<GameplayInstructionsOverlay>();
                if (desktopCamera == null || designStart == null || game == null || instructions == null)
                    throw new InvalidOperationException("Gameplay camera, design start, game, or instructions are missing.");

                if (stage == 0) instructions.Show();
                designStart.ApplyDesktopPose(desktopCamera.GetComponent<DesktopCameraOrbit>());
                ForcePicoPreview(desktopCamera);

                if (stage == 0)
                {
                    Capture(desktopCamera, Path.Combine(ActiveOutputDirectory, "02-pico-tutorial-layout.png"));
                    instructions.Dismiss();
                    stage = 1;
                    frame = 0;
                    return;
                }
                if (stage == 1)
                {
                    Capture(desktopCamera, Path.Combine(ActiveOutputDirectory, "03-pico-hud-layout.png"));
                    for (var index = 0; index < 32 && !game.IsLevelCompleted; index++) game.Hint();
                    if (!game.IsLevelCompleted) throw new InvalidOperationException("Tutorial level did not complete for completion-card evidence.");
                    stage = 2;
                    frame = 0;
                    return;
                }
                if (stage == 2)
                {
                    Capture(desktopCamera, Path.Combine(ActiveOutputDirectory, "04-pico-completion-layout.png"));
                    if (IsStartupLayout)
                    {
                        game.NextLevel();
                        if (game.CurrentLevelNumber != 2 || game.IsLevelCompleted)
                            throw new InvalidOperationException("Reference evidence did not advance to the actual second level.");
                        stage = 3;
                        frame = 0;
                        return;
                    }
                    Debug.Log("STRUCTURE_PICO_UI_VISUAL_OK: tutorial, aligned HUD/guide, action dock, and completion card captured from the authored gameplay start without changing scene models.");
                    SessionState.SetString(StateKey, "passed");
                    Stop();
                }
                if (stage == 3)
                {
                    Capture(desktopCamera, Path.Combine(ActiveOutputDirectory, "05-pico-level2-reference-layout.png"));
                    // The reference was captured through a tracked head, not
                    // the desktop look-at camera. Keep the actual authored-eye
                    // capture above, and label this separate approximate head
                    // pose explicitly; never persist it to the XR camera.
                    desktopCamera.transform.rotation = Quaternion.Euler(18f, designStart.yaw, 0f);
                    ForcePicoPreview(desktopCamera);
                    Capture(desktopCamera, Path.Combine(ActiveOutputDirectory, "06-approximate-reference-head-pose.png"));
                    Debug.Log("STRUCTURE_STARTUP_LAYOUT_VISUAL_OK: tutorial, HUD, completion, and actual level 2 captured at 1372x1374 with an approximate 80-degree PICO preview field of view.");
                    SessionState.SetString(StateKey, "passed");
                    Stop();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SessionState.SetString(StateKey, "failed");
                Stop();
            }
        }

        private static void ForcePicoPreview(Camera camera)
        {
            var instructions = UnityEngine.Object.FindAnyObjectByType<GameplayInstructionsOverlay>();
            var tutorialVisible = instructions != null && instructions.picoPanelRoot != null && instructions.picoPanelRoot.activeSelf;
            foreach (var hud in UnityEngine.Object.FindObjectsByType<StructureScreenHUD>(FindObjectsInactive.Include))
            {
                if (hud.canvas != null) hud.canvas.enabled = hud.showOnlyWhenXRActive && !tutorialVisible;
            }
            foreach (var visibility in UnityEngine.Object.FindObjectsByType<PicoRuntimeCanvasVisibility>(FindObjectsInactive.Include))
            {
                if (visibility.canvas != null) visibility.canvas.enabled = !tutorialVisible;
            }
            // Enable the tutorial before visiting followers: otherwise its
            // first capture retains the old authored canvas transform.
            if (instructions != null)
            {
                if (instructions.desktopCanvas != null) instructions.desktopCanvas.enabled = false;
                if (instructions.picoCanvas != null) instructions.picoCanvas.enabled = tutorialVisible;
            }
            foreach (var follower in UnityEngine.Object.FindObjectsByType<PicoHeadLockedCanvas>(FindObjectsInactive.Include))
            {
                var followerCanvas = follower.GetComponent<Canvas>();
                if (followerCanvas == null || !followerCanvas.enabled) continue;
                follower.targetCamera = camera;
                follower.transform.position = camera.transform.position + camera.transform.forward * follower.distance +
                                              camera.transform.right * follower.viewOffset.x + camera.transform.up * follower.viewOffset.y;
                follower.transform.rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
                follower.transform.localScale = Vector3.one * follower.worldScale;
            }
        }

        private static void Capture(Camera camera, string path)
        {
            var width = IsStartupLayout ? 1372 : 1920;
            var height = IsStartupLayout ? 1374 : 1080;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var oldFieldOfView = camera.fieldOfView;
            var oldAspect = camera.aspect;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var particleStates = UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsInactive.Exclude)
                .Select(renderer => (renderer, wasEnabled: renderer.enabled)).ToList();
            var trailStates = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude)
                .Select(renderer => (renderer, wasEnabled: renderer.enabled)).ToList();
            try
            {
                foreach (var state in particleStates) state.renderer.enabled = false;
                foreach (var state in trailStates) state.renderer.enabled = false;
                if (IsStartupLayout)
                {
                    camera.fieldOfView = 80f;
                    camera.aspect = (float)width / height;
                }
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                WriteMetadata(camera, path, width, height);
            }
            finally
            {
                foreach (var state in particleStates) if (state.renderer != null) state.renderer.enabled = state.wasEnabled;
                foreach (var state in trailStates) if (state.renderer != null) state.renderer.enabled = state.wasEnabled;
                camera.targetTexture = oldTarget;
                if (IsStartupLayout)
                {
                    camera.fieldOfView = oldFieldOfView;
                    camera.aspect = oldAspect;
                }
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(image);
            }
        }

        private static void WriteMetadata(Camera camera, string path, int width, int height)
        {
            var start = UnityEngine.Object.FindAnyObjectByType<DesignPlayerStart>();
            var game = UnityEngine.Object.FindAnyObjectByType<StructureGameController>();
            captureRecords.Add(new CaptureRecord
            {
                image = Path.GetFileName(path),
                width = width,
                height = height,
                fieldOfView = camera.fieldOfView,
                aspect = camera.aspect,
                eyePosition = camera.transform.position,
                eyeEulerAngles = camera.transform.eulerAngles,
                focusPosition = start != null ? start.FocusPosition : Vector3.zero,
                viewingDistance = start != null ? start.viewingDistance : 0f,
                eyeHeight = start != null ? start.eyeHeight : 0f,
                level = game != null ? game.CurrentLevelNumber : 0,
                panels = UnityEngine.Object.FindObjectsByType<PicoHeadLockedCanvas>(FindObjectsInactive.Include)
                    .OrderBy(follower => follower.name)
                    .Select(follower => new PanelRecord
                    {
                        name = follower.name,
                        distance = follower.distance,
                        viewOffset = follower.viewOffset,
                        worldScale = follower.worldScale,
                        canvasEnabled = follower.GetComponent<Canvas>()?.enabled == true,
                        active = follower.gameObject.activeInHierarchy,
                        worldPosition = follower.transform.position
                    }).ToArray()
            });
            File.WriteAllText(Path.Combine(ActiveOutputDirectory, "metadata.json"), JsonUtility.ToJson(new EvidenceRecord
            {
                capturedAtUtc = DateTime.UtcNow.ToString("O"),
                profile = IsStartupLayout ? "StartupLayout-20260909" : "DesignedPanels",
                note = IsStartupLayout ? "Editor preview with approximate 80-degree FOV; not a measured headset projection. Image 06 uses an estimated 18-degree tracked-head pitch, while images 02-05 retain the derived desktop look-at pitch. Neither changes the saved scene or live XR tracking." : "Original 1920x1080 editor evidence profile.",
                captures = captureRecords.ToArray()
            }, true));
        }

        [Serializable]
        private sealed class EvidenceRecord
        {
            public string capturedAtUtc;
            public string profile;
            public string note;
            public CaptureRecord[] captures;
        }

        [Serializable]
        private sealed class CaptureRecord
        {
            public string image;
            public int width;
            public int height;
            public float fieldOfView;
            public float aspect;
            public Vector3 eyePosition;
            public Vector3 eyeEulerAngles;
            public Vector3 focusPosition;
            public float viewingDistance;
            public float eyeHeight;
            public int level;
            public PanelRecord[] panels;
        }

        [Serializable]
        private sealed class PanelRecord
        {
            public string name;
            public float distance;
            public Vector2 viewOffset;
            public float worldScale;
            public bool canvasEnabled;
            public bool active;
            public Vector3 worldPosition;
        }

        private static void Stop()
        {
            EditorApplication.update -= Tick;
            EditorApplication.ExitPlaymode();
        }
    }
}
#endif
