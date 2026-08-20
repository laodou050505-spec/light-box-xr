using UnityEngine;

namespace StructureBuild
{
    public sealed class InPlaceSpinMotion : MonoBehaviour
    {
        public Vector3 worldAxis = Vector3.up;
        public float degreesPerSecond = 18f;

        private Vector3 visualPivotLocal;
        private bool pivotReady;

        private void Awake() => CacheVisualPivot();

        private void Update()
        {
            if (worldAxis.sqrMagnitude < 0.0001f || Mathf.Approximately(degreesPerSecond, 0f)) return;
            if (!pivotReady) CacheVisualPivot();
            var pivot = transform.TransformPoint(visualPivotLocal);
            transform.RotateAround(pivot, worldAxis.normalized, degreesPerSecond * Time.deltaTime);
        }

        private void CacheVisualPivot()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                visualPivotLocal = Vector3.zero;
                pivotReady = true;
                return;
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            visualPivotLocal = transform.InverseTransformPoint(bounds.center);
            pivotReady = true;
        }
    }
}
