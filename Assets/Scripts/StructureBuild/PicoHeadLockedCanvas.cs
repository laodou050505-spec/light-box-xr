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

        private PicoRigBootstrap picoRig;

        private void LateUpdate()
        {
            var camera = ResolveTrackedCamera();
            if (camera == null) return;

            var cameraTransform = camera.transform;
            transform.position = cameraTransform.position
                                 + cameraTransform.forward * distance
                                 + cameraTransform.right * viewOffset.x
                                 + cameraTransform.up * viewOffset.y;

            if (followRotation)
            {
                // A World-Space Canvas is read from its local -Z side.  Its
                // +Z normal therefore needs to point in the same direction
                // as the player's gaze, away from the player. Looking back
                // toward the eye mirrored every TMP label in the PICO view.
                transform.rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
            }

            transform.localScale = Vector3.one * worldScale;
        }

        private Camera ResolveTrackedCamera()
        {
            // Do not cache Camera.main on the first compositor frame. On PICO
            // the desktop fallback camera can temporarily remain MainCamera
            // while PXR starts, which left the HUD behind the physical table.
            // The active PICO rig is the authority for all Android HUD poses.
            if (Application.platform == RuntimePlatform.Android)
            {
                if (picoRig == null) picoRig = FindAnyObjectByType<PicoRigBootstrap>();
                if (picoRig != null && picoRig.xrCamera != null) return picoRig.xrCamera;
            }

            if (targetCamera != null && targetCamera.isActiveAndEnabled) return targetCamera;
            return Camera.main;
        }
    }
}
