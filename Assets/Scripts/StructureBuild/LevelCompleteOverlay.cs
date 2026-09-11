using TMPro;
using UnityEngine;

namespace StructureBuild
{
    /// <summary>
    /// Keeps the campaign sequential by pausing on a completed level until the player confirms the next step.
    /// </summary>
    public sealed class LevelCompleteOverlay : MonoBehaviour
    {
        public StructureGameController game;
        public GameObject panelRoot;
        public TMP_Text titleText;
        public TMP_Text detailText;
        public TMP_Text progressText;
        public TMP_Text continueLabel;
        [Tooltip("PICO can render a previously-hidden world canvas more reliably when the completion copy is emitted by one TMP mesh.")]
        public TMP_Text combinedPayloadText;
        public StructureAudioController audio;
        [Tooltip("Optional placement helper for non-following completion cards.")]
        public CompletionPanelPresenter worldPresenter;

        private int shownLevel = -1;
        // PICO's compositor occasionally keeps the meshes of a previously
        // inactive nested canvas layer stale for its first submitted frame.
        // Retrying the TMP rebuild for a handful of frames makes the title,
        // result detail and next-archive text as reliable as the button label.
        private int pendingTextRefreshFrames;

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<StructureGameController>();
            if (audio == null) audio = FindAnyObjectByType<StructureAudioController>();
            Hide();
            if (game != null) game.onLevelComplete.AddListener(Show);
        }

        private void OnDestroy()
        {
            if (game != null) game.onLevelComplete.RemoveListener(Show);
        }

        private void Update()
        {
            if (game == null || panelRoot == null || !panelRoot.activeSelf) return;
            if (!game.IsLevelCompleted || game.CurrentLevelNumber != shownLevel) Hide();
            if (pendingTextRefreshFrames <= 0) return;
            pendingTextRefreshFrames--;
            RefreshTextMeshes();
        }

        public void Show()
        {
            if (game == null || panelRoot == null) return;
            shownLevel = game.CurrentLevelNumber;
            var finalLevel = shownLevel >= game.TotalLevelCount;
            var title = finalLevel ? "全部结构已归档" : "结构档案完成";
            var detail = finalLevel
                ? "12 个空间结构均已通过双面投影校验"
                : $"档案 {shownLevel:00} 已通过双面投影校验";
            var progress = finalLevel
                ? "本轮探索完成"
                : $"准备进入档案 {shownLevel + 1:00}";
            if (combinedPayloadText != null)
            {
                // PICO's compositor is most reliable when the two lower
                // archive readouts remain one mesh. The heading is a stable
                // dedicated element in the upper verification frame.
                if (titleText != null)
                {
                    titleText.alignment = TextAlignmentOptions.Center;
                    titleText.text = title;
                }
                combinedPayloadText.alignment = TextAlignmentOptions.Center;
                combinedPayloadText.lineSpacing = 30f;
                combinedPayloadText.text = $"<align=\"center\"><size=25><color=#B8DDE6>{detail}</color></size>\n<size=23><color=#73F4FF>{progress}</color></size></align>";
            }
            else
            {
                if (titleText != null) titleText.text = title;
                if (detailText != null) detailText.text = detail;
                if (progressText != null) progressText.text = progress;
            }
            if (continueLabel != null) continueLabel.text = finalLevel ? "重新开始" : "下一关";
            // Sample the active gaze once. The completion card then stays
            // in the room so the player can lean and look around it.
            GetComponent<PicoHeadLockedCanvas>()?.CaptureWorldPose();
            panelRoot.SetActive(true);
            pendingTextRefreshFrames = 4;
            RefreshTextMeshes();
        }

        public void Continue()
        {
            if (game == null || !game.IsLevelCompleted) return;
            game.ContinueAfterCompletion();
            Hide();
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            pendingTextRefreshFrames = 0;
        }

        private void RefreshTextMeshes()
        {
            Refresh(titleText);
            Refresh(detailText);
            Refresh(progressText);
            Refresh(combinedPayloadText);
            Refresh(continueLabel);
            Canvas.ForceUpdateCanvases();
        }

        private static void Refresh(TMP_Text text)
        {
            if (text == null) return;
            text.enabled = true;
            if (!text.gameObject.activeSelf) text.gameObject.SetActive(true);
            text.SetAllDirty();
            text.ForceMeshUpdate(true, true);
        }
    }
}
