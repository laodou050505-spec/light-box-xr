using System.Collections;
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
            var title = finalLevel ? "All Archives Complete" : "Archive Complete";
            var detail = finalLevel
                ? "All 12 spatial structures passed dual-projection verification"
                : $"Archive {shownLevel:00} passed dual-projection verification";
            var progress = finalLevel
                ? "Exploration complete"
                : $"Ready for Archive {shownLevel + 1:00}";
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
            if (continueLabel != null) continueLabel.text = finalLevel ? "Restart" : "Next";
            // Sample the active gaze once. The completion card then stays
            // in the room so the player can lean and look around it.
            GetComponent<PicoHeadLockedCanvas>()?.CaptureWorldPose();
            panelRoot.SetActive(true);
            // Polish: brief scale-entrance punch so the card pops open rather than just appearing
            StartCoroutine(EntrancePunch());
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

        // Polish 2026-10-22: scale-punch entrance so the card feels satisfying rather than just appearing
        private IEnumerator EntrancePunch()
        {
            if (panelRoot == null) yield break;
            var endScale   = panelRoot.transform.localScale;
            var startScale = endScale * 0.84f;
            const float duration = 0.20f;
            panelRoot.transform.localScale = startScale;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                // ease-out cubic: fast open, smooth settle
                var t    = Mathf.Clamp01(elapsed / duration);
                var ease = 1f - Mathf.Pow(1f - t, 3f);
                panelRoot.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, ease);
                yield return null;
            }
            panelRoot.transform.localScale = endScale;
        }
    }
}
