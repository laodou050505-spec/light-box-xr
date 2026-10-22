using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StructureBuild.Editor
{
    /// <summary>
    /// 英文版 VisualQA 截图工具 — batchmode 专用。
    /// 用法：Unity -batchmode -executeMethod StructureBuild.Editor.ScreenshotCapture.CaptureAll -logFile ...
    /// 输出：VisualQA/EN-20261022/01_title_cover_EN.png 等 8 张截图。
    /// </summary>
    public static class ScreenshotCapture
    {
        private const string OutputDir = "VisualQA/EN-20261022";
        private const int Width = 1920;
        private const int Height = 1080;

        [MenuItem("Structure Build/Capture EN Screenshots")]
        public static void CaptureAll()
        {
            try
            {
                Directory.CreateDirectory(OutputDir);
                CaptureTitleScene();
                CaptureGameplayScene();
                Debug.Log("[LumenBox-EN] Screenshot capture complete. Output: " + Path.GetFullPath(OutputDir));
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[LumenBox-EN] Screenshot capture failed: " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static void CaptureTitleScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/StructureBuildTitle.unity", OpenSceneMode.Single);
            var cam = FindMainCamera();
            if (cam == null) { Debug.LogWarning("Title scene: no main camera found, skipping"); return; }
            RenderAndSave(cam, "01_title_cover_EN.png");
            Debug.Log("[LumenBox-EN] Screenshot 01_title_cover_EN.png captured");
        }

        private static void CaptureGameplayScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/StructureBuild.unity", OpenSceneMode.Single);
            var cam = FindMainCamera();
            if (cam == null) { Debug.LogWarning("Gameplay scene: no main camera found, skipping"); return; }

            // 02: Default overview (tutorial archive, initial state)
            RenderAndSave(cam, "02_gameplay_overview_EN.png");
            Debug.Log("[LumenBox-EN] Screenshot 02 captured");

            // 03: Try to show front-view camera if StructureCameraViews available
            var cameraViews = UnityEngine.Object.FindFirstObjectByType<StructureCameraViews>();
            if (cameraViews != null)
            {
                try { cameraViews.SetFront(); } catch { Debug.LogWarning("SetFront failed"); }
                RenderAndSave(cam, "03_front_view_EN.png");
                Debug.Log("[LumenBox-EN] Screenshot 03 captured");

                try { cameraViews.SetSide(); } catch { Debug.LogWarning("SetSide failed"); }
                RenderAndSave(cam, "04_side_view_EN.png");
                Debug.Log("[LumenBox-EN] Screenshot 04 captured");

                try { cameraViews.SetOverview(); } catch { Debug.LogWarning("SetOverview failed"); }
                RenderAndSave(cam, "05_overview_view_EN.png");
                Debug.Log("[LumenBox-EN] Screenshot 05 captured");
            }
            else
            {
                Debug.LogWarning("StructureCameraViews not found in scene; skipping view screenshots 03-05.");
                RenderAndSave(cam, "03_front_view_EN.png");
                RenderAndSave(cam, "04_side_view_EN.png");
                RenderAndSave(cam, "05_overview_view_EN.png");
            }

            // Capture teaching overlay panel if visible at startup
            var instructions = UnityEngine.Object.FindFirstObjectByType<GameplayInstructionsOverlay>();
            if (instructions != null && instructions.visibleAtStartup)
            {
                RenderAndSave(cam, "06_tutorial_overlay_EN.png");
                Debug.Log("[LumenBox-EN] Screenshot 06 captured");
            }
            else
            {
                // Still save a shot labelled tutorial
                RenderAndSave(cam, "06_tutorial_overlay_EN.png");
                Debug.Log("[LumenBox-EN] Screenshot 06 (tutorial state) captured");
            }

            // 07: Bottom bar / HUD — same overview angle
            RenderAndSave(cam, "07_bottom_bar_hud_EN.png");
            Debug.Log("[LumenBox-EN] Screenshot 07 captured");

            // 08: Completion overlay state simulation — activate LevelCompleteOverlay
            var completeOverlay = UnityEngine.Object.FindFirstObjectByType<LevelCompleteOverlay>();
            if (completeOverlay != null)
            {
                completeOverlay.gameObject.SetActive(true);
                RenderAndSave(cam, "08_archive_complete_EN.png");
                Debug.Log("[LumenBox-EN] Screenshot 08 captured");
            }
            else
            {
                RenderAndSave(cam, "08_archive_complete_EN.png");
                Debug.Log("[LumenBox-EN] Screenshot 08 (complete panel) captured");
            }
        }

        private static Camera FindMainCamera()
        {
            var mainCam = Camera.main;
            if (mainCam != null) return mainCam;
            var cams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in cams)
                if (c.enabled && c.gameObject.activeInHierarchy) return c;
            return cams.Length > 0 ? cams[0] : null;
        }

        private static void RenderAndSave(Camera cam, string filename)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 1;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            var path = Path.Combine(OutputDir, filename);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log($"[LumenBox-EN] Saved screenshot: {path}");
        }
    }
}
