using System.Collections;
using System.Collections.Generic;
using ByteDance.PICO.XR;
using UnityEngine;
using UnityEngine.XR;

namespace StructureBuild
{
    // Run before PXR_Manager.Awake so Camera.main is the stereo XR camera when
    // PICO initializes its compositor.  Otherwise the manager can bind the
    // desktop preview camera and the system presents the app as a flat window.
    [DefaultExecutionOrder(-10000)]
    public class PicoRigBootstrap : MonoBehaviour
    {
        public Camera xrCamera;
        public PXR_Manager picoManager;
        [Tooltip("The sole authored starting pose shared by the desktop Game View and the PICO rig.")]
        public DesignPlayerStart designStart;
        private Camera desktopCamera;
        private AudioListener desktopAudioListener;
        private AudioListener xrAudioListener;
        private bool xrActive;
        private bool recenterWasPressed;
        private bool picoRuntime;
        private Coroutine pendingPoseRestore;
        private PicoViewPitchOffset viewPitchOffset;
        private bool awaitingFirstTrackedPose;

        public bool HasAppliedDesignedPose { get; private set; }
        public PicoViewPitchOffset ViewPitchOffset => viewPitchOffset;

        private void Awake()
        {
            picoRuntime = Application.platform == RuntimePlatform.Android;
            if (picoManager != null) picoManager.enabled = picoRuntime;
            desktopCamera = FindDesktopCamera();
            if (designStart == null) designStart = FindAnyObjectByType<DesignPlayerStart>();
            EnsureViewPitchOffset();

            // Android builds are PICO immersive builds, not an Android-window
            // fallback.  Select the stereo camera before PXR_Manager's Awake.
            if (picoRuntime) SetImmersiveCameraState(true);
        }

        private void Start()
        {
            desktopCamera ??= FindDesktopCamera();
            desktopAudioListener = EnsureAudioListener(desktopCamera);
            xrAudioListener = EnsureAudioListener(xrCamera);
            SetImmersiveCameraState(picoRuntime);
            if (picoRuntime)
            {
                PXR_Plugin.System.RecenterSuccess += HandlePicoRecenter;
                StartCoroutine(ConfigureImmersiveTracking());
            }
        }

        private void OnDestroy()
        {
            PXR_Plugin.System.RecenterSuccess -= HandlePicoRecenter;
        }

        private void Update()
        {
            var headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            var hasHeadset = headset.isValid;
            if (picoRuntime && awaitingFirstTrackedPose && pendingPoseRestore == null && HasUsableHeadPosition(headset))
            {
                awaitingFirstTrackedPose = false;
                ApplyDesignedPoseNow();
                // A late device sample can replace the serialized fallback
                // eye offset. Re-centre the visible teaching card exactly once
                // at the corrected eye instead of stranding it behind us.
                foreach (var panel in FindObjectsByType<PicoHeadLockedCanvas>(FindObjectsInactive.Include))
                    if (panel.centerOnShow) panel.RequestCenterOnNextShow();
                LogDesignedPose("first-live-tracking-after-fallback", false);
            }
            var rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            var recenterPressed = false;
            if (rightController.isValid) rightController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out recenterPressed);
            if (recenterPressed && !recenterWasPressed)
            {
                var inputSubsystems = new List<XRInputSubsystem>();
                SubsystemManager.GetSubsystems(inputSubsystems);
                foreach (var subsystem in inputSubsystems)
                {
                    if (subsystem.running && subsystem.TryRecenter()) break;
                }
                RestoreDesignedPoseAfterTrackingUpdate();
            }
            recenterWasPressed = recenterPressed;

            // A real PICO build must stay on its stereo camera even while the
            // runtime is bringing the HMD device online.  Waiting on
            // InputDevice.isValid here caused the desktop preview camera to
            // render first and produced a shared-space flat window.
            if (picoRuntime)
            {
                if (!xrActive)
                {
                    xrActive = true;
                    SetImmersiveCameraState(true);
                }
                return;
            }

            if (hasHeadset == xrActive) return;
            xrActive = hasHeadset;
            SetImmersiveCameraState(xrActive);
        }

        private IEnumerator ConfigureImmersiveTracking()
        {
            // Let XR Management start PXR_Loader first, then use a floor-level
            // tracking origin for the authored tabletop scene.
            yield return null;
            yield return null;
            PXR_System.SetTrackingOrigin(PxrTrackingOrigin.Floor);
            RestoreDesignedPoseAfterTrackingUpdate();
            Debug.Log($"STRUCTURE_PICO_IMMERSIVE_BOOT: PICO stereo camera active with Floor tracking origin; shared design start '{designStart?.name ?? "missing"}'.");
        }

        private void HandlePicoRecenter()
        {
            // PICO dispatches this after its tracking origin changes. Delay a
            // couple of frames so we read the post-recenter local head offset.
            RestoreDesignedPoseAfterTrackingUpdate();
        }

