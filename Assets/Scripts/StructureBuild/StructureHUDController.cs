using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StructureBuild
{
    public enum StructureUIButtonAction
    {
        Hint,
        Undo,
        Reset,
        Overview,
        Front,
        Side,
        Continue,
        DismissInstructions,
    }

    public sealed class StructureHUDController : MonoBehaviour
    {
        public StructureGameController game;
        public StructureCameraViews cameraViews;
        public TMP_Text viewLabel;
        public TMP_Text messageLabel;
        public GameplayInstructionsOverlay instructions;
        public StructureAudioController audio;

        private void Awake()
        {
            if (game == null) game = FindAnyObjectByType<StructureGameController>();
            if (cameraViews == null) cameraViews = FindAnyObjectByType<StructureCameraViews>();
            if (instructions == null) instructions = FindAnyObjectByType<GameplayInstructionsOverlay>();
            if (audio == null) audio = FindAnyObjectByType<StructureAudioController>();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) cameraViews?.SetOverview();
            if (Keyboard.current.digit2Key.wasPressedThisFrame) cameraViews?.SetFront();
            if (Keyboard.current.digit3Key.wasPressedThisFrame) cameraViews?.SetSide();
            if (Keyboard.current.vKey.wasPressedThisFrame) cameraViews?.CycleView();
        }

        public void Execute(StructureUIButtonAction action)
        {
            switch (action)
            {
                case StructureUIButtonAction.Hint: game?.Hint(); break;
                case StructureUIButtonAction.Undo: game?.Undo(); break;
                case StructureUIButtonAction.Reset: game?.ResetCurrentLevel(); break;
                case StructureUIButtonAction.Overview: cameraViews?.SetOverview(); break;
                case StructureUIButtonAction.Front: cameraViews?.SetFront(); break;
                case StructureUIButtonAction.Side: cameraViews?.SetSide(); break;
                case StructureUIButtonAction.Continue: game?.ContinueAfterCompletion(); break;
                case StructureUIButtonAction.DismissInstructions: instructions?.Dismiss(); break;
            }
            RefreshViewLabel();
        }

        public void RefreshViewLabel()
        {
            if (viewLabel != null && cameraViews != null) viewLabel.text = $"视角 · {cameraViews.CurrentViewName}";
        }
    }

}
