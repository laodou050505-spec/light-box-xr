#if UNITY_EDITOR
using System;
using System.Linq;
using StructureBuild;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StructureBuild.Editor
{
    /// <summary>
    /// Applies the current compact PICO HUD composition to the authored
    /// gameplay scene.  Kept as an explicit small command so UI tuning never
    /// needs to touch the user's scene models, lights, table or projection
    /// boards.
    /// </summary>
    public static class StructureBuildUiLayoutApplier
    {
        private const string ScenePath = "Assets/Scenes/StructureBuild.unity";

        [MenuItem("Structure Build/Apply Current PICO UI Layout")]
        public static void ApplyCurrentPicoUiLayout()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying the PICO UI layout.");

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var root = Find("PicoWorldHUD");
            var dock = Find("PicoActionDock");
            var completion = Find("CompletionOverlay_Pico");
            if (root == null || dock == null || completion == null)
                throw new InvalidOperationException("PICO UI roots are missing. Install the gameplay presentation before applying this layout.");

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(940f, 390f);
            ConfigureFollower(root, 1.68f, 0.00116f, new Vector2(-0.66f, 0.88f));

            var info = FindChild(root.transform, "MissionReadout")?.GetComponent<RectTransform>();
            if (info == null) throw new InvalidOperationException("PICO MissionReadout is missing.");
            SetAnchors(info, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(896f, 326f), new Vector2(0f, 1f));
            LayoutReadout(info);

            var dockRect = dock.GetComponent<RectTransform>();
            dockRect.sizeDelta = new Vector2(1600f, 190f);
            ConfigureFollower(dock, 1.70f, 0.00125f, new Vector2(0f, -1.14f));

            // The heading occupies the large upper validation frame. The
            // two lower archive lines remain one rich-text mesh so PICO's
            // compositor receives a stable paired readout.
            var payload = FindChild(completion.transform, "CompletionPayload") as RectTransform;
            if (payload != null)
            {
                var payloadText = payload.GetComponent<TextMeshProUGUI>();
                var title = FindChild(completion.transform, "CompletionTitle") as RectTransform;
                if (title == null)
                {
                    var titleObject = new GameObject("CompletionTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    title = titleObject.GetComponent<RectTransform>();
                    title.SetParent(payload.parent, false);
                    var createdTitle = title.GetComponent<TextMeshProUGUI>();
                    createdTitle.font = payloadText.font;
                    createdTitle.fontSharedMaterial = payloadText.fontSharedMaterial;
                    createdTitle.color = new Color(0.82f, 0.96f, 1f, 1f);
                    createdTitle.raycastTarget = false;
                }

                var titleText = title.GetComponent<TextMeshProUGUI>();
                titleText.text = "结构档案完成";
                titleText.fontSize = 50f;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                titleText.overflowMode = TextOverflowModes.Overflow;
                SetAnchors(title, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 145f), new Vector2(940f, 84f), new Vector2(0.5f, 0.5f));

                payloadText.richText = true;
                payloadText.text = "<align=\"center\"><size=29><color=#B8DDE6>档案 01 已通过双面投影校验</color></size>\n<size=27><color=#73F4FF>准备进入档案 02</color></size></align>";
                payloadText.textWrappingMode = TextWrappingModes.NoWrap;
                payloadText.overflowMode = TextOverflowModes.Overflow;
                payloadText.alignment = TextAlignmentOptions.Center;
                payloadText.fontStyle = FontStyles.Normal;
                payloadText.lineSpacing = 30f;
                SetAnchors(payload, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -52f), new Vector2(940f, 120f), new Vector2(0.5f, 0.5f));

                var overlay = completion.GetComponent<LevelCompleteOverlay>();
                if (overlay != null)
                {
                    overlay.titleText = titleText;
                    overlay.combinedPayloadText = payloadText;
                    EditorUtility.SetDirty(overlay);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("STRUCTURE_PICO_UI_LAYOUT_OK: compact upper-left information readout, lower edge action dock and full completion copy applied without moving authored gameplay models.");
        }

        private static void LayoutReadout(RectTransform info)
        {
            LayoutText(info, "Level", 38f, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -18f), new Vector2(-42f, 52f), new Vector2(0f, 1f));
            LayoutText(info, "Rule", 25f, FontStyles.Normal, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -72f), new Vector2(-42f, 36f), new Vector2(0f, 1f));
            LayoutText(info, "Status", 28f, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -122f), new Vector2(-42f, 38f), new Vector2(0f, 1f));
            LayoutText(info, "CubeCount", 25f, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(60f, 24f), new Vector2(-8f, 34f), new Vector2(0f, 0f));
            LayoutText(info, "View", 25f, FontStyles.Bold, TextAlignmentOptions.Right,
                new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(12f, 24f), new Vector2(-34f, 34f), new Vector2(0f, 0f));
        }

        private static void LayoutText(RectTransform parent, string name, float size, FontStyles style, TextAlignmentOptions alignment,
            Vector2 min, Vector2 max, Vector2 position, Vector2 delta, Vector2 pivot)
        {
            var rect = FindChild(parent, name) as RectTransform;
            if (rect == null) return;
            var text = rect.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.fontSize = size;
                text.fontStyle = style;
                text.alignment = alignment;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
            SetAnchors(rect, min, max, position, delta, pivot);
        }

        private static void ConfigureFollower(GameObject root, float distance, float scale, Vector2 offset)
        {
            var follower = root.GetComponent<PicoHeadLockedCanvas>();
            if (follower == null) throw new InvalidOperationException(root.name + " is missing PicoHeadLockedCanvas.");
            follower.distance = distance;
            follower.worldScale = scale;
            follower.viewOffset = offset;
            follower.followRotation = true;
            EditorUtility.SetDirty(follower);
        }

        private static Transform FindChild(Transform parent, string name)
        {
            return parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
        }

        private static GameObject Find(string name)
        {
            return UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == name)?.gameObject;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 delta, Vector2 pivot)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = delta;
        }
    }
}
#endif
