using UnityEngine;
using UnityEngine.XR;

namespace StructureBuild
{
    /// <summary>
    /// First-run play prompt. Both canvases share this state so desktop and PICO
    /// never show two independent instruction flows.
    /// </summary>
    public sealed class GameplayInstructionsOverlay : MonoBehaviour
    {
        public GameObject desktopPanelRoot;
        public GameObject picoPanelRoot;
        public Canvas desktopCanvas;
        public Canvas picoCanvas;
        public StructureAudioController audio;
        // The isolated title scene owns the real start stage. This optional
        // in-world card is a post-load control reminder, not a second title
        // page, so it begins dismissed.
        public bool visibleAtStartup;

        private bool dismissed;

        private void Awake()
        {
            if (audio == null) audio = FindAnyObjectByType<StructureAudioController>();
            dismissed = !visibleAtStartup;
            ApplyVisibility();
        }

        private void Update() => ApplyVisibility();

        public void Dismiss()
        {
            if (dismissed) return;
            dismissed = true;
            ApplyVisibility();
        }

        public void Show()
        {
            dismissed = false;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            var headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            // A PICO Android player must always choose the spatial prompt,
            // including the few compositor-start frames before the Head
            // InputDevice has registered.
            var xrActive = Application.platform == RuntimePlatform.Android || headset.isValid;
            var visible = !dismissed;
            if (desktopPanelRoot != null) desktopPanelRoot.SetActive(visible);
            if (picoPanelRoot != null) picoPanelRoot.SetActive(visible);
            if (desktopCanvas != null) desktopCanvas.enabled = visible && !xrActive;
            if (picoCanvas != null) picoCanvas.enabled = visible && xrActive;
        }
    }
}
