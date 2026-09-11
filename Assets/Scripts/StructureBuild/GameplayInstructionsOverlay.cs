using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace StructureBuild
{
    /// <summary>
    /// First-run play prompt. Both canvases share this state so desktop and PICO
    /// never show two independent instruction flows.
    /// </summary>
    public sealed class GameplayInstructionsOverlay : MonoBehaviour
    {
        public GameObject desktopPanelRoot;
        public GameObject picoPanelRoot;
        public Canvas desktopCanvas;
        public Canvas picoCanvas;
        public StructureAudioController audio;
        // The isolated title scene owns the title stage. When enabled here,
        // this post-load teaching card gates practice until it is dismissed.
        public bool visibleAtStartup;
        public bool useSpatialUiOnDesktop;

        private bool dismissed;
        private bool initialized;
        private bool waitingForInputRelease;
        private int dismissedFrame = -1;
        private int leftHeldButtons;
        private int rightHeldButtons;
        private StructureGameController game;
        private WorldSpaceInteraction[] worldInteractions;
        private PicoRuntimeCanvasVisibility[] spatialHudVisibility;
        private StructureScreenHUD[] huds;
        private bool appliedVisibility;
        private bool lastVisible;
        private bool lastSpatialRoute;
        // Awake order is not guaranteed: gameplay must already respect the
        // authored startup state before this component initializes.
        public bool IsVisible => initialized ? !dismissed : visibleAtStartup;
        public bool UsesSpatialUi => useSpatialUiOnDesktop || Application.platform == RuntimePlatform.Android;
        public bool IsGameplayInputAllowed
        {
            get
            {
                if (IsVisible)
                {
                    // Remember actual held controls before a controller can
                    // disappear between confirming the card and the next frame.
                    ObserveControllerControls();
                    return false;
                }
                if (!waitingForInputRelease) return true;
                var controlsHeld = IsGameplayControlHeld();
                // The closing click/trigger belongs exclusively to the card,
                // including Update callbacks later in this same frame.
                if (Time.frameCount <= dismissedFrame || controlsHeld) return false;
                waitingForInputRelease = false;
                return true;
            }
        }

        private void Awake()
        {
            if (audio == null) audio = FindAnyObjectByType<StructureAudioController>();
            game = GetComponent<StructureGameController>();
            if (game == null) game = FindAnyObjectByType<StructureGameController>();
            worldInteractions = FindObjectsByType<WorldSpaceInteraction>(FindObjectsInactive.Include);
            spatialHudVisibility = FindObjectsByType<PicoRuntimeCanvasVisibility>(FindObjectsInactive.Include);
            huds = FindObjectsByType<StructureScreenHUD>(FindObjectsInactive.Include);
            dismissed = !visibleAtStartup;
            initialized = true;
            ApplyVisibility();
        }

        private void Update()
        {
            // Observe release even when there is no attempted board action.
            _ = IsGameplayInputAllowed;
            ApplyVisibility();
        }

        public void Dismiss()
        {
            if (dismissed) return;
            ObserveControllerControls();
            dismissed = true;
            dismissedFrame = Time.frameCount;
            waitingForInputRelease = true;
            ApplyVisibility();
        }

        public void Show()
        {
            dismissed = false;
            // Cancelling through the controller restores a grabbed existing
            // cube to its original occupied cell and clears all previews.
            if (game != null) game.CancelDrag();
            ApplyVisibility();
        }

        private bool IsGameplayControlHeld()
        {
            // Sample both hands before early-returning for a desktop key.
            // Invalid/untracked devices retain only buttons previously held;
            // an absent controller that was never held cannot lock desktop.
            var controllersHeld = ObserveControllerControls();
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed)) return true;
            var keyboard = Keyboard.current;
            if (keyboard != null &&
                (keyboard.enterKey.isPressed || keyboard.spaceKey.isPressed ||
                 keyboard.zKey.isPressed || keyboard.rKey.isPressed || keyboard.hKey.isPressed ||
                 keyboard.deleteKey.isPressed || keyboard.backspaceKey.isPressed ||
                 keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed ||
                 keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed)) return true;
            if (controllersHeld) return true;
            if (worldInteractions != null)
                foreach (var interaction in worldInteractions)
                {
                    if (interaction == null) continue;
                    if (interaction.selectAction.action != null && interaction.selectAction.action.IsPressed()) return true;
                    if (interaction.secondaryAction.action != null && interaction.secondaryAction.action.IsPressed()) return true;
                }
            return false;
        }

        private bool ObserveControllerControls()
        {
            leftHeldButtons = ObserveControllerButtons(XRNode.LeftHand, leftHeldButtons);
            rightHeldButtons = ObserveControllerButtons(XRNode.RightHand, rightHeldButtons);
            return leftHeldButtons != 0 || rightHeldButtons != 0;
        }

        private static int ObserveControllerButtons(XRNode node, int heldButtons)
        {
            var controller = InputDevices.GetDeviceAtXRNode(node);
            if (!controller.isValid ||
                !controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var tracked) || !tracked)
                return heldButtons;
            var validButtons = 0;
            var pressedButtons = 0;
            if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var trigger))
            { validButtons |= 1; if (trigger) pressedButtons |= 1; }
            if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out var grip))
            { validButtons |= 2; if (grip) pressedButtons |= 2; }
            if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var primary))
            { validButtons |= 4; if (primary) pressedButtons |= 4; }
            if (controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out var secondary))
            { validButtons |= 8; if (secondary) pressedButtons |= 8; }
            return UpdateObservedControllerButtons(heldButtons, validButtons, pressedButtons);
        }

        private static int UpdateObservedControllerButtons(int heldButtons, int validButtons, int pressedButtons) =>
            (heldButtons & ~validButtons) | (pressedButtons & validButtons);

        private void ApplyVisibility()
        {
            // One explicit route owns tutorial and persistent HUD visibility.
            // A virtual editor headset must not choose a second UI route.
            var xrActive = UsesSpatialUi;
            var visible = !dismissed;
            if (appliedVisibility && visible == lastVisible && xrActive == lastSpatialRoute) return;
            appliedVisibility = true;
            lastVisible = visible;
            lastSpatialRoute = xrActive;
            if (visible && xrActive && picoCanvas != null)
                picoCanvas.GetComponent<PicoHeadLockedCanvas>()?.RequestCenterOnNextShow();
            // While the centred teaching card is open, suppress the three
            // persistent PICO HUD canvases so the first instruction screen is
            // calm and unobstructed. They return immediately after dismissal
            // and remain available through all later levels.
            if (desktopPanelRoot != null) desktopPanelRoot.SetActive(visible && !xrActive);
            if (picoPanelRoot != null) picoPanelRoot.SetActive(visible && xrActive);
            if (desktopCanvas != null) desktopCanvas.enabled = visible && !xrActive;
            if (picoCanvas != null) picoCanvas.enabled = visible && xrActive;
            if (spatialHudVisibility != null)
                foreach (var visibility in spatialHudVisibility)
                    if (visibility != null) visibility.RefreshVisibility();
            if (huds != null)
                foreach (var hud in huds)
                    if (hud != null) hud.RefreshVisibility();
        }
    }
}
