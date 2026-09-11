using UnityEngine;
using UnityEngine.XR;

namespace StructureBuild
{
    [DefaultExecutionOrder(-9100)]
    public class PicoControllerPose : MonoBehaviour
    {
        public XRNode node = XRNode.RightHand;
        private InputDevice device;
        private Vector3 fallbackPosition;
        private Quaternion fallbackRotation = Quaternion.identity;
        private bool hasFallbackPose;
        private PicoViewPitchOffset poseCoordinator;

        private void Awake() { CacheFallbackPose(); }
        private void Update()
        {
            if (poseCoordinator != null && poseCoordinator.isActiveAndEnabled) return;
            RefreshTrackedPose();
        }

        public void SetPoseCoordinator(PicoViewPitchOffset coordinator)
        {
            CacheFallbackPose();
            poseCoordinator = coordinator;
        }

        /// <summary>Read the raw tracking-space pose, before any view offset.</summary>
        public void RefreshTrackedPose()
        {
            CacheFallbackPose();
            if (!device.isValid) device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) { transform.localPosition = fallbackPosition; transform.localRotation = fallbackRotation; return; }
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out var position)) transform.localPosition = position;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation)) transform.localRotation = rotation;
        }

        private void CacheFallbackPose()
        {
            if (hasFallbackPose) return;
            fallbackPosition = transform.localPosition;
            fallbackRotation = transform.localRotation;
            hasFallbackPose = true;
        }
    }

}
