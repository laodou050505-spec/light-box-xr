using UnityEngine;
using UnityEngine.XR;

namespace StructureBuild
{
    /// <summary>Minimal PICO ray interaction for the isolated title stage.</summary>
    public sealed class TitleWorldInteraction : MonoBehaviour
    {
        public Transform rayOrigin;
        public LineRenderer rayLine;
        public XRNode controllerNode = XRNode.RightHand;
        public float maxDistance = 12f;
        public float uiAimAssistRadius = 0.032f;

        private InputDevice device;
        private bool triggerWasPressed;
        private StructureTitleButton hovered;

        private void OnEnable() => HideRay();
        private void OnDisable() => HideRay();

        private void Update()
        {
            if (rayOrigin == null) rayOrigin = transform;
            if (!device.isValid) device = InputDevices.GetDeviceAtXRNode(controllerNode);
            var tracked = false;
            if (device.isValid) device.TryGetFeatureValue(CommonUsages.isTracked, out tracked);
            if (!device.isValid || !tracked)
            {
                HideRay();
                return;
            }

            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            var hasHit = Physics.Raycast(ray, out var hit, maxDistance, ~0, QueryTriggerInteraction.Collide);
            var button = hasHit ? hit.collider.GetComponentInParent<StructureTitleButton>() : null;
            var endpoint = hasHit ? hit.point : ray.GetPoint(maxDistance);
            if (button == null && TryGetAssistedButton(ray, out var assistedButton, out var assistedPoint))
            {
                button = assistedButton;
                endpoint = assistedPoint;
            }
            if (rayLine != null)
            {
                rayLine.enabled = true;
                rayLine.positionCount = 2;
                rayLine.SetPosition(0, ray.origin);
                rayLine.SetPosition(1, endpoint);
            }

            if (button != hovered)
            {
                if (hovered != null) hovered.SetHighlighted(this, false);
                hovered = button;
                if (hovered != null) hovered.SetHighlighted(this, true);
            }

            var pressed = false;
            device.TryGetFeatureValue(CommonUsages.triggerButton, out pressed);
            if (pressed && !triggerWasPressed && button != null)
            {
                button.Execute();
                device.SendHapticImpulse(0u, 0.22f, 0.05f);
            }
            triggerWasPressed = pressed;
        }

        private bool TryGetAssistedButton(Ray ray, out StructureTitleButton button, out Vector3 hitPoint)
        {
            button = null;
            hitPoint = ray.GetPoint(maxDistance);
            var bestMiss = float.PositiveInfinity;
            var bestDistance = float.PositiveInfinity;
            foreach (var candidate in Physics.SphereCastAll(ray, uiAimAssistRadius, maxDistance, ~0, QueryTriggerInteraction.Collide))
            {
                var candidateButton = candidate.collider.GetComponentInParent<StructureTitleButton>();
                if (candidateButton == null) continue;
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

        private void HideRay()
        {
            if (rayLine != null) rayLine.enabled = false;
            if (hovered != null) hovered.SetHighlighted(this, false);
            hovered = null;
            triggerWasPressed = false;
        }
    }
}
