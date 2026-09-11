using System.Collections.Generic;
using UnityEngine;

namespace StructureBuild
{
    /// <summary>
    /// Applies the authored gameplay look angle about the tracked eye, without
    /// pitching the floor-level XR root or any scene geometry. Head translation
    /// stays in upright tracking space; the head/hand relative pose shares one
    /// view rotation so the controller model and its ray agree with the camera.
    /// This is a ray-interaction view remap, not a rigid direct-touch space.
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public sealed class PicoViewPitchOffset : MonoBehaviour
    {
        private DesignPlayerStart designStart;
        private Transform trackingRoot;
        private Transform trackedHead;
        private PicoControllerPose[] trackedPoses = new PicoControllerPose[0];

        public Transform TrackingRoot => trackingRoot;
        public Transform TrackedHead => trackedHead;
        public float PitchDegrees => designStart != null && designStart.useReferenceViewPitch
            ? designStart.referenceViewPitch : 0f;

        /// <summary>
        /// Configure a dedicated child of the upright rig. Only raw tracked
        /// roots are reparented; authored world models and UI stay untouched.
        /// </summary>
        public void Configure(DesignPlayerStart start, Transform rig, Transform head)
        {
            designStart = start;
            trackingRoot = rig;
            trackedHead = head;
            if (rig == null || head == null) return;

            transform.SetParent(rig, false);
            transform.localScale = Vector3.one;
            var poses = new List<PicoControllerPose>();
            foreach (var pose in rig.GetComponentsInChildren<PicoControllerPose>(true))
            {
                // A pose below an unrelated authored transform is not raw
                // tracking space and must not be silently rearranged.
                if (pose.transform.parent != rig && pose.transform.parent != transform) continue;
                pose.SetPoseCoordinator(this);
                pose.transform.SetParent(transform, false);
                poses.Add(pose);
            }
            if (head.parent == rig) head.SetParent(transform, false);
            if (head.parent != transform)
            {
                Debug.LogError("STRUCTURE_XR_VIEW_OFFSET: tracked head must be a direct rig child or a child of GameplayViewOrientation.");
                foreach (var pose in poses) pose.SetPoseCoordinator(null);
                trackedHead = null;
                return;
            }
            trackedPoses = poses.ToArray();
            ApplyCompensation();
        }

        private void OnEnable() { Application.onBeforeRender += BeforeRender; }

        private void OnDisable()
        {
            Application.onBeforeRender -= BeforeRender;
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void OnDestroy()
        {
            foreach (var pose in trackedPoses)
                if (pose != null) pose.SetPoseCoordinator(null);
        }

        private void Update() { RefreshTrackedPoseAndCompensation(); }

        // Matches Unity's TrackedPoseDriver phase: all three devices are read
        // together, then compensated, before later ray-visual callbacks run.
        [BeforeRenderOrder(-30000)]
        private void BeforeRender() { RefreshTrackedPoseAndCompensation(); }

        public void RefreshTrackedPoseAndCompensation()
        {
            foreach (var pose in trackedPoses)
                if (pose != null && pose.isActiveAndEnabled) pose.RefreshTrackedPose();
            ApplyCompensation();
        }

        /// <summary>
        /// Also exposed for geometry QA using synthetic raw head/hand poses.
        /// For root yaw R, pitch P and raw head h, this gives world eye root+Rh
        /// and eye-to-hand vector RP(c-h), preserving upright physical movement.
        /// </summary>
        public void ApplyCompensation()
        {
            if (trackedHead == null) return;
            var pitch = Quaternion.Euler(PitchDegrees, 0f, 0f);
            var rawHead = trackedHead.localPosition;
            transform.SetLocalPositionAndRotation(rawHead - pitch * rawHead, pitch);
        }
    }
}
