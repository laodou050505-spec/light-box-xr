using TMPro;
using UnityEngine;

namespace StructureBuild
{
    public sealed class StructureScreenHUD : MonoBehaviour
    {
        public StructureGameController game;
        public StructureHUDController actions;
        public Canvas canvas;
        public bool hideWhenXRActive = true;
        public bool showOnlyWhenXRActive;
        public TMP_Text levelText;
        public TMP_Text ruleText;
        public TMP_Text statusText;
        public TMP_Text cubeText;
        public TMP_Text viewText;

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<StructureGameController>();
            if (actions == null) actions = FindAnyObjectByType<StructureHUDController>();
            if (canvas == null) canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            if (game == null) return;
            // The Editor may report a virtual headset during Game View
            // preview. That is still the desktop QA presentation, not the
            // PICO layout. Restrict the alternate HUD route to the packaged
            // Android player so the two operation docks can never overlap.
            var immersiveRuntime = Application.platform == RuntimePlatform.Android;
            if (canvas != null)
            {
                var visibleForMode = (!hideWhenXRActive || !immersiveRuntime) && (!showOnlyWhenXRActive || immersiveRuntime);
                canvas.enabled = visibleForMode;
            }
            if (levelText != null) levelText.text = $"结构档案 {game.CurrentLevelNumber:00} / {game.TotalLevelCount:00}  ·  {game.CurrentLevelName}";
            if (ruleText != null) ruleText.text = game.CurrentLevelRule;
            if (statusText != null) statusText.text = game.CurrentStatusText;
            if (cubeText != null) cubeText.text = $"结构单元  {game.CurrentCubeCount} / {game.CurrentCubeBudget}";
            if (viewText != null && actions != null && actions.cameraViews != null) viewText.text = $"视角  {actions.cameraViews.CurrentViewName}";
        }
    }

}
