using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEditor.Android;
using UnityEditor.Build;
using ByteDance.PICO.XR;

namespace StructureBuild.Editor
{
    public static class StructureBuildSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/StructureBuild.unity";
        private const string Root = "Assets/Models";

        [MenuItem("Structure Build/Create Future Tech Scene")]
        public static void CreateScene()
        {
            if (File.Exists(ScenePath) && !Application.isBatchMode && !EditorUtility.DisplayDialog("重新生成结构构建场景", "这会覆盖现有 StructureBuild.unity。若你已经手工调整过场景，请先备份或取消。", "继续生成", "取消")) return;
            EnsureTextMeshProResources();
            EnsureProjectSettings();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("STRUCTURE_BUILD_SCENE");
            var environment = new GameObject("Environment_Editable").transform; environment.SetParent(root.transform);
            CreateEnvironment(environment);
            var puzzle = new GameObject("PuzzleTable_Editable").transform; puzzle.SetParent(root.transform); puzzle.position = new Vector3(0, 0.95f, 0);
            CreatePuzzleTable(puzzle);
            var cubePrefab = CreatePuzzleCubePrefab();
            var gameObject = new GameObject("GameSystems"); gameObject.transform.SetParent(root.transform);
            var game = gameObject.AddComponent<StructureGameController>(); game.gridRoot = puzzle.Find("GridRoot"); game.cubeSource = puzzle.Find("CubeSource_Editable"); game.cubePrefab = cubePrefab; game.frontPanel = puzzle.Find("FrontProjectionPanel"); game.sidePanel = puzzle.Find("SideProjectionPanel");
            CreateCanvas(gameObject.transform, game);
            CreateDesktopCamera(root.transform, puzzle);
            CreateXRRoot(root.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Structure Build scene created at " + ScenePath);
        }

        private static void CreateEnvironment(Transform parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder); floor.name = "Floor_Base_Editable"; floor.transform.SetParent(parent); floor.transform.localScale = new Vector3(7, 0.12f, 7); floor.transform.localPosition = new Vector3(0, 0, 0); ApplyMaterial(floor, new Color(0.055f, 0.12f, 0.18f), 0.35f, 0.6f);
            var back = GameObject.CreatePrimitive(PrimitiveType.Cube); back.name = "Wall_Backdrop_Editable"; back.transform.SetParent(parent); back.transform.localPosition = new Vector3(0, 3.3f, 6.5f); back.transform.localScale = new Vector3(14, 6.5f, 0.2f); ApplyMaterial(back, new Color(0.035f, 0.09f, 0.14f), 0.8f, 0.2f);
            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube); leftWall.name = "Wall_Left_Editable"; leftWall.transform.SetParent(parent); leftWall.transform.localPosition = new Vector3(-7, 3.3f, 0); leftWall.transform.localScale = new Vector3(0.2f, 6.5f, 13); ApplyMaterial(leftWall, new Color(0.03f, 0.08f, 0.13f), 0.75f, 0.25f);
            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube); rightWall.name = "Wall_Right_Editable"; rightWall.transform.SetParent(parent); rightWall.transform.localPosition = new Vector3(7, 3.3f, 0); rightWall.transform.localScale = new Vector3(0.2f, 6.5f, 13); ApplyMaterial(rightWall, new Color(0.03f, 0.08f, 0.13f), 0.75f, 0.25f);
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube); ceiling.name = "Ceiling_Editable"; ceiling.transform.SetParent(parent); ceiling.transform.localPosition = new Vector3(0, 6.5f, 0); ceiling.transform.localScale = new Vector3(14, 0.16f, 13); ApplyMaterial(ceiling, new Color(0.04f, 0.10f, 0.15f), 0.65f, 0.35f);
            for (var x = -4; x <= 4; x += 2) { var rib = GameObject.CreatePrimitive(PrimitiveType.Cube); rib.name = "WallRib_Editable_" + x; rib.transform.SetParent(parent); rib.transform.localPosition = new Vector3(x, 3.25f, 3.1f); rib.transform.localScale = new Vector3(0.08f, 6f, 0.2f); ApplyMaterial(rib, new Color(0.2f, 0.75f, 0.9f), 0.25f, 0.7f); }
            var light = new GameObject("OverheadLightStrip_Editable"); light.transform.SetParent(parent); light.transform.localPosition = new Vector3(0, 6.2f, 0); var area = light.AddComponent<Light>(); area.type = LightType.Rectangle; area.color = new Color(0.65f, 0.9f, 1f); area.intensity = 850; area.range = 12; area.transform.localScale = new Vector3(5, 1, 1);
            var key = new GameObject("KeyLight"); key.transform.SetParent(parent); key.transform.localPosition = new Vector3(-3, 5, -3); var directional = key.AddComponent<Light>(); directional.type = LightType.Directional; directional.intensity = 1.6f; directional.color = new Color(0.75f, 0.9f, 1f); key.transform.rotation = Quaternion.Euler(45, -25, 0);
            var fill = new GameObject("WarmAccentLight"); fill.transform.SetParent(parent); fill.transform.localPosition = new Vector3(0, 2, -2); var point = fill.AddComponent<Light>(); point.type = LightType.Point; point.range = 6; point.intensity = 3; point.color = new Color(1f, 0.45f, 0.12f);
        }

        private static void CreatePuzzleTable(Transform parent)
        {
            var table = CreateModel("WorkbenchTop_Editable", FindModel(1), parent, new Vector3(0, 0.25f, 0), 3.5f);
            var baseModel = CreateModel("WorkbenchBase_Editable", FindModel(2), parent, new Vector3(0, -0.1f, 0), 2.6f);
            var frontModel = CreateModel("FrontProjectionModel_Editable", FindModel(3), parent, new Vector3(0, 2.7f, 2.25f), 1.55f); frontModel.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var sideModel = CreateModel("SideProjectionModel_Editable", FindModel(3), parent, new Vector3(2.25f, 2.7f, 0), 1.55f); sideModel.transform.localRotation = Quaternion.Euler(0, 90, 0);
            var front = CreateProjectionAnchor("FrontProjectionPanel", parent, new Vector3(0, 2.7f, 2.18f), Quaternion.Euler(0, 180, 0), 4);
            var side = CreateProjectionAnchor("SideProjectionPanel", parent, new Vector3(2.18f, 2.7f, 0), Quaternion.Euler(0, 90, 0), 4);
            CreateModel("CubeEmissionPedestal_Editable", FindModel(6), parent, new Vector3(-2.3f, 1.2f, 1.8f), 0.85f);
            CreateModel("SanctuaryWallRib_Editable", FindModel(8), parent, new Vector3(-3.7f, 3.0f, 3.0f), 2.1f);
            CreateModel("OverheadLightStripModel_Editable", FindModel(9), parent, new Vector3(0, 5.9f, 0), 2.6f);
            var source = CreateModel("CubeSource_Editable", FindModel(5), parent, new Vector3(-2.3f, 1.5f, 1.8f), 0.30f); source.AddComponent<CubeSourceInteractable>(); EnsureModelCollider(source);
            var grid = new GameObject("GridRoot"); grid.transform.SetParent(parent); grid.transform.localPosition = new Vector3(0, 0.82f, 0);
            for (var x = 0; x < 4; x++) for (var z = 0; z < 4; z++) { var marker = GameObject.CreatePrimitive(PrimitiveType.Quad); marker.name = $"GridMarker_{x}_{z}"; marker.transform.SetParent(grid.transform); marker.transform.localPosition = new Vector3((x - 1.5f) * 0.34f, 0, (z - 1.5f) * 0.34f); marker.transform.localRotation = Quaternion.Euler(90, 0, 0); marker.transform.localScale = Vector3.one * 0.30f; ApplyMaterial(marker, new Color(0.08f, 0.32f, 0.4f, 0.18f), 0.3f, 0.3f); Object.DestroyImmediate(marker.GetComponent<Collider>()); }
        }

        private static GameObject CreateModel(string name, string path, Transform parent, Vector3 localPosition, float targetSize)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var instance = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = name; instance.transform.localPosition = localPosition; instance.transform.localScale = Vector3.one;
            var bounds = CalculateBounds(instance); var max = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (max > 0.0001f) instance.transform.localScale = Vector3.one * (targetSize / max);
            return instance;
        }
        private static Bounds CalculateBounds(GameObject root)
        { var renderers = root.GetComponentsInChildren<Renderer>(); if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one); var bounds = renderers[0].bounds; for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds); return bounds; }
        private static void EnsureModelCollider(GameObject root)
        {
            if (root.GetComponentInChildren<Collider>() != null) return;
            var bounds = CalculateBounds(root);
            var scale = root.transform.lossyScale;
            var collider = root.AddComponent<BoxCollider>();
            collider.center = root.transform.InverseTransformPoint(bounds.center);
            collider.size = new Vector3(bounds.size.x / Mathf.Max(Mathf.Abs(scale.x), 0.0001f), bounds.size.y / Mathf.Max(Mathf.Abs(scale.y), 0.0001f), bounds.size.z / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
        }
        private static GameObject CreatePuzzleCubePrefab()
        {
            const string prefabFolder = "Assets/Prefabs";
            const string prefabPath = prefabFolder + "/PuzzleCube.prefab";
            if (!AssetDatabase.IsValidFolder(prefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            var temporaryRoot = new GameObject("PuzzleCube");
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FindModel(5));
            if (modelAsset != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, temporaryRoot.transform);
                var bounds = CalculateBounds(temporaryRoot);
                var max = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                if (max > 0.0001f) visual.transform.localScale *= 0.30f / max;
                bounds = CalculateBounds(temporaryRoot);
                visual.transform.position -= bounds.center;
            }
            else
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(temporaryRoot.transform);
                visual.transform.localScale = Vector3.one * 0.30f;
                ApplyMaterial(visual, new Color(1f, 0.42f, 0.05f), 0.15f, 0.65f);
            }
            EnsureModelCollider(temporaryRoot);
            var prefab = PrefabUtility.SaveAsPrefabAsset(temporaryRoot, prefabPath);
            Object.DestroyImmediate(temporaryRoot);
            return prefab;
        }
        private static Transform CreateProjectionAnchor(string name, Transform parent, Vector3 position, Quaternion rotation, int columns)
        {
            var anchor = new GameObject(name).transform; anchor.SetParent(parent); anchor.localPosition = position; anchor.localRotation = rotation;
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube); board.name = "ProjectionBoard_4x3_Editable"; board.transform.SetParent(anchor); board.transform.localPosition = new Vector3(0, 0, 0.035f); board.transform.localRotation = Quaternion.identity; var boardWidth = columns * 0.34f + 0.24f; board.transform.localScale = new Vector3(boardWidth, boardWidth * 0.75f, 0.025f); ApplyTransparentMaterial(board, new Color(0.03f, 0.20f, 0.28f, 0.28f));
            for (var y = 0; y < 3; y++) for (var column = 0; column < columns; column++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube); tile.name = $"ProjectionTile_{column}_{y}"; tile.transform.SetParent(anchor); tile.transform.localPosition = new Vector3((column - 1.5f) * 0.34f, (y - 1f) * 0.34f, 0); tile.transform.localScale = new Vector3(0.29f, 0.29f, 0.035f); ApplyTransparentMaterial(tile, new Color(0.04f, 0.22f, 0.3f, 0.08f)); tile.GetComponent<Collider>().enabled = false;
            }
            return anchor;
        }
        private static string FindModel(int index) { var guids = AssetDatabase.FindAssets("t:Model", new[] { Root + "/" + index }); foreach (var guid in guids) { var path = AssetDatabase.GUIDToAssetPath(guid); if (path.EndsWith(".fbx")) return path; } return null; }
        private static void ApplyMaterial(GameObject go, Color color, float metallic, float smoothness)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null) return;
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            renderer.sharedMaterial = mat;
        }
        private static void ApplyTransparentMaterial(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var shader = Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader == null) return;
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            renderer.sharedMaterial = mat;
        }
        private static void CreateCanvas(Transform parent, StructureGameController game)
        {
            var canvas = new GameObject("WorldSpaceHUD");
            canvas.transform.SetParent(parent);
            canvas.transform.localPosition = new Vector3(0, 3.95f, 1.55f);
            canvas.transform.localRotation = Quaternion.Euler(0, 180, 0);
            game.levelLabel = CreateHudText(canvas.transform, "LevelLabel", "档案 01 / 12", new Vector3(0, 0.22f, 0), 0.17f, new Color(0.65f, 0.95f, 1f));
            game.statusLabel = CreateHudText(canvas.transform, "StatusLabel", "等待结构输入", new Vector3(0, 0, 0), 0.12f, new Color(0.95f, 0.72f, 0.28f));
            game.cubeCountLabel = CreateHudText(canvas.transform, "CubeCountLabel", "方块 0 / 3", new Vector3(0, -0.19f, 0), 0.11f, new Color(0.50f, 0.85f, 0.92f));
        }
        private static TMPro.TextMeshPro CreateHudText(Transform parent, string name, string content, Vector3 localPosition, float size, Color color)
        {
            var text = new GameObject(name).AddComponent<TMPro.TextMeshPro>();
            text.transform.SetParent(parent);
            text.transform.localPosition = localPosition;
            text.text = content;
            text.fontSize = size;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = color;
            return text;
        }
        private static void CreateDesktopCamera(Transform parent, Transform target)
        {
            var camera = new GameObject("DesktopCamera");
            camera.tag = "MainCamera";
            camera.transform.SetParent(parent);
            camera.transform.position = new Vector3(7, 5.2f, -8);
            camera.transform.LookAt(target.position + Vector3.up * 1.8f);
            camera.AddComponent<Camera>();
            var orbit = camera.AddComponent<DesktopCameraOrbit>();
            orbit.target = target;
            orbit.targetOffset = Vector3.up * 1.45f;
            orbit.distance = 10f;
            orbit.yaw = 138f;
            orbit.pitch = 22f;
        }
        private static void CreateXRRoot(Transform parent)
        {
            var xr = new GameObject("XR Rig"); xr.transform.SetParent(parent); var picoManager = xr.AddComponent<PXR_Manager>(); picoManager.enabled = false;
            var camera = new GameObject("XR Camera"); camera.transform.SetParent(xr.transform); camera.transform.localPosition = new Vector3(0, 1.6f, -5.5f); camera.AddComponent<Camera>(); camera.GetComponent<Camera>().enabled = false; camera.tag = "Untagged"; camera.AddComponent<PicoControllerPose>().node = UnityEngine.XR.XRNode.Head;
            var left = CreateController("Left Controller", xr.transform, UnityEngine.XR.XRNode.LeftHand); var right = CreateController("Right Controller", xr.transform, UnityEngine.XR.XRNode.RightHand);
            var game = Object.FindAnyObjectByType<StructureGameController>(); AddInteraction(left, game, UnityEngine.XR.XRNode.LeftHand); AddInteraction(right, game, UnityEngine.XR.XRNode.RightHand);
            var bootstrap = xr.AddComponent<PicoRigBootstrap>(); bootstrap.xrCamera = camera.GetComponent<Camera>(); bootstrap.picoManager = picoManager;
        }
        private static GameObject CreateController(string name, Transform parent, UnityEngine.XR.XRNode node) { var controller = new GameObject(name); controller.transform.SetParent(parent); controller.AddComponent<PicoControllerPose>().node = node; return controller; }
        private static void AddInteraction(GameObject controller, StructureGameController game, UnityEngine.XR.XRNode node) { var line = controller.AddComponent<LineRenderer>(); line.startWidth = 0.008f; line.endWidth = 0.002f; line.material = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.2f, 0.85f, 1f, 0.75f) }; var interaction = controller.AddComponent<WorldSpaceInteraction>(); interaction.game = game; interaction.rayOrigin = controller.transform; interaction.rayLine = line; interaction.controllerNode = node; }
        private static void EnsureTextMeshProResources()
        {
            var settingsAssetPath = Path.Combine(Application.dataPath, "TextMesh Pro", "Resources", "TMP Settings.asset");
            if (File.Exists(settingsAssetPath)) return;
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var packageCache = Path.Combine(projectRoot, "Library", "PackageCache");
            var packagePath = Directory.Exists(packageCache)
                ? Directory.GetFiles(packageCache, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).FirstOrDefault()
                : null;
            if (string.IsNullOrEmpty(packagePath)) throw new FileNotFoundException("TMP Essential Resources.unitypackage was not found in the Unity package cache.");
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
        private static void EnsureProjectSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android); PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP); PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64; PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29; PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity; PlayerSettings.companyName = "Structure Build"; PlayerSettings.productName = "结构构建"; PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.structurebuild.pico"); PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan });
            var buildTargetSettings = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget"); XRGeneralSettingsPerBuildTarget perTarget = null; if (buildTargetSettings.Length > 0) perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(buildTargetSettings[0])); if (perTarget == null) { perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>(); AssetDatabase.CreateAsset(perTarget, "Assets/XRGeneralSettingsPerBuildTarget.asset"); }
            var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android); if (general == null) { general = ScriptableObject.CreateInstance<XRGeneralSettings>(); AssetDatabase.AddObjectToAsset(general, perTarget); var manager = ScriptableObject.CreateInstance<XRManagerSettings>(); AssetDatabase.AddObjectToAsset(manager, perTarget); general.Manager = manager; perTarget.SetSettingsForBuildTarget(BuildTargetGroup.Android, general); }
            general.InitManagerOnStart = true;
            if (general.Manager != null)
            {
                general.Manager.automaticLoading = true;
                general.Manager.automaticRunning = true;
                XRPackageMetadataStore.AssignLoader(general.Manager, "ByteDance.PICO.XR.PXR_Loader", BuildTargetGroup.Android);
                EditorUtility.SetDirty(general.Manager);
            }
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(perTarget);
            var settings = PXR_Settings.GetSettings();
            if (settings == null)
            {
                const string picoSettingsPath = "Assets/XR/Settings/PXR_Settings.asset";
                settings = AssetDatabase.LoadAssetAtPath<PXR_Settings>(picoSettingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<PXR_Settings>();
                    AssetDatabase.CreateAsset(settings, picoSettingsPath);
                    AssetDatabase.SaveAssets();
                }

                EditorBuildSettings.AddConfigObject("ByteDance.PICO.XR.Settings", settings, true);
            }

            settings.appMode = PXR_Settings.AppMode.XR;
            settings.stereoRenderingModeAndroid = PXR_Settings.StereoRenderingModeAndroid.Multiview;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }
    }
}
