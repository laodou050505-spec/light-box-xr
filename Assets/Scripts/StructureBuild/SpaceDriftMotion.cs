using UnityEngine;

namespace StructureBuild
{
    public sealed class SpaceDriftMotion : MonoBehaviour
    {
        [Tooltip("Planet-like decorations use a much quieter movement profile.")]
        public bool subtlePlanetMotion;
        public Vector3 rotationDegreesPerSecond = new Vector3(0.18f, 0.65f, 0.12f);
        public Vector3 swayAmplitude = new Vector3(0.08f, 0.18f, 0.06f);
        public float swaySpeed = 0.20f;
        [Range(0f, 6.2832f)] public float phaseOffset;

        private Vector3 origin;
        private Vector3 visualPivotLocal;
        private Vector3 previousDrift;

        private void Awake()
        {
            origin = transform.localPosition;
            previousDrift = Vector3.zero;
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                visualPivotLocal = Vector3.zero;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            // Imported Tripo prefabs often have an offset root pivot. Cache the
            // renderer-space center so the authored object spins around what the
            // player sees as its own center rather than orbiting around the root.
            visualPivotLocal = transform.InverseTransformPoint(bounds.center);
        }

        private void Update()
        {
            var drift = new Vector3(
                Mathf.Sin(Time.time * swaySpeed + phaseOffset) * swayAmplitude.x,
                Mathf.Sin(Time.time * swaySpeed * 0.73f + phaseOffset) * swayAmplitude.y,
                Mathf.Cos(Time.time * swaySpeed * 0.91f + phaseOffset) * swayAmplitude.z);
            transform.localPosition += drift - previousDrift;
            previousDrift = drift;

            var pivotWorld = transform.TransformPoint(visualPivotLocal);
            var localDelta = Quaternion.Euler(rotationDegreesPerSecond * Time.deltaTime);
            var worldDelta = transform.rotation * localDelta * Quaternion.Inverse(transform.rotation);
            transform.rotation = worldDelta * transform.rotation;
            transform.position = pivotWorld + worldDelta * (transform.position - pivotWorld);
        }
    }
}
