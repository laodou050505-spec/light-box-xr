using UnityEngine;
using UnityEngine.UI;

namespace StructureBuild
{
    /// <summary>
    /// Controls the shared spatial HUD on PICO and desktop. Tutorial visibility
    /// comes from its state owner rather than another component's Canvas flag,
    /// so component update order cannot revive or suppress the wrong canvas.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class PicoRuntimeCanvasVisibility : MonoBehaviour
    {
        public Canvas canvas;
        public bool showOnDesktop;
        [Min(0), Tooltip("Hide after this level is completed for the rest of the current campaign run. Zero keeps the canvas available.")]
        public int hideAfterCompletedLevel;

        private GameplayInstructionsOverlay instructions;
        private StructureGameController game;
        private GraphicRaycaster raycaster;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            raycaster = GetComponent<GraphicRaycaster>();
            instructions = FindAnyObjectByType<GameplayInstructionsOverlay>();
            game = FindAnyObjectByType<StructureGameController>();
            if (game != null) game.CampaignProgressChanged += RefreshVisibility;
            RefreshVisibility();
        }

        private void OnDestroy()
        {
            if (game != null) game.CampaignProgressChanged -= RefreshVisibility;
        }

        private void Update() => RefreshVisibility();

        public void RefreshVisibility()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (instructions == null) instructions = FindAnyObjectByType<GameplayInstructionsOverlay>();
            if (canvas == null) return;
            var spatialRoute = showOnDesktop || Application.platform == RuntimePlatform.Android ||
                               (instructions != null && instructions.UsesSpatialUi);
            var tutorialVisible = instructions != null && instructions.UsesSpatialUi && instructions.IsVisible;
            var guideExpired = hideAfterCompletedLevel > 0 && game != null &&
                               (game.HighestCompletedLevel >= hideAfterCompletedLevel ||
                                game.CurrentLevelNumber > hideAfterCompletedLevel);
            canvas.enabled = spatialRoute && !tutorialVisible && !guideExpired;
            if (raycaster == null) raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster != null) raycaster.enabled = canvas.enabled;
        }
    }
}
