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
        private const string TitlePanelSkinPath = "Assets/UI/Generated/NanoBanana20260908/OpticalArchiveFinal/01_generated_image_url.png";
        private const string TitleCoverFeatherMaterialPath = "Assets/UI/TitleCoverFeather.mat";
        private const string TitleCoverFeatherShaderPath = "Assets/Shaders/TitleCoverFeather.shader";
        public const float TitleViewingDistance = 3.45f;

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
            // The title is composed around the eye line, with only a subtle
            // downward glance (rather than the gameplay's elevated focus).
            focus.transform.position = Vector3.up * 1.58f;

            var designStartObject = new GameObject("TitleDesignPlayerStart_Anchor");
            designStartObject.transform.SetParent(titleRoot.transform, false);
            var designStart = designStartObject.AddComponent<DesignPlayerStart>();
            designStart.focalAnchor = focus.transform;
            designStart.focalOffset = Vector3.zero;
            designStart.yaw = 0f;
            designStart.viewingDistance = TitleViewingDistance;
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
            // Never open the title in Single mode: callers may have unsaved
            // edits in the currently active scene (including this title).
            // Validate the active title directly, or load a temporary
            // additive copy and restore the previous active scene afterward.
            var previous = SceneManager.GetActiveScene();
            var scene = previous.path == TitleScenePath
                ? previous
                : EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Additive);
            if (previous.path != TitleScenePath) SceneManager.SetActiveScene(scene);
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
            var titleFrame = Find("InnerFrame")?.GetComponent<Image>();
            var titleMotion = Find("InnerFrame")?.GetComponent<UiBreathingAccent>();
            var featherMaterial = AssetDatabase.LoadAssetAtPath<Material>(TitleCoverFeatherMaterialPath);
            Require(titleFrame != null && titleFrame.sprite != null && titleFrame.type == Image.Type.Simple && titleFrame.preserveAspect &&
                    titleMotion != null && titleMotion.dimColor.a >= 0.999f && titleMotion.scaleAmount <= 0.0031f &&
                    AssetDatabase.GetAssetPath(titleFrame.sprite) == TitlePanelSkinPath &&
                    featherMaterial != null && titleFrame.material == featherMaterial &&
                    Find("TitleCover")?.GetComponent<Image>()?.material == featherMaterial &&
                    FindObjects<TextMeshProUGUI>().Length == 0,
                "Generated title lettering must be preserved without duplicate Unity text, with the persisted feather material.");
            var coverTransform = Find("TitleCover")?.transform;
            var expectedCoverY = start.DesignedEyePosition.y - 0.10f;
            Require(coverTransform != null && Mathf.Abs(coverTransform.position.y - expectedCoverY) < 0.01f &&
                    Vector3.Distance(coverTransform.position, DesignedCoverPosition(start)) < 0.01f &&
                    Mathf.Abs(start.viewingDistance - TitleViewingDistance) < 0.001f &&
                    Mathf.Abs(Vector3.Distance(coverTransform.position, start.DesignedEyePosition) -
                              Mathf.Sqrt(TitleViewingDistance * TitleViewingDistance + 0.01f)) < 0.01f &&
                    Mathf.Abs(Mathf.DeltaAngle(coverTransform.eulerAngles.x, 0f)) < 0.1f &&
                    Mathf.Abs(Mathf.DeltaAngle(coverTransform.eulerAngles.z, 0f)) < 0.1f,
                "Title cover must be upright and centred just below the designed eye line.");
            var artAspect = titleFrame.sprite.rect.width / titleFrame.sprite.rect.height;
            Require(Mathf.Abs(titleFrame.rectTransform.rect.width / titleFrame.rectTransform.rect.height - artAspect) < 0.001f,
                "Cover geometry must use source artwork aspect for aligned button targets.");
            var buttons = FindObjects<StructureTitleButton>();
            Require(buttons.Length == 2 && buttons.All(button => button.focusLine != null && button.focusGraphic != null &&
                    button.normalColor.a == 0f && button.GetComponent<BoxCollider>() != null),
                "The two baked labels need transparent hit areas and visible focus feedback.");
            Require(FindObjects<StructureGameController>().Length == 0 && FindObjects<StructureHUDController>().Length == 0 &&
                    FindObjects<GameplayInstructionsOverlay>().Length == 0 && FindObjects<PicoHeadLockedCanvas>().Length == 0 &&
                    Find("PuzzleTable_Editable") == null && Find("GameplayPresentation_v2") == null,
                "The isolated title scene contains gameplay objects or head-locked UI.");
            Require(FindObjects<TitleWorldInteraction>().Length >= 2 &&
                    FindObjects<TitleWorldInteraction>().All(item => item.uiAimAssistRadius >= 0.025f),
                "Both title controller rays must keep UI aim assist enabled.");
            Debug.Log($"STRUCTURE_TITLE_VALIDATE_OK: opaque TitleCover only; shared title start eye={start.DesignedEyePosition:F2}; no gameplay objects or head-locked canvases.");
            if (previous.path != TitleScenePath)
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject CreateDesktopCamera(Transform parent, DesignPlayerStart designStart)
        {
            var cameraObject = new GameObject("TitleDesktopCamera", typeof(Camera), typeof(AudioListener), typeof(DesktopCameraOrbit));
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ink;
            camera.fieldOfView = 80f;
            var orbit = cameraObject.GetComponent<DesktopCameraOrbit>();
            orbit.target = designStart.focalAnchor;
            orbit.minDistance = 1f;
            orbit.maxDistance = 12f;
            orbit.minPitch = 0f;
            orbit.maxPitch = 72f;
            designStart.ApplyDesktopPose(orbit);
            return cameraObject;
        }

        private static void CreateOpaqueCover(Transform parent, DesignPlayerStart designStart, StructureTitleController title)
        {
            var cover = new GameObject("TitleCover", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            cover.transform.SetParent(parent, false);
            var rect = cover.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1400f, 1000f);
            // Keep the title fixed in world space, vertically upright and
            // centred just below the opening eye line (matching the supplied
            // cover reference rather than pitching the artwork downward).
            cover.transform.position = DesignedCoverPosition(designStart);
            cover.transform.localScale = Vector3.one * 0.0043f;
            cover.transform.rotation = Quaternion.Euler(0f, designStart.DesignedEyeRotation.eulerAngles.y, 0f);
            var canvas = cover.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            var background = cover.GetComponent<Image>();
            background.color = Ink;
            background.material = LoadTitleCoverFeatherMaterial();
            background.raycastTarget = false;

            // THESIS: enter an optical archive through a monumental aperture.
            // OWN-WORLD: midnight blue, porcelain silver, amber glass, image-authored lettering.
            // STORY: see cubes and their two projections, then START.
            // FIRST VIEWPORT: title left, optical sculpture right, two targets lower left.
            // FORM: aperture gateway, surface candidate 5, seed lightbox-cover-20260908.
            // FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, and DESIGN.md.
            var titleSkin = LoadPanelSprite(TitlePanelSkinPath);
            Require(titleSkin != null, "Optical Archive artwork is missing.");
            var inner = Panel("InnerFrame", cover.transform, Color.white);
            var artSize = new Vector2(1380f, 1380f * titleSkin.rect.height / titleSkin.rect.width);
            SetRect(inner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, artSize);
            inner.sprite = titleSkin;
            inner.type = Image.Type.Simple;
            inner.preserveAspect = true;
            inner.raycastTarget = false;
            inner.material = background.material;
            var breathing = inner.gameObject.AddComponent<UiBreathingAccent>();
            breathing.target = inner;
            breathing.dimColor = new Color(0.97f, 0.98f, 1f, 1f);
            breathing.brightColor = Color.white;
            breathing.speed = 0.10f;
            breathing.scaleAmount = 0f;

            // Bounds measured on the generated artwork, normalized from top left.
            CreateArtworkButton("START", inner.transform, artSize, new Rect(0.079f, 0.678f, 0.366f, 0.067f),
                title, StructureTitleButtonAction.Start, new Color(1f, 0.79f, 0.43f));
            CreateArtworkButton("EXIT", inner.transform, artSize, new Rect(0.080f, 0.777f, 0.121f, 0.066f),
                title, StructureTitleButtonAction.Exit, new Color(0.79f, 0.88f, 1f));
        }

        /// <summary>Applies the persisted feather material and upright cover pose to an already loaded title scene.</summary>
        public static void ApplyToLoadedScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "StructureBuildTitle")
                throw new InvalidOperationException("Load StructureBuildTitle before applying title layout.");
            var cover = Find("TitleCover");
            var start = Find("TitleDesignPlayerStart_Anchor")?.GetComponent<DesignPlayerStart>();
            if (cover == null || start == null) throw new InvalidOperationException("TitleCover or title DesignPlayerStart is missing.");
            var focus = Find("TitleFocus")?.transform;
            if (focus == null) throw new InvalidOperationException("TitleFocus is missing.");
            // Preserve the authored cover plane's X/Z. Raising only the
            // focus's Y keeps the upright artwork ahead of the eye, not at it.
            focus.position = new Vector3(focus.position.x, start.eyeHeight - 0.10f, focus.position.z);
            start.focalAnchor = focus;
            start.focalOffset = Vector3.zero;
            start.useReferenceViewPitch = false;
            start.viewingDistance = TitleViewingDistance;
            start.SynchronizeAnchor();
            var material = LoadTitleCoverFeatherMaterial();
            var coverImage = cover.GetComponent<Image>();
            if (coverImage != null) { coverImage.material = material; coverImage.raycastTarget = false; }
            var inner = Find("InnerFrame")?.GetComponent<Image>();
            if (inner != null) { inner.material = material; inner.raycastTarget = false; }
            cover.transform.position = DesignedCoverPosition(start);
            cover.transform.rotation = Quaternion.Euler(0f, start.DesignedEyeRotation.eulerAngles.y, 0f);
            cover.transform.localScale = Vector3.one * 0.0043f;
            var orbit = Find("TitleDesktopCamera")?.GetComponent<DesktopCameraOrbit>();
            if (orbit != null)
            {
                orbit.minPitch = 0f; orbit.maxPitch = 72f;
                orbit.GetComponent<Camera>().fieldOfView = 80f;
                start.ApplyDesktopPose(orbit);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
        }

        private static Vector3 DesignedCoverPosition(DesignPlayerStart start)
        {
            var focus = start.FocusPosition;
            return new Vector3(focus.x, start.DesignedEyePosition.y - 0.10f, focus.z);
        }

        private static Material LoadTitleCoverFeatherMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(TitleCoverFeatherShaderPath);
            if (shader == null) throw new InvalidOperationException("TitleCoverFeather shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(TitleCoverFeatherMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "TitleCoverFeather" };
                AssetDatabase.CreateAsset(material, TitleCoverFeatherMaterialPath);
            }
            else if (material.shader != shader) material.shader = shader;
            material.SetFloat("_Feather", 0.065f);
            EditorUtility.SetDirty(material);
            return material;
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
                interaction.uiAimAssistRadius = 0.032f;
            }

            var bootstrap = rig.GetComponent<PicoRigBootstrap>();
            bootstrap.xrCamera = xrCamera;
            bootstrap.picoManager = rig.GetComponent<PXR_Manager>();
            bootstrap.designStart = designStart;
        }

        private static void CreateArtworkButton(string name, Transform parent, Vector2 artSize, Rect uv,
            StructureTitleController title, StructureTitleButtonAction action, Color accent)
        {
            var size = new Vector2(uv.width * artSize.x, uv.height * artSize.y);
            var position = new Vector2((uv.center.x - 0.5f) * artSize.x, (0.5f - uv.center.y) * artSize.y);
            var button = Panel(name, parent, Color.clear);
            SetRect(button.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, position, size);
            var collider = button.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x + 14f, size.y + 14f, 16f);
            var behavior = button.gameObject.AddComponent<StructureTitleButton>();
            behavior.title = title;
            behavior.action = action;
            behavior.graphic = button;
            behavior.normalColor = Color.clear;
            behavior.highlightedColor = new Color(accent.r, accent.g, accent.b, 0.12f);
            behavior.focusColor = accent;
            var underline = Panel("FocusUnderline", button.transform, Color.clear);
            SetRect(underline.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -5f), new Vector2(size.x - 12f, 3f));
            underline.raycastTarget = false;
            behavior.focusLine = underline.rectTransform;
            behavior.focusGraphic = underline;
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

        private static Sprite LoadPanelSprite(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                // Keep the full artwork for desktop; Android must not load the
                // large uncompressed sprite before its first XR frame. That
                // stalled scene loading on PICO Emulator 0.13.0.
                var android = importer.GetPlatformTextureSettings("Android");
                android.overridden = true;
                android.maxTextureSize = 2048;
                android.format = TextureImporterFormat.ETC2_RGBA8;
                android.textureCompression = TextureImporterCompression.CompressedHQ;
                android.compressionQuality = 100;
                android.crunchedCompression = false;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
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
