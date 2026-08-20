using System.Collections.Generic;
using UnityEngine;

namespace StructureBuild
{
    public sealed class AuthoredLightStripGlow : MonoBehaviour
    {
        public Color emissionColor = new Color(0.42f, 0.96f, 1f, 1f);
        [Min(0f)] public float emissionIntensity = 3.8f;
        public float pulseAmount = 0.08f;
        public float pulseSpeed = 0.85f;

        private readonly List<Material> runtimeMaterials = new List<Material>();
        private float nextGlowUpdate;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                for (var index = 0; index < materials.Length; index++)
                {
                    var source = materials[index];
                    if (source == null) continue;
                    var material = new Material(source) { name = source.name + "_RuntimeStripGlow" };
                    material.EnableKeyword("_EMISSION");
                    materials[index] = material;
                    runtimeMaterials.Add(material);
                }
                renderer.materials = materials;
            }
            nextGlowUpdate = 0f;
            ApplyGlow(1f);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (Time.time < nextGlowUpdate) return;
            nextGlowUpdate = Time.time + 0.08f;
            var pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            ApplyGlow(pulse);
        }

        private void ApplyGlow(float pulse)
        {
            var color = emissionColor * (emissionIntensity * pulse);
            foreach (var material in runtimeMaterials)
            {
                if (material == null) continue;
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color);
                if (material.HasProperty("_EmissiveColor")) material.SetColor("_EmissiveColor", color);
            }
        }

        private void OnDestroy()
        {
            foreach (var material in runtimeMaterials)
            {
                if (material != null) Destroy(material);
            }
            runtimeMaterials.Clear();
        }
    }
}
