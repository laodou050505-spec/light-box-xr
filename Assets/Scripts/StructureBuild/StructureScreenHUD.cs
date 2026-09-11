using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StructureBuild
{
    public sealed class StructureScreenHUD : MonoBehaviour
    {
        public StructureGameController game;
        public StructureHUDController actions;
        public Canvas canvas;
        public bool hideWhenXRActive = true;
        public bool showOnlyWhenXRActive;
        public bool showWorldSpaceOnDesktop;
        public TMP_Text levelText;
        public TMP_Text ruleText;
        public TMP_Text statusText;
        public TMP_Text cubeText;
        public TMP_Text viewText;

        private GameplayInstructionsOverlay instructions;
        private GraphicRaycaster raycaster;

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<StructureGameController>();
            if (actions == null) actions = FindAnyObjectByType<StructureHUDController>();
            if (canvas == null) canvas = GetComponent<Canvas>();
            raycaster = GetComponent<GraphicRaycaster>();
            instructions = FindAnyObjectByType<GameplayInstructionsOverlay>();
        }

        private void Update()
        {
            RefreshVisibility();
            if (game == null) return;
            if (levelText != null) levelText.text = $"结构档案 {game.CurrentLevelNumber:00} / {game.TotalLevelCount:00}  ·  {game.CurrentLevelName}";
            if (ruleText != null) ruleText.text = game.CurrentLevelRule;
            if (statusText != null) statusText.text = game.CurrentStatusText;
            if (cubeText != null) cubeText.text = $"结构单元  {game.CurrentCubeCount} / {game.CurrentCubeBudget}";
            if (viewText != null && actions != null && actions.cameraViews != null) viewText.text = $"视角  {actions.cameraViews.CurrentViewName}";
        }

        public void RefreshVisibility()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (instructions == null) instructions = FindAnyObjectByType<GameplayInstructionsOverlay>();
            if (canvas == null) return;
            var immersiveRuntime = showWorldSpaceOnDesktop || Application.platform == RuntimePlatform.Android ||
                                   (instructions != null && instructions.UsesSpatialUi);
            var visibleForMode = (!hideWhenXRActive || !immersiveRuntime) && (!showOnlyWhenXRActive || immersiveRuntime);
            if (canvas.renderMode == RenderMode.WorldSpace && instructions != null && instructions.UsesSpatialUi && instructions.IsVisible)
                visibleForMode = false;
            canvas.enabled = visibleForMode;
            if (raycaster == null) raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = canvas.enabled;
        }
    }

}
