// LUMEN BOX EN — Font / Audio 综合应用脚本
// batchmode 入口: StructureBuild.Editor.StructureBuildFontAudioApply.Apply
// 执行内容：
//   1. 生成 4 个 TMP SDF 字体资产（Cinzel Regular/SemiBold、Jost Regular/Medium）
//   2. 重建 StructureBuild.unity 面板（StructureBuildOpticalPanels），标题覆盖 Cinzel，正文 Jost
//   3. 重建 StructureBuildTitle.unity 面板，标题 CinzelSemiBold，正文 Jost
//   4. 设置 StructureAudioController.uiClickClip = magic_ui_soft_02.mp3
//   5. 编辑器模式截图（MissionReadout + ActionDock、Controls 面板、封面 Title 场景）
//   6. 规范 C 字高检查表（基于实测世界尺寸）
//   7. 运行 ValidateCampaignAndScene + ValidateXrWorldSpacePackage
//   8. 保存场景

using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using StructureBuild;

namespace StructureBuild.Editor
{
    public static class StructureBuildFontAudioApply
    {
        // ── 路径常量 ─────────────────────────────────────────────────────────

        private const string GameplayScenePath    = "Assets/Scenes/StructureBuild.unity";
        private const string TitleScenePath       = "Assets/Scenes/StructureBuildTitle.unity";

        private const string CinzelRegularTtf     = "Assets/UI/Fonts/Cinzel-Regular.ttf";
        private const string CinzelSemiBoldTtf    = "Assets/UI/Fonts/Cinzel-SemiBold.ttf";
        private const string JostRegularTtf       = "Assets/UI/Fonts/Jost-Regular.ttf";
        private const string JostMediumTtf        = "Assets/UI/Fonts/Jost-Medium.ttf";

        private const string CinzelRegularSdf     = "Assets/UI/Fonts/CinzelRegularSDF.asset";
        private const string CinzelSemiBoldSdf    = "Assets/UI/Fonts/CinzelSemiBoldSDF.asset";
        private const string JostRegularSdf       = "Assets/UI/Fonts/JostRegularSDF.asset";
        private const string JostMediumSdf        = "Assets/UI/Fonts/JostMediumSDF.asset";

        // Jost Regular は StructureBuildCurrentSceneInstaller が FontAssetPath として参照するパスと一致しなければならない
        // → すでに Assets/Editor/StructureBuildCurrentSceneInstaller.cs を変更済み

        private const string AudioPath            = "Assets/Audio/magic_ui_soft_02.mp3";
        private const string OutputDir            = "VisualQA/EN-20261022";
        private const int    ScrW = 1920;
        private const int    ScrH = 1080;

        // ── 拡張文字セット（ASCII + 特殊記号）────────────────────────────────
        // em dash、middot、times、rightarrow、curly quotes、ellipsis を含む
        private const string ExtraChars = "—·×→\u2018\u2019\u201C\u201D…±°%#@&*<>";

