using System.Collections.Generic;
using UnityEngine;

namespace StructureBuild
{
    public sealed class SpaceBackdropController : MonoBehaviour
    {
        [Header("Scene only — no Light components are created")]
        public Transform sceneRoot;
        public int starCount = 1400;
        public float starRadius = 42f;
        public int meteorCount = 14;
        public int meteorShowerStreakCount = 10;
        public float meteorShowerInterval = 26f;
        public float meteorShowerDuration = 6.5f;
        public Color spaceColor = new Color(0.002f, 0.006f, 0.025f, 1f);

        private readonly List<MeteorStreak> meteors = new List<MeteorStreak>();
        private readonly List<ShowerMeteorStreak> showerMeteors = new List<ShowerMeteorStreak>();
        private Material starMaterial;
        private Material meteorMaterial;
        private ParticleSystem starSystem;
        private ParticleSystem.Particle[] starParticles;
        private Color[] starColors;
        private float[] starSizes;
        private float[] starSpeeds;
        private float[] starPhases;
        private float nextMeteorShower;
        private float meteorShowerEnd = -1f;
        [Min(0.02f)] public float starUpdateInterval = 0.08f;
        private float nextStarUpdate;

        private void Awake()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientSkyColor = spaceColor;
            RenderSettings.ambientEquatorColor = spaceColor;
            RenderSettings.ambientGroundColor = spaceColor;
            RenderSettings.ambientIntensity = 0f;
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = spaceColor;
            }
            CreateMaterials();
            CreateStars();
            CreateMeteors();
            CreateMeteorShower();
        }

        private void Update()
        {
            var time = Time.time;
            if (time >= nextStarUpdate)
            {
                UpdateStars(time);
                nextStarUpdate = time + starUpdateInterval;
            }
            foreach (var meteor in meteors) meteor.Tick(time);
            UpdateMeteorShower(time);
            foreach (var meteor in showerMeteors) meteor.Tick(time);
        }

        private void CreateMaterials()
        {
            var starShader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            // Keep streaks on the same unlit path as the stars so they remain visible in the light-free scene.
            var lineShader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            starMaterial = new Material(starShader) { color = Color.white };
            meteorMaterial = new Material(lineShader) { color = new Color(0.45f, 0.85f, 1f, 0.9f) };
            SetMaterialColor(meteorMaterial, Color.white);
        }

        private void CreateStars()
        {
            var root = new GameObject("Stars_Twinkle");
            root.transform.SetParent(transform, false);
            starSystem = root.AddComponent<ParticleSystem>();
            var main = starSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = starCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = 99999f;
            var emission = starSystem.emission;
            emission.enabled = false;
            var renderer = starSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = starMaterial;
            renderer.minParticleSize = 0.001f;
            renderer.maxParticleSize = 0.045f;

            starParticles = new ParticleSystem.Particle[starCount];
            starColors = new Color[starCount];
            starSizes = new float[starCount];
            starSpeeds = new float[starCount];
            starPhases = new float[starCount];
            var random = new System.Random(4127);
            for (var i = 0; i < starCount; i++)
            {
                // Reserve a dense band for the lower hemisphere so the star field continues below the table.
                var direction = i < starCount * 0.38f
                    ? RandomDirection(random, -0.98f, -0.08f)
                    : RandomDirection(random, -0.98f, 0.98f);
                starColors[i] = Color.Lerp(new Color(0.50f, 0.72f, 1f, 1f), Color.white, (float)random.NextDouble());
                starSizes[i] = Mathf.Lerp(0.035f, 0.14f, (float)random.NextDouble());
                starSpeeds[i] = 0.45f + (float)random.NextDouble() * 1.35f;
                starPhases[i] = (float)random.NextDouble() * 6.28f;
                starParticles[i] = new ParticleSystem.Particle
                {
                    position = transform.position + direction * (starRadius + (float)random.NextDouble() * 7f),
                    startLifetime = 99999f,
                    remainingLifetime = 99999f,
                    startSize = starSizes[i],
                    startColor = starColors[i],
                    randomSeed = (uint)(i + 1),
                };
            }
            starSystem.SetParticles(starParticles, starParticles.Length);
        }

        private void UpdateStars(float time)
        {
            if (starSystem == null || starParticles == null) return;
            for (var i = 0; i < starParticles.Length; i++)
            {
                var pulse = 0.12f + 0.88f * Mathf.Pow(Mathf.Abs(Mathf.Sin(time * starSpeeds[i] + starPhases[i])), 3f);
                var color = starColors[i] * Mathf.Lerp(0.16f, 2.25f, pulse);
                color.a = Mathf.Lerp(0.12f, 1f, pulse);
                starParticles[i].startColor = color;
                starParticles[i].startSize = starSizes[i] * (0.42f + pulse * 1.05f);
            }
            starSystem.SetParticles(starParticles, starParticles.Length);
        }

        private void CreateMeteors()
        {
            var root = new GameObject("Meteors").transform;
            root.SetParent(transform, false);
            var random = new System.Random(9812);
            for (var i = 0; i < meteorCount; i++)
            {
                var meteor = new GameObject($"Meteor_{i + 1:00}");
                meteor.transform.SetParent(root, false);
                var line = meteor.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = 0.085f;
                line.endWidth = 0.006f;
                line.alignment = LineAlignment.View;
                line.numCapVertices = 3;
                line.numCornerVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sharedMaterial = meteorMaterial;
                line.useWorldSpace = true;
                meteors.Add(new MeteorStreak(line, transform, Camera.main, meteorMaterial, starRadius * 0.82f, random.NextDouble() * 4.2 + 1.2));
            }
        }

        private void CreateMeteorShower()
        {
            var root = new GameObject("MeteorShower").transform;
            root.SetParent(transform, false);
            for (var index = 0; index < meteorShowerStreakCount; index++)
            {
                var meteor = new GameObject($"ShowerMeteor_{index + 1:00}");
                meteor.transform.SetParent(root, false);
                var line = meteor.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = 0.10f;
                line.endWidth = 0.008f;
                line.alignment = LineAlignment.View;
                line.numCapVertices = 3;
                line.numCornerVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sharedMaterial = meteorMaterial;
                line.useWorldSpace = true;
                showerMeteors.Add(new ShowerMeteorStreak(line, meteorMaterial));
            }
            nextMeteorShower = Time.time + 10f;
        }

        private void UpdateMeteorShower(float time)
        {
            if (showerMeteors.Count == 0) return;
            if (meteorShowerEnd < 0f && time >= nextMeteorShower)
            {
                meteorShowerEnd = time + meteorShowerDuration;
                var view = Camera.main != null ? Camera.main.transform : transform;
                var travel = (-view.up * 0.36f + view.forward * 0.16f + view.right * Random.Range(-0.12f, 0.12f)).normalized;
                for (var index = 0; index < showerMeteors.Count; index++)
                {
                    var spacing = meteorShowerDuration / Mathf.Max(1, showerMeteors.Count);
                    var delay = index * spacing + Random.Range(0f, spacing * 0.42f);
                    var origin = view.position + view.forward * Random.Range(24f, 31f)
                        + view.right * Random.Range(-12f, 12f)
                        + view.up * Random.Range(5f, 13f);
                    var direction = (travel + Random.insideUnitSphere * 0.055f).normalized;
                    showerMeteors[index].Schedule(time + delay, origin, direction, Random.Range(19f, 27f), Random.Range(0.72f, 1.05f));
                }
            }

            if (meteorShowerEnd > 0f && time >= meteorShowerEnd + 1.4f)
            {
                meteorShowerEnd = -1f;
                nextMeteorShower = time + meteorShowerInterval + Random.Range(-3f, 6f);
            }
        }

        private static Vector3 RandomDirection(System.Random random, float minY, float maxY)
        {
            var y = Mathf.Lerp(minY, maxY, (float)random.NextDouble());
            var angle = (float)random.NextDouble() * Mathf.PI * 2f;
            var radius = Mathf.Sqrt(1f - y * y);
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius).normalized;
        }

        private sealed class MeteorStreak
        {
            private readonly LineRenderer line;
            private readonly Transform center;
            private readonly Camera viewCamera;
            private readonly Transform head;
            private readonly float radius;
            private readonly double interval;
            private float nextStart;
            private float startTime = -99f;
            private Vector3 origin;
            private Vector3 direction;
            public MeteorStreak(LineRenderer line, Transform center, Camera viewCamera, Material headMaterial, float radius, double interval)
            {
                this.line = line; this.center = center; this.viewCamera = viewCamera; this.radius = radius; this.interval = interval; nextStart = (float)interval;
                head = CreateMeteorHead(line.transform.parent, headMaterial, "MeteorHead_Runtime");
                line.enabled = false;
            }
            public void Tick(float time)
            {
                if (time >= nextStart && startTime < 0f)
                {
                    var view = viewCamera != null ? viewCamera.transform : center;
                    origin = view.position + view.forward * Random.Range(24f, 32f)
                        + view.right * Random.Range(-10f, 10f)
                        + view.up * Random.Range(5f, 13f);
                    direction = (-view.up * Random.Range(0.24f, 0.48f) + view.forward * Random.Range(0.12f, 0.30f) + view.right * Random.Range(-0.20f, 0.20f)).normalized;
                    startTime = time;
                    line.enabled = true;
                    head.gameObject.SetActive(true);
                }
                if (startTime < 0f) return;
                var life = (time - startTime) / 0.75f;
                if (life >= 1f)
                {
                    line.enabled = false;
                    head.gameObject.SetActive(false);
                    startTime = -99f;
                    nextStart = time + (float)interval + Random.Range(1.5f, 7f);
                    return;
                }
                var headPosition = origin + direction * (life * 18f);
                var brightness = Mathf.Sin(life * Mathf.PI);
                line.startColor = new Color(0.56f, 0.90f, 1f, Mathf.Clamp01(brightness));
                line.endColor = new Color(0.56f, 0.90f, 1f, 0f);
                line.SetPosition(0, headPosition);
                line.SetPosition(1, headPosition - direction * Mathf.Lerp(0.8f, 6.8f, life));
                this.head.position = headPosition;
                this.head.localScale = Vector3.one * Mathf.Lerp(0.065f, 0.16f, brightness);
            }
        }

        private sealed class ShowerMeteorStreak
        {
            private readonly LineRenderer line;
            private readonly Transform head;
            private float scheduledStart = float.PositiveInfinity;
            private float startTime = -1f;
            private float speed;
            private float duration;
            private Vector3 origin;
            private Vector3 direction;

            public ShowerMeteorStreak(LineRenderer line, Material headMaterial)
            {
                this.line = line;
                head = CreateMeteorHead(line.transform.parent, headMaterial, "ShowerHead_Runtime");
                line.enabled = false;
            }

            public void Schedule(float start, Vector3 startPosition, Vector3 travelDirection, float travelSpeed, float lifeTime)
            {
                scheduledStart = start;
                startTime = -1f;
                origin = startPosition;
                direction = travelDirection;
                speed = travelSpeed;
                duration = lifeTime;
                line.enabled = false;
                head.gameObject.SetActive(false);
            }

            public void Tick(float time)
            {
                if (startTime < 0f && time >= scheduledStart)
                {
                    startTime = time;
                    line.enabled = true;
                    head.gameObject.SetActive(true);
                }
                if (startTime < 0f) return;

                var life = (time - startTime) / duration;
                if (life >= 1f)
                {
                    line.enabled = false;
                    head.gameObject.SetActive(false);
                    startTime = -1f;
                    scheduledStart = float.PositiveInfinity;
                    return;
                }

                var headPosition = origin + direction * (life * speed);
                var brightness = Mathf.Sin(life * Mathf.PI);
                var color = new Color(0.56f, 0.90f, 1f, Mathf.Clamp01(brightness));
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0f);
                line.SetPosition(0, headPosition);
                line.SetPosition(1, headPosition - direction * Mathf.Lerp(1.0f, 6.2f, brightness));
                this.head.position = headPosition;
                this.head.localScale = Vector3.one * Mathf.Lerp(0.08f, 0.2f, brightness);
            }
        }

        private static Transform CreateMeteorHead(Transform parent, Material material, string name)
        {
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = name;
            head.transform.SetParent(parent, true);
            var collider = head.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            head.GetComponent<Renderer>().sharedMaterial = material;
            head.transform.localScale = Vector3.one * 0.1f;
            head.SetActive(false);
            return head.transform;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", color);
        }
    }

}
