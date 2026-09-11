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
        [Tooltip("Small world-space radius used only to make UI buttons easier to acquire. Physical cubes still use the exact ray.")]
        public float uiAimAssistRadius = 0.032f;
        public InputActionProperty selectAction;
        public InputActionProperty secondaryAction;
        public XRNode controllerNode = XRNode.RightHand;
        private bool ownsDrag;
        private UnityEngine.XR.InputDevice device;
        private bool triggerWasPressed;
        private bool gripWasPressed;
        private bool primaryWasPressed;
        private bool secondaryWasPressed;
        private bool waitingForInputRelease = true;
        private StructureUIButton hoveredButton;

        private void OnEnable()
        {
            selectAction.action?.Enable();
            secondaryAction.action?.Enable();
            HideRayAndCancelDrag();
            Application.onBeforeRender += RefreshRayVisualBeforeRender;
        }

        private void OnDisable()
        {
            selectAction.action?.Disable();
            secondaryAction.action?.Disable();
            Application.onBeforeRender -= RefreshRayVisualBeforeRender;
            HideRayAndCancelDrag();
        }

        private void Update()
        {
            if (game == null || rayOrigin == null)
            {
                HideRayAndCancelDrag();
                return;
            }

            // The teaching card may cancel the shared drag between updates.
            // Keep UI selection active, but discard stale controller ownership.
            if (!game.IsGameplayInputAllowed) ownsDrag = false;

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
            var hasTriggerState = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out xrTrigger);
            var hasGripState = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out xrGrip);
            if (device.isValid) device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out xrPrimary);
            if (device.isValid) device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out xrSecondary);
            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            var hasHit = TryVisibleWorldHit(ray, out var hit);
            var button = hasHit ? hit.collider.GetComponentInParent<StructureUIButton>() : null;
            if (button != null && !button.IsInteractable) button = null;
            var buttonHitPoint = hasHit ? hit.point : ray.GetPoint(maxDistance);
            // PICO trigger selection should not demand pixel-perfect wrist aim.
            // When the exact ray misses a UI button, acquire the closest button
            // inside a very small cone. Cube grabbing and placement remain exact.
            if (button == null && TryGetAssistedButton(ray, out var assistedButton, out var assistedPoint))
            {
                button = assistedButton;
                buttonHitPoint = assistedPoint;
            }
            if (rayLine != null)
            {
                rayLine.positionCount = 2;
                rayLine.SetPosition(0, ray.origin);
                rayLine.SetPosition(1, button != null ? buttonHitPoint : hasHit ? hit.point : ray.GetPoint(maxDistance));
            }
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
            var actionHeld = (selectAction.action != null && selectAction.action.IsPressed()) ||
                             (secondaryAction.action != null && secondaryAction.action.IsPressed());
            if (ConsumeInputRearmFrame(hasTriggerState && hasGripState, xrTrigger, xrGrip, xrPrimary, xrSecondary, actionHeld)) return;
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
            else if (secondaryPressed && TryVisibleWorldHit(ray, out var removeHit))
            {
                var cube = removeHit.collider.GetComponentInParent<PuzzleCubeInteractable>();
                if (cube != null) { game.RemoveTopAt(cube.cell.x, cube.cell.z); Pulse(0.20f, 0.05f); }
            }
        }

        private bool ConsumeInputRearmFrame(bool hasUsableState, bool trigger, bool grip, bool primary, bool secondary, bool actionHeld)
        {
            if (!waitingForInputRelease) return false;
            triggerWasPressed = trigger;
            gripWasPressed = grip;
            primaryWasPressed = primary;
            secondaryWasPressed = secondary;
            if (hasUsableState && !trigger && !grip && !primary && !secondary && !actionHeld)
                waitingForInputRelease = false;
            // Enabling or regaining tracking never manufactures a press. Even
            // the verified release frame is consumed before accepting input.
            return true;
        }

        // The gameplay pose coordinator refreshes head and hands at -30000.
        // Refresh only the displayed ray afterwards, never replaying input,
        // hover audio, haptics, or drag mutations in the render phase.
        [BeforeRenderOrder(-29000)]
        private void RefreshRayVisualBeforeRender()
        {
            if (rayLine == null || !rayLine.enabled || rayOrigin == null || game == null) return;
            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            var hasHit = TryVisibleWorldHit(ray, out var hit);
            var button = hasHit ? hit.collider.GetComponentInParent<StructureUIButton>() : null;
            if (button != null && !button.IsInteractable) button = null;
            var endpoint = hasHit ? hit.point : ray.GetPoint(maxDistance);
            if (button == null && TryGetAssistedButton(ray, out _, out var assistedPoint)) endpoint = assistedPoint;
            rayLine.positionCount = 2;
            rayLine.SetPosition(0, ray.origin);
            rayLine.SetPosition(1, endpoint);
        }

        private void Pulse(float amplitude, float duration)
        {
            if (device.isValid) device.SendHapticImpulse(0u, amplitude, duration);
        }

        private bool TryGetAssistedButton(Ray ray, out StructureUIButton button, out Vector3 hitPoint)
        {
            button = null;
            hitPoint = ray.GetPoint(maxDistance);
            var bestMiss = float.PositiveInfinity;
            var bestDistance = float.PositiveInfinity;
            foreach (var candidate in Physics.SphereCastAll(ray, uiAimAssistRadius, maxDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                var candidateButton = candidate.collider.GetComponentInParent<StructureUIButton>();
                if (candidateButton == null || !candidateButton.IsInteractable) continue;
                var center = candidate.collider.bounds.center;
                var along = Mathf.Max(0f, Vector3.Dot(center - ray.origin, ray.direction));
                var miss = Vector3.Distance(center, ray.GetPoint(along));
                if (miss > bestMiss + 0.0001f || (Mathf.Abs(miss - bestMiss) < 0.0001f && candidate.distance >= bestDistance)) continue;
                bestMiss = miss;
                bestDistance = candidate.distance;
                button = candidateButton;
                hitPoint = candidate.point;
            }
            return button != null;
        }

        private bool TryVisibleWorldHit(Ray ray, out RaycastHit hit)
        {
            if (!Physics.Raycast(ray, out hit, maxDistance, interactionMask, QueryTriggerInteraction.Collide)) return false;
            var firstCanvas = hit.collider.GetComponentInParent<Canvas>();
            if (firstCanvas == null || firstCanvas.isActiveAndEnabled) return true;
            // The allocation-free exact ray remains the normal XR path. Only
            // a just-hidden hit volume needs a second pass through the ray.
            var nearest = float.PositiveInfinity;
            foreach (var candidate in Physics.RaycastAll(ray, maxDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                var canvas = candidate.collider.GetComponentInParent<Canvas>();
                if (canvas != null && !canvas.isActiveAndEnabled) continue;
                if (candidate.distance >= nearest) continue;
                nearest = candidate.distance;
                hit = candidate;
            }
            return nearest < float.PositiveInfinity;
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
            waitingForInputRelease = true;
        }
    }
}
