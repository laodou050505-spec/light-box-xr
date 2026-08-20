using UnityEngine;

namespace StructureBuild
{
    /// <summary>
    /// Keeps PICO world-space UI in the player's view so it reads like the
    /// desktop Unity HUD instead of being anchored to the puzzle table.
    /// </summary>
    public sealed class PicoHeadLockedCanvas : MonoBehaviour
    {
        public Camera targetCamera;
        public float distance = 1.55f;
        public float worldScale = 0.00115f;
        public Vector2 viewOffset = Vector2.zero;
        public bool followRotation = true;

        private void LateUpdate()
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            var cameraTransform = targetCamera.transform;
            transform.position = cameraTransform.position
                                 + cameraTransform.forward * distance
                                 + cameraTransform.right * viewOffset.x
                                 + cameraTransform.up * viewOffset.y;

            if (followRotation)
            {
                // World-space UI renders from its local +Z face. Point that
                // face back at the viewer, just like a real holographic HUD.
                transform.rotation = Quaternion.LookRotation(cameraTransform.position - transform.position, cameraTransform.up);
            }

            transform.localScale = Vector3.one * worldScale;
        }
    }
}
