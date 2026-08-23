using UnityEngine;

namespace StructureBuild
{
    /// <summary>
    /// Shows a PICO-only auxiliary world canvas exclusively in the packaged
    /// Android player. The Unity Editor can expose a virtual Head InputDevice
    /// while previewing an XR package; treating that as a PICO runtime made
    /// both the desktop HUD and this canvas appear together in Game View.
    /// Platform is the unambiguous split for this project: desktop gets the
    /// screen-space HUD, while the PICO APK gets the view-locked world HUD.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class PicoRuntimeCanvasVisibility : MonoBehaviour
    {
        public Canvas canvas;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            Apply();
        }

        private void Update() => Apply();

        private void Apply()
        {
            if (canvas == null) return;
            // Do not infer the presentation route from a connected/virtual
            // HMD here. Only the Android build is the PICO player route.
            canvas.enabled = Application.platform == RuntimePlatform.Android;
        }
    }
}
