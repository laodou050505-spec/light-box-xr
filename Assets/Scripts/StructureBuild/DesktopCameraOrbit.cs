using UnityEngine;
using UnityEngine.InputSystem;

namespace StructureBuild
{
    public class DesktopCameraOrbit : MonoBehaviour
    {
        public Transform target;
        public Vector3 targetOffset;
        public float distance = 10f;
        [Tooltip("Desktop preview can move closer to the tabletop without getting stuck at the old minimum.")]
        public float minDistance = 1.5f;
        [Tooltip("Large enough for the authored scene to be inspected from outside the projector volumes.")]
        public float maxDistance = 40f;
        [Tooltip("Lower pitch bound for the desktop preview. Title composition can use a level gaze; gameplay keeps its elevated tabletop view.")]
        public float minPitch = 8f;
        [Tooltip("Upper pitch bound for orbiting the scene.")]
        public float maxPitch = 72f;
        public float yaw = 138f;
        public float pitch = 22f;
        public float orbitSpeed = 0.22f;
        public float zoomSpeed = 0.0035f;

        private Vector3 worldFocus;
        private bool useWorldFocus;

        private void Start()
        {
            ApplyPose();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.middleButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    yaw += delta.x * orbitSpeed;
                    pitch = Mathf.Clamp(pitch - delta.y * orbitSpeed, minPitch, maxPitch);
                }

                distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * zoomSpeed, minDistance, maxDistance);
            }

            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                SetPose(45f, 28f, distance);
            }

            ApplyPose();
        }

        private void ApplyPose()
        {
            var focus = useWorldFocus ? worldFocus : target.TransformPoint(targetOffset);
            transform.position = focus + Quaternion.Euler(pitch, yaw, 0f) * Vector3.back * distance;
            transform.LookAt(focus);
        }

        public void SetFocusWorld(Vector3 focus)
        {
            worldFocus = focus;
            useWorldFocus = true;
            if (target != null) ApplyPose();
        }

        public void SetPose(float newYaw, float newPitch, float newDistance)
        {
            yaw = newYaw;
            pitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
            distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
            if (target != null) ApplyPose();
        }

        public void SetPose(Vector3 focus, float newYaw, float newPitch, float newDistance)
        {
            SetFocusWorld(focus);
            SetPose(newYaw, newPitch, newDistance);
        }
    }
}