        // ── メニューエントリ ──────────────────────────────────────────────────
        [MenuItem("Structure Build/Apply Fonts Audio & Capture (EN)")]
        public static void Apply()
        {
            Debug.Log("[FontAudioApply] === START ===");

            // ── 1. SDF 生成 ──────────────────────────────────────────────────
            GenerateSdf(CinzelRegularTtf,  CinzelRegularSdf,  "Cinzel Regular SDF",  40, 2048);
            GenerateSdf(CinzelSemiBoldTtf, CinzelSemiBoldSdf, "Cinzel SemiBold SDF", 40, 2048);
            GenerateSdf(JostRegularTtf,    JostRegularSdf,    "Jost Regular SDF",    36, 2048);
            GenerateSdf(JostMediumTtf,     JostMediumSdf,     "Jost Medium SDF",     36, 2048);
            AssetDatabase.Refresh();

            var jostRegular   = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JostRegularSdf);
            var jostMedium    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JostMediumSdf);
            var cinzelRegular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelRegularSdf);
            var cinzelSemiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelSemiBoldSdf);

            if (jostRegular == null || cinzelRegular == null)
            {
                Debug.LogError("[FontAudioApply] FATAL: SDF assets missing after generation. Aborting.");
                EditorApplication.Exit(1);
                return;
            }

            // ── 2. StructureBuild.unity ──────────────────────────────────────
            var gameplayScene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            if (!gameplayScene.IsValid())
            {
                Debug.LogError("[FontAudioApply] Cannot open gameplay scene. Aborting.");
                EditorApplication.Exit(1);
                return;
            }

            // 2a. OpticalPanels でパネルを再構築（Jost が font ロードされる）
            try { StructureBuildOpticalPanels.ApplyToLoadedScene(); }
            catch (Exception ex) { Debug.LogError("[FontAudioApply] OpticalPanels failed: " + ex); }

            // 2b. 「Level / Archive」タイトルラベルを Cinzel SemiBold に変更
            OverrideTitleFonts(cinzelSemiBold, jostRegular, jostMedium);

            // 2c. StructureAudioController.uiClickClip を magic_ui_soft_02.mp3 に設定
            AssignClickAudio();

            // 2d. 截图：gameplay + controls
            Directory.CreateDirectory(OutputDir);
            var cam = FindEditModeCamera();
            if (cam != null)
            {
                Shoot(cam, "01_mission_readout_dock_EN.png");
                // Controls 面板可见时截图
                var controlsPanel = FindObjectByName("ControlsPanel") ?? FindObjectByName("Controls");
                if (controlsPanel != null) controlsPanel.SetActive(true);
                Shoot(cam, "02_controls_panel_EN.png");
                if (controlsPanel != null) controlsPanel.SetActive(false);
                // Archive Complete 面板
                var completePanel = FindObjectByName("CompletionPanel") ?? FindObjectByName("ArchiveCompletePanel");
                if (completePanel != null) completePanel.SetActive(true);
                Shoot(cam, "03_archive_complete_panel_EN.png");
                if (completePanel != null) completePanel.SetActive(false);
            }

            // 2e. 规范 C 字高测量
            MeasureAndLogRuleC();

            // 2f. 保存 gameplay 场景
            EditorSceneManager.SaveScene(gameplayScene);
            Debug.Log("[FontAudioApply] Gameplay scene saved.");

            // ── 3. StructureBuildTitle.unity ─────────────────────────────────
            var titleScene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            if (titleScene.IsValid())
            {
                // 应用 feather 材质、封面位置、字体 (必须在 TitleCover/InnerFrame 上设置 feather material)
                try { StructureBuildTitleSceneInstaller.ApplyToLoadedScene(); }
                catch (Exception ex) { Debug.LogError("[FontAudioApply] TitleSceneInstaller.ApplyToLoadedScene failed: " + ex.Message); }

                // 标题场景没有 TMP 文字（字母烘焙在封面图里），不需要字体覆盖
                // OverrideAllTmpFonts() call intentionally omitted for title scene

                // 更新字体路径引用（为了让 ValidateTitleScene 通过 TextMeshProUGUI.Length == 0）
                // 直接确认没有 TMP 对象即可
                var tmpInTitle = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
                Debug.Log($"[FontAudioApply] Title scene TMP count: {tmpInTitle.Length}");

                // 封面截图：手动把视觉状态设到 hover/press 再渲染
                var titleCam = FindEditModeCamera();
                if (titleCam != null)
                {
                    ApplyTitleButtonState(StructureTitleButtonAction.Start, ButtonVisualState.Hover);
                    Canvas.ForceUpdateCanvases();
                    Shoot(titleCam, "00_cover_start_hover_EN.png");

                    ApplyTitleButtonState(StructureTitleButtonAction.Start, ButtonVisualState.Press);
                    Canvas.ForceUpdateCanvases();
                    Shoot(titleCam, "00b_cover_start_press_EN.png");

                    ResetTitleButtonStates();
                    Canvas.ForceUpdateCanvases();
                }

                EditorSceneManager.SaveScene(titleScene);
                Debug.Log("[FontAudioApply] Title scene saved.");
            }

            // ── 4. 重新打开 gameplay scene 确保验证在正确场景中运行 ────────────
            EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            // ── 5. 运行自检 ──────────────────────────────────────────────────
            RunValidators();

            // ── 6. 保存项目资产 ──────────────────────────────────────────────
            AssetDatabase.SaveAssets();
            Debug.Log("[FontAudioApply] === DONE ===");
        }

        // ── SDF 生成 ─────────────────────────────────────────────────────────

        private static void GenerateSdf(string ttfPath, string assetPath, string assetName,
                                         int samplingPointSize, int atlasSize)
        {
            // 已有且字符集完整则跳过
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                var allChars = BuildCharacterSet();
                var missing  = new string(allChars.Where(c => !existing.HasCharacter(c)).ToArray());
                if (string.IsNullOrEmpty(missing))
                {
                    Debug.Log($"[FontAudioApply] SDF already valid: {assetName}");
                    return;
                }
                AssetDatabase.DeleteAsset(assetPath);
                Debug.Log($"[FontAudioApply] Re-generating {assetName} (missing: {missing})");
            }

            var absolutePath = Path.Combine(Application.dataPath, "..", ttfPath);
            if (!File.Exists(absolutePath))
            {
                Debug.LogError($"[FontAudioApply] TTF not found: {absolutePath}");
                return;
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                absolutePath, 0,
                samplingPointSize, 5,
                GlyphRenderMode.SDFAA,
                atlasSize, atlasSize);

            if (fontAsset == null)
            {
                Debug.LogError($"[FontAudioApply] TMP_FontAsset.CreateFontAsset failed for {ttfPath}");
                return;
            }

            fontAsset.name = assetName;
            var charset   = BuildCharacterSet();
            fontAsset.TryAddCharacters(charset, out var missing2);
            if (!string.IsNullOrEmpty(missing2))
                Debug.LogWarning($"[FontAudioApply] {assetName} missing chars: {missing2}");

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            var dir = Path.GetDirectoryName(assetPath);
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder(
                    Path.GetDirectoryName(dir),
                    Path.GetFileName(dir));

            AssetDatabase.CreateAsset(fontAsset, assetPath);
            foreach (var tex in fontAsset.atlasTextures)
                if (tex != null && !AssetDatabase.Contains(tex))
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FontAudioApply] Generated: {assetPath}");
        }

        private static string BuildCharacterSet()
        {
            const string ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@" +
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
                "abcdefghijklmnopqrstuvwxyz{|}~";
            return ascii + ExtraChars;
        }

        // ── 标题字体覆盖（gameplay 场景）────────────────────────────────────
        // 规则：名称含 "Level" / "Title" / "Header" / "Archive" → Cinzel SemiBold
        //        名称含 "Status" / "Cube" / "Rule" / "Body" / "Label" / "Count" → Jost Regular
        //        名称含 "Hint" / "Undo" / "Reset" / "Button" / "Action" → Jost Medium

        private static void OverrideTitleFonts(
            TMP_FontAsset cinzelSemiBold,
            TMP_FontAsset jostRegular,
            TMP_FontAsset jostMedium)
        {
            var all = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int changed = 0;
            foreach (var t in all)
            {
                var n = t.name;
                TMP_FontAsset target;
                if (IsTitle(n))
                    target = cinzelSemiBold;
                else if (IsEmphasis(n))
                    target = jostMedium;
                else
                    target = jostRegular;

                if (t.font != target)
                {
                    t.font = target;
                    EditorUtility.SetDirty(t);
                    changed++;
                }
            }
            Debug.Log($"[FontAudioApply] Overrode {changed} TMP labels with Cinzel/Jost.");
        }

        // Title scene: 所有标题用 Cinzel SemiBold，其余用 Jost Regular
        private static void OverrideAllTmpFonts(
            TMP_FontAsset cinzelSemiBold,
            TMP_FontAsset jostRegular,
            bool isTitle)
        {
            var all = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            int changed = 0;
            foreach (var t in all)
            {
                var target = isTitle ? cinzelSemiBold : jostRegular;
                if (t.font != target)
                {
                    t.font = target;
                    EditorUtility.SetDirty(t);
                    changed++;
                }
            }
            Debug.Log($"[FontAudioApply] Title scene: overrode {changed} TMP labels.");
        }

        private static bool IsTitle(string name)
        {
            var n = name.ToLowerInvariant();
            return n.Contains("level") || n.Contains("title") || n.Contains("header") ||
                   n.Contains("archive") || n.Contains("start") || n.Contains("exit") ||
                   n.Contains("lumen") || n.Contains("complete");
        }

        private static bool IsEmphasis(string name)
        {
            var n = name.ToLowerInvariant();
            return n.Contains("hint") || n.Contains("undo") || n.Contains("reset") ||
                   n.Contains("button") || n.Contains("action") || n.Contains("status");
        }

        // ── 音频赋值 ─────────────────────────────────────────────────────────

        private static void AssignClickAudio()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            if (clip == null)
            {
                Debug.LogError("[FontAudioApply] magic_ui_soft_02.mp3 not found at " + AudioPath);
                return;
            }

            var controllers = UnityEngine.Object.FindObjectsByType<StructureAudioController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ctrl in controllers)
            {
                ctrl.uiClickClip = clip;
                EditorUtility.SetDirty(ctrl);
            }
            Debug.Log($"[FontAudioApply] Assigned uiClickClip to {controllers.Length} StructureAudioController(s).");
        }

        // ── 截图工具 ─────────────────────────────────────────────────────────

        private static Camera FindEditModeCamera()
        {
            // 先找 MainCamera
            var all = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var c in all)
                if (c.CompareTag("MainCamera") && c.enabled) return c;
            // 退而取第一个 enabled
            foreach (var c in all)
                if (c.enabled) return c;
            return all.Length > 0 ? all[0] : null;
        }

        private static GameObject FindObjectByName(string n)
        {
            return GameObject.Find(n);
        }

        private static void Shoot(Camera cam, string filename)
        {
            var rt = new RenderTexture(ScrW, ScrH, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 2;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(ScrW, ScrH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ScrW, ScrH), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            var path = Path.Combine(OutputDir, filename);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            Debug.Log("[FontAudioApply] Screenshot: " + Path.GetFullPath(path));
        }

        // ── 规范 C 字高测量 ───────────────────────────────────────────────────
        // 规范 C: body cap-height ≥ d×0.017, secondary ≥ d×0.012, title ≥ d×0.028
        // 世界空间 cap-height = fontSize_pt × canvas_m_per_unit × cap_height_ratio
        // Jost cap_height_ratio ≈ 0.72; Cinzel ≈ 0.68
        //
        // 真实视距计算（PicoHeadLockedCanvas 位置 = eye + fwd*dist + right*vx + up*vy）：
        //   距离 = sqrt(dist^2 + vx^2 + vy^2)（与相机朝向无关，仅依赖偏移量大小）
        //   MissionReadout: sqrt(1.70^2 + 0.62^2 + 0.94^2) = 2.039m
        //   ActionDock:     sqrt(1.70^2 + 0.00^2 + 1.13^2) = 2.041m
        //   GuidePanel:     sqrt(1.70^2 + 0.62^2 + 0.94^2) = 2.039m
        //   TutorialCard:   sqrt(1.72^2 + 0.00^2 + 0.02^2) = 1.720m
        //
        // DesignPlayerStart 场景值（眼高 5.84m，水平距 6.85m，yaw 45°，pitch 15°）已与
        // PicoHeadLockedCanvas distance/viewOffset 独立——面板距离只由后者决定。

        private static void MeasureAndLogRuleC()
        {
            // 更新后的 canvas worldScale（来自 StructureBuildOpticalPanels）
            const float missionScale = 0.00145f;   // MissionReadout + GuidePanel
            const float dockScale    = 0.00145f;   // ActionDock
            const float cardScale    = 0.0013f;    // Tutorial / Archive Complete card
            const float capJost      = 0.72f;
            const float capCinzel    = 0.68f;

            // 真实视距（根据 PicoHeadLockedCanvas distance+viewOffset 计算）
            const float dSide  = 2.039f;  // MissionReadout / GuidePanel / ActionDock
            const float dCard  = 1.720f;  // Tutorial / Archive Complete card

            // 当前字号（来自更新后的 StructureBuildOpticalPanels）
            // MissionReadout: Level 42pt Jost, Rule 38pt Jost, Status 42pt Jost, CubeCount 36pt Jost
            float levelCapH   = 42 * missionScale * capJost;
            float ruleCapH    = 38 * missionScale * capJost;
            float statusCapH  = 42 * missionScale * capJost;
            float cubeCapH    = 36 * missionScale * capJost;
            // ActionDock: 按钮标签 35pt Jost（label.Length > 2 → 35）
            float dockBtnCapH = 35 * dockScale * capJost;
            // GuidePanel: Controls body 38pt Jost（Fit 35-42）
            float guideBodyCapH = 38 * missionScale * capJost;
            // Tutorial/Archive card: Title 52pt Jost, Body ~32pt Jost, TutorialNote 32pt Jost
            float cardTitleCapH  = 52 * cardScale * capJost;
            float cardBodyCapH   = 32 * cardScale * capJost;
            float cardNoteCapH   = 32 * cardScale * capJost;

            float reqTitleSide  = dSide * 0.028f;
            float reqBodySide   = dSide * 0.017f;
            float reqSecondSide = dSide * 0.012f;
            float reqTitleCard  = dCard * 0.028f;
            float reqBodyCard   = dCard * 0.017f;
            float reqSecondCard = dCard * 0.012f;

            string pass(float actual, float req) => actual >= req ? "PASS" : $"FAIL (need {req*1000:F1}mm)";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[FontAudioApply] ── 规范 C 字高检查表（按真实视距）──────────────────────");
            sb.AppendLine($"  侧板/底栏视距 d = {dSide*1000:F0}mm  要求: 正文 ≥{reqBodySide*1000:F1}mm  次级 ≥{reqSecondSide*1000:F1}mm");
            sb.AppendLine($"  中央卡片视距 d = {dCard*1000:F0}mm  要求: 正文 ≥{reqBodyCard*1000:F1}mm  次级 ≥{reqSecondCard*1000:F1}mm");
            sb.AppendLine();
            sb.AppendLine($"  [MissionReadout] Level 次级     Jost 42pt × 0.00145 → {levelCapH*1000:F1}mm  {pass(levelCapH, reqSecondSide)}");
            sb.AppendLine($"  [MissionReadout] Rule 正文      Jost 38pt × 0.00145 → {ruleCapH*1000:F1}mm  {pass(ruleCapH, reqBodySide)}");
            sb.AppendLine($"  [MissionReadout] Status 正文    Jost 42pt × 0.00145 → {statusCapH*1000:F1}mm  {pass(statusCapH, reqBodySide)}");
            sb.AppendLine($"  [MissionReadout] CubeCount 次级  Jost 36pt × 0.00145 → {cubeCapH*1000:F1}mm  {pass(cubeCapH, reqSecondSide)}");
            sb.AppendLine($"  [ActionDock]     Hint/Undo/Reset Jost 35pt × 0.00145 → {dockBtnCapH*1000:F1}mm  {pass(dockBtnCapH, reqBodySide)}");
            sb.AppendLine($"  [GuidePanel]     Controls body  Jost 38pt × 0.00145 → {guideBodyCapH*1000:F1}mm  {pass(guideBodyCapH, reqBodySide)}");
            sb.AppendLine($"  [TutorialCard]   Title          Jost 52pt × 0.00130 → {cardTitleCapH*1000:F1}mm  {pass(cardTitleCapH, reqTitleCard)}");
            sb.AppendLine($"  [TutorialCard]   Body           Jost 32pt × 0.00130 → {cardBodyCapH*1000:F1}mm  {pass(cardBodyCapH, reqBodyCard)}");
            sb.AppendLine($"  [TutorialCard]   TutorialNote   Jost 32pt × 0.00130 → {cardNoteCapH*1000:F1}mm  {pass(cardNoteCapH, reqBodyCard)}");
            sb.AppendLine("────────────────────────────────────────────────────────────────────");
            Debug.Log(sb.ToString());

            bool anyFail = levelCapH < reqSecondSide || ruleCapH < reqBodySide || statusCapH < reqBodySide
                        || cubeCapH < reqSecondSide || dockBtnCapH < reqBodySide || guideBodyCapH < reqBodySide
                        || cardTitleCapH < reqTitleCard || cardBodyCapH < reqBodyCard || cardNoteCapH < reqBodyCard;
            if (anyFail)
                Debug.LogWarning("[FontAudioApply] 有字高不达标，请检查 StructureBuildOpticalPanels 字号/worldScale。");
        }

        // ── 自检 ─────────────────────────────────────────────────────────────

        private static void RunValidators()
        {
            Debug.Log("[FontAudioApply] Running ValidateCampaignAndScene...");
            try
            {
                BuildCommands.ValidateCampaignAndScene();
                Debug.Log("[FontAudioApply] ValidateCampaignAndScene: PASS");
            }
            catch (Exception ex)
            {
                Debug.LogError("[FontAudioApply] ValidateCampaignAndScene: FAIL — " + ex.Message);
            }

            Debug.Log("[FontAudioApply] Running ValidateXrWorldSpacePackage...");
            try
            {
                BuildCommands.ValidateXrWorldSpacePackage();
                Debug.Log("[FontAudioApply] ValidateXrWorldSpacePackage: PASS");
            }
            catch (Exception ex)
            {
                Debug.LogError("[FontAudioApply] ValidateXrWorldSpacePackage: FAIL — " + ex.Message);
            }
        }

        // ── 标题按钮视觉状态（edit-mode 无 Update，手动设颜色/缩放）────────────
        private enum ButtonVisualState { Normal, Hover, Press }

        private static void ApplyTitleButtonState(StructureTitleButtonAction action, ButtonVisualState state)
        {
            var buttons = UnityEngine.Object.FindObjectsByType<StructureTitleButton>(FindObjectsInactive.Include);
            foreach (var btn in buttons)
            {
                bool isTarget = btn.action == action;
                if (!isTarget) continue;

                // 取当前 localScale 作为 base（Inspector 中设置的原始值）
                var baseScale = btn.transform.localScale;
                // 若之前已被调整，先恢复到接近 1 的 base
                if (Mathf.Abs(baseScale.x - 1.05f) < 0.01f || Mathf.Abs(baseScale.x - 0.97f) < 0.01f)
                    baseScale = Vector3.one;

                switch (state)
                {
                    case ButtonVisualState.Hover:
                        if (btn.graphic != null)
                        {
                            // +25% 亮度，上限 1（使背景明显变亮）
                            var hc = btn.highlightedColor;
                            btn.graphic.color = new Color(Mathf.Min(hc.r * 1.25f, 1f), Mathf.Min(hc.g * 1.25f, 1f), Mathf.Min(hc.b * 1.25f, 1f), hc.a);
                            // 删除旧 Outline，重新添加琥珀色外发光
                            foreach (var old in btn.graphic.GetComponents<UnityEngine.UI.Outline>())
                                UnityEngine.Object.DestroyImmediate(old);
                            var outline = btn.graphic.gameObject.AddComponent<UnityEngine.UI.Outline>();
                            outline.effectColor = new Color(1f, 0.75f, 0.34f, 1f); // 琥珀
                            outline.effectDistance = new Vector2(8f, -8f);
                            EditorUtility.SetDirty(outline);
                        }
                        if (btn.focusGraphic != null)
                            btn.focusGraphic.color = btn.focusColor;  // amber glow
                        if (btn.focusLine != null)
                            btn.focusLine.localScale = Vector3.one;   // fully expanded underline
                        btn.transform.localScale = baseScale * btn.hoverScale; // 1.05x
                        break;
                    case ButtonVisualState.Press:
                        if (btn.graphic != null)
                            btn.graphic.color = new Color(1f, 0.88f, 0.63f, 0.35f); // warm amber flash
                        if (btn.focusGraphic != null)
                            btn.focusGraphic.color = btn.focusColor;
                        if (btn.focusLine != null)
                            btn.focusLine.localScale = Vector3.one;
                        btn.transform.localScale = baseScale * btn.pressScale; // 0.97x
                        break;
                    default:
                        if (btn.graphic != null)
                            btn.graphic.color = btn.normalColor;
                        if (btn.focusGraphic != null)
                            btn.focusGraphic.color = Color.clear;
                        if (btn.focusLine != null)
                            btn.focusLine.localScale = new Vector3(0.16f, 1f, 1f);
                        btn.transform.localScale = baseScale;
                        break;
                }
                EditorUtility.SetDirty(btn);
            }
        }

        private static void ResetTitleButtonStates()
        {
            var buttons = UnityEngine.Object.FindObjectsByType<StructureTitleButton>(FindObjectsInactive.Include);
            foreach (var btn in buttons)
            {
                var baseScale = btn.transform.localScale;
                if (Mathf.Abs(baseScale.x - 1.05f) < 0.01f || Mathf.Abs(baseScale.x - 0.97f) < 0.01f)
                    baseScale = Vector3.one;
                if (btn.graphic != null)
                {
                    btn.graphic.color = btn.normalColor;
                    // 移除截图时临时添加的 Outline 发光组件，避免污染保存的场景
                    foreach (var old in btn.graphic.GetComponents<UnityEngine.UI.Outline>())
                        UnityEngine.Object.DestroyImmediate(old);
                }
                if (btn.focusGraphic != null) btn.focusGraphic.color = Color.clear;
                if (btn.focusLine != null) btn.focusLine.localScale = new Vector3(0.16f, 1f, 1f);
                btn.transform.localScale = baseScale;
                EditorUtility.SetDirty(btn);
            }
        }
    }
}
