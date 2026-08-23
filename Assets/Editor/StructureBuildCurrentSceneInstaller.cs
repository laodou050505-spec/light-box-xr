using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ByteDance.PICO.XR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace StructureBuild.Editor
{
    [InitializeOnLoad]
    public static class StructureBuildCurrentSceneInstaller
    {
        private const string ScenePath = "Assets/Scenes/StructureBuild.unity";
        private const string InstallRootName = "GameplayPresentation_v2";
        private const string FontAssetPath = "Assets/UI/StructureBuildChineseSDF.asset";
        private const string BottomBarSkinPath = "Assets/UI/Generated/Prepared/StructureBuild_BottomBar.png";
        private const string ButtonSkinPath = "Assets/UI/Generated/Prepared/StructureBuild_Button.png";
        private const string CompletionPanelSkinPath = "Assets/UI/Generated/Prepared/StructureBuild_CompletionPanel.png";
        private const string LevelInfoPanelSkinPath = "Assets/UI/Generated/Prepared/StructureBuild_LevelInfoPanel.png";
        private const string SmokeStateKey = "StructureBuild.RuntimeSmokeState";
        // These two authored models are called out explicitly because their
        // motion is a visual design decision, not a geometry classification.
        private const string RotatingPodName = "tripo_convert_93c20735-cffb-4e55-a1d6-f07468a7be1e";
        private const string StaticStationName = "tripo_convert_7108f415-a864-4b34-ade0-80f0a7ae1b1b";
        private const string MeteorName = "tripo_convert_4f99b539-4d7b-4a67-a745-1517ac801736";
        private const string MeteorDuplicateName = "tripo_convert_4f99b539-4d7b-4a67-a745-1517ac801736 (1)";

        private static readonly Color Ink = new Color(0.012f, 0.035f, 0.055f, 0.94f);
        private static readonly Color Panel = new Color(0.018f, 0.075f, 0.105f, 0.94f);
        private static readonly Color Cyan = new Color(0.20f, 0.94f, 0.90f, 1f);
        private static readonly Color Ice = new Color(0.74f, 0.93f, 1f, 1f);
        private static readonly Color Amber = new Color(1f, 0.62f, 0.16f, 1f);
        private static readonly Color Muted = new Color(0.48f, 0.68f, 0.74f, 1f);

        [MenuItem("Structure Build/Reset Scene View To Current Layout")]
        public static void ResetSceneViewToCurrentLayout()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var puzzle = FindSceneObject("PuzzleTable_Editable")?.transform;
            if (puzzle == null) return;
            var renderers = puzzle.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            SceneView.lastActiveSceneView?.LookAt(bounds.center, Quaternion.Euler(30f, 135f, 0f),
                Mathf.Clamp(bounds.extents.magnitude * 2.4f, 5f, 36f));
            SceneView.RepaintAll();
            Debug.Log("STRUCTURE_SCENE_VIEW_RESET_OK: Scene view is focused on the current authored puzzle layout.");
        }

        [MenuItem("Structure Build/Restore Authored Light States From Latest Layout")]
        public static void RestoreAuthoredLightStatesFromLatestLayout()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var authoredLights = FindSceneObjects<Light>()
                .Where(light => light.GetComponentInParent<ProjectionLightingRig>() == null)
                .ToArray();
            // The previous installer disabled every non-projector Light. Only
            // repair that known state; never overwrite a light setup the user
            // has already enabled manually.
            if (authoredLights.Any(light => light.enabled))
            {
                Debug.Log("STRUCTURE_AUTHORED_LIGHTS_UNCHANGED: at least one authored light is already enabled; no light state was changed.");
                return;
            }

            var enabledByName = new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["Point Light"] = true,
                ["Point Light (1)"] = false,
                ["Point Light (2)"] = true,
                ["Point Light (3)"] = true,
                ["OverheadLightStrip_Editable"] = false
            };
            foreach (var light in authoredLights)
            {
                if (!enabledByName.TryGetValue(light.gameObject.name, out var enabled)) continue;
                light.enabled = enabled;
                EditorUtility.SetDirty(light);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("STRUCTURE_AUTHORED_LIGHTS_RESTORED: restored the latest user-authored light enabled states and will preserve them on future installs.");
        }

        private readonly struct TransformState
        {
            public readonly Transform parent;
            public readonly Vector3 localPosition;
            public readonly Quaternion localRotation;
            public readonly Vector3 localScale;

            public TransformState(Transform transform)
            {
                parent = transform.parent;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }
        }

        static StructureBuildCurrentSceneInstaller()
        {
            EditorApplication.delayCall += InstallWhenReady;
            EditorApplication.playModeStateChanged += HandleSmokeTestState;
        }

        [MenuItem("Structure Build/Install Gameplay Into Current Scene")]
        public static void InstallCurrentScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before installing Structure Build gameplay.");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var existingTransforms = CaptureExistingTransforms(scene);
            var root = FindSceneObject("STRUCTURE_BUILD_SCENE")?.transform;
            var puzzle = FindSceneObject("PuzzleTable_Editable")?.transform;
            var gameObject = FindSceneObject("GameSystems");
            var desktopCameraObject = FindSceneObject("DesktopCamera");
            if (root == null || puzzle == null || gameObject == null || desktopCameraObject == null)
            {
                throw new InvalidOperationException("Current scene is missing STRUCTURE_BUILD_SCENE, PuzzleTable_Editable, GameSystems, or DesktopCamera.");
            }

            RemoveMissingSceneScripts();
            var font = EnsureChineseFontAsset();
            var bottomBarSkin = LoadUiSkin(BottomBarSkinPath, new Vector4(42f, 42f, 42f, 42f));
            var buttonSkin = LoadUiSkin(ButtonSkinPath, new Vector4(22f, 22f, 22f, 22f));
            var completionPanelSkin = LoadUiSkin(CompletionPanelSkinPath, new Vector4(48f, 48f, 48f, 48f));
            var levelInfoPanelSkin = LoadUiSkin(LevelInfoPanelSkinPath, new Vector4(42f, 42f, 42f, 42f));
            var presentation = FindSceneObject(InstallRootName);
            if (presentation == null)
            {
                presentation = FindSceneObject("GameplayPresentation_v1");
                if (presentation != null) presentation.name = InstallRootName;
                else
                {
                    presentation = new GameObject(InstallRootName);
                    presentation.transform.SetParent(root, false);
                }
            }

            var game = GetOrAdd<StructureGameController>(gameObject);
            BindGameplayReferences(game, puzzle);
            var audio = GetOrAdd<StructureAudioController>(gameObject);
            ConfigureAudio(audio);
            game.audio = audio;
            foreach (var reparentedTransform in ConfigureProjectionPanelLayout(puzzle, game)) existingTransforms.Remove(reparentedTransform);
            DisableLegacyHud(gameObject, presentation);

            var orbit = GetOrAdd<DesktopCameraOrbit>(desktopCameraObject);
            orbit.target = puzzle;
            orbit.targetOffset = Vector3.up * 1.45f;
            orbit.minDistance = 1.5f;
            orbit.maxDistance = 40f;
            var cameraViews = GetOrAdd<StructureCameraViews>(desktopCameraObject);
            cameraViews.orbit = orbit;
            cameraViews.game = game;
            cameraViews.useFixedOverviewPose = true;
            cameraViews.overviewYaw = 45f;
            cameraViews.overviewPitch = 28f;
            cameraViews.overviewDistance = 10.40f;
            cameraViews.overviewFocusOffset = Vector3.zero;
            cameraViews.overviewDistanceScale = 0.42f;
            cameraViews.overviewDistanceOffset = -0.35f;
            var actions = GetOrAdd<StructureHUDController>(gameObject);
            actions.game = game;
            actions.cameraViews = cameraViews;
            actions.audio = audio;

            RemoveOwnedChild(presentation.transform, "DesktopHUD");
            RemoveOwnedChild(presentation.transform, "PicoWorldHUD");
            RemoveOwnedChild(presentation.transform, "PicoActionDock");
            RemoveOwnedChild(presentation.transform, "CompletionOverlay_Desktop");
            RemoveOwnedChild(presentation.transform, "CompletionOverlay_Pico");
            RemoveOwnedChild(presentation.transform, "GameplayInstructions_Desktop");
            RemoveOwnedChild(presentation.transform, "GameplayInstructions_Pico");
            RemoveOwnedChild(presentation.transform, "SpaceBackdrop");
            var designStart = ConfigureDesignPlayerStart(presentation.transform, puzzle, cameraViews);
            var instructions = GetOrAdd<GameplayInstructionsOverlay>(gameObject);
            instructions.audio = audio;
            // START belongs to the isolated title scene. This is only an
            // optional in-world refresher the player can reopen later.
            instructions.visibleAtStartup = false;
            EditorUtility.SetDirty(instructions);
            actions.instructions = instructions;
            CreateDesktopHud(presentation.transform, game, actions, font, bottomBarSkin, buttonSkin, completionPanelSkin, levelInfoPanelSkin);
            CreateWorldHud(presentation.transform, puzzle, designStart, game, actions, font, buttonSkin, completionPanelSkin, levelInfoPanelSkin);
            CreateGameplayInstructions(presentation.transform, puzzle, designStart, actions, instructions, font, buttonSkin);
            CreateSpaceBackdrop(presentation.transform, root);
            BindXRInteractions(game);
            BindPicoRig(designStart);
            ConfigureEnvironmentDrift(root, puzzle);
            ConfigureAuthoredPanelLightStrips();
            // The authored scene may contain user-owned light strips and point
            // lights. Preserve their enabled state and all light properties;
            // only the generated projector rigs are managed by the installer.
            PreserveAuthoredSceneLights();

            AssertExistingTransformsUnchanged(existingTransforms);
            EditorUtility.SetDirty(game);
            EditorUtility.SetDirty(actions);
            EditorUtility.SetDirty(cameraViews);
            EditorUtility.SetDirty(orbit);
            EditorUtility.SetDirty(designStart);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("STRUCTURE_BUILD_INSTALL_OK: world-space gameplay, shared desktop/XR design start, PICO controls, and space backdrop installed without changing existing model transforms.");
        }

        [MenuItem("Structure Build/Validate Installed Current Scene")]
        public static void ValidateInstalledCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = FindSceneObject("GameSystems")?.GetComponent<StructureGameController>();
            Require(game != null, "StructureGameController is missing.");
            Require(game.gridRoot != null, "GridRoot reference is missing.");
            Require(game.cubeSource != null, "CubeSource reference is missing.");
            Require(game.cubePrefab != null, "Puzzle cube prefab reference is missing.");
            Require(game.frontPanel != null && game.sidePanel != null, "Projection panel references are missing.");
            Require(Mathf.Abs(game.gridCubeFill - 0.80f) < 0.001f, "Placed cube fill must remain at the reduced 80% size.");
            var frontAssembly = FindSceneObject("FrontProjectionAssembly_Editable")?.transform;
            var sideAssembly = FindSceneObject("SideProjectionAssembly_Editable")?.transform;
            var frontModel = FindSceneObject("FrontProjectionModel_Editable")?.transform;
            var sideModel = FindSceneObject("SideProjectionModel_Editable")?.transform;
            Require(frontAssembly != null && sideAssembly != null && frontModel != null && sideModel != null &&
                    game.frontPanel.parent == frontAssembly && frontModel.parent == frontAssembly &&
                    game.sidePanel.parent == sideAssembly && sideModel.parent == sideAssembly &&
                    game.frontPanel.localPosition.sqrMagnitude < 0.000001f && game.sidePanel.localPosition.sqrMagnitude < 0.000001f,
                "Projection tiles and authored backing models are not grouped with their editable assemblies.");
            for (var x = 0; x < 4; x++)
            for (var z = 0; z < 4; z++)
                Require(game.gridRoot.Find($"GridMarker_{x}_{z}") != null, $"GridMarker_{x}_{z} is missing.");
            Require(FindSceneObject(InstallRootName) != null, "Gameplay presentation root is missing.");
            Require(FindSceneObject("DesktopHUD")?.GetComponent<StructureScreenHUD>() != null, "Desktop HUD is missing.");
            var picoWorldHud = FindSceneObject("PicoWorldHUD");
            var picoActionDock = FindSceneObject("PicoActionDock");
            var picoInstructions = FindSceneObject("GameplayInstructions_Pico");
            Require(picoWorldHud?.GetComponent<StructureScreenHUD>() != null, "PICO world console is missing.");
            Require(picoActionDock?.GetComponent<Canvas>() != null && picoActionDock.GetComponent<PicoHeadLockedCanvas>() != null &&
                    picoActionDock.GetComponent<PicoRuntimeCanvasVisibility>() != null,
                "PICO bottom action dock is missing its view-following world canvas.");
            Require(FindSceneObject("CompletionOverlay_Desktop")?.GetComponent<LevelCompleteOverlay>() != null, "Desktop completion overlay is missing.");
            var picoCompletionHost = FindSceneObject("CompletionOverlay_Pico");
            var picoCompletion = picoCompletionHost?.GetComponent<LevelCompleteOverlay>();
            var picoCompletionPresenter = picoCompletionHost?.GetComponent<CompletionPanelPresenter>();
            Require(picoCompletion != null && picoCompletionPresenter != null && picoCompletion.worldPresenter == picoCompletionPresenter,
                "PICO completion overlay or its front-of-player presenter is missing.");
            Require(FindSceneObject("GameSystems")?.GetComponent<StructureAudioController>() != null, "Structure audio controller is missing.");
            Require(FindSceneObject("GameSystems")?.GetComponent<GameplayInstructionsOverlay>() != null, "Gameplay instructions controller is missing.");
            Require(FindSceneObject("GameplayInstructions_Desktop") != null && picoInstructions != null, "Gameplay instructions canvases are missing.");
            var audio = FindSceneObject("GameSystems")?.GetComponent<StructureAudioController>();
            Require(audio != null && audio.musicClip != null && audio.grabClip != null && audio.placeClip != null &&
                    audio.rejectClip != null && audio.completeClip != null, "One or more required audio clips are not assigned.");
            var cameraViews = FindSceneObject("DesktopCamera")?.GetComponent<StructureCameraViews>();
            var designStart = FindSceneObject("DesignPlayerStart_Anchor")?.GetComponent<DesignPlayerStart>();
            var picoRig = FindSceneObject("XR Rig")?.GetComponent<PicoRigBootstrap>();
            Require(cameraViews != null, "Camera views are missing.");
            Require(designStart != null && designStart.focalAnchor == FindSceneObject("PuzzleTable_Editable")?.transform,
                "The shared DesignPlayerStart anchor is missing or not focused on the puzzle table.");
            Require(cameraViews.designStart == designStart && picoRig != null && picoRig.designStart == designStart,
                "Desktop Camera Views and PICO Rig must reference the same DesignPlayerStart anchor.");
            var picoHudFollower = picoWorldHud.GetComponent<PicoHeadLockedCanvas>();
            var picoDockFollower = picoActionDock.GetComponent<PicoHeadLockedCanvas>();
            Require(picoWorldHud.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace &&
                    picoInstructions.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace &&
                    picoHudFollower != null &&
                    picoWorldHud.GetComponent<PicoRuntimeCanvasVisibility>() != null &&
                    picoInstructions.GetComponent<PicoHeadLockedCanvas>() != null,
                "PICO HUD and instructions must be world-space canvases following the active player view.");
            Require(picoWorldHud.GetComponent<PicoRuntimeCanvasVisibility>()?.canvas == picoWorldHud.GetComponent<Canvas>() &&
                    picoActionDock.GetComponent<PicoRuntimeCanvasVisibility>()?.canvas == picoActionDock.GetComponent<Canvas>() &&
                    picoWorldHud.GetComponent<StructureScreenHUD>()?.showOnlyWhenXRActive == true &&
                    FindSceneObject("DesktopHUD")?.GetComponent<StructureScreenHUD>()?.hideWhenXRActive == true,
                "Desktop and PICO HUD routes must be mutually exclusive.");
            Require(picoActionDock.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace && picoDockFollower != null &&
                    picoHudFollower.viewOffset.x < -0.45f && picoHudFollower.viewOffset.x > -0.70f && picoHudFollower.viewOffset.y > 0.65f &&
                    Mathf.Abs(picoDockFollower.viewOffset.x) < 0.01f && picoDockFollower.viewOffset.y < -0.80f,
                "PICO bottom action dock must use a world-space canvas.");
            Require(picoCompletionHost.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace &&
                    picoCompletionHost.GetComponent<PicoHeadLockedCanvas>() != null &&
                    !picoCompletionHost.transform.IsChildOf(picoWorldHud.transform),
                "PICO completion card must be an independent world-space card following the active player view.");
            Require(FindSceneObject("SpaceBackdrop")?.GetComponent<SpaceBackdropController>() != null, "Space backdrop is missing.");
            Require(picoRig != null, "PICO rig bootstrap is missing.");
            Require(game.cubeSource.GetComponent<CubeSourceInteractable>() != null, "Cube source interaction is missing.");
            var xrInteractions = FindSceneObjects<WorldSpaceInteraction>();
            Require(xrInteractions.Length >= 2 && xrInteractions.All(item => item.rayOrigin != null && item.rayLine != null),
                "Both PICO controller world-space rays must be bound.");
            Require(FindSceneObjects<SpaceDriftMotion>().All(IsAuthorizedSpaceDecorMotion),
                "A fixed gameplay model has an unauthorized drift motion.");
            var spaceDecorRoots = FindSceneObjects<Transform>().Where(IsSpaceDecorRoot).ToArray();
            var staticSpaceDecorRoots = spaceDecorRoots.Where(IsStaticSpaceDecorRoot).ToArray();
            var movingSpaceDecorRoots = spaceDecorRoots.Where(root => !IsStaticSpaceDecorRoot(root)).ToArray();
            Require(staticSpaceDecorRoots.Length == 1 && staticSpaceDecorRoots[0].name == StaticStationName &&
                    staticSpaceDecorRoots[0].GetComponent<SpaceDriftMotion>() == null,
                "The large authored space structure must remain static.");
            Require(movingSpaceDecorRoots.Length > 0 && movingSpaceDecorRoots.All(root => root.GetComponent<SpaceDriftMotion>() != null),
                "The moving space decoration models are missing drift motion.");
            var rotatingPod = spaceDecorRoots.FirstOrDefault(root => root.name == RotatingPodName);
            Require(rotatingPod != null && rotatingPod.GetComponent<SpaceDriftMotion>() != null &&
                    !rotatingPod.GetComponent<SpaceDriftMotion>().subtlePlanetMotion,
                "The round pod must have visible rotation and drift motion.");
            var authoredMeteors = spaceDecorRoots.Where(root => root.name == MeteorName || root.name == MeteorDuplicateName).ToArray();
            Require(authoredMeteors.Length == 2 && authoredMeteors.All(root => root.GetComponent<SpaceDriftMotion>() != null &&
                                                                                !root.GetComponent<SpaceDriftMotion>().subtlePlanetMotion),
                "Both authored meteor models must have independent center rotation and gentle drift motion.");
            var spins = FindSceneObjects<InPlaceSpinMotion>();
            Require(spins.Length == 1 && spins[0].transform == game.cubeSource, "Only the authored source cube may spin.");
            var projectorRigs = FindSceneObjects<ProjectionLightingRig>();
            Require(projectorRigs.Length == 2 && projectorRigs.All(rig => rig.spotLight != null && rig.spotLight.enabled),
                "The two projector lighting rigs are missing or disabled.");
            var authoredStripRoots = FindSceneObjects<Transform>().Where(item => item.name.StartsWith("OverheadLightStripModel_Editable", StringComparison.Ordinal)).ToArray();
            Require(authoredStripRoots.Length == 4 && authoredStripRoots.All(root => root.GetComponent<AuthoredLightStripGlow>() != null),
                "The four authored panel light strips are missing.");
            // User-authored lights are allowed and must remain untouched. The
            // generated projector lights are validated separately above.
            Require(FindSceneObjects<Transform>().Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)) == 0,
                "The scene contains missing script components.");
            Require(CampaignData.Levels.Length == 12, "Campaign must contain exactly 12 sequential levels.");
            for (var index = 0; index < CampaignData.Levels.Length; index++)
            {
                var level = CampaignData.Levels[index];
                var cells = new HashSet<Vector3Int>(level.referenceSolution.Select(cell => cell.ToVector3Int()));
                Require(ProjectionRules.Evaluate(cells, level).complete, $"Level {index + 1:00} reference solution is invalid.");
            }
            Debug.Log($"STRUCTURE_BUILD_VALIDATE_OK: physical table gameplay, desktop/PICO-exclusive HUDs with fixed left-top readout and lower-centre action dock, shared DesignPlayerStart eye={designStart.DesignedEyePosition:F2}, 16 grid markers, 12 levels, PICO rays, static authored models, panel glow, and two projector lights.");
        }

        [MenuItem("Structure Build/Run Runtime Smoke Test")]
        public static void RunRuntimeSmokeTest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SessionState.SetString(SmokeStateKey, "requested");
            EditorApplication.EnterPlaymode();
        }

        private static void InstallWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += InstallWhenReady;
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath || FindSceneObject(InstallRootName) != null) return;
            try
            {
                InstallCurrentScene();
                ValidateInstalledCurrentScene();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void HandleSmokeTestState(PlayModeStateChange state)
        {
            var smokeState = SessionState.GetString(SmokeStateKey, string.Empty);
            if (state == PlayModeStateChange.EnteredPlayMode && smokeState == "requested")
            {
                try
                {
                    var game = UnityEngine.Object.FindAnyObjectByType<StructureGameController>();
                    var views = UnityEngine.Object.FindAnyObjectByType<StructureCameraViews>();
                    Require(game != null && views != null, "Runtime gameplay or camera views did not initialize.");
                    var designStart = UnityEngine.Object.FindAnyObjectByType<DesignPlayerStart>();
                    var picoRig = FindSceneObject("XR Rig")?.GetComponent<PicoRigBootstrap>();
                    Require(designStart != null && views.designStart == designStart && picoRig != null && picoRig.designStart == designStart,
                        "Runtime desktop and PICO setup are not sharing one DesignPlayerStart.");
                    views.SetOverview();
                    var expectedEye = designStart.DesignedEyePosition;
                    Require(Vector3.Distance(views.transform.position, expectedEye) < 0.02f,
                        $"Desktop opening camera is not derived from DesignPlayerStart. Camera={views.transform.position:F2}, expected={expectedEye:F2}.");
                    var worldConsole = FindSceneObject("PicoWorldHUD");
                    var worldInstructions = FindSceneObject("GameplayInstructions_Pico");
                    Require(worldConsole != null && worldInstructions != null &&
                            worldConsole.GetComponent<PicoHeadLockedCanvas>() != null && worldInstructions.GetComponent<PicoHeadLockedCanvas>() != null,
                        "PICO gameplay panels are not following the active player view at runtime.");
                    Require(worldConsole.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace &&
                            worldInstructions.GetComponent<Canvas>()?.renderMode == RenderMode.WorldSpace,
                        "PICO gameplay panels are not world-space at runtime.");
                    var xrInteractions = UnityEngine.Object.FindObjectsByType<WorldSpaceInteraction>(FindObjectsInactive.Include);
                    Require(xrInteractions.Length > 0 && xrInteractions.All(interaction => interaction.rayLine == null || !interaction.rayLine.enabled),
                        "PICO controller rays must stay hidden when no tracked controller is available.");
                    Require(game.CurrentLevelNumber == 1 && game.TotalLevelCount == 12, "Sequential campaign did not start at level 1 of 12.");
                    game.Hint();
                    Require(game.CurrentCubeCount == 1, "Hint did not place the first cube on the authored grid.");
                    game.Undo();
                    Require(game.CurrentCubeCount == 0, "Undo did not restore the previous structure.");
                    var dragCell = new Vector3Int(0, 0, 0);
                    var dragRay = new Ray(game.CellToWorld(dragCell) + game.gridRoot.up * 4f, -game.gridRoot.up);
                    Require(game.BeginDragFromSource(dragRay), "Cube source did not begin a visible drag.");
                    var forgivingRay = new Ray(game.CellToWorld(dragCell) + game.gridRoot.up * 4f + game.gridRoot.right * game.cellSize * 0.78f, -game.gridRoot.up);
                    Require(game.UpdateDrag(forgivingRay) && game.CurrentDropCell == dragCell, "Outside-edge magnetic snap did not resolve the intended ground cell.");
                    var grazingOrigin = game.CellToWorld(dragCell) - game.gridRoot.forward * 3f + game.gridRoot.up * 0.05f;
                    var grazingRay = new Ray(grazingOrigin, game.gridRoot.forward);
                    Require(game.UpdateDrag(grazingRay) && game.CurrentDropCell == dragCell, "Near-horizontal controller ray did not resolve the intended drop cell.");
                    Require(game.UpdateDrag(dragRay) && game.CurrentDropCell == dragCell, "Drag did not resolve the authored ground cell.");
                    Require(GameObject.Find("DraggedCubePreview_Runtime") != null && GameObject.Find("DropIndicator_Runtime")?.activeInHierarchy == true,
                        "Dragged cube preview or drop indicator is missing.");
                    Require(game.CommitDrag(dragRay) && game.CurrentCubeCount == 1, "Releasing the dragged cube did not place it.");
                    var placedCube = UnityEngine.Object.FindAnyObjectByType<PuzzleCubeInteractable>();
                    var sourceMesh = game.cubeSource.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
                    var placedMesh = placedCube?.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
                    var sourceMaterial = game.cubeSource.GetComponentInChildren<Renderer>(true)?.sharedMaterial;
                    var placedMaterial = placedCube?.GetComponentInChildren<Renderer>(true)?.sharedMaterial;
                    Require(placedCube != null && sourceMesh != null && placedMesh == sourceMesh && placedMaterial == sourceMaterial,
                        "Placed cubes are not preserving the scene-authored cube mesh and material.");
                    game.Undo();
                    Require(game.CurrentCubeCount == 0, "Undo did not restore the drag placement.");
                    views.SetFront();
                    Require(views.CurrentViewName == "正面", "Front camera view failed.");
                    var frontViewDistance = views.orbit != null ? views.orbit.distance : float.NaN;
                    views.SetSide();
                    Require(views.CurrentViewName == "侧面", "Side camera view failed.");
                    var sideViewDistance = views.orbit != null ? views.orbit.distance : float.NaN;
                    Require(!float.IsNaN(frontViewDistance) && Mathf.Abs(frontViewDistance - sideViewDistance) < 0.001f,
                        $"Front and side camera distances must match. Front={frontViewDistance:F3}, Side={sideViewDistance:F3}.");
                    views.SetOverview();
                    Require(views.CurrentViewName == "概览", "Overview camera view failed.");
                    var overviewViewDistance = views.orbit != null ? views.orbit.distance : float.NaN;
                    Require(!float.IsNaN(overviewViewDistance) && Mathf.Abs(overviewViewDistance - designStart.DerivedDesktopDistance) < 0.02f,
                        $"Opening overview camera distance is not derived from DesignPlayerStart: {overviewViewDistance:F3}.");
                    Debug.Log($"STRUCTURE_CAMERA_DESIGN_START=eye:{expectedEye:F2}; desktopDistance:{overviewViewDistance:F3}; yaw:{designStart.yaw:F1}; eyeHeight:{designStart.eyeHeight:F2}");
                    for (var hintIndex = 0; hintIndex < 32 && !game.IsLevelCompleted; hintIndex++) game.Hint();
                    Require(game.IsLevelCompleted && game.CurrentLevelNumber == 1, "Completing a level did not pause on its completion state.");
                    var desktopCompletion = FindSceneObject("CompletionOverlay_Desktop")?.GetComponent<LevelCompleteOverlay>();
                    var picoCompletion = FindSceneObject("CompletionOverlay_Pico")?.GetComponent<LevelCompleteOverlay>();
                    Require(desktopCompletion != null && picoCompletion != null && desktopCompletion.panelRoot.activeSelf && picoCompletion.panelRoot.activeSelf,
                        "Completion overlays did not appear after the level was solved.");
                    var completionPresenter = picoCompletion.worldPresenter;
                    var activeView = Camera.main;
                    Require(completionPresenter != null && activeView != null && picoCompletion.GetComponent<PicoHeadLockedCanvas>() != null,
                        "PICO completion card cannot resolve the active player view.");
                    var completionOffset = picoCompletion.transform.position - activeView.transform.position;
                    var horizontalViewForward = activeView.transform.forward;
                    var horizontalCompletionOffset = completionOffset;
                    horizontalViewForward.y = 0f;
                    horizontalCompletionOffset.y = 0f;
                    var completionFollower = picoCompletion.GetComponent<PicoHeadLockedCanvas>();
                    var expectedCompletionDistance = completionFollower.distance;
                    Require(Mathf.Abs(completionOffset.magnitude - expectedCompletionDistance) < 0.40f &&
                            Vector3.Dot(horizontalViewForward.normalized, horizontalCompletionOffset.normalized) > 0.90f &&
                            Vector3.Dot(picoCompletion.transform.forward, activeView.transform.forward) > 0.90f,
                        "PICO completion card did not face the player with its readable canvas side.");
                    game.ContinueAfterCompletion();
                    Require(game.CurrentLevelNumber == 2 && !game.IsLevelCompleted, "Next-level action did not advance the sequential campaign.");
                    var backdrop = UnityEngine.Object.FindAnyObjectByType<SpaceBackdropController>();
                    Require(backdrop != null && GameObject.Find("Stars_Twinkle") != null && GameObject.Find("Meteors") != null && GameObject.Find("MeteorShower") != null,
                        "Runtime starfield, meteors, or meteor shower did not initialize.");
                    Require(backdrop.starCount >= 700 && backdrop.meteorCount >= 6 && backdrop.meteorShowerStreakCount >= 4 && backdrop.meteorShowerStreakCount <= 6,
                        "Space backdrop density is below the intended starfield and meteor-shower presentation.");
                    var driftMotions = UnityEngine.Object.FindObjectsByType<SpaceDriftMotion>(FindObjectsInactive.Include);
                    Require(driftMotions.All(IsAuthorizedSpaceDecorMotion),
                        "A fixed gameplay model still has drift motion at runtime.");
                    var visibleDrifts = driftMotions.Where(motion => !motion.subtlePlanetMotion &&
                                                                      motion.gameObject.name != RotatingPodName &&
                                                                      motion.gameObject.name != MeteorName &&
                                                                      motion.gameObject.name != MeteorDuplicateName).ToArray();
                    Require(visibleDrifts.Length >= 8 && visibleDrifts.All(motion =>
                                motion.rotationDegreesPerSecond.magnitude >= 5f && motion.swayAmplitude.y >= 0.30f),
                        "Non-planet space models are not configured with clearly visible drift and self rotation.");
                    var staticStation = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                        .FirstOrDefault(item => item.name == StaticStationName);
                    Require(staticStation != null && staticStation.GetComponent<SpaceDriftMotion>() == null,
                        "The large authored space structure is moving at runtime.");
                    var rotatingPod = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                        .FirstOrDefault(item => item.name == RotatingPodName);
                    Require(rotatingPod != null && rotatingPod.GetComponent<SpaceDriftMotion>() != null &&
                            !rotatingPod.GetComponent<SpaceDriftMotion>().subtlePlanetMotion,
                        "The round pod is not visibly rotating at runtime.");
                    var runtimeMeteors = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                        .Where(item => item.name == MeteorName || item.name == MeteorDuplicateName).ToArray();
                    Require(runtimeMeteors.Length == 2 && runtimeMeteors.All(item => item.GetComponent<SpaceDriftMotion>() != null &&
                                                                                     !item.GetComponent<SpaceDriftMotion>().subtlePlanetMotion &&
                                                                                     item.GetComponent<SpaceDriftMotion>().rotationDegreesPerSecond.magnitude >= 4.5f &&
                                                                                     item.GetComponent<SpaceDriftMotion>().swayAmplitude.y >= 0.08f),
                        "Both authored meteor models are missing runtime center drift motion.");
                    Require(UnityEngine.Object.FindObjectsByType<PanelEdgeGlow>(FindObjectsInactive.Include).Length == 0,
                        "A procedural panel glow is still covering the four authored light-strip models.");
                    var runtimeStrips = UnityEngine.Object.FindObjectsByType<AuthoredLightStripGlow>(FindObjectsInactive.Include);
                    Require(runtimeStrips.Length == 4, "The four authored light strips are missing their runtime emission.");
                    Require(game.cubeSource.GetComponent<InPlaceSpinMotion>() != null,
                        "The authored source cube is not configured to spin in place.");
                    var projectorRigs = UnityEngine.Object.FindObjectsByType<ProjectionLightingRig>(FindObjectsInactive.Include);
                    Require(projectorRigs.Length == 2 && projectorRigs.All(rig => rig.spotLight != null && rig.spotLight.enabled && rig.beam != null && rig.lensGlow != null),
                        "The projector beams or projector-only lights failed at runtime.");
                    SessionState.SetString(SmokeStateKey, "passed");
                    Debug.Log("STRUCTURE_BUILD_RUNTIME_OK: world-space tabletop gameplay, shared desktop/XR design start, static authored models, one spinning source cube, panel glow, two projector beams, stars, and meteors.");
                }
                catch (Exception exception)
                {
                    SessionState.SetString(SmokeStateKey, "failed");
                    Debug.LogException(exception);
                }
                EditorApplication.ExitPlaymode();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode || (smokeState != "passed" && smokeState != "failed")) return;
            var succeeded = smokeState == "passed";
            SessionState.EraseString(SmokeStateKey);
            if (Application.isBatchMode) EditorApplication.Exit(succeeded ? 0 : 1);
        }

        private static void BindGameplayReferences(StructureGameController game, Transform puzzle)
        {
            game.gridRoot = FindSceneObject("GridRoot")?.transform ?? puzzle.Find("GridRoot");
            game.cubeSource = FindSceneObject("CubeSource_Editable")?.transform;
            game.frontPanel = FindSceneObject("FrontProjectionPanel")?.transform;
            game.sidePanel = FindSceneObject("SideProjectionPanel")?.transform;
            game.cubePrefab ??= AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PuzzleCube.prefab");
            game.cellSize = MeasureCellSize(game.gridRoot, 0.34f);
            game.usePlacedGroundCells = true;
            game.groundCellNamePrefix = "WorkbenchTop_Editable";
            game.usePlacedGridMarkers = true;
            game.fitCubeToPrefabBounds = true;
            game.gridCubeFill = 0.80f;
            Require(game.gridRoot != null && game.cubeSource != null && game.frontPanel != null && game.sidePanel != null && game.cubePrefab != null,
                "Unable to bind the placed grid, cube source, projection panels, or cube prefab.");
            GetOrAdd<CubeSourceInteractable>(game.cubeSource.gameObject);
            if (game.cubeSource.GetComponentInChildren<Collider>(true) == null) game.cubeSource.gameObject.AddComponent<BoxCollider>();
        }

        private static DesignPlayerStart ConfigureDesignPlayerStart(
            Transform presentation,
            Transform puzzle,
            StructureCameraViews cameraViews)
        {
            var anchor = presentation.Find("DesignPlayerStart_Anchor")?.GetComponent<DesignPlayerStart>();
            if (anchor == null)
            {
                var anchorObject = new GameObject("DesignPlayerStart_Anchor");
                anchorObject.transform.SetParent(presentation, false);
                anchor = anchorObject.AddComponent<DesignPlayerStart>();
            }

            // The table centre is the common spatial landmark for both the
            // editor Game View and the PICO player.  The selected values
            // reproduce the existing 45-degree overview as a real standing
            // viewpoint around this fixed tabletop.
            anchor.focalAnchor = puzzle;
            anchor.focalOffset = new Vector3(0f, 1.67f, 0f);
            anchor.yaw = 45f;
            anchor.viewingDistance = 9.18f;
            anchor.eyeHeight = 7.49f;
            anchor.fallbackHeadLocalPosition = Vector3.up * 1.60f;
            anchor.SynchronizeAnchor();
            cameraViews.designStart = anchor;
            return anchor;
        }

        private static Transform[] ConfigureProjectionPanelLayout(Transform puzzle, StructureGameController game)
        {
            var frontModel = FindSceneObject("FrontProjectionModel_Editable")?.transform;
            var sideModel = FindSceneObject("SideProjectionModel_Editable")?.transform;
            Require(frontModel != null && sideModel != null, "Authored front or side projection backing model is missing.");

            ConfigureProjectionAssembly(puzzle, "FrontProjectionAssembly_Editable", game.frontPanel, frontModel);
            ConfigureProjectionAssembly(puzzle, "SideProjectionAssembly_Editable", game.sidePanel, sideModel);
            return new[] { game.frontPanel, game.sidePanel, frontModel, sideModel };
        }

        private static void ConfigureProjectionAssembly(
            Transform puzzle,
            string assemblyName,
            Transform tiles,
            Transform backingModel)
        {
            var assembly = FindSceneObject(assemblyName)?.transform;
            if (assembly == null)
            {
                assembly = new GameObject(assemblyName).transform;
                assembly.SetParent(puzzle, false);
            }

            ReparentWithoutMoving(tiles, assembly);
            ReparentWithoutMoving(backingModel, assembly);
        }

        private static void ReparentWithoutMoving(Transform child, Transform parent)
        {
            if (child.parent == parent) return;
            var position = child.position;
            var rotation = child.rotation;
            var scale = child.lossyScale;
            child.SetParent(parent, true);
            Require(Vector3.Distance(child.position, position) < 0.00001f &&
                    Quaternion.Angle(child.rotation, rotation) < 0.001f &&
                    Vector3.Distance(child.lossyScale, scale) < 0.00001f,
                $"Grouping moved authored projection object: {child.name}.");
        }

        private static void DisableLegacyHud(GameObject gameObject, GameObject presentation)
        {
            foreach (Transform child in gameObject.transform)
            {
                if (child.gameObject != presentation && child.name == "WorldSpaceHUD") child.gameObject.SetActive(false);
            }
        }

        private static void ConfigureAudio(StructureAudioController audio)
        {
            audio.musicClip = LoadAudioClip("Environment_space_atmo-glued.mp3");
            audio.uiHoverClip = LoadAudioClip("scifi_ui_soft_02.mp3");
            audio.uiClickClip = LoadAudioClip("scifi_ui_soft_03.mp3");
            audio.grabClip = LoadAudioClip("scifi_ui_hard_03.mp3");
            audio.placeClip = LoadAudioClip("scifi_ui_hard_03.mp3");
            audio.rejectClip = LoadAudioClip("scifi_neg_soft_02.mp3");
            audio.hintClip = LoadAudioClip("scifi_ui_soft_02.mp3");
            audio.undoClip = LoadAudioClip("bio_ui_soft_02.mp3");
            audio.resetClip = LoadAudioClip("bio_ui_soft_02.mp3");
            audio.completeClip = LoadAudioClip("scifi_pos_soft_01.mp3");
            audio.continueClip = LoadAudioClip("scifi_pos_soft_01.mp3");
            audio.musicVolume = 0.18f;
            audio.effectsVolume = 0.52f;
            EditorUtility.SetDirty(audio);
        }

        private static AudioClip LoadAudioClip(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{fileName}");
        }

        private static void CreateGameplayInstructions(
            Transform parent,
            Transform puzzle,
            DesignPlayerStart designStart,
            StructureHUDController actions,
            GameplayInstructionsOverlay controller,
            TMP_FontAsset font,
            Sprite buttonSkin)
        {
            var desktop = CreateRect("GameplayInstructions_Desktop", parent);
            var desktopCanvas = desktop.AddComponent<Canvas>();
            desktopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            desktopCanvas.sortingOrder = 160;
            var desktopScaler = desktop.AddComponent<CanvasScaler>();
            desktopScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            desktopScaler.referenceResolution = new Vector2(1920f, 1080f);
            desktopScaler.matchWidthOrHeight = 0.5f;
            desktop.AddComponent<GraphicRaycaster>();
            var desktopLayer = CreateRect("Layer", desktop.transform);
            SetAnchors(desktopLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var desktopDimmer = CreatePanel("Dimmer", desktopLayer.transform, new Color(0.002f, 0.009f, 0.018f, 0.82f));
            SetAnchors(desktopDimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var desktopCard = CreatePanel("Card", desktopLayer.transform, new Color(0.012f, 0.08f, 0.11f, 0.985f));
            SetAnchors(desktopCard.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 470f), new Vector2(0.5f, 0.5f));
            var desktopAccent = CreatePanel("Accent", desktopCard.transform, Cyan);
            SetAnchors(desktopAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 6f), new Vector2(0.5f, 1f));
            var desktopEyebrow = CreateText("Eyebrow", desktopCard.transform, font, "STRUCTURE ARCHIVE  /  CONTROL REMINDER", 15f, Amber, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(desktopEyebrow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(0f, 26f), new Vector2(0.5f, 1f));
            var desktopTitle = CreateText("Title", desktopCard.transform, font, "玩法说明", 36f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(desktopTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -76f), new Vector2(0f, 58f), new Vector2(0.5f, 1f));
            var desktopBody = CreateText("Body", desktopCard.transform, font,
                "1  从方块源抓取结构方块\n2  拖到 4 × 4 棋盘，松手后自动吸附\n3  同时匹配正面与侧面投影提示\n4  可用提示、撤回、重置；关卡按顺序推进",
                20f, Muted, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchors(desktopBody.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(92f, -158f), new Vector2(-184f, 150f), new Vector2(0.5f, 1f));
            var desktopButton = CreatePanel("StartButton", desktopCard.transform, new Color(0.02f, 0.27f, 0.29f, 1f));
            ApplyUiSkin(desktopButton, buttonSkin);
            SetAnchors(desktopButton.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(280f, 68f), new Vector2(0.5f, 0f));
            var desktopButtonBehaviour = desktopButton.gameObject.AddComponent<StructureScreenButton>();
            desktopButtonBehaviour.hud = actions;
            desktopButtonBehaviour.action = StructureUIButtonAction.DismissInstructions;
            desktopButtonBehaviour.background = desktopButton;
            desktopButtonBehaviour.normalColor = Color.white;
            desktopButtonBehaviour.hoverColor = new Color(0.78f, 1f, 1f, 1f);
            var desktopButtonLabel = CreateText("Label", desktopButton.transform, font, "知道了", 24f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(desktopButtonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            var pico = CreateRect("GameplayInstructions_Pico", parent);
            // The onboarding card deliberately occupies the central comfort
            // zone while it is visible, so its copy can be read inside a
            // headset without leaning toward the board.
            pico.GetComponent<RectTransform>().sizeDelta = new Vector2(1320f, 820f);
            ConfigureViewLockedCanvas(pico, 1.72f, 0.00130f, new Vector2(0f, -0.02f));
            var picoCanvas = pico.AddComponent<Canvas>();
            picoCanvas.renderMode = RenderMode.WorldSpace;
            picoCanvas.sortingOrder = 140;
            pico.AddComponent<GraphicRaycaster>();
            var picoPanel = pico.AddComponent<Image>();
            picoPanel.color = new Color(0.008f, 0.04f, 0.065f, 0.97f);
            var picoLayer = CreateRect("Layer", pico.transform);
            SetAnchors(picoLayer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var picoAccent = CreatePanel("Accent", picoLayer.transform, Cyan);
            SetAnchors(picoAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 8f), new Vector2(0.5f, 1f));
            var picoTitle = CreateText("Title", picoLayer.transform, font, "操作提示", 54f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(picoTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(0f, 66f), new Vector2(0.5f, 1f));
            var picoBody = CreateText("Body", picoLayer.transform, font,
                "从方块源抓取方块，拖到 4 × 4 棋盘\n松手后会自动吸附到合法格\n同时匹配正面与侧面投影提示\n提示、撤回、重置可随时使用\n关卡会按顺序推进",
                37f, Muted, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchors(picoBody.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(98f, -184f), new Vector2(-196f, 268f), new Vector2(0.5f, 1f));
            var picoButton = CreatePanel("StartButton", picoLayer.transform, new Color(0.02f, 0.27f, 0.29f, 1f));
            ApplyUiSkin(picoButton, buttonSkin);
            SetAnchors(picoButton.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(430f, 116f), new Vector2(0.5f, 0f));
            var picoCollider = picoButton.gameObject.AddComponent<BoxCollider>();
            picoCollider.size = new Vector3(430f, 116f, 10f);
            var picoButtonBehaviour = picoButton.gameObject.AddComponent<StructureUIButton>();
            picoButtonBehaviour.hud = actions;
            picoButtonBehaviour.action = StructureUIButtonAction.DismissInstructions;
            picoButtonBehaviour.backgroundGraphic = picoButton;
            picoButtonBehaviour.normalColor = Color.white;
            picoButtonBehaviour.highlightedColor = new Color(0.78f, 1f, 1f, 1f);
            var picoButtonLabel = CreateText("Label", picoButton.transform, font, "知道了", 38f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(picoButtonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            controller.desktopPanelRoot = desktopLayer.gameObject;
            controller.picoPanelRoot = picoLayer.gameObject;
            controller.desktopCanvas = desktopCanvas;
            controller.picoCanvas = picoCanvas;
            controller.audio = FindSceneObject("GameSystems")?.GetComponent<StructureAudioController>();
            desktop.SetActive(true);
            pico.SetActive(true);
        }

        private static void CreateDesktopHud(
            Transform parent,
            StructureGameController game,
            StructureHUDController actions,
            TMP_FontAsset font,
            Sprite bottomBarSkin,
            Sprite buttonSkin,
            Sprite completionPanelSkin,
            Sprite levelInfoPanelSkin)
        {
            var root = CreateRect("DesktopHUD", parent);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var info = CreatePanel("MissionReadout", root.transform, Panel);
            ApplyUiSkin(info, levelInfoPanelSkin);
            SetAnchors(info.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(510f, 186f), new Vector2(0f, 1f));
            var accent = CreatePanel("Accent", info.transform, Amber);
            SetAnchors(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(5f, 0f), new Vector2(0f, 0.5f));

            var level = CreateText("Level", info.transform, font, "结构档案 01 / 12", 25f, Ice, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetAnchors(level.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -12f), new Vector2(-42f, 38f), new Vector2(0f, 1f));
            var rule = CreateText("Rule", info.transform, font, "分析两面投影，复原空间结构", 16f, Muted, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchors(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -53f), new Vector2(-42f, 26f), new Vector2(0f, 1f));
            var status = CreateText("Status", info.transform, font, "等待结构输入", 17f, Amber, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchors(status.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -91f), new Vector2(-42f, 28f), new Vector2(0f, 1f));
            var cube = CreateText("CubeCount", info.transform, font, "结构单元 0 / 3", 15f, Cyan, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchors(cube.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(22f, 19f), new Vector2(-8f, 24f), new Vector2(0f, 0f));
            var view = CreateText("View", info.transform, font, "视角 概览", 15f, Cyan, FontStyles.Normal, TextAlignmentOptions.Right);
            SetAnchors(view.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(8f, 19f), new Vector2(-20f, 24f), new Vector2(0f, 0f));

            var dock = CreatePanel("ActionDock", root.transform, Ink);
            ApplyUiSkin(dock, bottomBarSkin);
            SetAnchors(dock.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(800f, 70f), new Vector2(0.5f, 0f));
            var horizontal = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(10, 10, 10, 10);
            horizontal.spacing = 8f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;
            CreateScreenButton(dock.transform, font, "提示", StructureUIButtonAction.Hint, actions, Amber, buttonSkin);
            CreateScreenButton(dock.transform, font, "撤回", StructureUIButtonAction.Undo, actions, Cyan, buttonSkin);
            CreateScreenButton(dock.transform, font, "重置", StructureUIButtonAction.Reset, actions, new Color(0.92f, 0.38f, 0.26f, 1f), buttonSkin);
            CreateScreenButton(dock.transform, font, "概览", StructureUIButtonAction.Overview, actions, Ice, buttonSkin);
            CreateScreenButton(dock.transform, font, "正面", StructureUIButtonAction.Front, actions, Ice, buttonSkin);
            CreateScreenButton(dock.transform, font, "侧面", StructureUIButtonAction.Side, actions, Ice, buttonSkin);

            CreateDesktopCompletionOverlay(root.transform, game, actions, font, buttonSkin, completionPanelSkin);

            var hud = root.AddComponent<StructureScreenHUD>();
            hud.game = game;
            hud.actions = actions;
            hud.canvas = canvas;
            hud.hideWhenXRActive = true;
            hud.levelText = level;
            hud.ruleText = rule;
            hud.statusText = status;
            hud.cubeText = cube;
            hud.viewText = view;
            EnsureEventSystem(parent);
        }

        private static void CreateWorldHud(
            Transform parent,
            Transform puzzle,
            DesignPlayerStart designStart,
            StructureGameController game,
            StructureHUDController actions,
            TMP_FontAsset font,
            Sprite buttonSkin,
            Sprite completionPanelSkin,
            Sprite levelInfoPanelSkin)
        {
            var root = CreateRect("PicoWorldHUD", parent);
            // The mission panel occupies the upper-left HUD field. It is
            // intentionally large enough for a headset read at a relaxed
            // distance, while keeping the physical board unobstructed.
            // Keep the diagnostic readout compact: the physical target cards
            // remain the player's focal point.  The old 1160 x 580 layout
            // left a large unused lower field in the headset view.
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(940f, 390f);
            // Keep the diagnostic board above the physical projection cards:
            // it belongs in the upper-left comfort field, not across the
            // puzzle's actual front/side prompts.  Pull it a little inward
            // too, so its full left edge stays inside the headset view.
            ConfigureViewLockedCanvas(root, 1.68f, 0.00116f, new Vector2(-0.66f, 0.88f));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 80;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<PicoRuntimeCanvasVisibility>().canvas = canvas;

            var info = CreatePanel("MissionReadout", root.transform, Panel);
            ApplyUiSkin(info, levelInfoPanelSkin);
            SetAnchors(info.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(896f, 326f), new Vector2(0f, 1f));
            var accent = CreatePanel("Accent", info.transform, Amber);
            SetAnchors(accent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(5f, 0f), new Vector2(0f, 0.5f));
            // Four concise, evenly spaced information lines use the full
            // panel instead of leaving a decorative but empty lower half.
            var level = CreateText("Level", info.transform, font, "结构档案 01 / 12", 38f, Ice, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            SetAnchors(level.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -18f), new Vector2(-42f, 52f), new Vector2(0f, 1f));
            var rule = CreateText("Rule", info.transform, font, "分析两面投影，复原空间结构", 25f, Muted, FontStyles.Normal, TextAlignmentOptions.Left);
            SetAnchors(rule.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -72f), new Vector2(-42f, 36f), new Vector2(0f, 1f));
            var status = CreateText("Status", info.transform, font, "等待结构输入", 28f, Amber, FontStyles.Bold, TextAlignmentOptions.Left);
            SetAnchors(status.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -122f), new Vector2(-42f, 38f), new Vector2(0f, 1f));
            var cube = CreateText("CubeCount", info.transform, font, "结构单元 0 / 3", 25f, Cyan, FontStyles.Bold, TextAlignmentOptions.Left);
            SetAnchors(cube.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(60f, 24f), new Vector2(-8f, 34f), new Vector2(0f, 0f));
            var view = CreateText("View", info.transform, font, "视角  概览", 25f, Cyan, FontStyles.Bold, TextAlignmentOptions.Right);
            SetAnchors(view.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(12f, 24f), new Vector2(-34f, 34f), new Vector2(0f, 0f));

            var dockRoot = CreateRect("PicoActionDock", parent);
            dockRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(1600f, 190f);
            // A wide lower-centre strip matches the designed XR composition
            // and provides touch targets large enough for a controller ray.
            // The action dock is deliberately low enough to leave the board
            // and both projection prompts unobscured at the natural gaze.
            ConfigureViewLockedCanvas(dockRoot, 1.70f, 0.00125f, new Vector2(0f, -1.14f));
            var dockCanvas = dockRoot.AddComponent<Canvas>();
            dockCanvas.renderMode = RenderMode.WorldSpace;
            dockCanvas.sortingOrder = 100;
            dockRoot.AddComponent<GraphicRaycaster>();
            dockRoot.AddComponent<PicoRuntimeCanvasVisibility>().canvas = dockCanvas;
            var dock = CreatePanel("ActionDock", dockRoot.transform, Ink);
            ApplyUiSkin(dock, buttonSkin);
            SetAnchors(dock.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            // Keep the whole six-action loop available in XR, matching the
            // desktop and Three.js prototype: hint, undo, reset and the three
            // deliberate camera views.  The compressed but unwarped buttons
            // fit in one lower-centre panel inside the PICO comfort frame.
            const float buttonWidth = 222f;
            const float buttonHeight = 108f;
            CreateWorldButton(dock.transform, font, "提示", StructureUIButtonAction.Hint, actions, new Vector2(-590f, 0f), Amber, buttonSkin, new Vector2(buttonWidth, buttonHeight));
            CreateWorldButton(dock.transform, font, "撤回", StructureUIButtonAction.Undo, actions, new Vector2(-354f, 0f), Cyan, buttonSkin, new Vector2(buttonWidth, buttonHeight));
            CreateWorldButton(dock.transform, font, "重置", StructureUIButtonAction.Reset, actions, new Vector2(-118f, 0f), new Color(0.92f, 0.38f, 0.26f, 1f), buttonSkin, new Vector2(buttonWidth, buttonHeight));
            CreateWorldButton(dock.transform, font, "概览", StructureUIButtonAction.Overview, actions, new Vector2(118f, 0f), Ice, buttonSkin, new Vector2(buttonWidth, buttonHeight));
            CreateWorldButton(dock.transform, font, "正面", StructureUIButtonAction.Front, actions, new Vector2(354f, 0f), Ice, buttonSkin, new Vector2(buttonWidth, buttonHeight));
            CreateWorldButton(dock.transform, font, "侧面", StructureUIButtonAction.Side, actions, new Vector2(590f, 0f), Ice, buttonSkin, new Vector2(buttonWidth, buttonHeight));
            // The completion card sits at the centre of the user's view and
            // follows head movement so the next-level action is never lost.
            CreatePicoCompletionOverlay(parent, designStart, game, actions, font, buttonSkin, completionPanelSkin);

            var hud = root.AddComponent<StructureScreenHUD>();
            hud.game = game;
            hud.actions = actions;
            hud.canvas = canvas;
            hud.hideWhenXRActive = false;
            hud.showOnlyWhenXRActive = true;
            hud.levelText = level;
            hud.ruleText = rule;
            hud.statusText = status;
            hud.cubeText = cube;
            hud.viewText = view;
            game.levelLabel = level;
            game.statusLabel = status;
            game.cubeCountLabel = cube;
            actions.viewLabel = view;
            actions.messageLabel = status;
        }

        private static void CreateSpaceBackdrop(Transform parent, Transform sceneRoot)
        {
            var backdrop = new GameObject("SpaceBackdrop");
            backdrop.transform.SetParent(parent, false);
            var controller = backdrop.AddComponent<SpaceBackdropController>();
            controller.sceneRoot = sceneRoot;
            controller.starCount = 900;
            controller.starRadius = 42f;
            controller.meteorCount = 8;
            controller.meteorShowerStreakCount = 4;
            controller.meteorShowerInterval = 26f;
            controller.meteorShowerDuration = 6.5f;
        }

        private static void ConfigureEnvironmentDrift(Transform sceneRoot, Transform puzzle)
        {
            foreach (var drift in FindSceneObjects<SpaceDriftMotion>()) UnityEngine.Object.DestroyImmediate(drift);

            // Newly added space decorations are authored as root-level Tripo
            // prefabs. The gameplay scene remains static; only these roots get
            // the gentle zero-gravity motion treatment.
            foreach (var decoration in FindSceneObjects<Transform>().Where(IsSpaceDecorRoot))
            {
                var renderers = decoration.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                // The large cylindrical station is deliberately a fixed
                // landmark. Leave it untouched and do not add a motion script.
                if (IsStaticSpaceDecorRoot(decoration)) continue;
                var bounds = renderers[0].bounds;
                for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
                var extents = bounds.extents;
                var smallest = Mathf.Max(0.001f, Mathf.Min(extents.x, Mathf.Min(extents.y, extents.z)));
                var largest = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
                var isPlanet = IsPlanetLike(bounds, largest / smallest);
                var isRotatingPod = decoration.name == RotatingPodName;
                var isMeteor = decoration.name == MeteorName || decoration.name == MeteorDuplicateName;
                Debug.Log($"SPACE_DECOR_MOTION {decoration.name}: extents={extents:F2}, ratio={(largest / smallest):F2}, planet={isPlanet}");
                var motion = decoration.gameObject.AddComponent<SpaceDriftMotion>();
                motion.subtlePlanetMotion = isPlanet && !isRotatingPod && !isMeteor;
                // Make the zero-gravity motion legible at the authored scene
                // scale while keeping planets noticeably calmer than props.
                motion.rotationDegreesPerSecond = isRotatingPod
                    ? new Vector3(0.72f, 3.15f, 0.48f)
                    : isMeteor
                        ? new Vector3(1.25f, 4.8f, 0.92f)
                    : isPlanet
                        ? new Vector3(0.04f, 0.22f, 0.03f)
                        : new Vector3(2.4f, 7.0f, 1.8f);
                var driftScale = Mathf.Clamp(bounds.extents.magnitude * 0.045f, 0.28f, 1.15f);
                motion.swayAmplitude = isRotatingPod
                    ? new Vector3(driftScale * 0.34f, driftScale * 0.56f, driftScale * 0.28f)
                    : isMeteor
                        ? new Vector3(driftScale * 0.34f, driftScale * 0.48f, driftScale * 0.26f)
                    : isPlanet
                        ? new Vector3(0.012f, 0.028f, 0.010f)
                        : new Vector3(driftScale * 0.82f, driftScale * 1.24f, driftScale * 0.68f);
                motion.swaySpeed = isRotatingPod ? 0.24f : isMeteor ? 0.20f : isPlanet ? 0.10f : 0.36f;
                motion.phaseOffset = Mathf.Repeat(Mathf.Abs(decoration.name.GetHashCode()) * 0.017f, Mathf.PI * 2f);
                EditorUtility.SetDirty(motion);
            }
        }

        private static void ConfigureAuthoredPanelLightStrips()
        {
            var stripRoots = FindSceneObjects<Transform>()
                .Where(item => item.name.StartsWith("OverheadLightStripModel_Editable", StringComparison.Ordinal))
                .ToArray();
            Require(stripRoots.Length == 4, $"Expected four authored panel light strips, found {stripRoots.Length}.");
            foreach (var stripRoot in stripRoots)
            {
                var glow = GetOrAdd<AuthoredLightStripGlow>(stripRoot.gameObject);
                glow.emissionColor = new Color(0.42f, 0.96f, 1f, 1f);
                glow.emissionIntensity = 3.8f;
                glow.pulseAmount = 0.08f;
                glow.pulseSpeed = 0.85f;
                EditorUtility.SetDirty(glow);
            }
        }

        private static bool IsSpaceDecorRoot(Transform transform)
        {
            return transform != null && transform.parent == null &&
                   transform.name.StartsWith("tripo_convert_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStaticSpaceDecorRoot(Transform transform)
        {
            return transform != null && string.Equals(transform.name, StaticStationName, StringComparison.Ordinal);
        }

        private static bool IsPlanetLike(Bounds bounds, float shapeRatio)
        {
            // Only near-spherical, large bodies are treated as planets. Small
            // mechanical satellites and irregular spacecraft must still drift.
            return shapeRatio < 1.16f && bounds.extents.magnitude > 1.35f;
        }

        private static bool IsAuthorizedSpaceDecorMotion(SpaceDriftMotion motion)
        {
            return motion != null && IsSpaceDecorRoot(motion.transform);
        }

        private static void PreserveAuthoredSceneLights()
        {
            // Intentionally empty. This hook documents the ownership boundary:
            // user-authored light strips and point lights must never be changed
            // by gameplay/effects installation. Generated projector lights are
            // created and configured by StructureBuildProjectionEffectInstaller.
        }

        private static void BindXRInteractions(StructureGameController game)
        {
            foreach (var interaction in FindSceneObjects<WorldSpaceInteraction>())
            {
                interaction.game = game;
                interaction.rayOrigin ??= interaction.transform;
                interaction.rayLine ??= interaction.GetComponent<LineRenderer>();
                EditorUtility.SetDirty(interaction);
            }
        }

        private static void BindPicoRig(DesignPlayerStart designStart)
        {
            var rig = FindSceneObject("XR Rig");
            var xrCamera = FindSceneObject("XR Camera")?.GetComponent<Camera>();
            if (rig == null || xrCamera == null) return;
            var bootstrap = GetOrAdd<PicoRigBootstrap>(rig);
            bootstrap.xrCamera = xrCamera;
            bootstrap.picoManager = rig.GetComponent<PXR_Manager>();
            bootstrap.designStart = designStart;
            EditorUtility.SetDirty(bootstrap);
        }

        private static void RemoveMissingSceneScripts()
        {
            var removed = 0;
            foreach (var transform in FindSceneObjects<Transform>())
            {
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            }
            if (removed > 0) Debug.Log($"Removed {removed} stale missing-script components before rebinding gameplay.");
        }

        private static TMP_FontAsset EnsureChineseFontAsset()
        {
            var characters = " 0123456789/·,.，；、…:-+×ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
                "结构档案分析两面投影复原空间单元等待输入正面侧面吻合完成当前视角概览提示撤回重置方块已放置未命中合法网格位置全部个三点校准先让枚地同时亮起初层阶梯第一次使用垂直堆叠双峰折线座高点在错位对应信标匹配四列不同高度棱镜辨认塔与的回声需要正确换位终端光门组合进入深层折光回廊让共用深度双核偏振同一必须在深处留下第二核心暗列阵把藏进条光柱四壁共振用主同时遮住三层终极光栅满压缩成脊游戏操作面板释放拖动高亮区域落点返回取消请先移走上方结构用完移动下一关重新开始归档准备进入本轮探索已通过校验全结构将到棋盘磁性吸附玩法说明从左侧源抓取一个拖到松手后自动吸附同时匹配可用关卡按顺序推进开始游戏启动会随以有限两道控制器射线指向按钮并按下扳机桌面版可直接点击退出光匣";
            foreach (var level in CampaignData.Levels) characters += level.displayName + level.rule;
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
            {
                var existingMissingCharacters = new string(characters.Distinct().Where(character => !existing.HasCharacter(character)).ToArray());
                if (string.IsNullOrEmpty(existingMissingCharacters)) return existing;
                AssetDatabase.DeleteAsset(FontAssetPath);
            }
            if (!AssetDatabase.IsValidFolder("Assets/UI")) AssetDatabase.CreateFolder("Assets", "UI");
            var fontPath = new[]
            {
                "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
                "/System/Library/Fonts/STHeiti Medium.ttc",
            }.FirstOrDefault(File.Exists);
            if (fontPath == null) return TMP_Settings.defaultFontAsset;

            var fontAsset = TMP_FontAsset.CreateFontAsset(fontPath, 0, 56, 5, GlyphRenderMode.SDFAA, 4096, 4096);
            if (fontAsset == null) return TMP_Settings.defaultFontAsset;
            fontAsset.name = "StructureBuild Chinese SDF";
            fontAsset.TryAddCharacters(characters, out var missingCharacters);
            if (!string.IsNullOrEmpty(missingCharacters)) Debug.LogWarning("CJK font is missing characters: " + missingCharacters);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            foreach (var texture in fontAsset.atlasTextures)
                if (texture != null && !AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture, fontAsset);
            if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material)) AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        [MenuItem("Structure Build/Rebuild Chinese Font Asset")]
        public static void RebuildChineseFontAsset()
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
            EnsureChineseFontAsset();
            AssetDatabase.Refresh();
            Debug.Log("STRUCTURE_BUILD_FONT_OK: rebuilt the static CJK font atlas with drag feedback characters.");
        }

        private static LevelCompleteOverlay CreateDesktopCompletionOverlay(
            Transform parent,
            StructureGameController game,
            StructureHUDController actions,
            TMP_FontAsset font,
            Sprite buttonSkin,
            Sprite completionPanelSkin)
        {
            var host = CreateRect("CompletionOverlay_Desktop", parent);
            SetAnchors(host.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var overlay = host.AddComponent<LevelCompleteOverlay>();
            overlay.game = game;

            var layer = CreateRect("Layer", host.transform);
            SetAnchors(layer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var dimmer = CreatePanel("Dimmer", layer.transform, new Color(0.002f, 0.012f, 0.021f, 0.78f));
            SetAnchors(dimmer.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            var card = CreatePanel("Card", layer.transform, new Color(0.015f, 0.085f, 0.12f, 0.985f));
            ApplyUiSkin(card, completionPanelSkin);
            SetAnchors(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 390f), new Vector2(0.5f, 0.5f));
            var topAccent = CreatePanel("TopAccent", card.transform, Cyan);
            SetAnchors(topAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 5f), new Vector2(0.5f, 1f));
            var eyebrow = CreateText("Eyebrow", card.transform, font, "STRUCTURE ARCHIVE  /  VALIDATED", 16f, Amber, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(eyebrow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -50f), new Vector2(0f, 32f), new Vector2(0.5f, 1f));
            var title = CreateText("Title", card.transform, font, "结构档案完成", 39f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -98f), new Vector2(0f, 58f), new Vector2(0.5f, 1f));
            var detail = CreateText("Detail", card.transform, font, "档案 01 已通过双面投影校验", 20f, Muted, FontStyles.Normal, TextAlignmentOptions.Center);
            SetAnchors(detail.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -166f), new Vector2(0f, 36f), new Vector2(0.5f, 1f));
            var progress = CreateText("Progress", card.transform, font, "准备进入档案 02", 18f, Cyan, FontStyles.Normal, TextAlignmentOptions.Center);
            SetAnchors(progress.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -207f), new Vector2(0f, 30f), new Vector2(0.5f, 1f));
            var button = CreatePanel("ContinueButton", card.transform, new Color(0.02f, 0.27f, 0.29f, 1f));
            ApplyUiSkin(button, buttonSkin);
            SetAnchors(button.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(300f, 68f), new Vector2(0.5f, 0f));
            var buttonBehaviour = button.gameObject.AddComponent<StructureScreenButton>();
            buttonBehaviour.hud = actions;
            buttonBehaviour.action = StructureUIButtonAction.Continue;
            buttonBehaviour.background = button;
            buttonBehaviour.normalColor = Color.white;
            buttonBehaviour.hoverColor = new Color(0.78f, 1f, 1f, 1f);
            var buttonLabel = CreateText("Label", button.transform, font, "下一关", 23f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(buttonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            overlay.panelRoot = layer.gameObject;
            overlay.titleText = title;
            overlay.detailText = detail;
            overlay.progressText = progress;
            overlay.continueLabel = buttonLabel;
            layer.gameObject.SetActive(false);
            return overlay;
        }

        private static LevelCompleteOverlay CreatePicoCompletionOverlay(
            Transform parent,
            DesignPlayerStart designStart,
            StructureGameController game,
            StructureHUDController actions,
            TMP_FontAsset font,
            Sprite buttonSkin,
            Sprite completionPanelSkin)
        {
            var host = CreateRect("CompletionOverlay_Pico", parent);
            var hostRect = host.GetComponent<RectTransform>();
            hostRect.sizeDelta = new Vector2(1180f, 600f);
            ConfigureViewLockedCanvas(host, 1.72f, 0.00130f, new Vector2(0f, -0.02f));
            var hostCanvas = host.AddComponent<Canvas>();
            hostCanvas.renderMode = RenderMode.WorldSpace;
            hostCanvas.sortingOrder = 180;
            host.AddComponent<GraphicRaycaster>();
            var overlay = host.AddComponent<LevelCompleteOverlay>();
            overlay.game = game;
            var presenter = host.AddComponent<CompletionPanelPresenter>();
            presenter.designStart = designStart;
            overlay.worldPresenter = presenter;

            var layer = CreateRect("Layer", host.transform);
            SetAnchors(layer.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var card = CreatePanel("Card", layer.transform, new Color(0.012f, 0.09f, 0.12f, 0.985f));
            ApplyUiSkin(card, completionPanelSkin);
            SetAnchors(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 600f), new Vector2(0.5f, 0.5f));
            var topAccent = CreatePanel("TopAccent", card.transform, Cyan);
            SetAnchors(topAccent.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 5f), new Vector2(0.5f, 1f));
            var eyebrow = CreateText("Eyebrow", card.transform, font, "STRUCTURE ARCHIVE  /  VALIDATED", 15f, Amber, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(eyebrow.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -42f), new Vector2(0f, 28f), new Vector2(0.5f, 1f));
            // The title belongs inside the large upper verification frame.
            // It is separate from the two lower archive readouts so the card
            // follows its graphic hierarchy: title in the frame, details on
            // the two cyan information rules beneath it.
            var title = CreateText("CompletionTitle", card.transform, font, "结构档案完成", 50f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Overflow;
            SetAnchors(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 145f), new Vector2(940f, 84f), new Vector2(0.5f, 0.5f));
            // One TMP mesh deliberately carries the two lower information
            // lines.  This retains the PICO compositor reliability guard
            // while aligning each line to one of the card's lower rules.
            var payload = CreateText("CompletionPayload", card.transform, font,
                "<align=\"center\"><size=29><color=#B8DDE6>档案 01 已通过双面投影校验</color></size>\n<size=27><color=#73F4FF>准备进入档案 02</color></size></align>",
                29f, new Color(0.82f, 0.96f, 1f, 1f), FontStyles.Normal, TextAlignmentOptions.Center);
            payload.richText = true;
            payload.textWrappingMode = TextWrappingModes.NoWrap;
            payload.overflowMode = TextOverflowModes.Overflow;
            payload.alignment = TextAlignmentOptions.Center;
            payload.lineSpacing = 30f;
            SetAnchors(payload.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(940f, 120f), new Vector2(0.5f, 0.5f));
            var button = CreatePanel("ContinueButton", card.transform, new Color(0.02f, 0.27f, 0.29f, 1f));
            ApplyUiSkin(button, buttonSkin);
            SetAnchors(button.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(350f, 86f), new Vector2(0.5f, 0f));
            var collider = button.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(350f, 86f, 10f);
            var buttonBehaviour = button.gameObject.AddComponent<StructureUIButton>();
            buttonBehaviour.hud = actions;
            buttonBehaviour.action = StructureUIButtonAction.Continue;
            buttonBehaviour.backgroundGraphic = button;
            buttonBehaviour.normalColor = Color.white;
            buttonBehaviour.highlightedColor = new Color(0.78f, 1f, 1f, 1f);
            var buttonLabel = CreateText("Label", button.transform, font, "下一关", 32f, Ice, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(buttonLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            overlay.panelRoot = layer.gameObject;
            overlay.titleText = title;
            overlay.combinedPayloadText = payload;
            overlay.continueLabel = buttonLabel;
            layer.gameObject.SetActive(false);
            return overlay;
        }

        private static void CreateScreenButton(
            Transform parent,
            TMP_FontAsset font,
            string label,
            StructureUIButtonAction action,
            StructureHUDController hud,
            Color accent,
            Sprite buttonSkin)
        {
            var button = CreatePanel(label, parent, new Color(0.025f, 0.14f, 0.17f, 0.96f));
            ApplyUiSkin(button, buttonSkin);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 120f;
            var behaviour = button.gameObject.AddComponent<StructureScreenButton>();
            behaviour.hud = hud;
            behaviour.action = action;
            behaviour.background = button;
            behaviour.normalColor = Color.white;
            behaviour.hoverColor = new Color(0.78f, 1f, 1f, 1f);
            var text = CreateText("Label", button.transform, font, label, 17f, accent, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        private static void CreateWorldButton(
            Transform parent,
            TMP_FontAsset font,
            string label,
            StructureUIButtonAction action,
            StructureHUDController hud,
            Vector2 position,
            Color accent,
            Sprite buttonSkin,
            Vector2 size)
        {
            var button = CreatePanel(label + "Button", parent, new Color(0.025f, 0.14f, 0.17f, 1f));
            ApplyUiSkin(button, buttonSkin);
            SetAnchors(button.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size, new Vector2(0.5f, 0.5f));
            var collider = button.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(size.x, size.y, 10f);
            var behaviour = button.gameObject.AddComponent<StructureUIButton>();
            behaviour.hud = hud;
            behaviour.action = action;
            behaviour.backgroundGraphic = button;
            behaviour.normalColor = Color.white;
            behaviour.highlightedColor = new Color(0.78f, 1f, 1f, 1f);
            var text = CreateText("Label", button.transform, font, label, 29f, accent, FontStyles.Bold, TextAlignmentOptions.Center);
            SetAnchors(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        private static void ConfigureFixedWorldCanvas(GameObject root, Vector3 worldPosition, float worldScale, DesignPlayerStart designStart)
        {
            var follower = root.GetComponent<PicoHeadLockedCanvas>();
            if (follower != null) UnityEngine.Object.DestroyImmediate(follower);
            root.transform.position = worldPosition;
            root.transform.localScale = Vector3.one * worldScale;
            designStart?.FaceWorldPanel(root.transform);
        }

        private static void ConfigureViewLockedCanvas(GameObject root, float distance, float worldScale, Vector2 viewOffset)
        {
            var follower = GetOrAdd<PicoHeadLockedCanvas>(root);
            follower.targetCamera = null;
            follower.distance = distance;
            follower.worldScale = worldScale;
            follower.viewOffset = viewOffset;
            follower.followRotation = true;
        }

        private static void EnsureEventSystem(Transform parent)
        {
            var current = FindSceneObjects<EventSystem>().FirstOrDefault();
            if (current != null)
            {
                var oldModule = current.GetComponent<StandaloneInputModule>();
                if (oldModule != null) UnityEngine.Object.DestroyImmediate(oldModule);
                var inputModule = GetOrAdd<InputSystemUIInputModule>(current.gameObject);
                inputModule.AssignDefaultActions();
                return;
            }
            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(parent, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static GameObject CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Sprite LoadUiSkin(string assetPath, Vector4 border)
        {
            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"STRUCTURE_UI_SKIN_MISSING: {assetPath}; using the built-in panel color.");
                return null;
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                var changed = importer.textureType != TextureImporterType.Sprite ||
                              importer.spriteImportMode != SpriteImportMode.Single ||
                              importer.spriteBorder != border ||
                              importer.wrapMode != TextureWrapMode.Clamp;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.spritePixelsPerUnit = 100f;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                if (changed) importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ApplyUiSkin(Image image, Sprite skin)
        {
            if (image == null || skin == null) return;
            image.sprite = skin;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var gameObject = CreateRect(name, parent);
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string content, float size, Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            var gameObject = CreateRect(name, parent);
            var text = gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static float MeasureCellSize(Transform gridRoot, float fallback)
        {
            if (gridRoot == null) return fallback;
            var a = gridRoot.Find("GridMarker_0_0");
            var b = gridRoot.Find("GridMarker_1_0");
            if (a == null || b == null) return fallback;
            var distance = Vector3.Distance(a.localPosition, b.localPosition);
            return distance > 0.001f ? distance : fallback;
        }

        private static Dictionary<Transform, TransformState> CaptureExistingTransforms(Scene scene)
        {
            var result = new Dictionary<Transform, TransformState>();
            foreach (var transform in FindSceneObjects<Transform>()) result[transform] = new TransformState(transform);
            return result;
        }

        private static void AssertExistingTransformsUnchanged(Dictionary<Transform, TransformState> before)
        {
            foreach (var transform in FindSceneObjects<Transform>())
            {
                if (!before.TryGetValue(transform, out var original)) continue;
                if (transform.parent != original.parent || transform.localPosition != original.localPosition ||
                    transform.localRotation != original.localRotation || transform.localScale != original.localScale)
                {
                    throw new InvalidOperationException($"Installer attempted to change existing transform: {GetPath(transform)}");
                }
            }
        }

        private static string GetPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }

        private static void RemoveOwnedChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static GameObject FindSceneObject(string name)
        {
            var active = SceneManager.GetActiveScene();
            return FindSceneObjects<Transform>().FirstOrDefault(item => item.name == name && item.gameObject.scene == active)?.gameObject;
        }

        private static T[] FindSceneObjects<T>() where T : UnityEngine.Object
        {
            var active = SceneManager.GetActiveScene();
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include)
                .Where(item => item is Component component && component.gameObject.scene == active)
                .ToArray();
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
