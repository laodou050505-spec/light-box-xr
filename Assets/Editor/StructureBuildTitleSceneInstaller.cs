using System;
using System.Linq;
using ByteDance.PICO.XR;
using StructureBuild;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

namespace StructureBuild.Editor
{
    /// <summary>
    /// Builds an isolated boot scene. It intentionally creates no puzzle
    /// table, gameplay controller, gameplay HUD, placed cubes or gameplay
    /// colliders: beginning the game loads StructureBuild in Single mode.
    /// </summary>
    public static class StructureBuildTitleSceneInstaller
    {
        public const string TitleScenePath = "Assets/Scenes/StructureBuildTitle.unity";
        private const string GameplayScenePath = "Assets/Scenes/StructureBuild.unity";
        private const string FontAssetPath = "Assets/UI/StructureBuildChineseSDF.asset";

        private static readonly Color Ink = new Color(0.002f, 0.010f, 0.022f, 1f);
        private static readonly Color Deep = new Color(0.008f, 0.045f, 0.070f, 1f);
        private static readonly Color Cyan = new Color(0.20f, 0.94f, 0.90f, 1f);
        private static readonly Color Ice = new Color(0.78f, 0.94f, 1f, 1f);
        private static readonly Color Amber = new Color(1f, 0.62f, 0.16f, 1f);
        private static readonly Color Muted = new Color(0.48f, 0.68f, 0.74f, 1f);

        [MenuItem("Structure Build/Create or Refresh Isolated Title Scene")]
        public static void CreateOrRefreshTitleScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before creating the title scene.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "StructureBuildTitle";
            var titleRoot = new GameObject("TitleRoot");
            var focus = new GameObject("TitleFocus");
            focus.transform.SetParent(titleRoot.transform, false);
            focus.transform.position = Vector3.up * 1.00f;

            var designStartObject = new GameObject("TitleDesignPlayerStart_Anchor");
            designStartObject.transform.SetParent(titleRoot.transform, false);
            var designStart = designStartObject.AddComponent<DesignPlayerStart>();
            designStart.focalAnchor = focus.transform;
            designStart.focalOffset = Vector3.zero;
            designStart.yaw = 0f;
            designStart.viewingDistance = 4.00f;
            designStart.eyeHeight = 1.68f;
            designStart.fallbackHeadLocalPosition = Vector3.up * 1.60f;
            designStart.SynchronizeAnchor();

            var desktopCamera = CreateDesktopCamera(titleRoot.transform, designStart);
            var titleController = titleRoot.AddComponent<StructureTitleController>();
            titleController.designStart = designStart;
            titleController.desktopOrbit = desktopCamera.GetComponent<DesktopCameraOrbit>();
            titleController.gameplaySceneName = StructureTitleController.GameplaySceneName;

            CreateOpaqueCover(titleRoot.transform, designStart, titleController);
            CreatePicoRig(titleRoot.transform, designStart);
            // Save and import the scene before adding it to Build Settings.
            // EditorBuildSettingsScene resolves its serialized GUID from the
            // AssetDatabase; calling ConfigureBuildSettings while this new
            // scene only exists in memory wrote an all-zero GUID and made
            // Android builds omit the isolated boot stage.
            EditorSceneManager.SaveScene(scene, TitleScenePath, false);
            AssetDatabase.ImportAsset(TitleScenePath, ImportAssetOptions.ForceUpdate);
            ConfigureBuildSettings();

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("STRUCTURE_TITLE_INSTALL_OK: isolated opaque title scene created; title stage contains no gameplay root or gameplay components.");
        }

