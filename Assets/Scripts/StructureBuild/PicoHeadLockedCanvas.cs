using UnityEngine;

namespace StructureBuild
{
    /// <summary>
    /// Legacy component name retained for serialized scenes. Supports real
    /// screen-mounted and fixed-world canvases as well as the old HUD route.
    /// </summary>
    public sealed class PicoHeadLockedCanvas : MonoBehaviour
    {
        public Camera targetCamera;
        public float distance = 1.55f;
        public float worldScale = 0.00115f;
        public Vector2 viewOffset = Vector2.zero;
        public bool followRotation = true;
        [Header("Optional spatial placement")]
        [Tooltip("When assigned, this canvas is placed just in front of the target world screen and copies its orientation.")]
        public Transform spatialTarget;
        [Min(0f)] public float spatialGap = 0.12f;
        public Vector3 spatialLocalOffset;
        public Vector3 spatialRotationOffset;
        [Tooltip("Use the authored world position, never a startup tracking sample.")]
        public bool fixedWorldAnchor;
        public Vector3 fixedWorldPosition;
        public Vector3 fixedWorldEuler;
        [Tooltip("When shown, capture the current view centre once, then remain fixed in space.")]
        public bool centerOnShow;

        private PicoRigBootstrap picoRig;
        private bool pendingCenter = true;

        public void RequestCenterOnNextShow() => pendingCenter = true;

        public bool TryCenterAfterPoseReady(Camera camera, bool poseReady)
        {
            if (!centerOnShow || !pendingCenter || !poseReady || camera == null) return false;
            var canvas = GetComponent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled) return false;
            fixedWorldPosition = camera.transform.position + camera.transform.forward * distance;
            fixedWorldEuler = camera.transform.eulerAngles;
            fixedWorldAnchor = true;
            followRotation = false;
            pendingCenter = false;
            return true;
        }

        public void CaptureWorldPose()
        {
            var camera = ResolveTrackedCamera();
            if (camera == null) return;
            fixedWorldPosition = camera.transform.position + camera.transform.forward * distance
                + camera.transform.right * viewOffset.x + camera.transform.up * viewOffset.y;
            fixedWorldEuler = camera.transform.eulerAngles;
            fixedWorldAnchor = true;
            followRotation = false;
            ApplyPose(camera);
        }

        private void LateUpdate()
        {
            ApplyPose(ResolveTrackedCamera());
        }

        public void ApplyPose(Camera camera)
        {
            if (camera == null) return;
            if (centerOnShow && Application.isPlaying && pendingCenter)
            {
                var canvas = GetComponent<Canvas>();
                if (canvas != null && canvas.isActiveAndEnabled)
                {
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        if (picoRig == null) picoRig = FindAnyObjectByType<PicoRigBootstrap>();
                        if (picoRig == null || !picoRig.HasAppliedDesignedPose) return;
                    }
                    TryCenterAfterPoseReady(camera, true);
                }
            }

            if (spatialTarget != null)
            {
                // The canvas front is local -Z.  Move it toward the player
                // from the display surface while preserving the display's
                // own tilt/yaw, so the panel reads as a separate physical
                // layer rather than a flat HUD pasted over the screen.
                transform.rotation = spatialTarget.rotation * Quaternion.Euler(spatialRotationOffset);
                transform.position = spatialTarget.position + spatialTarget.rotation * spatialLocalOffset
                                     - transform.forward * spatialGap;
                transform.localScale = Vector3.one * worldScale;
                return;
            }

            if (fixedWorldAnchor)
            {
                transform.position = fixedWorldPosition;
                transform.rotation = followRotation ? camera.transform.rotation : Quaternion.Euler(fixedWorldEuler);
                transform.localScale = Vector3.one * worldScale;
                return;
            }

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
