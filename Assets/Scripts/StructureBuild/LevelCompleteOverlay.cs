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
        public StructureAudioController audio;

        private int shownLevel = -1;

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
        }

        public void Show()
        {
            if (game == null || panelRoot == null) return;
            shownLevel = game.CurrentLevelNumber;
            var finalLevel = shownLevel >= game.TotalLevelCount;
            if (titleText != null) titleText.text = finalLevel ? "全部结构已归档" : "结构档案完成";
            if (detailText != null) detailText.text = finalLevel
                ? "12 个空间结构均已通过双面投影校验"
                : $"档案 {shownLevel:00} 已通过双面投影校验";
            if (progressText != null) progressText.text = finalLevel
                ? "本轮探索完成"
                : $"准备进入档案 {shownLevel + 1:00}";
            if (continueLabel != null) continueLabel.text = finalLevel ? "重新开始" : "下一关";
            panelRoot.SetActive(true);
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
        }
    }
}
