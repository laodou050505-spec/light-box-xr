using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StructureBuild.Editor
{
    public static class StructureBuildStartupLayout
    {
        [MenuItem("Structure Build/Apply Closer Startup And Edge UI")]
        public static void ApplyAndSave()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/StructureBuild.unity", OpenSceneMode.Single);
            var authored = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Where(t => t.name.EndsWith("_Editable"))
                .ToDictionary(t => t, t => (t.parent, t.localPosition, t.localRotation, t.localScale));
            var start = UnityEngine.Object.FindAnyObjectByType<DesignPlayerStart>();
            start.viewingDistance = DesignPlayerStart.GameplayViewingDistance;
            start.eyeHeight = DesignPlayerStart.GameplayEyeHeight;
            start.SynchronizeAnchor();
            EditorUtility.SetDirty(start);
            var views = UnityEngine.Object.FindAnyObjectByType<StructureCameraViews>();
            views.overviewDistance = start.DerivedDesktopDistance;
            start.ApplyDesktopPose(views.orbit);
            EditorUtility.SetDirty(views);
            EditorUtility.SetDirty(views.orbit);
            var rig = UnityEngine.Object.FindAnyObjectByType<PicoRigBootstrap>();
            start.ApplyXrRigPose(rig.transform);
            foreach (var follower in UnityEngine.Object.FindObjectsByType<PicoHeadLockedCanvas>(FindObjectsInactive.Include))
            {
                if (follower.name == "PicoWorldHUD" || follower.name == "PicoQuickGuide")
                    follower.viewOffset.y = StructureBuildOpticalPanels.UpperPanelOffset;
                else if (follower.name == "PicoActionDock")
                    follower.viewOffset.y = StructureBuildOpticalPanels.LowerPanelOffset;
                else continue;
                EditorUtility.SetDirty(follower);
            }
            var desktop = GameObject.Find("DesktopHUD").transform;
            SetDesktopY(desktop, "MissionReadout", -16f);
            SetDesktopY(desktop, "GuidePanel", -16f);
            SetDesktopY(desktop, "ActionDock", 12f);
            foreach (var entry in authored)
                if (entry.Key == null || (entry.Key.parent, entry.Key.localPosition, entry.Key.localRotation, entry.Key.localScale) != entry.Value)
                    throw new InvalidOperationException("Authored object changed: " + entry.Key?.name);
            StructureBuildCurrentSceneInstaller.ValidateInstalledCurrentScene();
            StructureBuildOpticalPanels.ValidatePanels();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"STRUCTURE_STARTUP_LAYOUT_OK: eye={start.DesignedEyePosition:F3}; distance={start.DerivedDesktopDistance:F3}; upper=0.94; lower=-1.13; authored objects preserved.");
        }

        private static void SetDesktopY(Transform desktop, string name, float y)
        {
            var rect = desktop.Find(name).GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }
    }
}