        [MenuItem("Structure Build/Validate Isolated Title Scene")]
        public static void ValidateTitleScene()
        {
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            var titleRoot = Find("TitleRoot");
            var cover = Find("TitleCover");
            var title = titleRoot?.GetComponent<StructureTitleController>();
            var start = Find("TitleDesignPlayerStart_Anchor")?.GetComponent<DesignPlayerStart>();
            var desktop = Find("TitleDesktopCamera")?.GetComponent<DesktopCameraOrbit>();
            var rig = Find("TitleXR Rig")?.GetComponent<PicoRigBootstrap>();
            Require(titleRoot != null && cover != null && title != null, "TitleRoot, opaque TitleCover, or StructureTitleController is missing.");
            Require(cover.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace && cover.GetComponent<Image>() != null,
                "The title cover must be an opaque fixed world-space canvas.");
            Require(start != null && desktop != null && rig != null && title.designStart == start && desktop != null && rig.designStart == start,
                "Title desktop and PICO rig must share the title DesignPlayerStart.");
            Require(Find("START")?.GetComponent<StructureTitleButton>() != null && Find("EXIT")?.GetComponent<StructureTitleButton>() != null,
                "Title START/EXIT world buttons are missing.");
            Require(FindObjects<StructureGameController>().Length == 0 && FindObjects<StructureHUDController>().Length == 0 &&
                    FindObjects<GameplayInstructionsOverlay>().Length == 0 && FindObjects<PicoHeadLockedCanvas>().Length == 0 &&
                    Find("PuzzleTable_Editable") == null && Find("GameplayPresentation_v2") == null,
                "The isolated title scene contains gameplay objects or head-locked UI.");
            Require(FindObjects<TitleWorldInteraction>().Length >= 2, "Both title controller-ray interactions are missing.");
            Debug.Log($"STRUCTURE_TITLE_VALIDATE_OK: opaque TitleCover only; shared title start eye={start.DesignedEyePosition:F2}; no gameplay objects or head-locked canvases.");
        }

        private static GameObject CreateDesktopCamera(Transform parent, DesignPlayerStart designStart)
        {
            var cameraObject = new GameObject("TitleDesktopCamera", typeof(Camera), typeof(AudioListener), typeof(DesktopCameraOrbit));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ink;
            camera.fieldOfView = 60f;
            var orbit = cameraObject.GetComponent<DesktopCameraOrbit>();
            orbit.target = designStart.focalAnchor;
            orbit.minDistance = 1f;
            orbit.maxDistance = 12f;
            designStart.ApplyDesktopPose(orbit);
            return cameraObject;
        }

