using UnityEngine;
using UnityEngine.UI;

namespace StructureBuild
{
    /// <summary>Subtle low-cost luminance motion for authored holographic UI.</summary>
    public sealed class UiBreathingAccent : MonoBehaviour
    {
        public Graphic target;
        public Color dimColor = new Color(0.72f, 0.95f, 1f, 0.82f);
        public Color brightColor = Color.white;
        [Min(0.05f)] public float speed = 0.42f;
        public float phase;
        [Min(0f)] public float scaleAmount = 0.008f;

        private Vector3 baseScale;

        private void Awake()
        {
            if (target == null) target = GetComponent<Graphic>();
            baseScale = transform.localScale;
        }

        private void Update()
        {
            if (target == null) return;
            var amount = 0.5f + Mathf.Sin((Time.unscaledTime * speed + phase) * Mathf.PI * 2f) * 0.5f;
            target.color = Color.Lerp(dimColor, brightColor, amount);
            transform.localScale = baseScale * (1f + amount * scaleAmount);
        }
    }
}
