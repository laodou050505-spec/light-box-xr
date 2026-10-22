// LUMEN BOX EN — VisualQA Play Mode 自动截图工具
// 必须在 Play Mode 内运行（投影 RenderTexture 才会初始化）
// 用法（Unity Editor 菜单）：Structure Build / Capture EN Screenshots (Play Mode)
// 输出：VisualQA/EN-20261022/ 下 10 张截图
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StructureBuild.Editor
{
    [InitializeOnLoad]
    public static class ScreenshotCapture
    {
        private const string OutputDir = "VisualQA/EN-20261022";
        private const int W = 1920;
        private const int H = 1080;

        private static bool _pending    = false;
        private static int  _frameCount = 0;

        static ScreenshotCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeState;
        }

        // ── 菜单入口 ──────────────────────────────────────────────────────────

        [MenuItem("Structure Build/Capture EN Screenshots (Play Mode)")]
        public static void StartCapture()
        {
            if (!EditorSceneManager.GetActiveScene().name.Contains("StructureBuild"))
                EditorSceneManager.OpenScene("Assets/Scenes/StructureBuild.unity",
                    OpenSceneMode.Single);

            Directory.CreateDirectory(OutputDir);
            _pending    = true;
            _frameCount = 0;
            EditorApplication.EnterPlaymode();
            Debug.Log("[LumenBox-EN] EnterPlaymode triggered for screenshots.");
        }

        // ── Play mode 回调 ────────────────────────────────────────────────────

        private static void OnPlayModeState(PlayModeStateChange state)
        {
            if (!_pending) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Debug.Log("[LumenBox-EN] In play mode. Waiting 80 frames for game init…");
                EditorApplication.update += WaitAndCapture;
            }
        }

        private static void WaitAndCapture()
        {
            _frameCount++;
            if (_frameCount < 80) return;

            EditorApplication.update -= WaitAndCapture;
            _pending = false;

            try   { RunAllCaptures(); }
            catch (Exception ex) { Debug.LogError("[LumenBox-EN] Screenshot error: " + ex); }
            finally { EditorApplication.ExitPlaymode(); }
        }

        // ── 截图主流程（在 Play Mode 内执行）────────────────────────────────

        private static void RunAllCaptures()
        {
            var cam = FindMainCamera();
            if (cam == null) { Debug.LogError("[LumenBox-EN] No camera in play mode!"); return; }

            var ctrl = UnityEngine.Object.FindFirstObjectByType<StructureGameController>(
                FindObjectsInactive.Exclude);

            // ── 01: 第 1 关初始状态（投影网格 + 橙色目标轮廓） ──────────────
            if (ctrl != null) ctrl.LoadLevel(0);   // archive-001
            PauseFrames();
            Shoot(cam, "01_initial_projection_grid_EN.png");

            // ── 02: 放入第 1 块方块（橙色格子被填充） ──────────────────────
            if (ctrl != null) ctrl.TryPlaceAt(new Vector3Int(0, 0, 0));
            PauseFrames();
            Shoot(cam, "02_one_block_placed_EN.png");

            // ── 03: 放入第 2 块方块（投影变青绿，完成） ─────────────────────
            if (ctrl != null) ctrl.TryPlaceAt(new Vector3Int(1, 0, 1));
            PauseFrames();
            Shoot(cam, "03_two_blocks_teal_complete_EN.png");

            // ── 04: 显示 Archive Complete 面板 ──────────────────────────────
            // 完成后游戏自动显示完成面板（此时面板刚出现）
            PauseFrames();
            Shoot(cam, "04_archive_complete_panel_EN.png");

            // ── 05: Reset 后（回到第 1 关初始状态） ─────────────────────────
            if (ctrl != null) ctrl.ResetCurrentLevel();
            PauseFrames();
            Shoot(cam, "05_after_reset_EN.png");

            // ── 06: Hint 状态 ────────────────────────────────────────────────
            if (ctrl != null) ctrl.Hint();
            PauseFrames();
            Shoot(cam, "06_hint_placed_EN.png");

            // ── 07: 预算用完提示 ─────────────────────────────────────────────
            // 第 1 关 budget=2，先 Hint（消耗 0，hint 不消耗），
            // 再放 2 块让格满，再尝试放第 3 块触发提示
            if (ctrl != null)
            {
                ctrl.ResetCurrentLevel();
                PauseFrames();
                ctrl.TryPlaceAt(new Vector3Int(0, 0, 0));
                ctrl.TryPlaceAt(new Vector3Int(1, 0, 1));
                // 再尝试放一块（会超出 budget）
                ctrl.TryPlaceAt(new Vector3Int(2, 0, 0));
            }
            PauseFrames();
            Shoot(cam, "07_budget_exceeded_EN.png");

            // ── 08: Undo ─────────────────────────────────────────────────────
            if (ctrl != null)
            {
                ctrl.ResetCurrentLevel();
                PauseFrames();
                ctrl.TryPlaceAt(new Vector3Int(0, 0, 0));
                ctrl.TryPlaceAt(new Vector3Int(1, 0, 1));
                ctrl.Undo();
            }
            PauseFrames();
            Shoot(cam, "08_after_undo_EN.png");

            // ── 09: All Archives Complete（跳到第 12 关并完成）──────────────
            if (ctrl != null)
            {
                ctrl.LoadLevel(11);   // archive-012（index 11）
                PauseFrames();
                // 第 12 关 archive-012 需要 18 块，尝试放满所有方块
                var cells12 = new[]
                {
                    new Vector3Int(0,0,0),new Vector3Int(0,1,0),new Vector3Int(0,0,1),new Vector3Int(0,1,1),
                    new Vector3Int(0,2,1),new Vector3Int(1,0,1),new Vector3Int(1,1,1),new Vector3Int(1,2,1),
                    new Vector3Int(1,0,2),new Vector3Int(1,1,2),new Vector3Int(2,0,1),new Vector3Int(2,1,1),
                    new Vector3Int(2,2,1),new Vector3Int(2,0,3),new Vector3Int(2,1,3),new Vector3Int(3,0,1),
                    new Vector3Int(3,1,1),new Vector3Int(3,2,1)
                };
                foreach (var c in cells12) ctrl.TryPlaceAt(c);
                PauseFrames();
            }
            Shoot(cam, "09_all_archives_complete_EN.png");

            // ── 10: 教学卡标题 "LUMEN BOX — Tutorial" ───────────────────────
            // 回到第 1 关，暴露教学 overlay
            if (ctrl != null)
            {
                ctrl.LoadLevel(0);
                PauseFrames();
            }
            // 找到教学 overlay GameObject 并激活
            var overlayGo = GameObject.Find("InstructionsOverlay");
            if (overlayGo == null) overlayGo = GameObject.Find("TeachingOverlay");
            if (overlayGo != null) overlayGo.SetActive(true);
            PauseFrames();
            Shoot(cam, "10_tutorial_overlay_card_EN.png");

            Debug.Log("[LumenBox-EN] 10 screenshots done. Dir: " + Path.GetFullPath(OutputDir));
        }

        // ── 工具函数 ─────────────────────────────────────────────────────────

        private static void PauseFrames()
        {
            // 在 EditorApplication.update 回调外无法真正等帧；
            // 强制 Canvas 更新确保 UI 处于最新状态
            UnityEngine.Canvas.ForceUpdateCanvases();
        }

        private static Camera FindMainCamera()
        {
            var m = Camera.main;
            if (m != null) return m;
            var cams = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in cams)
                if (c.enabled && c.gameObject.activeInHierarchy) return c;
            return cams.Length > 0 ? cams[0] : null;
        }

        private static void Shoot(Camera cam, string filename)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 2;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            var path = Path.Combine(OutputDir, filename);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[LumenBox-EN] Saved: " + path);
        }
    }
}
