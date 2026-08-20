using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace StructureBuild
{
    public class WorldSpaceInteraction : MonoBehaviour
    {
        public StructureGameController game;
        public Transform rayOrigin;
        public float maxDistance = 12f;
        public LayerMask interactionMask = ~0;
        public LineRenderer rayLine;
        public InputActionProperty selectAction;
        public InputActionProperty secondaryAction;
        public XRNode controllerNode = XRNode.RightHand;
        private bool ownsDrag;
        private UnityEngine.XR.InputDevice device;
        private bool triggerWasPressed;
        private bool gripWasPressed;
        private bool primaryWasPressed;
        private bool secondaryWasPressed;
        private StructureUIButton hoveredButton;

        private void OnEnable()
        {
            selectAction.action?.Enable();
            secondaryAction.action?.Enable();
            HideRayAndCancelDrag();
        }

        private void OnDisable()
        {
            selectAction.action?.Disable();
            secondaryAction.action?.Disable();
            HideRayAndCancelDrag();
        }

        private void Update()
        {
            if (game == null || rayOrigin == null)
            {
                HideRayAndCancelDrag();
                return;
            }

            if (!device.isValid) device = InputDevices.GetDeviceAtXRNode(controllerNode);
            var isTracked = false;
            if (device.isValid) device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out isTracked);
            if (!device.isValid || !isTracked)
            {
                HideRayAndCancelDrag();
                return;
            }

            if (rayLine != null) rayLine.enabled = true;
            var xrTrigger = false;
            var xrGrip = false;
            var xrPrimary = false;
            var xrSecondary = false;
            if (device.isValid) device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out xrTrigger);
            if (device.isValid) device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out xrGrip);
            if (device.isValid) device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out xrPrimary);
            if (device.isValid) device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out xrSecondary);
            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            var hasHit = Physics.Raycast(ray, out var hit, maxDistance, interactionMask);
            if (rayLine != null) { rayLine.positionCount = 2; rayLine.SetPosition(0, ray.origin); rayLine.SetPosition(1, hasHit ? hit.point : ray.origin + ray.direction * maxDistance); }
            var button = hasHit ? hit.collider.GetComponentInParent<StructureUIButton>() : null;
            if (hoveredButton != button)
            {
                if (hoveredButton != null) hoveredButton.SetHighlighted(false);
                hoveredButton = button;
                if (hoveredButton != null)
                {
                    hoveredButton.SetHighlighted(true);
                    game.GetComponent<StructureAudioController>()?.PlayUiHover();
                }
            }
            var pressed = (selectAction.action != null && selectAction.action.WasPressedThisFrame()) || (xrTrigger && !triggerWasPressed);
            var released = (selectAction.action != null && selectAction.action.WasReleasedThisFrame()) || (!xrTrigger && triggerWasPressed);
            var secondaryPressed = (secondaryAction.action != null && secondaryAction.action.WasPressedThisFrame()) || (xrGrip && !gripWasPressed);
            triggerWasPressed = xrTrigger;
            gripWasPressed = xrGrip;
            if (xrPrimary && !primaryWasPressed)
            {
                if (controllerNode == XRNode.LeftHand) game.Hint(); else game.Undo();
                Pulse(0.18f, 0.04f);
            }
            if (xrSecondary && !secondaryWasPressed)
            {
                game.ResetCurrentLevel();
                Pulse(0.18f, 0.04f);
            }
            primaryWasPressed = xrPrimary;
            secondaryWasPressed = xrSecondary;
            if (pressed)
            {
                if (button != null)
                {
                    button.Execute();
                    Pulse(0.16f, 0.035f);
                }
                else if (hasHit)
                {
                    var source = hit.collider.GetComponentInParent<CubeSourceInteractable>();
                    var cube = hit.collider.GetComponentInParent<PuzzleCubeInteractable>();
                    ownsDrag = source != null ? game.BeginDragFromSource(ray) : game.BeginDragFromCube(cube, ray);
                    if (ownsDrag) Pulse(0.16f, 0.035f);
                }
            }
            if (ownsDrag)
            {
                game.UpdateDrag(ray);
                if (released)
                {
                    var placed = game.CommitDrag(ray);
                    Pulse(placed ? 0.32f : 0.12f, placed ? 0.08f : 0.035f);
                    ownsDrag = false;
                }
            }
            if (secondaryPressed && ownsDrag)
            {
                game.CancelDrag();
                ownsDrag = false;
                Pulse(0.14f, 0.035f);
            }
            else if (secondaryPressed && Physics.Raycast(ray, out var removeHit, maxDistance, interactionMask))
            {
                var cube = removeHit.collider.GetComponentInParent<PuzzleCubeInteractable>();
                if (cube != null) { game.RemoveTopAt(cube.cell.x, cube.cell.z); Pulse(0.20f, 0.05f); }
            }
        }

        private void Pulse(float amplitude, float duration)
        {
            if (device.isValid) device.SendHapticImpulse(0u, amplitude, duration);
        }

        private void HideRayAndCancelDrag()
        {
            if (rayLine != null) rayLine.enabled = false;
            if (hoveredButton != null) hoveredButton.SetHighlighted(false);
            hoveredButton = null;
            if (ownsDrag && game != null) game.CancelDrag();
            ownsDrag = false;
            triggerWasPressed = false;
            gripWasPressed = false;
            primaryWasPressed = false;
            secondaryWasPressed = false;
        }
    }
}
