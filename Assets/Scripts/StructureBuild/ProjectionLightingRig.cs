using UnityEngine;
using UnityEngine.Rendering;

namespace StructureBuild
{
    [ExecuteAlways]
    public sealed class ProjectionLightingRig : MonoBehaviour
    {
        public Transform projectorModel;
        public Transform gameplayTarget;
        [Tooltip("Editable world-space origin for this projector beam. Falls back to the projector bounds when unassigned.")]
        public Transform originAnchor;
        [Tooltip("Editable world-space landing point for this projector beam. Falls back to Gameplay Target when unassigned.")]
        public Transform targetAnchor;
        public Light spotLight;
        public LineRenderer beam;
        public LineRenderer lensGlow;
        public Color lightColor = new Color(0.38f, 0.90f, 1f, 1f);
        public float baseIntensity = 820f;
        public float footprintRadius = 2.8f;
        [Min(0.05f)] public float beamEndWidthMultiplier = 2.12f;
        public float pulseSpeed = 0.75f;
        public float pulsePhase;

        private Renderer[] cachedRenderers;
        private float nextBoundsRefresh;

        private void OnEnable()
        {
            cachedRenderers = null;
            nextBoundsRefresh = 0f;
            Refresh();
        }
        private void Update() => Refresh();

        public void Refresh()
        {
            if (projectorModel == null || gameplayTarget == null || !TryGetBounds(projectorModel, out var bounds)) return;

            var fallbackTarget = gameplayTarget.position;
            var towardTarget = fallbackTarget - bounds.center;
            if (towardTarget.sqrMagnitude < 0.0001f) return;
            towardTarget.Normalize();
            var horizontal = Vector3.ProjectOnPlane(towardTarget, Vector3.up).normalized;
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = towardTarget;
            var horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            var fallbackOrigin = bounds.center + Vector3.up * bounds.extents.y * 0.46f + horizontal * horizontalExtent * 0.62f;
            var origin = originAnchor != null ? originAnchor.position : fallbackOrigin;
            var target = targetAnchor != null ? targetAnchor.position : fallbackTarget;
            var direction = target - origin;
            var distance = direction.magnitude;
            if (distance < 0.01f) return;
            direction /= distance;

            var pulse = Application.isPlaying ? 0.94f + Mathf.Sin(Time.time * pulseSpeed + pulsePhase) * 0.06f : 1f;

            if (spotLight != null)
            {
                spotLight.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction, Vector3.up));
                spotLight.type = LightType.Spot;
                spotLight.color = lightColor;
                spotLight.intensity = baseIntensity * pulse;
                spotLight.range = distance * 1.24f;
                spotLight.spotAngle = Mathf.Clamp(Mathf.Atan2(footprintRadius, distance) * Mathf.Rad2Deg * 2.15f, 30f, 72f);
                spotLight.innerSpotAngle = spotLight.spotAngle * 0.62f;
                spotLight.shadows = LightShadows.None;
                spotLight.enabled = true;
            }

            if (beam != null)
            {
                beam.useWorldSpace = true;
                beam.positionCount = 2;
                beam.SetPosition(0, origin + direction * 0.04f);
                beam.SetPosition(1, target);
                beam.startWidth = Mathf.Clamp(horizontalExtent * 0.28f, 0.10f, 0.34f);
                beam.endWidth = footprintRadius * beamEndWidthMultiplier;
                beam.startColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.38f * pulse);
                beam.endColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.035f);
                beam.alignment = LineAlignment.View;
                beam.numCapVertices = 6;
                beam.shadowCastingMode = ShadowCastingMode.Off;
                beam.receiveShadows = false;
            }

            if (lensGlow != null)
            {
                lensGlow.useWorldSpace = true;
                lensGlow.positionCount = 2;
                lensGlow.SetPosition(0, origin - direction * 0.008f);
                lensGlow.SetPosition(1, origin + direction * 0.024f);
                var lensSize = Mathf.Clamp(horizontalExtent * 0.34f, 0.13f, 0.34f);
                lensGlow.startWidth = lensSize;
                lensGlow.endWidth = lensSize * 0.82f;
                lensGlow.startColor = new Color(0.86f, 0.98f, 1f, 0.95f * pulse);
                lensGlow.endColor = new Color(lightColor.r, lightColor.g, lightColor.b, 0.52f * pulse);
                lensGlow.alignment = LineAlignment.View;
                lensGlow.numCapVertices = 10;
                lensGlow.shadowCastingMode = ShadowCastingMode.Off;
                lensGlow.receiveShadows = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (originAnchor == null || targetAnchor == null) return;
            Gizmos.color = new Color(lightColor.r, lightColor.g, lightColor.b, 0.85f);
            Gizmos.DrawLine(originAnchor.position, targetAnchor.position);
            Gizmos.DrawWireSphere(originAnchor.position, 0.09f);
            Gizmos.DrawWireSphere(targetAnchor.position, 0.13f);
        }

        private bool TryGetBounds(Transform root, out Bounds bounds)
        {
            var now = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
            if (cachedRenderers == null || now >= nextBoundsRefresh)
            {
                cachedRenderers = root.GetComponentsInChildren<Renderer>(true);
                nextBoundsRefresh = now + 0.08f;
            }
            var renderers = cachedRenderers;
            if (renderers.Length == 0) { bounds = default; return false; }
            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return true;
        }
    }
}
