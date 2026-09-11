using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace StructureBuild.Editor
{
    // THESIS: An optical instrument readout keeps the puzzle and its next action clear.
    // OWN-WORLD: Cover-derived midnight enamel, thin silver rims, warm amber controls,
    // ivory live Chinese type, and generous unornamented text fields.
    // STORY: Read the current archive, build its two projections, confirm completion.
    // FIRST VIEWPORT: Matching-height mission/guide panels above the table; three
    // horizontal actions below it. Teaching and completion own the central field.
    // FORM: User-pinned Optical Archive extension; fixed, explicitly padded text regions.
    // FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, and DESIGN.md
    public static class StructureBuildOpticalPanels
    {
        public const string AssetRoot = "Assets/UI/Generated/OpticalPanels20260908/";
        public const float UpperPanelOffset = 0.94f;
        public const float LowerPanelOffset = -1.13f;
        private const string ScenePath = "Assets/Scenes/StructureBuild.unity";
        private static readonly Color Paper = Hex(0xF2EFE7);
        private static readonly Color Muted = Hex(0xBDC9DA);
        private static readonly Color Amber = Hex(0xECC582);
        private static readonly Color Ink = Hex(0x0B1327);
        private static readonly Color ButtonInk = Hex(0x18263F);
        private static readonly Color Rule = Hex(0x495870);
        private static TMP_FontAsset font;
        private static Sprite readoutSkin, guideSkin, dockSkin;
        private static StructureHUDController actions;

        [MenuItem("Structure Build/Apply Optical Archive Panels")]
        public static void ApplyAndSave()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before rebuilding panels.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // The requested opening pose may move generated viewing anchors;
            // all other authored world transforms remain protected.
            var world = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Where(t => t.GetComponentInParent<Canvas>(true) == null)
                .Where(t => t.GetComponent<Camera>() == null && t.GetComponent<DesignPlayerStart>() == null &&
                            t.GetComponent<PicoRigBootstrap>() == null)
                .ToDictionary(t => t, t => (t.parent, t.localPosition, t.localRotation, t.localScale));
            ApplyToLoadedScene();
            foreach (var pair in world)
            {
                var t = pair.Key;
                if (t == null || (t.parent, t.localPosition, t.localRotation, t.localScale) != pair.Value)
                    throw new InvalidOperationException("Optical UI changed an authored world transform.");
            }
            StructureBuildCurrentSceneInstaller.ValidateInstalledCurrentScene();
            ValidatePanels();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("STRUCTURE_OPTICAL_PANELS_OK: all panels rebuilt; only Hint, Undo and Reset in both docks; authored world transforms preserved.");
        }

        public static void ApplyToLoadedScene()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/StructureBuildChineseSDF.asset");
            actions = Find("GameSystems").GetComponent<StructureHUDController>();
            guideSkin = Skin("Guide");
            dockSkin = Skin("Dock");
            readoutSkin = Skin("Readout");
            BuildMission(false);
            BuildMission(true);
            BuildDock(false);
            BuildDock(true);
            BuildGuide(false);
            BuildGuide(true);
            BuildTeaching(false);
            BuildTeaching(true);
            BuildCompletion(false);
            BuildCompletion(true);
            Canvas.ForceUpdateCanvases();
            foreach (var image in UnityEngine.Object.FindObjectsByType<Image>(FindObjectsInactive.Include))
            {
                if (image.sprite != readoutSkin && image.sprite != guideSkin && image.sprite != dockSkin) continue;
                // Scale the protected metal corners uniformly. Only the empty
                // centre stretches when a readout or dock changes its height.
                image.pixelsPerUnitMultiplier = image.sprite.rect.width / Mathf.Max(1, image.rectTransform.rect.width);
            }
            StructureBuildSpatialUiLayout.ApplyToLoadedGameplayScene();
        }

        private static void BuildMission(bool xr)
        {
            var host = Find(xr ? "PicoWorldHUD" : "DesktopHUD");
            DeleteChild(host.transform, "MissionReadout");
            if (xr) Spatial(host, new Vector2(900, 506), new Vector2(-0.62f, UpperPanelOffset), 1.70f, 0.00108f);
            var panel = Panel("MissionReadout", host.transform, readoutSkin);
            if (xr) Box(panel.rectTransform, 0, 0, 900, 506);
            else Box(panel.rectTransform, 36, 16, 585, 329);
            float scale = xr ? 1 : 0.65f;
            var content = Content(panel.transform, 900, 506, scale);
            var title = Text("Level", content, "结构档案 01 / 12  ·  首次校准", 38, Paper, 68, 54, 744, 58, true);
            Fit(title, 30, 38, false);
            TextMeshProUGUI rule = Text("Rule", content, "先放两块地面方块，让正面与侧面同时亮起", 27, Muted, 68, 134, 744, 76);
            Fit(rule, 22, 27, true);
            Line(content, 68, 232, 744);
            var status = Text("Status", content, "正面 0/2 · 侧面 0/2", 30, Amber, 68, 264, 744, 70, true);
            Fit(status, 24, 30, true);
            var cubes = Text("CubeCount", content, "结构单元  0 / 2", 29, Paper, 68, 386, 744, 48);
            var hud = host.GetComponent<StructureScreenHUD>();
            hud.levelText = title; hud.ruleText = rule; hud.statusText = status; hud.cubeText = cubes; hud.viewText = null;
            if (xr)
            {
                actions.game.levelLabel = title;
                actions.game.statusLabel = status;
                actions.game.cubeCountLabel = cubes;
                actions.viewLabel = null; actions.messageLabel = status;
            }
            EditorUtility.SetDirty(hud);
        }

        private static void BuildDock(bool xr)
        {
            var host = Find(xr ? "PicoActionDock" : "DesktopHUD");
            DeleteChild(host.transform, "ActionDock");
            if (xr) Spatial(host, new Vector2(820, 246), new Vector2(0, LowerPanelOffset), 1.70f, 0.00118f);
            var panel = Panel("ActionDock", host.transform, dockSkin);
            if (xr) Fill(panel.rectTransform);
            else
            {
                panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(0.5f, 0);
                panel.rectTransform.pivot = new Vector2(0.5f, 0);
                panel.rectTransform.anchoredPosition = new Vector2(0, 12);
                panel.rectTransform.sizeDelta = new Vector2(574, 172);
            }
            var content = Content(panel.transform, 820, 246, xr ? 1 : 0.7f);
            Button(content, "提示", StructureUIButtonAction.Hint, 52, 55, 222, 136, xr, true);
            Button(content, "撤回", StructureUIButtonAction.Undo, 299, 55, 222, 136, xr, false);
            Button(content, "重置", StructureUIButtonAction.Reset, 546, 55, 222, 136, xr, false);
        }

        private static void BuildGuide(bool xr)
        {
            var host = Find(xr ? "PicoQuickGuide" : "DesktopHUD");
            DeleteChild(host.transform, "GuidePanel");
            if (xr) Spatial(host, new Vector2(690, 506), new Vector2(0.62f, UpperPanelOffset), 1.70f, 0.00108f);
            var panel = Panel("GuidePanel", host.transform, guideSkin);
            if (xr) Fill(panel.rectTransform);
            else
            {
                panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = Vector2.one;
                panel.rectTransform.pivot = Vector2.one;
                panel.rectTransform.anchoredPosition = new Vector2(-36, -16);
                panel.rectTransform.sizeDelta = new Vector2(449, 299);
            }
            var content = Content(panel.transform, 690, 506, xr ? 1 : 0.65f);
            Text("Title", content, "操作提示", 38, Paper, 58, 48, 560, 54, true);
            Line(content, 58, 120, 560);
            var body = Text("Body", content,
                "1  按扳机抓取左侧方块\n2  拖到橙色格，松开后吸附\n3  让两面投影都变成青绿色\n\n完成后点击下一关",
                27, Muted, 58, 148, 560, 300);
            body.lineSpacing = 10;
            Fit(body, 23, 27, true);
        }

        private static void BuildTeaching(bool xr)
        {
            var host = Find(xr ? "GameplayInstructions_Pico" : "GameplayInstructions_Desktop");
            var layer = Child(host.transform, "Layer");
            Clear(layer);
            Image card;
            if (xr)
            {
                Spatial(host, new Vector2(1080, 720), new Vector2(0, -0.02f), 1.72f, 0.0013f);
                card = host.GetComponent<Image>();
                if (card != null) UnityEngine.Object.DestroyImmediate(card);
                card = Panel("Card", layer, guideSkin); Fill(card.rectTransform);
            }
            else
            {
                var dimmer = Panel("Dimmer", layer, null); dimmer.color = new Color(0.015f, 0.022f, 0.04f, 0.82f); Fill(dimmer.rectTransform);
                card = Panel("Card", layer, guideSkin); Center(card.rectTransform, 864, 576);
            }
            var content = Content(card.transform, 1080, 720, xr ? 1 : 0.8f);
            Text("Title", content, "光匣教学关", 52, Paper, 84, 68, 912, 70, true);
            Line(content, 84, 166, 912);
            var body = Text("Body", content,
                "教学关只需 2 块地面方块\n\n1  指向左侧方块源，按扳机抓取\n2  拖到橙色高亮格，松开后自动吸附\n3  让正面与侧面投影都变成青绿色",
                32, Muted, 84, 200, 912, 272);
            body.lineSpacing = 10;
            Fit(body, 27, 32, true);
            Text("NextArchiveNote", content, "完成后点击下一关，继续后面的档案", 27, Muted, 84, 496, 912, 42);
            Button(content, "开始练习", StructureUIButtonAction.DismissInstructions, 84, 570, 360, 88, xr, true);
        }

        private static void BuildCompletion(bool xr)
        {
            var host = Find(xr ? "CompletionOverlay_Pico" : "CompletionOverlay_Desktop");
            var layer = Child(host.transform, "Layer");
            Clear(layer);
            if (xr) Spatial(host, new Vector2(1080, 720), new Vector2(0, -0.02f), 1.72f, 0.0013f);
            else
            {
                var dimmer = Panel("Dimmer", layer, null); dimmer.color = new Color(0.015f, 0.022f, 0.04f, 0.82f); Fill(dimmer.rectTransform);
            }
            var card = Panel("Card", layer, guideSkin);
            if (xr) Fill(card.rectTransform); else Center(card.rectTransform, 864, 576);
            var content = Content(card.transform, 1080, 720, xr ? 1 : 0.8f);
            var title = Text(xr ? "CompletionTitle" : "Title", content, "结构档案完成", 52, Paper, 84, 90, 912, 76, true);
            Line(content, 84, 206, 912);
            var detail = Text(xr ? "CompletionDetail" : "Detail", content, "档案 01 已通过双面投影校验", 32, Muted, 84, 252, 912, 76);
            Fit(detail, 26, 32, true);
            var progress = Text(xr ? "CompletionProgress" : "Progress", content, "准备进入档案 02", 30, Amber, 84, 380, 912, 52);
            Fit(progress, 25, 30, false);
            var label = Button(content, "下一关", StructureUIButtonAction.Continue, 84, 550, 360, 90, xr, true);
            var overlay = host.GetComponent<LevelCompleteOverlay>();
            overlay.titleText = title; overlay.detailText = detail; overlay.progressText = progress;
            overlay.continueLabel = label; overlay.combinedPayloadText = null;
            EditorUtility.SetDirty(overlay);
        }

        private static TextMeshProUGUI Button(Transform parent, string label, StructureUIButtonAction action,
            float x, float y, float w, float h, bool xr, bool primary)
        {
            var rim = Panel(label + "Button", parent, null); Box(rim.rectTransform, x, y, w, h);
            rim.raycastTarget = true;
            var rest = primary ? Hex(0xD9B471) : ButtonInk;
            var hover = primary ? Hex(0xF0CD92) : Hex(0x2C405E);
            rim.color = rest;
            var lower = Panel("FocusLine", rim.transform, null);
            lower.color = Color.clear; Box(lower.rectTransform, 20, h - 11, w - 40, 3);
            var text = Text("Label", rim.transform, label, label.Length > 2 ? 35 : 38,
                primary ? Ink : Paper, 20, 16, w - 40, h - 32, true);
            text.alignment = TextAlignmentOptions.Center;
            if (xr)
            {
                var collider = rim.gameObject.AddComponent<BoxCollider>();
                // Top-left panel pivot: the hit volume must be centred on its visual rectangle.
                collider.center = new Vector3(w * 0.5f, -h * 0.5f, 0);
                collider.size = new Vector3(w + 10, h + 18, 18);
                var button = rim.gameObject.AddComponent<StructureUIButton>();
                button.hud = actions; button.action = action; button.backgroundGraphic = rim;
                button.normalColor = rest; button.highlightedColor = hover; button.focusGraphic = lower;
            }
            else
            {
                var button = rim.gameObject.AddComponent<StructureScreenButton>();
                button.hud = actions; button.action = action; button.background = rim;
                button.normalColor = rest; button.hoverColor = hover; button.focusGraphic = lower;
            }
            return text;
        }

        public static void ValidatePanels()
        {
            var pico = Find("PicoActionDock").GetComponentsInChildren<StructureUIButton>(true);
            var desktop = Child(Find("DesktopHUD").transform, "ActionDock").GetComponentsInChildren<StructureScreenButton>(true);
            var required = new[] { StructureUIButtonAction.Hint, StructureUIButtonAction.Undo, StructureUIButtonAction.Reset };
            if (!pico.Select(b => b.action).SequenceEqual(required) || !desktop.Select(b => b.action).SequenceEqual(required))
                throw new InvalidOperationException("Both docks must contain exactly Hint, Undo, Reset in that order.");
            foreach (var b in pico)
            {
                var r = b.GetComponent<RectTransform>().rect;
                var c = b.GetComponent<BoxCollider>();
                if (Vector2.Distance(c.center, r.center) > 0.01f) throw new InvalidOperationException("Button collider is off-centre.");
            }
            foreach (var panelName in new[] { "MissionReadout", "GuidePanel", "Card", "ActionDock" })
            foreach (var panel in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).Where(r => r.name == panelName))
                ValidateTextLayout(panel);
            Debug.Log("STRUCTURE_OPTICAL_PANEL_VALIDATE_OK");
        }

        private static Sprite Skin(string name)
        {
            string path = AssetRoot + name + "/01_generated_image_url.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing optical panel texture: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            float edge = texture == null ? 96 : Mathf.Min(texture.width, texture.height) * 0.055f;
            importer.spriteBorder = new Vector4(edge, edge, edge, edge);
            importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true; android.maxTextureSize = 2048;
            android.format = TextureImporterFormat.ETC2_RGBA8;
            android.textureCompression = TextureImporterCompression.CompressedHQ;
            android.compressionQuality = 100; android.crunchedCompression = false;
            importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Transform Content(Transform parent, float w, float h, float scale)
        {
            var r = Rect("Content", parent); Box(r, 0, 0, w, h); r.localScale = Vector3.one * scale; return r;
        }
        private static Image Panel(string name, Transform parent, Sprite skin)
        {
            var r = Rect(name, parent); var image = r.gameObject.AddComponent<Image>();
            image.sprite = skin; image.type = skin != null ? Image.Type.Sliced : Image.Type.Simple; image.preserveAspect = false;
            image.color = skin != null ? Color.white : Ink; image.raycastTarget = false; return image;
        }
        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color,
            float x, float y, float w, float h, bool bold = false)
        {
            var r = Rect(name, parent); Box(r, x, y, w, h);
            var text = r.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false;
            text.margin = Vector4.zero; return text;
        }
        private static void Fit(TextMeshProUGUI text, float minimum, float maximum, bool wrap)
        {
            text.enableAutoSizing = true; text.fontSizeMin = minimum; text.fontSizeMax = maximum;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
        }
        private static void ValidateTextLayout(RectTransform panel)
        {
            var texts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                text.ForceMeshUpdate();
                if (text.isTextOverflowing) throw new InvalidOperationException($"Text overflows its box: {panel.name}/{text.name}");
                var r = text.rectTransform;
                if (r.rect.width < 1 || r.rect.height < 1) throw new InvalidOperationException($"Invalid text box: {panel.name}/{text.name}");
            }
            for (int i = 0; i < texts.Length; i++)
            for (int j = i + 1; j < texts.Length; j++)
            {
                if (texts[i].transform.parent != texts[j].transform.parent) continue;
                var a = WorldRect(texts[i].rectTransform); var b = WorldRect(texts[j].rectTransform);
                if (a.Overlaps(b)) throw new InvalidOperationException($"Text boxes overlap: {panel.name}/{texts[i].name} and {texts[j].name}");
            }
        }
        private static UnityEngine.Rect WorldRect(RectTransform r)
        {
            var corners = new Vector3[4]; r.GetWorldCorners(corners);
            return UnityEngine.Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
        private static void Line(Transform parent, float x, float y, float width)
        {
            var line = Panel("Divider", parent, null); line.color = Rule; Box(line.rectTransform, x, y, width, 1);
        }
        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go.GetComponent<RectTransform>();
        }
        private static void Box(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        }
        private static void Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero; r.sizeDelta = Vector2.zero;
        }
        private static void Center(RectTransform r, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(w, h);
        }
        private static void Spatial(GameObject host, Vector2 size, Vector2 offset, float distance, float scale)
        {
            host.GetComponent<RectTransform>().sizeDelta = size;
            var follower = host.GetComponent<PicoHeadLockedCanvas>();
            follower.distance = distance; follower.worldScale = scale; follower.viewOffset = offset;
            follower.followRotation = true; EditorUtility.SetDirty(follower);
        }
        private static GameObject Find(string name) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .First(t => t.name == name).gameObject;
        private static Transform Child(Transform parent, string name) => parent.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
        private static void DeleteChild(Transform parent, string name)
        {
            var child = parent.Find(name); if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        private static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
        private static Color Hex(int c) => new Color(((c >> 16) & 255) / 255f, ((c >> 8) & 255) / 255f, (c & 255) / 255f, 1);
    }
}
