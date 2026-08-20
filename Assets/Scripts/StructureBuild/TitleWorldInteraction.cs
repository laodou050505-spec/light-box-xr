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
            var hasHit = Physics.Raycast(ray, out var hit, maxDistance);
            if (rayLine != null)
            {
                rayLine.enabled = true;
                rayLine.positionCount = 2;
                rayLine.SetPosition(0, ray.origin);
                rayLine.SetPosition(1, hasHit ? hit.point : ray.GetPoint(maxDistance));
            }

            var button = hasHit ? hit.collider.GetComponentInParent<StructureTitleButton>() : null;
            if (button != hovered)
            {
                if (hovered != null) hovered.SetHighlighted(false);
                hovered = button;
                if (hovered != null) hovered.SetHighlighted(true);
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

        private void HideRay()
        {
            if (rayLine != null) rayLine.enabled = false;
            if (hovered != null) hovered.SetHighlighted(false);
            hovered = null;
            triggerWasPressed = false;
        }
    }
}
