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

        private void Awake()
        {
            picoRuntime = Application.platform == RuntimePlatform.Android;
            if (picoManager != null) picoManager.enabled = picoRuntime;
            desktopCamera = FindDesktopCamera();
            if (designStart == null) designStart = FindAnyObjectByType<DesignPlayerStart>();

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
            pendingPoseRestore = StartCoroutine(ApplyDesignedPoseAfterTrackingUpdate());
        }

        private IEnumerator ApplyDesignedPoseAfterTrackingUpdate()
        {
            yield return null;
            yield return null;
            designStart.ApplyXrRigPose(transform, xrCamera != null ? xrCamera.transform : null);
            Debug.Log($"STRUCTURE_XR_DESIGN_START_APPLIED: eye={designStart.DesignedEyePosition:F2}, yaw={designStart.yaw:F1}, floorEyeHeight={designStart.eyeHeight:F2}.");
            pendingPoseRestore = null;
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
