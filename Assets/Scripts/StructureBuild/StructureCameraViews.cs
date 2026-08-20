using UnityEngine;

namespace StructureBuild
{
    public sealed class StructureCameraViews : MonoBehaviour
    {
        public DesktopCameraOrbit orbit;
        public StructureGameController game;
        [Tooltip("The authoritative authored opening pose. Desktop and PICO both derive their initial position from this object.")]
        public DesignPlayerStart designStart;
        [Tooltip("Use the authored opening composition instead of recomputing a distant overview from every renderer.")]
        public bool useFixedOverviewPose = true;
        public float overviewYaw = 45f;
        public float overviewPitch = 28f;
        public float overviewDistance = 10.40f;
        public Vector3 overviewFocusOffset = Vector3.zero;
        [Range(0.3f, 1.2f)] public float overviewDistanceScale = 0.42f;
        public float overviewDistanceOffset = -0.35f;
        public string CurrentViewName { get; private set; } = "概览";

        private Camera viewCamera;
        private Bounds overviewBounds;
        private Bounds frontBounds;
        private Bounds sideBounds;

        private void Awake()
        {
            if (orbit == null) orbit = FindAnyObjectByType<DesktopCameraOrbit>();
            if (game == null) game = FindAnyObjectByType<StructureGameController>();
            if (designStart == null) designStart = FindAnyObjectByType<DesignPlayerStart>();
            viewCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            RefreshLayout();
            SetOverview();
        }

        public void SetOverview()
        {
            RefreshLayout();
            if (designStart != null)
            {
                designStart.ApplyDesktopPose(orbit);
                CurrentViewName = "概览";
                return;
            }
            if (orbit != null)
            {
                var focus = GetOverviewFocus() + overviewFocusOffset;
                var distance = useFixedOverviewPose ? overviewDistance : FitOverviewDistance(overviewBounds);
                orbit.SetPose(focus, overviewYaw, overviewPitch, distance);
            }
            CurrentViewName = "概览";
        }

        public void SetFront()
        {
            RefreshLayout();
            if (orbit != null) orbit.SetPose(frontBounds.center, 0f, 10f, FitUnifiedPanelDistance());
            CurrentViewName = "正面";
        }

        public void SetSide()
        {
            RefreshLayout();
            if (orbit != null) orbit.SetPose(sideBounds.center, 90f, 10f, FitUnifiedPanelDistance());
            CurrentViewName = "侧面";
        }

        public void CycleView()
        {
            if (CurrentViewName == "概览") SetFront();
            else if (CurrentViewName == "正面") SetSide();
            else SetOverview();
        }

        private void RefreshLayout()
        {
            if (game == null) return;
            overviewBounds = game.StructureBounds;
            frontBounds = game.StructureBounds;
            sideBounds = game.StructureBounds;
            EncapsulateRendererBounds(ref frontBounds, game.frontPanel != null && game.frontPanel.parent != null ? game.frontPanel.parent : game.frontPanel);
            EncapsulateRendererBounds(ref sideBounds, game.sidePanel != null && game.sidePanel.parent != null ? game.sidePanel.parent : game.sidePanel);
            overviewBounds = frontBounds;
            overviewBounds.Encapsulate(sideBounds);
            EncapsulateRendererBounds(ref overviewBounds, game.cubeSource);
        }

        private float FitAxisDistance(Bounds bounds, float horizontalSize, float depthSize)
        {
            var camera = viewCamera != null ? viewCamera : Camera.main;
            var verticalFov = camera != null ? camera.fieldOfView * Mathf.Deg2Rad : 60f * Mathf.Deg2Rad;
            var aspect = camera != null ? Mathf.Max(camera.aspect, 1f) : 16f / 9f;
            var horizontalFov = 2f * Mathf.Atan(Mathf.Tan(verticalFov * 0.5f) * aspect);
            var verticalFit = bounds.size.y * 0.5f / Mathf.Tan(verticalFov * 0.5f);
            var horizontalFit = horizontalSize * 0.5f / Mathf.Tan(horizontalFov * 0.5f);
            return Mathf.Clamp(Mathf.Max(verticalFit, horizontalFit) + depthSize * 0.62f + 1.6f, 4.8f, 32f);
        }

        /// <summary>
        /// Keep the front and side presets on the same camera radius.  The two
        /// directions can have different panel extents, so use the larger fit
        /// distance for both; this prevents the wider direction from cropping
        /// while making the preset-to-preset distance predictable.
        /// </summary>
        private float FitUnifiedPanelDistance()
        {
            var frontDistance = FitAxisDistance(frontBounds, frontBounds.size.x, frontBounds.size.z);
            var sideDistance = FitAxisDistance(sideBounds, sideBounds.size.z, sideBounds.size.x);
            return Mathf.Max(frontDistance, sideDistance);
        }

        private float FitOverviewDistance(Bounds bounds)
        {
            var camera = viewCamera != null ? viewCamera : Camera.main;
            var verticalFov = camera != null ? camera.fieldOfView * Mathf.Deg2Rad : 60f * Mathf.Deg2Rad;
            var radius = bounds.extents.magnitude;
            var fittedDistance = radius / Mathf.Tan(verticalFov * 0.5f);
            return Mathf.Clamp(fittedDistance * overviewDistanceScale + overviewDistanceOffset, 3.15f, 36f);
        }

        private Vector3 GetOverviewFocus()
        {
            if (game == null || game.GroundBounds.size == Vector3.zero) return overviewBounds.center;
            var platformFocus = game.GroundBounds.center + Vector3.up * 0.85f;
            return Vector3.Lerp(platformFocus, overviewBounds.center, 0.28f);
        }

        private static void EncapsulateRendererBounds(ref Bounds bounds, Transform root)
        {
            if (root == null) return;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
        }
    }
}