        private static void CreateOpaqueCover(Transform parent, DesignPlayerStart designStart, StructureTitleController title)
        {
            var cover = new GameObject("TitleCover", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            cover.transform.SetParent(parent, false);
            var rect = cover.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1400f, 1000f);
            cover.transform.position = new Vector3(0f, 2.10f, 0f);
            cover.transform.localScale = Vector3.one * 0.005f;
            designStart.FaceWorldPanel(cover.transform);
            // World-space UGUI draws toward its local -Z side. The generic
            // panel helper aims +Z at the player, which made the isolated
            // boot cover readable only from its mirrored back face. Flip this
            // standalone title canvas once so the authored design start sees
            // its actual front; it remains a physical world-space panel.
            cover.transform.Rotate(0f, 180f, 0f, Space.Self);
            var canvas = cover.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            var background = cover.GetComponent<Image>();
            background.color = Ink;

            var inner = Panel("InnerFrame", cover.transform, Deep);
            SetRect(inner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1190f, 790f));
            var topLine = Panel("TopLine", inner.transform, Cyan);
            SetRect(topLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 10f));
            var bottomLine = Panel("BottomLine", inner.transform, new Color(0.10f, 0.40f, 0.45f, 1f));
            SetRect(bottomLine.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f));

            var eyebrow = Text("Eyebrow", inner.transform, "PICO XR  /  SPATIAL PROJECTION PUZZLE", 28f, Amber, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(eyebrow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -125f), new Vector2(0f, 50f));
            // Keep the boot-stage typography entirely in the guaranteed
            // bundled Latin glyph range.  This is the PICO launch surface, so
            // it must never degrade into missing-glyph squares on a clean APK.
            var gameName = Text("GameName", inner.transform, "LUMEN BOX", 96f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(gameName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -275f), new Vector2(0f, 130f));
            var subtitle = Text("Subtitle", inner.transform, "SPATIAL PROJECTION PUZZLE", 34f, Muted, FontStyles.Normal, TextAlignmentOptions.Center);
            SetRect(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 40f), new Vector2(0f, 80f));
            var hint = Text("InteractionHint", inner.transform, "POINT A CONTROLLER RAY AT START  ·  CLICK TO BEGIN", 22f, new Color(0.42f, 0.76f, 0.80f, 1f), FontStyles.Normal, TextAlignmentOptions.Center);
            SetRect(hint.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -220f), new Vector2(0f, 48f));
            CreateButton("START", inner.transform, new Vector2(0f, -100f), "START", title, StructureTitleButtonAction.Start, Cyan);
            CreateButton("EXIT", inner.transform, new Vector2(0f, -310f), "EXIT", title, StructureTitleButtonAction.Exit, new Color(0.22f, 0.54f, 0.60f, 1f));
        }

        private static void CreatePicoRig(Transform parent, DesignPlayerStart designStart)
        {
            var rig = new GameObject("TitleXR Rig", typeof(PXR_Manager), typeof(PicoRigBootstrap));
            rig.transform.SetParent(parent, false);
            var xrCameraObject = new GameObject("TitleXR Camera", typeof(Camera), typeof(AudioListener), typeof(PicoControllerPose));
            xrCameraObject.transform.SetParent(rig.transform, false);
            xrCameraObject.tag = "Untagged";
            xrCameraObject.transform.localPosition = Vector3.up * 1.60f;
            var xrCamera = xrCameraObject.GetComponent<Camera>();
            xrCamera.enabled = false;
            xrCamera.clearFlags = CameraClearFlags.SolidColor;
            xrCamera.backgroundColor = Ink;
            xrCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            xrCameraObject.GetComponent<PicoControllerPose>().node = XRNode.Head;

            foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand })
            {
                var handName = node == XRNode.LeftHand ? "Title Left Controller" : "Title Right Controller";
                var hand = new GameObject(handName, typeof(PicoControllerPose), typeof(LineRenderer), typeof(TitleWorldInteraction));
                hand.transform.SetParent(rig.transform, false);
                hand.GetComponent<PicoControllerPose>().node = node;
                var line = hand.GetComponent<LineRenderer>();
                line.enabled = false;
                line.startWidth = 0.008f;
                line.endWidth = 0.003f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = Cyan;
                line.endColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.15f);
                var interaction = hand.GetComponent<TitleWorldInteraction>();
                interaction.rayOrigin = hand.transform;
                interaction.rayLine = line;
                interaction.controllerNode = node;
            }

            var bootstrap = rig.GetComponent<PicoRigBootstrap>();
            bootstrap.xrCamera = xrCamera;
            bootstrap.picoManager = rig.GetComponent<PXR_Manager>();
            bootstrap.designStart = designStart;
        }

        private static void CreateButton(string name, Transform parent, Vector2 position, string label, StructureTitleController title, StructureTitleButtonAction action, Color accent)
        {
            var button = Panel(name, parent, new Color(0.035f, 0.24f, 0.30f, 1f));
            SetRect(button.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(420f, 116f));
            var collider = button.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(420f, 116f, 12f);
            var behavior = button.gameObject.AddComponent<StructureTitleButton>();
            behavior.title = title;
            behavior.action = action;
            behavior.graphic = button;
            behavior.normalColor = new Color(0.035f, 0.24f, 0.30f, 1f);
            behavior.highlightedColor = accent;
            var text = Text("Label", button.transform, label, 42f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static Image Panel(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) ?? TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(item => item.path != TitleScenePath && item.path != GameplayScenePath).ToList();
            var titleGuid = AssetDatabase.GUIDFromAssetPath(TitleScenePath);
            var gameplayGuid = AssetDatabase.GUIDFromAssetPath(GameplayScenePath);
            Require(titleGuid.ToString() != "00000000000000000000000000000000" &&
                    gameplayGuid.ToString() != "00000000000000000000000000000000",
                "Title or gameplay scene must be saved and imported before Build Settings are updated.");
            // The path constructor resolves the GUID after the forced import.
            // (The GUID overload can retain the all-zero temporary entry when
            // an untitled scene was saved over an existing asset in batch mode.)
            var titleEntry = new EditorBuildSettingsScene(titleGuid, true);
            var gameplayEntry = new EditorBuildSettingsScene(gameplayGuid, true);
            Debug.Log($"STRUCTURE_TITLE_BUILD_GUIDS: titleAsset={titleGuid}; titleEntry={titleEntry.guid}; gameplayAsset={gameplayGuid}; gameplayEntry={gameplayEntry.guid}.");
            scenes.Insert(0, titleEntry);
            scenes.Insert(1, gameplayEntry);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static GameObject Find(string name)
        {
            var active = SceneManager.GetActiveScene();
            return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .FirstOrDefault(item => item.name == name && item.gameObject.scene == active)?.gameObject;
        }

        private static T[] FindObjects<T>() where T : UnityEngine.Object
        {
            var active = SceneManager.GetActiveScene();
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include)
                .Where(item => item is Component component && component.gameObject.scene == active)
                .ToArray();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
