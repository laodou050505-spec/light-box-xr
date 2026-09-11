#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StructureBuild;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StructureBuild.Editor
{
    /// <summary>
    /// Play-mode QA for the spatial PICO HUD.  This intentionally calls the
    /// runtime PicoHeadLockedCanvas.ApplyPose method instead of
    /// reproducing its placement formula in an editor helper. Canvas route
    /// visibility is only inspected, never rewritten to make captures pass.
    /// </summary>
    public static class StructureBuildSpatialUiEvidence
    {
        private const string TitleScene = "Assets/Scenes/StructureBuildTitle.unity";
        private const string GameplayScene = "Assets/Scenes/StructureBuild.unity";
        private const string StateKey = "StructureBuild.SpatialUiEvidence";
        private const string ProfileKey = "StructureBuild.SpatialUiEvidence.Profile";
        private static bool IsPositionOnly => SessionState.GetString(ProfileKey, string.Empty) == "position-only";
        private static bool IsTutorialView => SessionState.GetString(ProfileKey, string.Empty) == "tutorial-view";
        private static bool IsGuideCover => IsTutorialView || IsPositionOnly || SessionState.GetString(ProfileKey, string.Empty) == "guide-cover";
        private static string Output => IsTutorialView ? "VisualQA/TutorialView-20260910" : IsPositionOnly ? "VisualQA/PositionOnly-20260910" : IsGuideCover ? "VisualQA/GuideCover-20260910" : "VisualQA/SpatialUi-20260909";
        private static int frame;
        private static int stage;
        private static Camera camera;
        private static PoseSnapshot left;
        private static PoseSnapshot right;
        private static PoseSnapshot dock;
        private static Vector3 openingPosition;
        private static Quaternion openingRotation;
        private static int pointerStep;
        private static int lastPointerFrame = -1;
        private static Vector3Int gateReferenceCell;

        [MenuItem("Structure Build/Run Spatial UI Evidence")]
        public static void Run()
        {
            StartReview("spatial-ui");
        }

        [MenuItem("Structure Build/Run Guide And Cover Review")]
        public static void RunGuideCoverReview()
        {
            StartReview("guide-cover");
        }

        [MenuItem("Structure Build/Run Position Only Review")]
        public static void RunPositionOnlyReview()
        {
            StartReview("position-only");
        }

        [MenuItem("Structure Build/Run Tutorial And View Review")]
        public static void RunTutorialViewReview()
        {
            StartReview("tutorial-view");
        }

        private static void StartReview(string profile)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before running spatial UI evidence.");
            SessionState.SetString(ProfileKey, profile);
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene(TitleScene, OpenSceneMode.Single);
            frame = 0; stage = 0;
            SessionState.SetString(StateKey, "requested");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void InstallHook()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            var status = SessionState.GetString(StateKey, string.Empty);
            if (state == PlayModeStateChange.EnteredPlayMode && status == "requested")
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode && (status == "passed" || status == "failed"))
            {
                var passed = status == "passed";
                SessionState.EraseString(StateKey);
                SessionState.EraseString(ProfileKey);
                if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            }
        }

        private static void Tick()
        {
            try
            {
                frame++;
                if (frame < 4) return;
                if (stage == 0)
                {
                    Require(SceneManager.GetActiveScene().path == TitleScene, "Title scene did not start in isolation.");
                    var title = Find<StructureTitleController>();
                    var titleCamera = GameObject.Find("TitleDesktopCamera")?.GetComponent<Camera>();
                    Require(title != null && titleCamera != null && GameObject.Find("TitleCover") != null, "Title cover/camera missing.");
                    var titleStart = Find<DesignPlayerStart>();
                    Require(titleStart != null && Vector3.Distance(titleCamera.transform.position, titleStart.DesignedEyePosition) < 0.02f &&
                            Quaternion.Angle(titleCamera.transform.rotation, titleStart.DesignedEyeRotation) < 0.25f,
                        "The natural title camera pose does not match its authored start.");
                    Require(Mathf.Abs(titleCamera.fieldOfView - 80f) < 0.1f, "The authored title camera FOV must be 80 degrees.");
                    ConfigureCamera(titleCamera);
                    Capture(titleCamera, "01-title.png");
                    if (IsGuideCover)
                    {
                        AssertTitleCover(titleStart, titleCamera);
                        Capture(titleCamera, "01-title-full-artwork.png", 1920, 1080);
                        var startButton = RaycastTitleButton(titleCamera, StructureTitleButtonAction.Start);
                        startButton.SetHighlighted(titleCamera, true);
                        stage = 100; frame = 0; return;
                    }
                    title.BeginGame();
                    stage = 1; frame = 0; return;
                }
                if (stage == 100)
                {
                    if (frame < 14) return;
                    var titleCamera = GameObject.Find("TitleDesktopCamera")?.GetComponent<Camera>();
                    Require(titleCamera != null, "Title disappeared before START interaction.");
                    Capture(titleCamera, "01-title-start-focus.png");
                    RaycastTitleButton(titleCamera, StructureTitleButtonAction.Exit);
                    var startButton = RaycastTitleButton(titleCamera, StructureTitleButtonAction.Start);
                    Require(startButton.focusGraphic != null && startButton.focusGraphic.color.a > 0.01f,
                        "START hover feedback did not become visible.");
                    startButton.Execute();
                    Require(Find<StructureTitleController>().IsTransitioning, "Physical START target did not begin gameplay transition.");
                    stage = 1; frame = 0; return;
                }
                if (SceneManager.GetActiveScene().path != GameplayScene || frame < 6) return;
                camera = GameObject.Find("DesktopCamera")?.GetComponent<Camera>();
                var game = Find<StructureGameController>();
                var start = Find<DesignPlayerStart>();
                var instructions = Find<GameplayInstructionsOverlay>();
                Require(camera != null && game != null && start != null && instructions != null, "Gameplay camera, game, start, or instructions missing.");
                ConfigureCamera(camera);
                Require(GameObject.Find("TitleRoot") == null && Find<StructureTitleController>() == null,
                    "Title objects survived the Single-mode gameplay transition.");
                AssertNoVisibleScreenSpaceUi();

                if (stage == 1)
                {
                    var horizontalCoordinate = -DesignPlayerStart.GameplayViewingDistance / Mathf.Sqrt(2f);
                    openingPosition = IsPositionOnly || IsTutorialView ? new Vector3(horizontalCoordinate, DesignPlayerStart.GameplayEyeHeight, horizontalCoordinate)
                        : new Vector3(-5.197235f, 6.52f, -5.197234f);
                    if (IsTutorialView) openingPosition += Quaternion.Euler(0f, 45f, 0f) * Vector3.right * DesignPlayerStart.GameplayLateralOffset;
                    openingRotation = Quaternion.Euler(IsTutorialView ? DesignPlayerStart.GameplayReferencePitch : IsPositionOnly ? 0f : 18f, 45f, 0f);
                    Require(Vector3.Distance(start.DesignedEyePosition, openingPosition) < 0.03f,
                        "Authored opening eye position drifted from the screenshot reference.");
                    Require(Vector3.Distance(camera.transform.position, openingPosition) < 0.03f &&
                            Quaternion.Angle(camera.transform.rotation, openingRotation) < 0.25f,
                        "The natural gameplay startup camera differs from the reference pose.");
                    Require(Mathf.Abs(camera.fieldOfView - 80f) < 0.1f, "The authored gameplay camera FOV must be 80 degrees.");
                    Require(instructions.picoCanvas != null && instructions.picoCanvas.isActiveAndEnabled &&
                            instructions.picoPanelRoot != null && instructions.picoPanelRoot.activeInHierarchy,
                        "The actual startup tutorial is not visible as spatial UI.");
                    ApplyAll(camera);
                    Capture(camera, "02-gameplay-tutorial.png");
                    if (IsPositionOnly)
                    {
                        AssertPositionOnlyWorldUnchanged();
                        AssertCanvasCornersInViewport("GameplayInstructions_Pico");
                    }
                    if (IsTutorialView)
                    {
                        AssertControllerReconnectGate();
                        AssertTutorialCentered(camera, "natural startup");
                        AssertTutorialGateBlocked(game, "before START PRACTICE");
                    }
                    ClickSpatialButton("GameplayInstructions_Pico", StructureUIButtonAction.DismissInstructions);
                    Require(!instructions.IsVisible, "Pointer click did not dismiss the spatial tutorial.");
                    if (IsTutorialView)
                    {
                        AssertTutorialGateBlocked(game, "same frame as tutorial dismissal");
                        stage = 110; frame = 0; return;
                    }
                    stage = 2; frame = 0; return;
                }
                if (stage == 110)
                {
                    // No synthetic held input is sent in this profile: the
                    // real gate must unlock only after its natural frame and
                    // release checks have observed the idle input devices.
                    Require(game.IsGameplayInputAllowed, "Tutorial gate did not unlock after a later released-input frame.");
                    gateReferenceCell = CampaignData.Levels[0].referenceSolution.Select(item => item.ToVector3Int()).OrderBy(item => item.y).First();
                    Require(game.TryPlaceAt(gateReferenceCell) && game.CurrentCubeCount == 1, "Placement remained blocked after tutorial unlock.");
                    var existing = UnityEngine.Object.FindObjectsByType<PuzzleCubeInteractable>().SingleOrDefault(item => item.cell == gateReferenceCell);
                    Require(existing != null, "Placed reference cube is missing its draggable component.");
                    var grabRay = new Ray(camera.transform.position, (existing.transform.position - camera.transform.position).normalized);
                    Require(game.BeginDragFromCube(existing, grabRay) && game.IsDragging && game.CurrentCubeCount == 0,
                        "Unlocked cube could not begin a tracked-style drag.");
                    instructions.Show();
                    Require(!game.IsDragging && game.CurrentCubeCount == 1 && !game.IsGameplayInputAllowed,
                        "Showing tutorial mid-drag did not cancel the drag and restore the original cube.");
                    AssertTutorialGateBlocked(game, "tutorial reopened during cube drag");
                    ApplyAll(camera);
                    AssertTutorialCentered(camera, "tutorial reopened during cube drag");
                    Capture(camera, "02b-tutorial-mid-drag-restored.png");
                    stage = 111; frame = 0; return;
                }
                if (stage == 111)
                {
                    ApplyAll(camera);
                    ClickSpatialButton("GameplayInstructions_Pico", StructureUIButtonAction.DismissInstructions);
                    AssertTutorialGateBlocked(game, "reopened tutorial dismissal same frame");
                    stage = 112; frame = 0; return;
                }
                if (stage == 112)
                {
                    Require(game.IsGameplayInputAllowed, "Reopened tutorial did not unlock after input release.");
                    var sourceRay = new Ray(camera.transform.position, (game.cubeSource.position - camera.transform.position).normalized);
                    Require(game.BeginDragFromSource(sourceRay) && game.IsDragging,
                        "Source grabbing remained blocked after the later released-input frame.");
                    game.CancelDrag();
                    game.ResetCurrentLevel();
                    Require(game.CurrentCubeCount == 0 && !game.IsDragging, "Gate QA cleanup failed to reset first level.");
                    Debug.Log("STRUCTURE_TUTORIAL_INPUT_GATE_OK: all placement/grab/commit paths blocked before tutorial confirmation and in its dismissal frame; released later frame unlocks; reopening during drag cancels and restores the original cube.");
                    stage = 2; frame = 0; return;
                }
                if (stage == 2)
                {
                    AssertPersistentUiVisible();
                    if (IsGuideCover) AssertGuideState(game, true, "level 1");
                    Require(!instructions.picoCanvas.enabled, "Tutorial did not close after dismissal.");
                    ApplyAll(camera);
                    if (IsGuideCover) VerifyRuntimeXrPose(start);
                    AssertScreenPlacement("PicoWorldHUD", "FrontProjectionPanel", "FrontProjectionModel_Editable");
                    AssertScreenPlacement("PicoQuickGuide", "SideProjectionPanel", "SideProjectionModel_Editable");
                    Capture(camera, "03-spatial-hud.png");
                    if (IsPositionOnly || IsTutorialView) AssertPositionOnlyComposition(game);
                    Snapshot(out left, out right, out dock);
                    var originalPosition = camera.transform.position;
                    var originalRotation = camera.transform.rotation;
                    camera.transform.position += camera.transform.right * 0.55f + camera.transform.up * 0.08f;
                    camera.transform.rotation = Quaternion.Euler(IsTutorialView ? DesignPlayerStart.GameplayReferencePitch : IsPositionOnly ? 0f : 18f, 70f, 0f);
                    ApplyAll(camera);
                    Require(Approximately(left, FindFollower("PicoWorldHUD")) && Approximately(right, FindFollower("PicoQuickGuide")),
                        "Top spatial panels moved or rotated when the head moved.");
                    var movedDock = SnapshotOf(FindFollower("PicoActionDock"));
                    Require(Vector3.Distance(dock.position, movedDock.position) < 0.002f && Quaternion.Angle(dock.rotation, movedDock.rotation) > 4f,
                        "Dock must keep its world position while following the view rotation.");
                    Capture(camera, "04-head-shifted-spatial-hud.png");
                    Debug.Log($"STRUCTURE_SPATIAL_UI_HEAD_MOTION_OK: top panels fixed; dock position delta={Vector3.Distance(dock.position, movedDock.position):F5}; dock rotation delta={Quaternion.Angle(dock.rotation, movedDock.rotation):F2} degrees.");
                    camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
                    ApplyAll(camera);
                    pointerStep = 0;
                    stage = 20; frame = 0; return;
                }
                if (stage == 20)
                {
                    // Buttons reject duplicate input in a single game frame.
                    // Preserve that real input contract instead of looping
                    // Execute calls in one editor update.
                    if (Time.frameCount == lastPointerFrame) return;
                    AssertPersistentUiVisible();
                    ApplyAll(camera);
                    if (pointerStep == 0 || pointerStep == 2)
                    {
                        ClickSpatialButton("PicoActionDock", StructureUIButtonAction.Hint);
                        Require(game.CurrentCubeCount == 1 && !game.IsLevelCompleted, "Hint pointer click did not place exactly one cube.");
                    }
                    else if (pointerStep == 1)
                    {
                        ClickSpatialButton("PicoActionDock", StructureUIButtonAction.Undo);
                        Require(game.CurrentCubeCount == 0, "Undo pointer click did not restore an empty board.");
                    }
                    else if (pointerStep == 3)
                    {
                        ClickSpatialButton("PicoActionDock", StructureUIButtonAction.Reset);
                        Require(game.CurrentCubeCount == 0, "Reset pointer click did not clear the board.");
                    }
                    else
                    {
                        var previousCount = game.CurrentCubeCount;
                        ClickSpatialButton("PicoActionDock", StructureUIButtonAction.Hint);
                        Require(game.CurrentCubeCount == previousCount + 1, "Hint pointer sequence stopped adding cubes.");
                        if (game.IsLevelCompleted)
                        {
                            Debug.Log("STRUCTURE_SPATIAL_POINTER_LOOP_OK: EventSystem raycast and pointer click dismissed tutorial, placed a hint cube, undid it, reset it, and solved the first level.");
                            stage = 3; frame = 0; return;
                        }
                    }
                    pointerStep++;
                    Require(pointerStep < 32, "Pointer hint sequence did not complete the first level.");
                    frame = 0; return;
                }
                if (stage == 3)
                {
                    ApplyAll(camera);
                    var completion = GameObject.Find("CompletionOverlay_Pico")?.GetComponent<LevelCompleteOverlay>();
                    var completionCanvas = completion != null ? completion.GetComponent<Canvas>() : null;
                    Require(completion != null && completion.panelRoot != null && completion.panelRoot.activeInHierarchy &&
                            completionCanvas != null && completionCanvas.isActiveAndEnabled, "PICO completion overlay is not visible.");
                    Capture(camera, "05-completion.png");
                    var legacyCompletion = UnityEngine.Object.FindObjectsByType<LevelCompleteOverlay>(FindObjectsInactive.Include)
                        .FirstOrDefault(item => item.name == "CompletionOverlay_Desktop");
                    Require(legacyCompletion != null && !legacyCompletion.gameObject.activeInHierarchy,
                        "The legacy desktop completion card must stay inactive while the spatial card is visible.");
                    ClickSpatialButton("CompletionOverlay_Pico", StructureUIButtonAction.Continue);
                    Require(game.CurrentLevelNumber == 2 && !game.IsLevelCompleted, "Next-level action did not advance to level 2.");
                    stage = 4; frame = 0; return;
                }
                if (stage == 4)
                {
                    AssertPersistentUiVisible();
                    ApplyAll(camera);
                    Capture(camera, "06-next-level.png");
                    if (IsGuideCover)
                    {
                        AssertGuideState(game, true, "level 2");
                        stage = 40; frame = 0; return;
                    }
                    Debug.Log("STRUCTURE_SPATIAL_UI_EVIDENCE_OK: title, tutorial, spatial HUD, head shift, completion, and next level captured; top panels stayed on screen targets and dock kept its fixed world anchor.");
                    SessionState.SetString(StateKey, "passed");
                    Stop();
                }
                if (stage == 40)
                {
                    AssertGuideState(game, true, "level 2 before completion");
                    SolveCurrentLevel(game);
                    AssertGuideState(game, true, "level 2 after completion");
                    ApplyAll(camera);
                    Capture(camera, "07-level2-complete-guide-visible.png");
                    ClickSpatialButton("CompletionOverlay_Pico", StructureUIButtonAction.Continue);
                    Require(game.CurrentLevelNumber == 3 && !game.IsLevelCompleted, "Completion did not advance to level 3.");
                    stage = 41; frame = 0; return;
                }
                if (stage == 41)
                {
                    AssertGuideState(game, true, "level 3 before completion");
                    ApplyAll(camera);
                    Capture(camera, "08-level3-guide-visible.png");
                    SolveCurrentLevel(game);
                    // Intentionally inspect in the same call stack. Do not
                    // wait a frame or RefreshVisibility to conceal latency.
                    AssertGuideState(game, false, "level 3 immediate completion");
                    Require(game.HighestCompletedLevel == 3, "Third completion did not record campaign progress.");
                    Capture(camera, "09-level3-complete-guide-hidden.png");
                    Debug.Log("STRUCTURE_GUIDE_IMMEDIATE_HIDE_OK: Canvas and GraphicRaycaster disabled in the same frame as third-level completion; mission and dock retained.");
                    stage = 42; frame = 0; return;
                }
                if (stage == 42)
                {
                    AssertGuideState(game, false, "level 3 completed next frame");
                    ApplyAll(camera);
                    ClickSpatialButton("CompletionOverlay_Pico", StructureUIButtonAction.Continue);
                    Require(game.CurrentLevelNumber == 4 && !game.IsLevelCompleted, "Completion did not advance to level 4.");
                    AssertGuideState(game, false, "level 4 immediate load");
                    stage = 43; frame = 0; return;
                }
                if (stage == 43)
                {
                    AssertGuideState(game, false, "level 4");
                    ApplyAll(camera);
                    Capture(camera, "10-level4-guide-hidden.png");
                    ClickSpatialButton("PicoActionDock", StructureUIButtonAction.Reset);
                    AssertGuideState(game, false, "level 4 reset");
                    game.LoadLevel(2);
                    AssertGuideState(game, false, "reload level 3 after completion");
                    game.ResetCurrentLevel();
                    AssertGuideState(game, false, "reset revisited level 3");
                    game.LoadLevel(3);
                    stage = 44; frame = 0; return;
                }
                if (stage == 44)
                {
                    AssertGuideState(game, false, "level 4 after reset/revisit");
                    ApplyAll(camera);
                    Capture(camera, "11-level4-reset-guide-still-hidden.png");
                    game.RestartCampaign();
                    Require(game.CurrentLevelNumber == 1 && game.HighestCompletedLevel == 0,
                        "RestartCampaign did not begin a fresh campaign.");
                    AssertGuideState(game, true, "fresh campaign level 1");
                    if (IsPositionOnly || IsTutorialView) AssertPositionOnlyWorldUnchanged();
                    Debug.Log((IsTutorialView ? "STRUCTURE_TUTORIAL_VIEW_REVIEW_OK" : IsPositionOnly ? "STRUCTURE_POSITION_ONLY_REVIEW_OK" : "STRUCTURE_GUIDE_COVER_REVIEW_OK") + ": title feather/front/START checked; real neutral XR placement and relative head movement checked; guide visible levels 1-3, immediately hidden at third completion, stayed hidden on level 4/reset/revisited level 3, restored only on fresh campaign.");
                    SessionState.SetString(StateKey, "passed");
                    Stop();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SessionState.SetString(StateKey, "failed");
                Stop();
            }
        }

        private static void ConfigureCamera(Camera value)
        {
            camera = value;
            camera.aspect = 1372f / 1374f;
            var orbit = camera.GetComponent<DesktopCameraOrbit>();
            if (orbit != null) orbit.enabled = false;
        }

        private static void AssertTitleCover(DesignPlayerStart start, Camera value)
        {
            var cover = GameObject.Find("TitleCover")?.GetComponent<RectTransform>();
            var artwork = GameObject.Find("InnerFrame")?.GetComponent<Image>();
            Require(cover != null && artwork != null && artwork.sprite != null, "Title cover/artwork missing.");
            Require(Vector3.Distance(start.DesignedEyePosition, new Vector3(0f, 1.68f, -StructureBuildTitleSceneInstaller.TitleViewingDistance)) < 0.02f,
                "Title eye does not use the closer authored cover-view distance.");
            var horizontalDistance = Vector2.Distance(new Vector2(cover.position.x, cover.position.z),
                new Vector2(start.DesignedEyePosition.x, start.DesignedEyePosition.z));
            Require(Mathf.Abs(horizontalDistance - StructureBuildTitleSceneInstaller.TitleViewingDistance) < 0.02f && Mathf.Abs(cover.position.y - 1.58f) < 0.02f,
                $"Title must sit {StructureBuildTitleSceneInstaller.TitleViewingDistance:F2}m ahead at y=1.58m; got distance={horizontalDistance:F3}, y={cover.position.y:F3}.");
            Require(Vector3.Distance(cover.localScale, Vector3.one * 0.0043f) < 0.00001f,
                "Title must retain the approved 0.0043 artwork scale.");
            var away = (cover.position - value.transform.position).normalized;
            Require(Vector3.Dot(cover.forward, away) > 0.99f && Vector3.Dot(cover.up, Vector3.up) > 0.999f,
                "Title artwork is not upright with its readable front toward the eye.");
            Require(artwork.preserveAspect && Mathf.Abs(artwork.rectTransform.rect.width / artwork.rectTransform.rect.height -
                    artwork.sprite.rect.width / artwork.sprite.rect.height) < 0.001f,
                "Title artwork aspect ratio was stretched.");
            var material = artwork.material;
            Require(material != null && AssetDatabase.GetAssetPath(material) == "Assets/UI/TitleCoverFeather.mat" &&
                    material.HasProperty("_Feather") && material.GetFloat("_Feather") >= 0.055f && material.GetFloat("_Feather") <= 0.08f,
                "Title must retain the persisted feather material and its safe 5.5-8% edge width.");
            var corners = new Vector3[4];
            artwork.rectTransform.GetWorldCorners(corners);
            var originalAspect = value.aspect;
            try
            {
                value.aspect = 1920f / 1080f;
                var viewport = corners.Select(value.WorldToViewportPoint).ToArray();
                Require(viewport.All(point => point.z > 0 && point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1),
                    "Complete title artwork no longer fits the desktop wide viewport.");
                Require(viewport[3].x > viewport[0].x && viewport[1].y > viewport[0].y,
                    "Title artwork is mirrored or inverted.");
            }
            finally { value.aspect = originalAspect; }
            // The near-square reference is intentionally near full-width;
            // only its feather fringe may leave the view. These measured
            // source-image regions contain the title and interaction copy.
            foreach (var uvRect in new[] { new Rect(0.078f, 0.18f, 0.39f, 0.415f),
                         new Rect(0.079f, 0.678f, 0.366f, 0.067f), new Rect(0.080f, 0.777f, 0.121f, 0.066f) })
            {
                foreach (var uv in new[] { new Vector2(uvRect.xMin, uvRect.yMin), new Vector2(uvRect.xMax, uvRect.yMin),
                             new Vector2(uvRect.xMin, uvRect.yMax), new Vector2(uvRect.xMax, uvRect.yMax) })
                {
                    var rect = artwork.rectTransform.rect;
                    var world = artwork.rectTransform.TransformPoint(new Vector3(rect.xMin + uv.x * rect.width, rect.yMax - uv.y * rect.height, 0f));
                    var viewport = value.WorldToViewportPoint(world);
                    Require(viewport.z > 0 && viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1,
                        "Title lettering or START/EXIT region is cropped by the reference viewport.");
                }
            }
            Debug.Log($"STRUCTURE_GUIDE_COVER_TITLE_OK: eye={start.DesignedEyePosition:F3}; cover={cover.position:F3}; distance={horizontalDistance:F3}m; feather={material.GetFloat("_Feather"):F3}; native artwork aspect preserved and complete in wide viewport.");
        }

        private static StructureTitleButton RaycastTitleButton(Camera value, StructureTitleButtonAction action)
        {
            var button = UnityEngine.Object.FindObjectsByType<StructureTitleButton>().Single(item => item.action == action);
            var collider = button.GetComponent<BoxCollider>();
            Require(collider != null && collider.enabled, "Title target collider missing for " + action);
            var screenPoint = value.WorldToScreenPoint(collider.bounds.center);
            var viewport = value.WorldToViewportPoint(collider.bounds.center);
            Require(viewport.z > 0 && viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1,
                "Title button center is outside the reference viewport: " + action);
            Physics.SyncTransforms();
            var ray = value.ScreenPointToRay(screenPoint);
            Require(Physics.Raycast(ray, out var hit, 20f) && hit.collider.GetComponentInParent<StructureTitleButton>() == button,
                "Mouse-style physics ray does not resolve the title button: " + action);
            Debug.Log($"STRUCTURE_GUIDE_COVER_TITLE_TARGET_OK: {action}; viewport={viewport:F3}.");
            return button;
        }

        private static void AssertGuideState(StructureGameController game, bool visible, string context)
        {
            var guide = GameObject.Find("PicoQuickGuide");
            var canvas = guide != null ? guide.GetComponent<Canvas>() : null;
            var raycaster = guide != null ? guide.GetComponent<GraphicRaycaster>() : null;
            var visibility = guide != null ? guide.GetComponent<PicoRuntimeCanvasVisibility>() : null;
            Require(canvas != null && raycaster != null && visibility != null && visibility.hideAfterCompletedLevel == 3,
                "Guide expiry is not configured for completion of level 3.");
            Require(canvas.isActiveAndEnabled == visible && raycaster.isActiveAndEnabled == visible,
                $"Guide visibility mismatch at {context}: canvas={canvas.isActiveAndEnabled}, raycaster={raycaster.isActiveAndEnabled}, expected={visible}.");
            AssertPersistentUiVisible(false);
            Debug.Log($"STRUCTURE_GUIDE_LIFECYCLE_OK: {context}; current={game.CurrentLevelNumber}; completed={game.HighestCompletedLevel}; guide={visible}; mission/dock visible.");
        }

        private static void SolveCurrentLevel(StructureGameController game)
        {
            var definition = CampaignData.Levels[game.CurrentLevelNumber - 1];
            foreach (var cell in definition.referenceSolution.Select(item => item.ToVector3Int()).OrderBy(item => item.y))
                Require(game.TryPlaceAt(cell), "Reference placement failed at " + cell);
            Require(game.IsLevelCompleted, "Reference solution did not complete level " + game.CurrentLevelNumber);
        }

        private static void AssertControllerReconnectGate()
        {
            var observe = typeof(GameplayInstructionsOverlay).GetMethod("UpdateObservedControllerButtons",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Require(observe != null, "Tutorial controller-state observer is missing.");
            var observations = new[]
            {
                new[] { 0, 0, 0, 0 }, new[] { 0, 15, 1, 1 }, new[] { 1, 0, 0, 1 },
                new[] { 1, 14, 0, 1 }, new[] { 1, 15, 1, 1 }, new[] { 1, 15, 0, 0 }
            };
            foreach (var sample in observations)
                Require((int)observe.Invoke(null, new object[] { sample[0], sample[1], sample[2] }) == sample[3],
                    $"Controller disconnect/reconnect lost a held input: held={sample[0]}, valid={sample[1]}, pressed={sample[2]}.");

            var probe = new GameObject("TutorialView_QA_ControllerReconnect") { hideFlags = HideFlags.HideAndDontSave };
            probe.SetActive(false);
            try
            {
                var interaction = probe.AddComponent<WorldSpaceInteraction>();
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var consume = typeof(WorldSpaceInteraction).GetMethod("ConsumeInputRearmFrame", flags);
                var hide = typeof(WorldSpaceInteraction).GetMethod("HideRayAndCancelDrag", flags);
                Require(consume != null && hide != null, "Controller input rearm methods are missing.");
                var states = new[]
                {
                    new[] { false, false, false, false, false, false, true },
                    new[] { true, true, false, false, false, false, true },
                    new[] { true, false, false, false, false, true, true },
                    new[] { true, false, false, false, false, false, true },
                    new[] { true, false, false, false, false, false, false }
                };
                foreach (var state in states)
                    Require((bool)consume.Invoke(interaction, state.Take(6).Select(value => (object)value).ToArray()) == state[6],
                        "Controller rearm accepted unavailable/held input or failed to consume the release frame.");
                hide.Invoke(interaction, null);
                var released = new object[] { true, false, false, false, false, false };
                Require((bool)consume.Invoke(interaction, released), "Tracking loss did not rearm controller input.");
                Require(!(bool)consume.Invoke(interaction, released), "Controller input did not unlock after confirmed release.");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
            Debug.Log("STRUCTURE_CONTROLLER_RECONNECT_GATE_OK: disconnect retains held state; unavailable/held input stays blocked; reconnect consumes a genuine release frame before accepting input.");
        }

        private static void AssertTutorialGateBlocked(StructureGameController game, string context)
        {
            var before = game.CurrentCubeCount;
            var cell = CampaignData.Levels[game.CurrentLevelNumber - 1].referenceSolution.Select(item => item.ToVector3Int()).OrderBy(item => item.y).First();
            var source = game.cubeSource != null ? game.cubeSource.position : game.CellToWorld(cell);
            var sourceRay = new Ray(camera.transform.position, (source - camera.transform.position).normalized);
            var dropRay = new Ray(game.CellToWorld(cell) + Vector3.up * 4f, Vector3.down);
            Require(!game.IsGameplayInputAllowed, "Gameplay input gate should be locked: " + context);
            Require(!game.TryPlaceAt(cell), "TryPlaceAt bypassed tutorial input gate: " + context);
            game.Hint();
            Require(!game.BeginDragFromSource(sourceRay), "Source grab bypassed tutorial input gate: " + context);
            Require(!game.CommitDrag(dropRay), "CommitDrag bypassed tutorial input gate: " + context);
            Require(game.CurrentCubeCount == before && !game.IsDragging,
                $"Tutorial gate changed cube/drag state at {context}: count {before}->{game.CurrentCubeCount}, drag={game.IsDragging}.");
            Debug.Log($"STRUCTURE_TUTORIAL_GATE_BLOCKED_OK: {context}; cubes={before}; placement/hint/source-grab/commit all blocked.");
        }

        private static void AssertTutorialCentered(Camera view, string context)
        {
            var rect = GameObject.Find("GameplayInstructions_Pico")?.GetComponent<RectTransform>();
            var canvas = rect != null ? rect.GetComponent<Canvas>() : null;
            Require(rect != null && canvas != null && canvas.isActiveAndEnabled, "Spatial tutorial is not visible: " + context);
            var center = view.WorldToViewportPoint(rect.TransformPoint(rect.rect.center));
            Require(center.z > view.nearClipPlane && Mathf.Abs(center.x - 0.5f) <= 0.02f && Mathf.Abs(center.y - 0.5f) <= 0.02f,
                $"Tutorial is not centered in actual view at {context}: viewport center={center:F3}.");
            AssertCanvasCornersInViewport("GameplayInstructions_Pico");
            Debug.Log($"STRUCTURE_TUTORIAL_CENTER_OK: {context}; viewport={center:F3}; camera={view.transform.position:F3}/{view.transform.eulerAngles:F3}.");
        }

        private static void AssertPositionOnlyWorldUnchanged()
        {
            AssertAuthoredTransform("STRUCTURE_BUILD_SCENE", Vector3.zero, Quaternion.identity, Vector3.one, true);
            AssertAuthoredTransform("PuzzleTable_Editable", new Vector3(0f, 0.95f, 0f), Quaternion.identity, Vector3.one, true);
            AssertAuthoredTransform("FrontProjectionAssembly_Editable", new Vector3(0f, 3.95f, 2.85f), Quaternion.identity, Vector3.one * 1.32f, true);
            AssertAuthoredTransform("SideProjectionAssembly_Editable", new Vector3(2.85f, 3.95f, 0f), Quaternion.identity, Vector3.one * 1.32f, true);
            AssertAuthoredTransform("FrontProjectionPanel", Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one, false);
            AssertAuthoredTransform("SideProjectionPanel", Vector3.zero, Quaternion.Euler(0f, 90f, 0f), Vector3.one, false);
            AssertAuthoredTransform("FrontProjectionModel_Editable", new Vector3(-0.44f, -0.972f, 0.066900015f), new Quaternion(0.5f, -0.5f, -0.5f, -0.5f), Vector3.one * 3.303488f, false);
            AssertAuthoredTransform("SideProjectionModel_Editable", new Vector3(0.076073885f, -0.9696002f, -0.444f), new Quaternion(0f, 0.7071068f, 0.7071068f, 0f), Vector3.one * 3.2664864f, false);
            var fixedDock = FindFollower("PicoActionDock");
            var teaching = FindFollower("GameplayInstructions_Pico");
            var dockPoint = StructureBuildSpatialUiLayout.DockWorldPoint;
            var teachingPoint = new Vector3(-4.0449076f, 5.9694695f, -4.044907f);
            Require(IsTutorialView || Mathf.Abs(dockPoint.x - -4.3009014f) < 0.0001f && Mathf.Abs(dockPoint.z - -4.300901f) < 0.0001f &&
                    dockPoint.y < 4.919977f,
                "The authorized dock adjustment must only lower Y, preserving its original X/Z point.");
            Require(fixedDock != null && fixedDock.fixedWorldAnchor &&
                    Vector3.Distance(fixedDock.fixedWorldPosition, dockPoint) < 0.002f && Vector3.Distance(fixedDock.transform.position, dockPoint) < 0.002f,
                "Dock does not use the authorized lower fixed-world point.");
            Require(IsTutorialView || teaching != null && teaching.fixedWorldAnchor &&
                    Vector3.Distance(teaching.fixedWorldPosition, teachingPoint) < 0.002f && Vector3.Distance(teaching.transform.position, teachingPoint) < 0.002f,
                "Position-only edit moved the authored teaching-card point.");
            Debug.Log($"STRUCTURE_POSITION_ONLY_WORLD_UNCHANGED_OK: scene root/table/display assemblies/display meshes preserve prior transforms; tutorial recenter permitted={IsTutorialView}; dock uses authored point {dockPoint:F3}; original X/Z required={!IsTutorialView}.");
        }

        private static void AssertAuthoredTransform(string name, Vector3 position, Quaternion rotation, Vector3 scale, bool world)
        {
            var value = GameObject.Find(name)?.transform;
            Require(value != null, "Missing position-only preservation target: " + name);
            Require(Vector3.Distance(world ? value.position : value.localPosition, position) < 0.002f &&
                    Quaternion.Angle(world ? value.rotation : value.localRotation, rotation) < 0.05f &&
                    Vector3.Distance(world ? value.lossyScale : value.localScale, scale) < 0.002f,
                "Position-only camera edit changed a scene transform: " + name);
        }

        private static Vector2[] AssertCanvasCornersInViewport(string name)
        {
            var rect = GameObject.Find(name)?.GetComponent<RectTransform>();
            Require(rect != null, "Missing spatial canvas: " + name);
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return AssertViewportPoints(name, corners);
        }

        private static Vector2[] AssertViewportPoints(string label, IEnumerable<Vector3> points)
        {
            var projected = points.Select(camera.WorldToViewportPoint).ToArray();
            Require(projected.Length > 0 && projected.All(point => point.z > camera.nearClipPlane &&
                    point.x >= -0.005f && point.x <= 1.005f && point.y >= -0.005f && point.y <= 1.005f),
                $"Position-only opening crops {label}. Viewport points: {string.Join(", ", projected.Select(point => point.ToString("F3")))}.");
            Debug.Log($"STRUCTURE_POSITION_ONLY_VIEWPORT_OK: {label}; min=({projected.Min(point => point.x):F3},{projected.Min(point => point.y):F3}); max=({projected.Max(point => point.x):F3},{projected.Max(point => point.y):F3}).");
            return projected.Select(point => new Vector2(point.x, point.y)).ToArray();
        }

        private static void AssertPositionOnlyComposition(StructureGameController game)
        {
            AssertPositionOnlyWorldUnchanged();
            var mission = AssertCanvasCornersInViewport("PicoWorldHUD");
            var guide = AssertCanvasCornersInViewport("PicoQuickGuide");
            var dockCorners = AssertCanvasCornersInViewport("PicoActionDock");
            foreach (var name in new[] { "FrontProjectionModel_Editable", "SideProjectionModel_Editable" })
            {
                var filters = GameObject.Find(name).GetComponentsInChildren<MeshFilter>(true).Where(item => item.sharedMesh != null);
                var corners = new List<Vector3>();
                foreach (var filter in filters)
                {
                    var bounds = filter.sharedMesh.bounds;
                    foreach (var x in new[] { bounds.min.x, bounds.max.x })
                    foreach (var y in new[] { bounds.min.y, bounds.max.y })
                    foreach (var z in new[] { bounds.min.z, bounds.max.z })
                        corners.Add(filter.transform.TransformPoint(new Vector3(x, y, z)));
                }
                AssertViewportPoints(name, corners);
            }
            // Scope this to the exact 16 rendered top modules. The two
            // WorkbenchBase objects are separate scenery at local X/Z
            // (-5.52,11.4) and (10.66,-6.44), not the tabletop: including
            // those props falsely requires distant scenery to fit the view.
            // GroundBounds is also unsuitable here because its cached
            // gameplay surface bounds can describe only a small module.
            var table = GameObject.Find("PuzzleTable_Editable").transform;
            var expectedTopNames = Enumerable.Range(0, 16).Select(index => index == 0 ? "WorkbenchTop_Editable" : $"WorkbenchTop_Editable ({index})").ToArray();
            var tableChildren = table.GetComponentsInChildren<Transform>(false);
            var boardMeshes = new List<MeshFilter>();
            foreach (var topName in expectedTopNames)
            {
                var modules = tableChildren.Where(value => value.name == topName).ToArray();
                Require(modules.Length == 1, $"Expected exactly one visible board module {topName}, got {modules.Length}.");
                var meshes = modules[0].GetComponentsInChildren<MeshFilter>(false).Where(filter =>
                    filter.sharedMesh != null && filter.GetComponent<Renderer>()?.enabled == true).ToArray();
                Require(meshes.Length > 0, "Visible board module has no rendered geometry: " + topName);
                boardMeshes.AddRange(meshes);
            }
            var boardWorldCorners = MeshWorldBoundsCorners(boardMeshes).ToArray();
            Require(boardWorldCorners.Length > 0, "Rendered workbench meshes missing; cannot validate board occlusion.");
            var boardWorldWidth = boardWorldCorners.Max(point => point.x) - boardWorldCorners.Min(point => point.x);
            var boardWorldDepth = boardWorldCorners.Max(point => point.z) - boardWorldCorners.Min(point => point.z);
            Require(boardWorldWidth > 2.5f && boardWorldDepth > 2.5f,
                $"Board QA geometry is too small ({boardWorldWidth:F3} x {boardWorldDepth:F3}m); a marker proxy must not pass as the visible board.");
            Debug.Log($"STRUCTURE_POSITION_ONLY_BOARD_TARGETS_OK: exactly 16 WorkbenchTop modules; rendered mesh count={boardMeshes.Count}; excluded both distant WorkbenchBase scenery objects; world span={boardWorldWidth:F3}x{boardWorldDepth:F3}m.");
            var boardCorners = ConvexHull(AssertViewportPoints("rendered full 16-module workbench board", boardWorldCorners));
            Require(boardCorners.Max(point => point.x) - boardCorners.Min(point => point.x) > 0.25f,
                "Rendered board footprint is implausibly narrow; inspect geometry selection before trusting occlusion QA.");
            var bottomMargin = dockCorners.Min(point => point.y);
            var leftMargin = dockCorners.Min(point => point.x);
            var rightMargin = 1f - dockCorners.Max(point => point.x);
            var boardDockClearance = boardCorners.Min(point => point.y) - dockCorners.Max(point => point.y);
            Require(bottomMargin >= 0.045f,
                $"Dock is too close to bottom edge: {bottomMargin:P2} ({bottomMargin * 1374f:F1}px), requires at least 4.5%/62px.");
            Require(leftMargin >= 0.05f && rightMargin >= 0.05f,
                $"Dock side margins are too small: left={leftMargin:P2}, right={rightMargin:P2}, requires at least 5% each.");
            Require(boardDockClearance >= 0.015f,
                $"Dock is not sufficiently below the actual rendered board: clearance={boardDockClearance:P2} ({boardDockClearance * 1374f:F1}px), requires at least 1.5%/20px.");
            Debug.Log($"STRUCTURE_POSITION_ONLY_DOCK_SPACING_OK: bottom={bottomMargin:P2}/{bottomMargin * 1374f:F1}px; left={leftMargin:P2}; right={rightMargin:P2}; board-to-dock={boardDockClearance:P2}/{boardDockClearance * 1374f:F1}px at 1372x1374 reference viewport.");
            var panels = new[] { (name: "PicoWorldHUD", polygon: mission), (name: "PicoQuickGuide", polygon: guide), (name: "PicoActionDock", polygon: dockCorners) };
            var occlusions = new List<string>();
            foreach (var panel in panels)
                if (ConvexPolygonsOverlap(panel.polygon, boardCorners)) occlusions.Add(panel.name + " overlaps rendered full workbench board");
            foreach (var screenName in new[] { "FrontProjectionPanel", "SideProjectionPanel" })
            {
                var screen = GameObject.Find(screenName)?.transform;
                Require(screen != null, "Missing actual projection-grid parent: " + screenName);
                var tiles = screen.GetComponentsInChildren<MeshFilter>(false).Where(filter => filter.sharedMesh != null &&
                    filter.name.StartsWith("ProjectionTile_", StringComparison.Ordinal) && filter.GetComponent<Renderer>()?.enabled == true).ToArray();
                Require(tiles.Length >= 12, screenName + " has fewer than 12 rendered grid tiles; cannot validate projection-grid occlusion.");
                var gridCorners = ConvexHull(AssertViewportPoints(screenName + " actual 12-tile grid", MeshWorldBoundsCorners(tiles)));
                Require(gridCorners.Max(point => point.x) - gridCorners.Min(point => point.x) > 0.10f,
                    screenName + " grid footprint is too narrow to be a valid screen test.");
                foreach (var panel in panels)
                {
                    if (!ConvexPolygonsOverlap(panel.polygon, gridCorners)) continue;
                    var covered = tiles.Where(tile => ConvexPolygonsOverlap(panel.polygon,
                        ConvexHull(MeshWorldBoundsCorners(new[] { tile }).Select(point => (Vector2)camera.WorldToViewportPoint(point))))).Select(tile => tile.name).ToArray();
                    occlusions.Add($"{panel.name} overlaps {screenName} actual grid; tiles: {string.Join(", ", covered)}");
                }
            }
            Require(occlusions.Count == 0, "Position-only spatial UI occludes gameplay geometry: " + string.Join("; ", occlusions));
            Require(!ConvexPolygonsOverlap(mission, guide) && !ConvexPolygonsOverlap(mission, dockCorners) && !ConvexPolygonsOverlap(guide, dockCorners),
                "Position-only opening makes persistent spatial panels overlap one another.");
            Debug.Log("STRUCTURE_POSITION_ONLY_COMPOSITION_OK: key canvas/display/full-rendered-board corners in viewport; persistent panels neither overlap each other, the rendered tabletop, nor either actual projection-tile grid.");
        }

        private static IEnumerable<Vector3> MeshWorldBoundsCorners(IEnumerable<MeshFilter> filters)
        {
            foreach (var filter in filters)
            {
                var bounds = filter.sharedMesh.bounds;
                foreach (var x in new[] { bounds.min.x, bounds.max.x })
                foreach (var y in new[] { bounds.min.y, bounds.max.y })
                foreach (var z in new[] { bounds.min.z, bounds.max.z })
                    yield return filter.transform.TransformPoint(new Vector3(x, y, z));
            }
        }

        private static Vector2[] ConvexHull(IEnumerable<Vector2> points)
        {
            var sorted = points.Distinct().OrderBy(point => point.x).ThenBy(point => point.y).ToArray();
            Require(sorted.Length >= 3, "Occlusion polygon has fewer than three distinct points.");
            var hull = new List<Vector2>();
            foreach (var point in sorted)
            {
                while (hull.Count >= 2 && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], point - hull[hull.Count - 1]) <= 0f) hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            var lower = hull.Count;
            for (var index = sorted.Length - 2; index >= 0; index--)
            {
                var point = sorted[index];
                while (hull.Count > lower && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], point - hull[hull.Count - 1]) <= 0f) hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
            Require(hull.Count >= 3, "Occlusion polygon is degenerate.");
            return hull.ToArray();
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool ConvexPolygonsOverlap(Vector2[] a, Vector2[] b)
        {
            foreach (var polygon in new[] { a, b })
            for (var index = 0; index < polygon.Length; index++)
            {
                var edge = polygon[(index + 1) % polygon.Length] - polygon[index];
                var axis = new Vector2(-edge.y, edge.x).normalized;
                var ap = a.Select(point => Vector2.Dot(point, axis)).ToArray();
                var bp = b.Select(point => Vector2.Dot(point, axis)).ToArray();
                if (ap.Max() <= bp.Min() + 0.001f || bp.Max() <= ap.Min() + 0.001f) return false;
            }
            return true;
        }

        private static void VerifyTutorialRuntimeXrPose(DesignPlayerStart authored)
        {
            var originalPosition = camera.transform.position;
            var originalRotation = camera.transform.rotation;
            var authoredPose = new PoseSnapshot(authored.transform);
            var probeObject = new GameObject("TutorialView_QA_TemporaryStart") { hideFlags = HideFlags.HideAndDontSave };
            var rigObject = new GameObject("TutorialView_QA_TemporaryRig") { hideFlags = HideFlags.HideAndDontSave };
            var headObject = new GameObject("TutorialView_QA_TemporaryHead") { hideFlags = HideFlags.HideAndDontSave };
            var handObject = new GameObject("TutorialView_QA_TemporaryHand") { hideFlags = HideFlags.HideAndDontSave };
            GameObject teachingProbe = null;
            try
            {
                var probe = probeObject.AddComponent<DesignPlayerStart>();
                probe.focalAnchor = authored.focalAnchor; probe.focalOffset = authored.focalOffset;
                probe.yaw = authored.yaw; probe.viewingDistance = authored.viewingDistance; probe.eyeHeight = authored.eyeHeight;
                probe.viewingLateralOffset = authored.viewingLateralOffset;
                probe.useReferenceViewPitch = authored.useReferenceViewPitch; probe.referenceViewPitch = authored.referenceViewPitch;
                probe.fallbackHeadLocalPosition = authored.fallbackHeadLocalPosition;
                var head = headObject.transform;
                head.SetParent(rigObject.transform, false);
                head.localPosition = new Vector3(0f, 1.60f, 0f);
                head.localRotation = Quaternion.identity;
                var headCamera = headObject.AddComponent<Camera>();
                headCamera.enabled = false;
                headCamera.fieldOfView = camera.fieldOfView;
                headCamera.aspect = camera.aspect;
                var hand = handObject.transform;
                hand.SetParent(rigObject.transform, false);
                hand.localPosition = new Vector3(0.25f, 1.20f, 0.40f);
                hand.localRotation = Quaternion.Euler(8f, 4f, 0f);
                var handPose = handObject.AddComponent<PicoControllerPose>();
                handPose.enabled = false;
                var bootstrap = rigObject.AddComponent<PicoRigBootstrap>();
                bootstrap.enabled = false;
                bootstrap.designStart = probe;
                bootstrap.xrCamera = headCamera;
                Require(!bootstrap.HasAppliedDesignedPose, "Temporary XR bootstrap incorrectly starts pose-ready.");
                // Clone the actual card, but never its gameplay owner. Use
                // the same runtime readiness gate without changing the live
                // scene's teaching pose or simulating the Android platform.
                var liveTeaching = GameObject.Find("GameplayInstructions_Pico");
                teachingProbe = UnityEngine.Object.Instantiate(liveTeaching);
                teachingProbe.name = "TutorialView_QA_ReadinessTeaching";
                teachingProbe.hideFlags = HideFlags.HideAndDontSave;
                var teaching = teachingProbe.GetComponent<PicoHeadLockedCanvas>();
                teaching.enabled = false;
                teachingProbe.GetComponent<Canvas>().enabled = true;
                teachingProbe.GetComponent<GraphicRaycaster>().enabled = false;
                foreach (var button in teachingProbe.GetComponentsInChildren<StructureUIButton>(true)) button.enabled = false;
                foreach (var collider in teachingProbe.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (Transform child in teachingProbe.transform) child.gameObject.SetActive(true);
                teaching.centerOnShow = true;
                teaching.RequestCenterOnNextShow();
                var preReadyPoint = teaching.fixedWorldPosition;
                Require(!teaching.TryCenterAfterPoseReady(headCamera, bootstrap.HasAppliedDesignedPose) &&
                        Vector3.Distance(teaching.fixedWorldPosition, preReadyPoint) < 0.0001f,
                    "Teaching card sampled an unready XR camera pose.");
                bootstrap.EnsureViewPitchOffset();
                var pivot = bootstrap.ViewPitchOffset;
                Require(pivot != null && head.parent == pivot.transform && hand.parent == pivot.transform,
                    "Runtime view pivot did not own the head and controller tracking roots.");
                bootstrap.ApplyDesignedPoseNow();
                Require(bootstrap.HasAppliedDesignedPose, "XR runtime pose-ready signal did not follow actual pose application.");
                var rootRotation = rigObject.transform.rotation;
                Require(Quaternion.Angle(rootRotation, Quaternion.Euler(0f, authored.yaw, 0f)) < 0.01f &&
                        Vector3.Dot(rigObject.transform.up, Vector3.up) > 0.99999f,
                    "Tutorial view tilts the floor/tracking root instead of its head-centered view pivot.");
                Require(Vector3.Distance(head.position, authored.DesignedEyePosition) < 0.002f &&
                        Quaternion.Angle(head.rotation, authored.DesignedEyeRotation) < 0.25f,
                    "Neutral runtime XR camera does not match the final authored reference eye and view pitch.");
                head.localPosition += new Vector3(0.11f, 0.04f, -0.09f);
                head.localRotation = Quaternion.Euler(-3f, 5f, 0f);
                pivot.ApplyCompensation();
                Require(teaching.TryCenterAfterPoseReady(headCamera, bootstrap.HasAppliedDesignedPose),
                    "Teaching card did not center after the runtime XR ready signal.");
                teaching.ApplyPose(headCamera);
                var teachingRect = teaching.GetComponent<RectTransform>();
                var centered = headCamera.WorldToViewportPoint(teachingRect.TransformPoint(teachingRect.rect.center));
                Require(Mathf.Abs(centered.x - 0.5f) <= 0.02f && Mathf.Abs(centered.y - 0.5f) <= 0.02f,
                    "Teaching card sampled authored/static pose instead of the actual ready tracked camera.");
                camera.transform.SetPositionAndRotation(head.position, head.rotation);
                Capture(camera, "02c-tutorial-ready-xr-actual-view.png");
                var centeredPose = new PoseSnapshot(teaching.transform);
                head.localPosition += new Vector3(0.18f, 0f, 0.08f);
                head.localRotation = Quaternion.Euler(-3f, 18f, 0f);
                pivot.ApplyCompensation();
                Require(!teaching.TryCenterAfterPoseReady(headCamera, true), "Teaching card resampled the eye every frame instead of once per show.");
                teaching.ApplyPose(headCamera);
                Require(Vector3.Distance(teaching.transform.position, centeredPose.position) < 0.0001f &&
                        Quaternion.Angle(teaching.transform.rotation, centeredPose.rotation) < 0.001f,
                    "Centered tutorial followed the head after its one-time spatial placement.");
                UnityEngine.Object.DestroyImmediate(teachingProbe);
                teachingProbe = null;
                head.localPosition = new Vector3(0f, 1.60f, 0f);
                head.localRotation = Quaternion.identity;
                bootstrap.ApplyDesignedPoseNow();
                var rootPosition = rigObject.transform.position;
                var initialHead = head.localPosition;
                var neutralHeight = head.position.y;
                var pitch = Quaternion.Euler(DesignPlayerStart.GameplayReferencePitch, 0f, 0f);
                camera.transform.SetPositionAndRotation(head.position, head.rotation);
                ApplyAll(camera);
                Capture(camera, "03a-runtime-xr-neutral-pose.png");
                foreach (var offset in new[] { new Vector3(0f, 0f, 0.40f), new Vector3(0.30f, 0f, 0f), new Vector3(-0.22f, 0f, -0.31f) })
                {
                    head.localPosition = initialHead + offset;
                    pivot.ApplyCompensation();
                    Require(Mathf.Abs(head.position.y - neutralHeight) < 0.0001f &&
                            Vector3.Distance(head.position, rootPosition + rootRotation * head.localPosition) < 0.001f,
                        "View-pivot compensation turns physical planar head motion into vertical motion.");
                }
                head.localPosition = initialHead + new Vector3(0.20f, 0.06f, -0.12f);
                head.localRotation = Quaternion.Euler(-7f, 14f, 2f);
                hand.localPosition = new Vector3(0.34f, 1.18f, 0.55f);
                hand.localRotation = Quaternion.Euler(12f, -8f, 3f);
                pivot.ApplyCompensation();
                Require(Quaternion.Angle(head.rotation, rootRotation * pitch * head.localRotation) < 0.02f &&
                        Vector3.Distance(hand.position - head.position, rootRotation * pitch * (hand.localPosition - head.localPosition)) < 0.002f &&
                        Quaternion.Angle(hand.rotation, rootRotation * pitch * hand.localRotation) < 0.02f,
                    "Relative tracked head/controller orientation or the controller ray is inconsistent with the view pivot.");
                camera.transform.SetPositionAndRotation(head.position, head.rotation);
                ApplyAll(camera);
                Capture(camera, "03b-runtime-xr-relative-head-motion.png");
                var rawPosition = head.localPosition;
                var rawRotation = head.localRotation;
                bootstrap.ApplyDesignedPoseNow();
                Require(Vector3.Distance(head.position, authored.DesignedEyePosition) < 0.002f &&
                        Vector3.Distance(head.localPosition, rawPosition) < 0.0001f && Quaternion.Angle(head.localRotation, rawRotation) < 0.001f,
                    "Runtime recenter overwrote the tracked local head pose or failed to restore the design eye.");
                Debug.Log("STRUCTURE_TUTORIAL_VIEW_XR_RUNTIME_OK: actual bootstrap pose-ready and pivot paths tested; neutral reference camera, upright root, planar motion height, tracked orientation, controller-relative pose/ray, and recenter passed.");
            }
            finally
            {
                if (teachingProbe != null) UnityEngine.Object.DestroyImmediate(teachingProbe);
                camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
                ApplyAll(camera);
                UnityEngine.Object.DestroyImmediate(rigObject);
                UnityEngine.Object.DestroyImmediate(probeObject);
            }
            Require(Vector3.Distance(authored.transform.position, authoredPose.position) < 0.0001f &&
                    Quaternion.Angle(authored.transform.rotation, authoredPose.rotation) < 0.001f,
                "Temporary runtime XR test changed the actual scene's authored anchor.");
        }

        private static void VerifyRuntimeXrPose(DesignPlayerStart authored)
        {
            if (IsTutorialView)
            {
                VerifyTutorialRuntimeXrPose(authored);
                return;
            }
            var originalPosition = camera.transform.position;
            var originalRotation = camera.transform.rotation;
            var authoredPosition = authored.transform.position;
            var authoredRotation = authored.transform.rotation;
            var probeObject = new GameObject("GuideCover_QA_TemporaryStart") { hideFlags = HideFlags.HideAndDontSave };
            var rigObject = new GameObject("GuideCover_QA_TemporaryRig") { hideFlags = HideFlags.HideAndDontSave };
            var headObject = new GameObject("GuideCover_QA_TemporaryHead") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var probe = probeObject.AddComponent<DesignPlayerStart>();
                probe.focalAnchor = authored.focalAnchor; probe.focalOffset = authored.focalOffset;
                probe.yaw = authored.yaw; probe.viewingDistance = authored.viewingDistance; probe.eyeHeight = authored.eyeHeight;
                probe.viewingLateralOffset = authored.viewingLateralOffset;
                probe.useReferenceViewPitch = authored.useReferenceViewPitch; probe.referenceViewPitch = authored.referenceViewPitch;
                probe.fallbackHeadLocalPosition = authored.fallbackHeadLocalPosition;
                var head = headObject.transform;
                head.SetParent(rigObject.transform, false);
                var initialLocalPosition = new Vector3(0f, 1.60f, 0f);
                head.localPosition = initialLocalPosition;
                head.localRotation = Quaternion.identity;
                probe.ApplyXrRigPose(rigObject.transform, head);
                Require(Vector3.Distance(head.position, authored.DesignedEyePosition) < 0.002f &&
                        Quaternion.Angle(head.rotation, Quaternion.Euler(IsPositionOnly ? 0f : 18f, 45f, 0f)) < 0.25f,
                    "Actual ApplyXrRigPose with neutral tracked head does not produce the reference eye/pitch.");
                Require(Vector3.Distance(head.localPosition, initialLocalPosition) < 0.0001f && Quaternion.Angle(head.localRotation, Quaternion.identity) < 0.001f,
                    "Applying the XR root overwrote the tracked local head pose.");
                camera.transform.SetPositionAndRotation(head.position, head.rotation);
                ApplyAll(camera);
                Capture(camera, "03a-runtime-xr-neutral-pose.png");
                var rootPosition = rigObject.transform.position;
                var rootRotation = rigObject.transform.rotation;
                if (IsPositionOnly)
                {
                    Require(Vector3.Dot(rigObject.transform.up, Vector3.up) > 0.99999f &&
                            Quaternion.Angle(rootRotation, Quaternion.Euler(0f, authored.yaw, 0f)) < 0.001f &&
                            Vector3.Dot(head.up, Vector3.up) > 0.99999f,
                        "Position-only camera must retain a yaw-only root and world-up neutral head.");
                    var worldHeight = head.position.y;
                    foreach (var offset in new[] { new Vector3(0f, 0f, 0.40f), new Vector3(0.30f, 0f, 0f), new Vector3(-0.22f, 0f, -0.31f) })
                    {
                        head.localPosition = initialLocalPosition + offset;
                        Require(Mathf.Abs(head.position.y - worldHeight) < 0.0001f,
                            "Horizontal tracked-head motion introduced vertical world movement; the XR root is tilted.");
                    }
                    head.localPosition = initialLocalPosition;
                    Debug.Log("STRUCTURE_POSITION_ONLY_TRACKING_UP_OK: neutral root/head up equals world up; forward/back/side local movement leaves world eye height unchanged.");
                }
                head.localPosition += new Vector3(0.25f, 0.06f, -0.12f);
                head.localRotation = Quaternion.Euler(-7f, 14f, 2f);
                var trackedPosition = head.localPosition;
                var trackedRotation = head.localRotation;
                Require(Vector3.Distance(head.position, rootPosition + rootRotation * trackedPosition) < 0.001f &&
                        Quaternion.Angle(head.rotation, rootRotation * trackedRotation) < 0.01f,
                    "Relative tracked-head translation or rotation is not preserved after the authored root pose.");
                camera.transform.SetPositionAndRotation(head.position, head.rotation);
                ApplyAll(camera);
                Capture(camera, "03b-runtime-xr-relative-head-motion.png");
                probe.ApplyXrRigPose(rigObject.transform, head);
                Require(Vector3.Distance(head.position, authored.DesignedEyePosition) < 0.002f &&
                        Vector3.Distance(head.localPosition, trackedPosition) < 0.0001f && Quaternion.Angle(head.localRotation, trackedRotation) < 0.001f,
                    "Recenter changed the tracked local head pose or failed to restore the designed eye point.");
                Debug.Log($"STRUCTURE_GUIDE_COVER_XR_POSE_OK: real ApplyXrRigPose on temporary root/head; neutral eye={authored.DesignedEyePosition:F3}, rotation={rootRotation.eulerAngles:F3}; tracked relative translation/rotation and recenter preserved.");
            }
            finally
            {
                camera.transform.SetPositionAndRotation(originalPosition, originalRotation);
                ApplyAll(camera);
                UnityEngine.Object.DestroyImmediate(headObject);
                UnityEngine.Object.DestroyImmediate(rigObject);
                UnityEngine.Object.DestroyImmediate(probeObject);
            }
            Require(Vector3.Distance(authored.transform.position, authoredPosition) < 0.0001f &&
                    Quaternion.Angle(authored.transform.rotation, authoredRotation) < 0.001f,
                "Temporary XR test changed the actual scene's authored anchor.");
        }

        private static void AssertNoVisibleScreenSpaceUi()
        {
            var legacy = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
                .Where(item => item.isActiveAndEnabled && item.renderMode != RenderMode.WorldSpace).ToArray();
            Require(legacy.Length == 0, "Legacy screen-space canvas is still visible: " + string.Join(", ", legacy.Select(item => item.name)));
        }

        private static void AssertPersistentUiVisible(bool includeGuide = true)
        {
            foreach (var name in includeGuide ? new[] { "PicoWorldHUD", "PicoQuickGuide", "PicoActionDock" } : new[] { "PicoWorldHUD", "PicoActionDock" })
            {
                var canvas = GameObject.Find(name)?.GetComponent<Canvas>();
                Require(canvas != null && canvas.isActiveAndEnabled && canvas.renderMode == RenderMode.WorldSpace,
                    name + " is not naturally visible as spatial UI.");
            }
            var buttons = GameObject.Find("PicoActionDock").GetComponentsInChildren<StructureUIButton>(false);
            Require(buttons.Length == 3 && buttons.Select(item => item.action).OrderBy(item => item).SequenceEqual(
                    new[] { StructureUIButtonAction.Hint, StructureUIButtonAction.Undo, StructureUIButtonAction.Reset }.OrderBy(item => item)),
                "The spatial action dock does not contain exactly Hint / Undo / Reset.");
        }

        private static void ClickSpatialButton(string hostName, StructureUIButtonAction action)
        {
            var host = GameObject.Find(hostName);
            var button = host != null ? host.GetComponentsInChildren<StructureUIButton>(false)
                .SingleOrDefault(item => item.action == action) : null;
            var events = EventSystem.current;
            Require(button != null && events != null && button.IsInteractable,
                $"Cannot pointer-click {hostName}/{action}: active button or EventSystem missing.");
            Canvas.ForceUpdateCanvases();
            var rect = button.GetComponent<RectTransform>();
            var screenPoint = camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
            Require(screenPoint.z > 0f, $"{hostName}/{action} is behind the camera.");
            var pointer = new PointerEventData(events)
            {
                position = new Vector2(screenPoint.x, screenPoint.y),
                button = PointerEventData.InputButton.Left,
                pointerId = -1,
                clickCount = 1,
                eligibleForClick = true
            };
            var results = new List<RaycastResult>();
            events.RaycastAll(pointer, results);
            var clickable = results.Select(result => new
                { result, handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(result.gameObject) })
                .FirstOrDefault(item => item.handler != null);
            Require(clickable != null && clickable.handler == button.gameObject,
                $"EventSystem raycast did not resolve {hostName}/{action}. First clickable: {clickable?.handler?.name ?? "none"}; hits: {string.Join(", ", results.Select(item => item.gameObject.name))}.");
            pointer.pointerCurrentRaycast = clickable.result;
            pointer.pointerPressRaycast = clickable.result;
            pointer.pointerPress = clickable.handler;
            Require(ExecuteEvents.Execute(clickable.handler, pointer, ExecuteEvents.pointerClickHandler),
                "Pointer click callback did not execute for " + action);
            lastPointerFrame = Time.frameCount;
            Debug.Log($"STRUCTURE_SPATIAL_POINTER_CLICK_OK: {hostName}/{action}; screen={pointer.position}; hit={clickable.result.gameObject.name}.");
        }

        private static void AssertScreenPlacement(string panelName, string screenName, string modelName)
        {
            var panel = FindFollower(panelName);
            var screen = GameObject.Find(screenName)?.transform;
            var model = GameObject.Find(modelName);
            Require(panel != null && screen != null && model != null && panel.spatialTarget == screen,
                panelName + " is not bound to its corresponding projection display.");
            var awayFromEye = (panel.transform.position - camera.transform.position).normalized;
            var readableSide = Vector3.Dot(panel.transform.forward, awayFromEye);
            var parallel = Mathf.Abs(Vector3.Dot(panel.transform.forward, screen.forward));
            Require(readableSide > 0.30f && parallel > 0.999f && Vector3.Dot(panel.transform.up, screen.up) > 0.999f,
                $"{panelName} is back-facing or is not aligned to its display. Front dot={readableSide:F3}; parallel={parallel:F3}.");
            var depthGap = -Vector3.Dot(panel.transform.position - screen.position, panel.transform.forward);
            Require(Mathf.Abs(depthGap - panel.spatialGap) < 0.005f && depthGap >= 0.10f,
                $"{panelName} has no separate forward space from its display: {depthGap:F3} m.");
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            Require(renderers.Length > 0, modelName + " has no renderer bounds.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var up = screen.up;
            var screenTop = Vector3.Dot(bounds.center, up) + Mathf.Abs(up.x) * bounds.extents.x +
                            Mathf.Abs(up.y) * bounds.extents.y + Mathf.Abs(up.z) * bounds.extents.z;
            var corners = new Vector3[4];
            panel.GetComponent<RectTransform>().GetWorldCorners(corners);
            var panelBottom = corners.Min(corner => Vector3.Dot(corner, up));
            var verticalGap = panelBottom - screenTop;
            Require(verticalGap >= 0.10f && verticalGap <= 0.18f,
                $"{panelName} must float above the display with clear separation, got {verticalGap:F3} m.");
            Debug.Log($"STRUCTURE_SPATIAL_SCREEN_PLACEMENT_OK: {panelName}; position={panel.transform.position:F3}; frontDot={readableSide:F3}; forwardGap={depthGap:F3}m; upperGap={verticalGap:F3}m.");
        }

        private static void ApplyAll(Camera value)
        {
            foreach (var follower in UnityEngine.Object.FindObjectsByType<PicoHeadLockedCanvas>(FindObjectsInactive.Include))
                follower.ApplyPose(value);
        }

        private static PicoHeadLockedCanvas FindFollower(string name) => GameObject.Find(name)?.GetComponent<PicoHeadLockedCanvas>();
        private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindAnyObjectByType<T>();

        private static void Snapshot(out PoseSnapshot a, out PoseSnapshot b, out PoseSnapshot c)
        {
            a = SnapshotOf(FindFollower("PicoWorldHUD"));
            b = SnapshotOf(FindFollower("PicoQuickGuide"));
            c = SnapshotOf(FindFollower("PicoActionDock"));
        }

        private static PoseSnapshot SnapshotOf(PicoHeadLockedCanvas follower) => new PoseSnapshot(follower != null ? follower.transform : null);
        private static bool Approximately(PoseSnapshot expected, PicoHeadLockedCanvas actual) => actual != null && Vector3.Distance(expected.position, actual.transform.position) < 0.002f && Quaternion.Angle(expected.rotation, actual.transform.rotation) < 0.5f;
        private readonly struct PoseSnapshot
        {
            public readonly Vector3 position;
            public readonly Quaternion rotation;
            public PoseSnapshot(Transform t) { position = t != null ? t.position : Vector3.positiveInfinity; rotation = t != null ? t.rotation : Quaternion.identity; }
        }

        private static void Capture(Camera value, string file, int width = 1372, int height = 1374)
        {
            var previous = RenderTexture.active;
            var previousTarget = value.targetTexture;
            var previousAspect = value.aspect;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var particles = UnityEngine.Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsInactive.Exclude)
                .Select(item => (item, enabled: item.enabled)).ToList();
            var trails = UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude)
                .Select(item => (item, enabled: item.enabled)).ToList();
            try
            {
                foreach (var item in particles) item.item.enabled = false;
                foreach (var item in trails) item.item.enabled = false;
                value.aspect = (float)width / height;
                value.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                value.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                image.Apply(false, false);
                File.WriteAllBytes(Path.Combine(Output, file), image.EncodeToPNG());
            }
            finally
            {
                foreach (var item in particles) if (item.item != null) item.item.enabled = item.enabled;
                foreach (var item in trails) if (item.item != null) item.item.enabled = item.enabled;
                value.targetTexture = previousTarget;
                value.aspect = previousAspect;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.Destroy(image);
            }
        }

        private static void Stop() { EditorApplication.update -= Tick; EditorApplication.ExitPlaymode(); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
