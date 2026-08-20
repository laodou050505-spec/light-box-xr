using UnityEngine;
using UnityEngine.Rendering;

namespace StructureBuild
{
    [ExecuteAlways]
    public sealed class PanelEdgeGlow : MonoBehaviour
    {
        public Transform panelModel;
        public Transform gameplayTarget;
        public LineRenderer leftHalo;
        public LineRenderer leftCore;
        public LineRenderer rightHalo;
        public LineRenderer rightCore;
        public Color coreColor = new Color(0.72f, 2.20f, 3.80f, 1f);
        public Color haloColor = new Color(0.12f, 1.15f, 2.40f, 0.72f);
        public float pulseSpeed = 0.82f;

        private void OnEnable() => Refresh();
        private void Update() => Refresh();

        public void Refresh()
        {
            if (panelModel == null || !TryGetLocalBounds(panelModel, out var bounds)) return;

            var width = Mathf.Max(0.01f, bounds.size.x);
            var height = Mathf.Max(0.01f, bounds.size.y);
            var insetX = width * 0.022f;
            var insetY = height * 0.035f;
            var frontSign = 1f;
            if (gameplayTarget != null)
            {
                var targetLocal = transform.InverseTransformPoint(gameplayTarget.position);
                frontSign = Mathf.Sign(targetLocal.z - bounds.center.z);
                if (Mathf.Approximately(frontSign, 0f)) frontSign = 1f;
            }

            var z = bounds.center.z + frontSign * (bounds.extents.z + Mathf.Max(0.006f, width * 0.002f));
            var bottom = bounds.min.y + insetY;
            var top = bounds.max.y - insetY;
            var leftX = bounds.min.x + insetX;
            var rightX = bounds.max.x - insetX;
            var coreWidth = Mathf.Clamp(Mathf.Min(width, height) * 0.020f, 0.020f, 0.085f);
            var pulse = Application.isPlaying ? 0.94f + Mathf.Sin(Time.time * pulseSpeed) * 0.06f : 1f;

            SetLine(leftHalo, new Vector3(leftX, bottom, z), new Vector3(leftX, top, z), coreWidth * 6.5f,
                new Color(haloColor.r, haloColor.g, haloColor.b, haloColor.a * pulse));
            SetLine(rightHalo, new Vector3(rightX, bottom, z), new Vector3(rightX, top, z), coreWidth * 6.5f,
                new Color(haloColor.r, haloColor.g, haloColor.b, haloColor.a * pulse));
            SetLine(leftCore, new Vector3(leftX, bottom, z), new Vector3(leftX, top, z), coreWidth, coreColor * pulse);
            SetLine(rightCore, new Vector3(rightX, bottom, z), new Vector3(rightX, top, z), coreWidth, coreColor * pulse);
        }

        private bool TryGetLocalBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var found = false;
            bounds = default;
            foreach (var renderer in renderers)
            {
                if (renderer is LineRenderer || renderer is ParticleSystemRenderer) continue;
                var worldBounds = renderer.bounds;
                for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                for (var z = -1; z <= 1; z += 2)
                {
                    var corner = worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z));
                    var local = transform.InverseTransformPoint(corner);
                    if (!found) { bounds = new Bounds(local, Vector3.zero); found = true; }
                    else bounds.Encapsulate(local);
                }
            }
            return found;
        }

        private static void SetLine(LineRenderer line, Vector3 start, Vector3 end, float width, Color color)
        {
            if (line == null) return;
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 6;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }
}
