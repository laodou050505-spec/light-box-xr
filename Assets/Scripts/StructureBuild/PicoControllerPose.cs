using UnityEngine;
using UnityEngine.XR;

namespace StructureBuild
{
    public class PicoControllerPose : MonoBehaviour
    {
        public XRNode node = XRNode.RightHand;
        private InputDevice device;
        private Vector3 fallbackPosition;
        private Quaternion fallbackRotation = Quaternion.identity;

        private void Awake() { fallbackPosition = transform.localPosition; fallbackRotation = transform.localRotation; }
        private void Update()
        {
            if (!device.isValid) device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) { transform.localPosition = fallbackPosition; transform.localRotation = fallbackRotation; return; }
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out var position)) transform.localPosition = position;
            if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotation)) transform.localRotation = rotation;
        }
    }

}
