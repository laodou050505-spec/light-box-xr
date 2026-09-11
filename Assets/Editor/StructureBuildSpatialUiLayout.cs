#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StructureBuild.Editor
{
    public static class StructureBuildSpatialUiLayout
    {
        private const string GameplayScene = "Assets/Scenes/StructureBuild.unity";
        private const string TitleScene = "Assets/Scenes/StructureBuildTitle.unity";
        private const float ScreenGap = 0.13f;
        // Author once from the reference view, then keep fixed during play.
        private static readonly Vector3 OpeningEye = new Vector3(0f, DesignPlayerStart.GameplayEyeHeight, 0f) +
            Quaternion.Euler(0f, 45f, 0f) * (Vector3.back * DesignPlayerStart.GameplayViewingDistance +
                Vector3.right * DesignPlayerStart.GameplayLateralOffset);
        public static readonly Vector3 DockWorldPoint = OpeningEye +
            Quaternion.Euler(DesignPlayerStart.GameplayReferencePitch, 45f, 0f) * new Vector3(0f, -1.59f, 2.3f);

        [MenuItem("Structure Build/Apply Spatial Screen UI and Feathered Cover")]
        public static void ApplyAndSave()
        {
            ApplyGameplay();
            ApplyTitle();
            AssetDatabase.SaveAssets();
        }

        private static void ApplyGameplay()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
            ApplyToLoadedGameplayScene();
            StructureBuildCurrentSceneInstaller.ValidateInstalledCurrentScene();
            StructureBuildOpticalPanels.ValidatePanels();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ApplyToLoadedGameplayScene()
        {
            ConfigureDesktopSpatialRoute();
            var start = Find("DesignPlayerStart_Anchor").GetComponent<DesignPlayerStart>();
            start.viewingDistance = DesignPlayerStart.GameplayViewingDistance;
            start.eyeHeight = DesignPlayerStart.GameplayEyeHeight;
            start.viewingLateralOffset = DesignPlayerStart.GameplayLateralOffset;
            start.useReferenceViewPitch = true;
            start.referenceViewPitch = DesignPlayerStart.GameplayReferencePitch;
            start.SynchronizeAnchor();
            var views = Find("DesktopCamera").GetComponent<StructureCameraViews>();
            views.designStart = start;
            views.orbit.minPitch = 0f;
            views.overviewPitch = start.referenceViewPitch;
            views.overviewDistance = start.DerivedDesktopDistance;
            start.ApplyDesktopPose(views.orbit);
            start.ApplyXrRigPose(Find("XR Rig").transform);
            var previewCamera = views.GetComponent<Camera>();
            previewCamera.fieldOfView = 80f;
            EditorUtility.SetDirty(start);
            EditorUtility.SetDirty(views);
            EditorUtility.SetDirty(views.orbit);
            EditorUtility.SetDirty(previewCamera);
            var mission = Find("PicoWorldHUD").GetComponent<PicoHeadLockedCanvas>();
            var guide = Find("PicoQuickGuide").GetComponent<PicoHeadLockedCanvas>();
            var dock = Find("PicoActionDock").GetComponent<PicoHeadLockedCanvas>();
            ConfigureScreenPanel(mission, "FrontProjectionPanel", "FrontProjectionModel_Editable");
            ConfigureScreenPanel(guide, "SideProjectionPanel", "SideProjectionModel_Editable");
            dock.spatialTarget = null;
            dock.fixedWorldAnchor = true;
            dock.viewOffset = new Vector2(0f, StructureBuildOpticalPanels.LowerPanelOffset);
            // Preserve the room's existing UI point when the player moves.
            var openingRotation = start.DesignedEyeRotation;
            dock.fixedWorldPosition = DockWorldPoint;
            dock.fixedWorldEuler = openingRotation.eulerAngles;
            dock.followRotation = true;
            dock.ApplyPose(previewCamera);
            if (Vector3.Distance(dock.transform.position, dock.fixedWorldPosition) > 0.001f)
                throw new InvalidOperationException("The action dock did not apply its authored fixed-world position.");
            var teaching = Find("GameplayInstructions_Pico").GetComponent<PicoHeadLockedCanvas>();
            teaching.spatialTarget = null;
            teaching.fixedWorldAnchor = true;
            teaching.followRotation = false;
            teaching.centerOnShow = true;
            teaching.viewOffset = Vector2.zero;
            teaching.fixedWorldPosition = start.DesignedEyePosition + openingRotation * Vector3.forward * teaching.distance;
            teaching.fixedWorldEuler = openingRotation.eulerAngles;
            teaching.ApplyPose(previewCamera);
            EditorUtility.SetDirty(teaching);
            EditorUtility.SetDirty(mission); EditorUtility.SetDirty(guide); EditorUtility.SetDirty(dock);
            ValidateSpatialPlacement(previewCamera, mission, guide, dock, teaching);
            Debug.Log($"STRUCTURE_SPATIAL_UI_OK: upper panels follow Front/Side projection screens with {ScreenGap:F2}m gap; dock fixed at {dock.fixedWorldPosition:F3} and view-rotating; opening pitch={start.referenceViewPitch:F1} degrees.");
        }

        private static void ValidateSpatialPlacement(Camera camera, PicoHeadLockedCanvas mission,
            PicoHeadLockedCanvas guide, PicoHeadLockedCanvas dock, PicoHeadLockedCanvas teaching)
        {
            foreach (var panel in new[] { mission, guide })
            {
                if (panel.spatialTarget == null || panel.fixedWorldAnchor)
                    throw new InvalidOperationException($"{panel.name} is missing its screen anchor.");
                var towardEye = camera.transform.position - panel.transform.position;
                if (Vector3.Dot(-panel.transform.forward, towardEye) <= 0f)
                    throw new InvalidOperationException($"{panel.name} faces away from the designed eye.");
                var mountingPoint = panel.spatialTarget.position + panel.spatialTarget.rotation * panel.spatialLocalOffset;
                var outwardGap = Vector3.Dot(panel.transform.position - mountingPoint, -panel.transform.forward);
                if (Mathf.Abs(panel.spatialGap - ScreenGap) > 0.001f || Mathf.Abs(outwardGap - ScreenGap) > 0.001f)
                    throw new InvalidOperationException($"{panel.name} does not retain its screen clearance.");
            }
            foreach (var panel in new[] { dock, teaching })
            {
                if (!panel.fixedWorldAnchor || panel.spatialTarget != null || panel.fixedWorldPosition.sqrMagnitude < 0.000001f)
                    throw new InvalidOperationException($"{panel.name} is missing its nonzero fixed-world anchor.");
            }
        }

        private static void ConfigureDesktopSpatialRoute()
        {
            var instructions = Find("GameSystems").GetComponent<GameplayInstructionsOverlay>();
            instructions.useSpatialUiOnDesktop = true;
            EditorUtility.SetDirty(instructions);
            foreach (var name in new[] { "PicoWorldHUD", "PicoQuickGuide", "PicoActionDock" })
            {
                var root = Find(name);
                var visibility = root.GetComponent<PicoRuntimeCanvasVisibility>();
                visibility.showOnDesktop = true;
                visibility.hideAfterCompletedLevel = name == "PicoQuickGuide" ? 3 : 0;
                visibility.canvas = root.GetComponent<Canvas>();
                EditorUtility.SetDirty(visibility);
                var hud = root.GetComponent<StructureScreenHUD>();
                if (hud != null)
                {
                    hud.showWorldSpaceOnDesktop = true;
                    EditorUtility.SetDirty(hud);
                }
            }
            var legacyHud = Find("DesktopHUD");
            legacyHud.GetComponent<StructureScreenHUD>().showWorldSpaceOnDesktop = true;
            EditorUtility.SetDirty(legacyHud.GetComponent<StructureScreenHUD>());
            // Keep original objects/bindings recoverable but disable the
            // entire old screen route, including its nested completion card.
            legacyHud.SetActive(false);
            Find("GameplayInstructions_Desktop").SetActive(false);
            Find("CompletionOverlay_Desktop").SetActive(false);
            Debug.Log("STRUCTURE_DESKTOP_SPATIAL_ROUTE_OK: desktop and PICO share spatial tutorial/HUD/dock; legacy screen-space roots are disabled.");
        }

        private static void ApplyTitle()
        {
            var scene = EditorSceneManager.OpenScene(TitleScene, OpenSceneMode.Single);
            StructureBuildTitleSceneInstaller.ApplyToLoadedScene();
            StructureBuildTitleSceneInstaller.ValidateTitleScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("STRUCTURE_TITLE_FEATHER_OK: upright cover at the designed viewing distance with 6.5% edge feather.");
        }

        private static void ConfigureScreenPanel(PicoHeadLockedCanvas panel, string screenName, string modelName)
        {
            var screen = Find(screenName).transform;
            var start = Find("DesignPlayerStart_Anchor").GetComponent<DesignPlayerStart>();
            var renderers = Find(modelName).GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Screen model has no renderers: " + modelName);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            // Both readouts retain the same text scale and height. They sit
            // above the physical screen rim, leaving the projection grid clear.
            panel.worldScale = 0.0046f;
            panel.spatialTarget = screen;
            panel.spatialGap = ScreenGap;
            panel.fixedWorldAnchor = false;
            var away = Vector3.Dot(screen.forward, screen.position - start.DesignedEyePosition) >= 0f;
            panel.spatialRotationOffset = new Vector3(0f, away ? 0f : 180f, 0f);
            var centre = Quaternion.Inverse(screen.rotation) * (bounds.center - screen.position);
            var halfHeight = panel.GetComponent<RectTransform>().sizeDelta.y * panel.worldScale * 0.5f;
            var screenHalfHeight = Mathf.Abs(screen.up.x) * bounds.extents.x
                                 + Mathf.Abs(screen.up.y) * bounds.extents.y
                                 + Mathf.Abs(screen.up.z) * bounds.extents.z;
            panel.spatialLocalOffset = new Vector3(centre.x, centre.y + screenHalfHeight + ScreenGap + halfHeight, 0f);
            panel.ApplyPose(Find("DesktopCamera").GetComponent<Camera>());
            EditorUtility.SetDirty(panel);
        }

        private static GameObject Find(string name)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == name) return t.gameObject;
            throw new InvalidOperationException("Missing scene object: " + name);
        }
    }
}
#endif
