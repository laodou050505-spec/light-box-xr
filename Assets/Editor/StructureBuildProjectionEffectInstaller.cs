using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace StructureBuild.Editor
{
    public static class StructureBuildProjectionEffectInstaller
    {
        private const string ScenePath = "Assets/Scenes/StructureBuild.unity";
        private const string EffectRootName = "ProjectionLightingEffects_Editable";
        private const string GlowMaterialPath = "Assets/Materials/StructureProjectionGlow.mat";
        private const string EmitterAssetPath = "Assets/Models/6/tripo_convert_40314d87-792c-416a-92c3-a6508253cf7a.fbx";

        [MenuItem("Structure Build/Install Static Scene Projection Effects")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var game = UnityEngine.Object.FindAnyObjectByType<StructureGameController>();
            Require(game != null && game.cubeSource != null && game.frontPanel != null && game.sidePanel != null,
                "Gameplay references are incomplete.");
            var frontModel = FindSceneObject("FrontProjectionModel_Editable")?.transform;
            var sideModel = FindSceneObject("SideProjectionModel_Editable")?.transform;
            Require(frontModel != null && sideModel != null, "Projection backing models are missing.");
            var emitters = FindEmitterModels(game).ToArray();
            if (emitters.Length != 2)
            {
                // The authored scene can use a different model for the projector housings.
                // Resolve the two editable projector assemblies by their spatial relationship
                // to the gameplay table, without changing their authored transforms.
                emitters = FindFallbackProjectorModels(game).ToArray();
            }
            Require(emitters.Length == 2, $"Expected exactly two authored projector buildings, found {emitters.Length}.");

            var preservedAnchorPositions = CaptureAnchorPositions();
            var previousRoot = FindSceneObject(EffectRootName);
            if (previousRoot != null) UnityEngine.Object.DestroyImmediate(previousRoot);
            // Panel glow hosts are parented to the authored panel assemblies so the
            // effect follows the user's editable screen placement. Remove any prior
            // generated hosts explicitly to keep reinstallation idempotent.
            // The four authored light-strip models beside the two projection
            // boards are the source of truth. Remove only our old procedural
            // overlay hosts so they cannot mask or replace those strips.
            foreach (var glow in FindSceneObjects<PanelEdgeGlow>()) UnityEngine.Object.DestroyImmediate(glow.gameObject);
            foreach (var motion in FindSceneObjects<SpaceDriftMotion>().Where(item => !IsAuthorizedSpaceDecorMotion(item)))
                UnityEngine.Object.DestroyImmediate(motion);
            foreach (var spin in FindSceneObjects<InPlaceSpinMotion>()) UnityEngine.Object.DestroyImmediate(spin);

            var authoredTransforms = CaptureTransforms();
            var glowMaterial = EnsureGlowMaterial();
            var presentationRoot = FindSceneObject("GameplayPresentation_v2")?.transform ?? game.transform.parent ?? game.transform;
            var effectRoot = new GameObject(EffectRootName).transform;
            effectRoot.SetParent(presentationRoot, false);
            effectRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            effectRoot.localScale = Vector3.one;

            var spinMotion = game.cubeSource.gameObject.AddComponent<InPlaceSpinMotion>();
            spinMotion.worldAxis = Vector3.up;
            spinMotion.degreesPerSecond = 16f;

            var target = new GameObject("ProjectionPresentationTarget_Runtime").transform;
            target.SetParent(effectRoot, false);
            var gameplayBounds = GetGameplayBounds(game);
            target.position = new Vector3(gameplayBounds.center.x, gameplayBounds.max.y + 0.62f, gameplayBounds.center.z);
            var footprintRadius = Mathf.Max(gameplayBounds.extents.x, gameplayBounds.extents.z) * 1.35f + 0.35f;

            // Do not create procedural panel glows. The scene already contains
            // four authored OverheadLightStripModel_Editable meshes.

            emitters = emitters.OrderBy(item => item.position.x).ThenBy(item => item.position.z).ToArray();
            for (var index = 0; index < emitters.Length; index++)
            {
                CreateProjectorRig(index == 0 ? "LeftProjectorFX_Editable" : "RightProjectorFX_Editable",
                    effectRoot, emitters[index], target, glowMaterial, footprintRadius, preservedAnchorPositions);
            }

            // Keep all user-authored light strips and point lights exactly as
            // placed. Only the generated projector rig owns its Spot Light.

            AssertTransformsUnchanged(authoredTransforms);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("STRUCTURE_PROJECTION_EFFECTS_OK: user layout preserved, all authored models static, cube source spins, panel strips glow, and two projector rigs illuminate the game table.");
        }

        [MenuItem("Structure Build/Apply Latest Gameplay And Effects")]
        public static void ApplyLatestGameplayAndEffects()
        {
            StructureBuildCurrentSceneInstaller.RestoreAuthoredLightStatesFromLatestLayout();
            StructureBuildCurrentSceneInstaller.InstallCurrentScene();
            Install();
            StructureBuildCurrentSceneInstaller.ValidateInstalledCurrentScene();
            Validate();
            Debug.Log("STRUCTURE_LATEST_APPLY_OK: latest gameplay, magnetic snapping, static-scene effects, and validations completed.");
        }

        [MenuItem("Structure Build/Validate Static Scene Projection Effects")]
        public static void Validate()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var game = UnityEngine.Object.FindAnyObjectByType<StructureGameController>();
            Require(game != null, "Game controller is missing.");
            Require(FindSceneObjects<SpaceDriftMotion>().All(IsAuthorizedSpaceDecorMotion),
                "A fixed gameplay model still has drift motion.");
            var spins = FindSceneObjects<InPlaceSpinMotion>();
            Require(spins.Length == 1 && spins[0].transform == game.cubeSource, "Only the authored source cube may spin.");
            var rigs = FindSceneObjects<ProjectionLightingRig>();
            Require(rigs.Length == 2 && rigs.All(rig => rig.projectorModel != null && rig.spotLight != null && rig.spotLight.enabled && rig.beam != null && rig.lensGlow != null),
                "The two projector lighting rigs are incomplete.");
            Require(rigs.All(rig => rig.originAnchor != null && rig.targetAnchor != null &&
                                      rig.originAnchor.parent == rig.transform && rig.targetAnchor.parent == rig.transform),
                "Each projector rig must contain editable origin and target anchors.");
            Require(rigs.Select(rig => rig.originAnchor).Distinct().Count() == 2 &&
                    rigs.Select(rig => rig.targetAnchor).Distinct().Count() == 2,
                "The left and right projector rigs must use independent editable anchors.");
            var authoredStrips = FindSceneObjects<Transform>().Count(item => item.name.StartsWith("OverheadLightStripModel_Editable", StringComparison.Ordinal));
            Require(authoredStrips == 4, $"Expected four authored panel light strips, found {authoredStrips}.");
            Debug.Log("STRUCTURE_PROJECTION_EFFECTS_VALIDATE_OK: static scene, one spinning cube, four authored panel light strips, and two projector-only lights.");
        }

        private static void CreatePanelGlow(string name, Transform root, Transform panelFrame, Transform panelModel, Transform target, Material material)
        {
            var glowRoot = new GameObject(name).transform;
            glowRoot.SetParent(panelFrame != null ? panelFrame : root, false);
            glowRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            glowRoot.localScale = Vector3.one;
            var glow = glowRoot.gameObject.AddComponent<PanelEdgeGlow>();
            glow.panelModel = panelModel;
            glow.gameplayTarget = target;
            glow.leftHalo = CreateLine("LeftHalo", glowRoot, material, 0);
            glow.rightHalo = CreateLine("RightHalo", glowRoot, material, 0);
            glow.leftCore = CreateLine("LeftCore", glowRoot, material, 1);
            glow.rightCore = CreateLine("RightCore", glowRoot, material, 1);
            glow.Refresh();
        }

        private static void CreateProjectorRig(string name, Transform root, Transform model, Transform target, Material material,
            float footprintRadius, IReadOnlyDictionary<string, Vector3> preservedAnchorPositions)
        {
            var rigObject = new GameObject(name);
            rigObject.transform.SetParent(root, false);
            rigObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            rigObject.transform.localScale = Vector3.one;
            var beam = CreateLine("VolumetricBeam", rigObject.transform, material, -2);
            var light = beam.gameObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.renderMode = LightRenderMode.ForcePixel;
            light.shadows = LightShadows.None;
            light.cullingMask = ~0;
            var rig = rigObject.AddComponent<ProjectionLightingRig>();
            rig.projectorModel = model;
            rig.gameplayTarget = target;
            rig.spotLight = light;
            rig.footprintRadius = footprintRadius;
            rig.pulsePhase = name.StartsWith("Left", StringComparison.Ordinal) ? 0f : Mathf.PI;
            rig.beam = beam;
            rig.lensGlow = CreateLine("LensGlow", rigObject.transform, material, 2);

            // Calculate the original automatic placement once, then expose it as
            // independent Scene-view anchors that the user can move freely.
            rig.Refresh();
            var side = name.StartsWith("Left", StringComparison.Ordinal) ? "Left" : "Right";
            rig.originAnchor = CreateAnchor($"{side}BeamOrigin_Editable", rigObject.transform,
                light.transform.position, preservedAnchorPositions);
            rig.targetAnchor = CreateAnchor($"{side}BeamTarget_Editable", rigObject.transform,
                target.position, preservedAnchorPositions);
            rig.Refresh();
        }

        private static Transform CreateAnchor(string name, Transform parent, Vector3 fallbackPosition,
            IReadOnlyDictionary<string, Vector3> preservedAnchorPositions)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.position = preservedAnchorPositions.TryGetValue(name, out var preservedPosition)
                ? preservedPosition
                : fallbackPosition;
            anchor.rotation = Quaternion.identity;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static Dictionary<string, Vector3> CaptureAnchorPositions()
        {
            return FindSceneObjects<Transform>()
                .Where(item => item.name.EndsWith("BeamOrigin_Editable", StringComparison.Ordinal) ||
                               item.name.EndsWith("BeamTarget_Editable", StringComparison.Ordinal))
                .GroupBy(item => item.name)
                .ToDictionary(group => group.Key, group => group.First().position);
        }

        private static LineRenderer CreateLine(string name, Transform parent, Material material, int sortingOrder)
        {
            var lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 6;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private static Material EnsureGlowMaterial()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
            var material = AssetDatabase.LoadAssetAtPath<Material>(GlowMaterialPath);
            if (material != null) return material;
            var shader = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Particles/Standard Unlit") ??
                         Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            Require(shader != null, "No compatible additive shader is available.");
            material = new Material(shader) { name = "StructureProjectionGlow", renderQueue = 3100 };
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, GlowMaterialPath);
            return material;
        }

        private static IEnumerable<Transform> FindEmitterModels(StructureGameController game)
        {
            var roots = new List<Transform>();
            foreach (var transform in FindSceneObjects<Transform>())
            {
                var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
                if (prefabRoot == null) continue;
                var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
                if (!string.Equals(path, EmitterAssetPath, StringComparison.Ordinal)) continue;
                if (prefabRoot.transform == game.cubeSource) continue;
                if (!roots.Contains(prefabRoot.transform)) roots.Add(prefabRoot.transform);
            }
            return roots;
        }

        private static IEnumerable<Transform> FindFallbackProjectorModels(StructureGameController game)
        {
            var tableBounds = GetGameplayBounds(game);
            var sceneRoot = FindSceneObject("STRUCTURE_BUILD_SCENE")?.transform;
            var excludedTokens = new[] { "projection", "panel", "screen", "cube", "overhead", "sanctuary", "environment", "gameplay", "ui", "camera", "rig", "xr" };
            var allCandidates = FindSceneObjects<Renderer>()
                .Where(renderer => renderer != null && renderer.gameObject.scene.IsValid())
                .Select(renderer => GetAuthoredModelRoot(renderer.transform, sceneRoot))
                .Where(item => item != null && item != game.cubeSource)
                .GroupBy(item => item)
                .Select(group => group.Key)
                .ToArray();
            Debug.Log("All authored roots: " + string.Join(" | ", allCandidates.Select(item => $"{item.name}@{item.position:F2}")));
            var namedProjectors = allCandidates
                .Where(item => item.name.StartsWith("WorkbenchBase_Editable", StringComparison.Ordinal))
                .OrderBy(item => item.position.x)
                .ThenBy(item => item.position.z)
                .ToArray();
            if (namedProjectors.Length == 2) return namedProjectors;
            var candidates = allCandidates
                .Where(item => !excludedTokens.Any(token => item.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
                .Where(item => item.GetComponentInChildren<Renderer>(true) != null)
                .Where(item => Vector3.Distance(item.position, tableBounds.center) > Mathf.Max(tableBounds.extents.x, tableBounds.extents.z) * 1.55f)
                .OrderByDescending(item => Vector3.Distance(item.position, tableBounds.center))
                .ThenBy(item => item.position.x)
                .ToArray();
            Debug.Log("Projection fallback candidates: " + string.Join(", ", candidates.Select(item => item.name)));
            return candidates.Length >= 2 ? candidates.Take(2) : Enumerable.Empty<Transform>();
        }

        private static Transform GetAuthoredModelRoot(Transform source, Transform sceneRoot)
        {
            var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(source.gameObject);
            if (prefabRoot != null) return prefabRoot.transform;
            var current = source;
            while (current.parent != null && current.parent != sceneRoot) current = current.parent;
            return current;
        }

        private static bool IsAuthorizedSpaceDecorMotion(SpaceDriftMotion motion)
        {
            return motion != null && motion.transform.parent == null &&
                   motion.transform.name.StartsWith("tripo_convert_", StringComparison.OrdinalIgnoreCase);
        }

        private static Bounds GetGameplayBounds(StructureGameController game)
        {
            var groundRenderers = FindSceneObjects<Transform>()
                .Where(item => item.name.StartsWith(game.groundCellNamePrefix, StringComparison.Ordinal))
                .Select(item => item.GetComponentInChildren<Renderer>(true))
                .Where(item => item != null)
                .Distinct()
                .ToArray();
            Require(groundRenderers.Length == 16, $"Expected 16 authored ground cells, found {groundRenderers.Length}.");
            var bounds = groundRenderers[0].bounds;
            for (var index = 1; index < groundRenderers.Length; index++) bounds.Encapsulate(groundRenderers[index].bounds);
            return bounds;
        }

        private static Dictionary<Transform, TransformState> CaptureTransforms()
        {
            return FindSceneObjects<Transform>().ToDictionary(item => item, item => new TransformState(item));
        }

        private static void AssertTransformsUnchanged(Dictionary<Transform, TransformState> states)
        {
            foreach (var pair in states)
            {
                var transform = pair.Key;
                var state = pair.Value;
                Require(transform != null && transform.parent == state.parent &&
                        Vector3.Distance(transform.localPosition, state.localPosition) < 0.00001f &&
                        Quaternion.Angle(transform.localRotation, state.localRotation) < 0.001f &&
                        Vector3.Distance(transform.localScale, state.localScale) < 0.00001f,
                    $"Authored transform changed while installing effects: {(transform != null ? transform.name : "deleted object")}.");
            }
        }

        private static GameObject FindSceneObject(string name)
        {
            return FindSceneObjects<Transform>().FirstOrDefault(item => item.name == name)?.gameObject;
        }

        private static T[] FindSceneObjects<T>() where T : UnityEngine.Object
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private readonly struct TransformState
        {
            public readonly Transform parent;
            public readonly Vector3 localPosition;
            public readonly Quaternion localRotation;
            public readonly Vector3 localScale;

            public TransformState(Transform transform)
            {
                parent = transform.parent;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }
        }
    }
}
