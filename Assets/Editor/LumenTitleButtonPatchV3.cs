using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StructureBuild.Editor
{
    /// <summary>
    /// 总控 v3：只在已保存的标题场景上增量替换 START / EXIT 的按钮外观（雕花银框贴图、颜色、字号、柔光），
    /// 不重建场景、不改按钮位置与碰撞体。完成后跑标题场景校验，并拍常态 / 悬停 / 按下三张截图。
    /// batchmode：-executeMethod StructureBuild.Editor.LumenTitleButtonPatchV3.ApplyAndCapture
    /// </summary>
    public static class LumenTitleButtonPatchV3
    {
        private const string ShotDir = "VisualQA/EN-20261022";

        [MenuItem("Structure Build/Apply V3 Title Button Frames")]
        public static void ApplyAndCapture()
        {
            var scene = EditorSceneManager.OpenScene(StructureBuildTitleSceneInstaller.TitleScenePath, OpenSceneMode.Single);
            var startSprite = StructureBuildTitleSceneInstaller.LoadPanelSprite(StructureBuildTitleSceneInstaller.ButtonBgStartSpritePath);
            var exitSprite = StructureBuildTitleSceneInstaller.LoadPanelSprite(StructureBuildTitleSceneInstaller.ButtonBgExitSpritePath);
            var glowSprite = StructureBuildTitleSceneInstaller.LoadPanelSprite(StructureBuildTitleSceneInstaller.ButtonGlowSpritePath);
            if (startSprite == null || exitSprite == null || glowSprite == null)
                throw new System.InvalidOperationException("LUMEN_V3_BUTTON_PATCH_FAIL: button sprites missing");

            var buttons = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<StructureTitleButton>(true)).ToArray();
            if (buttons.Length != 2) throw new System.InvalidOperationException($"LUMEN_V3_BUTTON_PATCH_FAIL: expected 2 buttons, got {buttons.Length}");
            var collidersBefore = buttons.Select(b => b.GetComponent<BoxCollider>().size).ToArray();
            var rectsBefore = buttons.Select(b => ((RectTransform)b.transform).anchoredPosition).ToArray();

            foreach (var btn in buttons)
            {
                var isStart = btn.action == StructureTitleButtonAction.Start;
                var img = btn.GetComponent<Image>();
                img.sprite = isStart ? startSprite : exitSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                btn.normalColor = StructureBuildTitleSceneInstaller.ButtonFrameNormal;
                btn.highlightedColor = StructureBuildTitleSceneInstaller.ButtonFrameHighlight;
                img.color = btn.normalColor;

                if (btn.glowImage is Image glow)
                {
                    glow.sprite = glowSprite;
                    glow.type = Image.Type.Simple;
                    glow.preserveAspect = false;
                    glow.rectTransform.sizeDelta = ((RectTransform)btn.transform).sizeDelta + new Vector2(70f, 60f);
                    glow.color = Color.clear;
                    EditorUtility.SetDirty(glow);
                }

                var label = btn.labelText;
                label.text = isStart ? "START" : "EXIT";
                label.fontSize = isStart ? 30f : 27f;
                label.enableAutoSizing = false;
                label.characterSpacing = 10f;
                label.overflowMode = TextOverflowModes.Overflow;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.alignment = TextAlignmentOptions.Center;
                label.color = StructureTitleButton.LabelNormalColor;

                EditorUtility.SetDirty(img);
                EditorUtility.SetDirty(label);
                EditorUtility.SetDirty(btn);
                Debug.Log($"LUMEN_V3_BUTTON_PATCH {btn.name} size={((RectTransform)btn.transform).sizeDelta} font={label.fontSize}");
            }

            // 命中区域和位置不能变
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].GetComponent<BoxCollider>().size != collidersBefore[i] ||
                    ((RectTransform)buttons[i].transform).anchoredPosition != rectsBefore[i])
                    throw new System.InvalidOperationException("LUMEN_V3_BUTTON_PATCH_FAIL: button hit area changed");
            }

            EditorSceneManager.SaveScene(scene);
            StructureBuildTitleSceneInstaller.ValidateTitleScene();

            Capture(buttons);
            // 截图会改动按钮状态，重新打开保存版，保证场景文件干净
            EditorSceneManager.OpenScene(StructureBuildTitleSceneInstaller.TitleScenePath, OpenSceneMode.Single);
            Debug.Log("LUMEN_V3_BUTTON_PATCH_OK");
        }

        private static void Capture(StructureTitleButton[] buttons)
        {
            var cam = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .OrderByDescending(c => c.CompareTag("MainCamera")).FirstOrDefault();
            if (cam == null) { Debug.LogWarning("LUMEN_V3_BUTTON_PATCH: no camera for screenshots"); return; }
            Directory.CreateDirectory(ShotDir);
            var start = buttons.First(b => b.action == StructureTitleButtonAction.Start);

            foreach (var b in buttons) b.ApplyVisual(0f, false, 1f);
            Shoot(cam, "00c_cover_normal_EN.png");
            start.ApplyVisual(1f, false, 1f);
            Shoot(cam, "00_cover_start_hover_EN.png");
            start.ApplyVisual(1f, true, 0.15f);
            Shoot(cam, "00b_cover_start_press_EN.png");
        }

        private static void Shoot(Camera cam, string file)
        {
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(ShotDir, file), tex.EncodeToPNG());
            cam.targetTexture = prev;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log("LUMEN_V3_BUTTON_PATCH_SHOT " + file);
        }
    }
}