        private void RestoreDesignedPoseAfterTrackingUpdate()
        {
            if (!picoRuntime || designStart == null) return;
            if (pendingPoseRestore != null) StopCoroutine(pendingPoseRestore);
            awaitingFirstTrackedPose = false;
            HasAppliedDesignedPose = false;
            pendingPoseRestore = StartCoroutine(ApplyDesignedPoseAfterTrackingUpdate());
        }

        private IEnumerator ApplyDesignedPoseAfterTrackingUpdate()
        {
            yield return null;
            yield return null;
            // XR initialization can take longer than a fixed frame delay.
            // Do not position the rig against the serialized no-device pose
            // while a real floor-space head sample is still coming online.
            var deadline = Time.realtimeSinceStartup + 5f;
            var hasTrackedPosition = HasUsableHeadPosition(InputDevices.GetDeviceAtXRNode(XRNode.Head));
            while (!hasTrackedPosition && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                hasTrackedPosition = HasUsableHeadPosition(InputDevices.GetDeviceAtXRNode(XRNode.Head));
            }
            awaitingFirstTrackedPose = !hasTrackedPosition;
            if (awaitingFirstTrackedPose)
                Debug.LogWarning("STRUCTURE_XR_TRACKING_FALLBACK: no usable head position after 5 seconds; using the authored fallback temporarily and will calibrate once when tracking becomes available.");
            ApplyDesignedPoseNow();
            LogDesignedPose("tracking-origin-settled", awaitingFirstTrackedPose);
            pendingPoseRestore = null;
        }

        private static bool HasUsableHeadPosition(InputDevice headset)
        {
            if (!headset.isValid || !headset.TryGetFeatureValue(CommonUsages.devicePosition, out var position)) return false;
            if (headset.TryGetFeatureValue(CommonUsages.isTracked, out var tracked) && !tracked) return false;
            return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z)
                && !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z);
        }

        private void LogDesignedPose(string source, bool trackingFallback)
        {
            Debug.Log($"STRUCTURE_XR_DESIGN_START_APPLIED: source={source}, trackingFallback={trackingFallback}, eye={designStart.DesignedEyePosition:F2}, yaw={designStart.yaw:F1}, rigEuler={transform.eulerAngles:F2}, rootUp={transform.up:F3}, cameraEuler={(xrCamera != null ? xrCamera.transform.eulerAngles : Vector3.zero):F2}, viewPitch={(viewPitchOffset != null ? viewPitchOffset.PitchDegrees : 0f):F1}, referencePitchEnabled={designStart.useReferenceViewPitch}, floorEyeHeight={designStart.eyeHeight:F2}.");
        }

        public void EnsureViewPitchOffset()
        {
            if (designStart == null || xrCamera == null || !designStart.useReferenceViewPitch) return;
            if (viewPitchOffset == null)
            {
                var existing = transform.Find("GameplayViewOrientation");
                var pivot = existing != null ? existing : new GameObject("GameplayViewOrientation").transform;
                viewPitchOffset = pivot.GetComponent<PicoViewPitchOffset>();
                if (viewPitchOffset == null) viewPitchOffset = pivot.gameObject.AddComponent<PicoViewPitchOffset>();
            }
            viewPitchOffset.Configure(designStart, transform, xrCamera.transform);
        }

        /// <summary>Apply only after the tracking origin has settled. Public for runtime QA.</summary>
        public void ApplyDesignedPoseNow()
        {
            if (designStart == null || xrCamera == null) return;
            EnsureViewPitchOffset();
            if (viewPitchOffset != null) viewPitchOffset.RefreshTrackedPoseAndCompensation();
            else xrCamera.GetComponent<PicoControllerPose>()?.RefreshTrackedPose();
            designStart.ApplyXrRigPose(transform, xrCamera.transform);
            viewPitchOffset?.ApplyCompensation();
            HasAppliedDesignedPose = true;
        }

        private Camera FindDesktopCamera()
        {
            var orbit = FindAnyObjectByType<DesktopCameraOrbit>();
            return orbit != null ? orbit.GetComponent<Camera>() : Camera.main;
        }

        private void SetImmersiveCameraState(bool immersive)
        {
            if (xrCamera != null)
            {
                xrCamera.enabled = immersive;
                xrCamera.tag = immersive ? "MainCamera" : "Untagged";
            }
            if (desktopCamera != null && desktopCamera != xrCamera)
            {
                desktopCamera.enabled = !immersive;
                desktopCamera.tag = immersive ? "Untagged" : "MainCamera";
            }
            if (xrAudioListener != null) xrAudioListener.enabled = immersive;
            if (desktopAudioListener != null) desktopAudioListener.enabled = !immersive;
        }

        private static AudioListener EnsureAudioListener(Camera camera)
        {
            if (camera == null) return null;
            var listener = camera.GetComponent<AudioListener>();
            return listener != null ? listener : camera.gameObject.AddComponent<AudioListener>();
        }
    }
}
