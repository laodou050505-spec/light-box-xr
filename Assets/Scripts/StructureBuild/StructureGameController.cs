using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace StructureBuild
{
    public class StructureGameController : MonoBehaviour
    {
        [Header("Scene references")]
        public Transform gridRoot;
        public Transform cubeSource;
        public Transform frontPanel;
        public Transform sidePanel;
        public TMPro.TMP_Text levelLabel;
        public TMPro.TMP_Text statusLabel;
        public TMPro.TMP_Text cubeCountLabel;
        public UnityEvent onLevelComplete;
        public StructureAudioController audio;

        [Header("Grid layout (metres)")]
        public float cellSize = 0.34f;
        public float gridBaseHeight = 0.03f;
        public float cubeSize = 0.30f;
        public GameObject cubePrefab;
        public Material cubeMaterial;
        public Material hologramMaterial;
        [Range(0.65f, 0.98f)] public float gridCubeFill = 0.80f;

        [Header("Scene-authored grid")]
        public bool usePlacedGroundCells = true;
        public string groundCellNamePrefix = "WorkbenchTop_Editable";
        public bool usePlacedGridMarkers = true;
        public bool fitCubeToPrefabBounds = true;

        [Header("Magnetic drag snapping")]
        [Range(0.8f, 1.5f)] public float snapRadiusMultiplier = 1.15f;
        [Range(1f, 1.8f)] public float snapHysteresisMultiplier = 1.32f;
        public float maxSnapRayDistance = 14f;

        private int levelIndex;
        private LevelDefinition level;
        private readonly HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();
        private readonly Dictionary<Vector3Int, GameObject> cubeObjects = new Dictionary<Vector3Int, GameObject>();
        private readonly Stack<List<Vector3Int>> history = new Stack<List<Vector3Int>>();
        private EvaluationResult evaluation;
        private GameObject preview;
        private Transform dropIndicator;
        private Material previewMaterial;
        private Material dropIndicatorMaterial;
        private Camera playerCamera;
        private Vector3Int keyboardColumn;
        private bool dragging;
        private bool desktopOwnsDrag;
        private Vector3Int? dragCell;
        private Vector3Int? dragOrigin;
        private List<Vector3Int> dragSnapshot;
        private float dragFollowDistance;
        private bool levelCompleted;
        private readonly Dictionary<Vector2Int, GridAnchor> gridAnchors = new Dictionary<Vector2Int, GridAnchor>();
        private readonly Dictionary<Renderer, Material> projectionMaterials = new Dictionary<Renderer, Material>();
        private float cubeVisualHeight;
        private float cubeVisualCenterY;
        private float cubeVisualWidth;
        private float cubeVisualDepth;
        private float cubeVisualCenterX;
        private float cubeVisualCenterZ;
        private float cubeVisualScale = 1f;

        private readonly struct GridAnchor
        {
            public readonly Vector3 surfaceCenter;

            public GridAnchor(Vector3 surfaceCenter)
            {
                this.surfaceCenter = surfaceCenter;
            }
        }

        public int CurrentLevelNumber => levelIndex + 1;
        public int TotalLevelCount => CampaignData.Levels.Length;
        public string CurrentLevelName => level != null ? level.displayName : string.Empty;
        public string CurrentLevelRule => level != null ? level.rule : string.Empty;
        public int CurrentCubeCount => occupied.Count;
        public int CurrentCubeBudget => level != null ? level.cubeLimit : 0;
        public string CurrentStatusText { get; private set; } = "等待结构输入";
        public bool IsDragging => dragging;
        public bool IsLevelCompleted => levelCompleted;
        public Vector3Int? CurrentDropCell => dragCell;
        public Bounds GroundBounds { get; private set; }
        public Bounds StructureBounds
        {
            get
            {
                var bounds = GroundBounds;
                if (bounds.size == Vector3.zero) return bounds;
                var top = bounds.max.y + Mathf.Max(cubeVisualHeight, cubeSize) * 3f;
                bounds.Encapsulate(new Vector3(bounds.center.x, top, bounds.center.z));
                return bounds;
            }
        }

        private void Awake()
        {
            playerCamera = Camera.main;
            if (audio == null) audio = FindAnyObjectByType<StructureAudioController>();
            if (gridRoot == null) gridRoot = transform;
            if (cubePrefab == null) cubePrefab = CreateFallbackCubePrefab();
            CachePlacedGridMarkers();
            CalibrateCubePrefab();
            LoadLevel(0);
        }

        private void Update()
        {
            HandleDesktopInput();
            UpdateWorldLabels();
        }

        public void LoadLevel(int index)
        {
            if (index < 0 || index >= CampaignData.Levels.Length) return;
            StopDragVisuals();
            levelIndex = index; level = CampaignData.Levels[index]; levelCompleted = false; history.Clear(); ClearCells();
            if (levelLabel) levelLabel.text = $"档案 {index + 1:00} / {CampaignData.Levels.Length} · {level.displayName}";
            EvaluateAndDisplay();
        }

        public void NextLevel() => ContinueAfterCompletion();

        public void ContinueAfterCompletion()
        {
            if (!levelCompleted) return;
            audio?.PlayContinue();
            if (levelIndex < CampaignData.Levels.Length - 1) LoadLevel(levelIndex + 1);
            else RestartCampaign();
        }

        public void RestartCampaign() => LoadLevel(0);

        public bool TryPlaceAt(Vector3Int cell) => TryPlaceAt(cell, true);

        private bool TryPlaceAt(Vector3Int cell, bool playFeedback)
        {
            if (dragging || levelCompleted || !InBounds(cell) || occupied.Contains(cell) || occupied.Count >= level.cubeLimit || !ProjectionRules.IsSupported(cell, occupied)) return false;
            PushHistory(); occupied.Add(cell); SpawnCube(cell); EvaluateAndDisplay();
            if (playFeedback) audio?.PlayPlace();
            return true;
        }

        public void RemoveTopAt(int x, int z)
        {
            if (dragging || levelCompleted) return;
            var y = level.height - 1;
            while (y >= 0 && !occupied.Contains(new Vector3Int(x, y, z))) y--;
            if (y < 0) return;
            var cell = new Vector3Int(x, y, z); PushHistory(); occupied.Remove(cell); DestroyCube(cell); EvaluateAndDisplay();
            audio?.PlayUndo();
        }

        public void Undo()
        {
            if (dragging || levelCompleted || history.Count == 0) return;
            var previous = history.Pop(); ClearCells(); foreach (var cell in previous) { occupied.Add(cell); SpawnCube(cell); } EvaluateAndDisplay();
            audio?.PlayUndo();
        }

        public void ResetCurrentLevel()
        {
            if (dragging || levelCompleted) return;
            if (occupied.Count > 0) { PushHistory(); audio?.PlayReset(); }
            ClearCells(); EvaluateAndDisplay();
        }

        public void Hint()
        {
            if (dragging || levelCompleted || occupied.Count >= level.cubeLimit) return;
            foreach (var data in level.referenceSolution)
            {
                var cell = data.ToVector3Int();
                if (!occupied.Contains(cell) && ProjectionRules.IsSupported(cell, occupied))
                {
                    TryPlaceAt(cell, false);
                    audio?.PlayHint();
                    CurrentStatusText = $"提示: 已放置 {cell.x},{cell.y},{cell.z}";
                    if (statusLabel) statusLabel.text = CurrentStatusText;
                    return;
                }
            }
        }

        public bool BeginDragFromSource(Ray ray)
        {
            if (dragging || levelCompleted || level == null) return false;
            if (occupied.Count >= level.cubeLimit)
            {
                SetStatus("结构单元已用完，请先移动或撤回方块");
                return false;
            }

            var start = cubeSource != null ? cubeSource.position : ray.GetPoint(2f);
            BeginDrag(null, start, ray);
            audio?.PlayGrab();
            return true;
        }

        public bool BeginDragFromCube(PuzzleCubeInteractable cube, Ray ray)
        {
            if (dragging || levelCompleted || cube == null || !occupied.Contains(cube.cell)) return false;
            if (occupied.Contains(cube.cell + Vector3Int.up))
            {
                SetStatus("请先移走上方方块");
                return false;
            }

            var origin = cube.cell;
            var start = cube.transform.position;
            BeginDrag(origin, start, ray);
            occupied.Remove(origin);
            DestroyCube(origin);
            EvaluateAndDisplay();
            SetStatus("拖动方块到高亮落点");
            audio?.PlayGrab();
            return true;
        }

        public bool UpdateDrag(Ray ray)
        {
            if (!dragging || preview == null) return false;

            var previousCell = dragCell;
            var valid = TryGetDropCell(ray, out var cell) && CanPlaceDraggedCell(cell);
            dragCell = valid ? cell : null;
            var target = valid
                ? CellToWorld(cell) + gridRoot.up * 0.025f
                : ray.GetPoint(dragFollowDistance);
            var changedCell = valid && (!previousCell.HasValue || previousCell.Value != cell);
            var smoothing = 1f - Mathf.Exp(-30f * Mathf.Max(Time.unscaledDeltaTime, 0.001f));
            preview.transform.position = changedCell ? target : Vector3.Lerp(preview.transform.position, target, smoothing);
            preview.transform.rotation = Quaternion.Slerp(preview.transform.rotation, gridRoot.rotation, smoothing);

            if (dropIndicator != null)
            {
                dropIndicator.gameObject.SetActive(valid);
                if (valid)
                {
                    dropIndicator.position = GetGridSurfacePosition(cell.x, cell.z) + gridRoot.up * (cell.y * cubeVisualHeight + 0.014f);
                    dropIndicator.rotation = gridRoot.rotation;
                    var pulse = 1f + Mathf.Sin(Time.time * 7f) * 0.035f;
                    dropIndicator.localScale = Vector3.one * pulse;
                }
            }

            SetStatus(valid ? $"释放以放置 {cell.x},{cell.y},{cell.z}" : "将方块移动到棋盘高亮区域");
            return valid;
        }

        public bool CommitDrag(Ray ray)
        {
            if (!dragging) return false;
            UpdateDrag(ray);
            var destination = dragCell;
            var origin = dragOrigin;
            var snapshot = dragSnapshot;
            StopDragVisuals();

            if (destination.HasValue && CanPlaceDraggedCell(destination.Value))
            {
                var cell = destination.Value;
                occupied.Add(cell);
                SpawnCube(cell);
                if (!origin.HasValue || origin.Value != cell) history.Push(snapshot ?? new List<Vector3Int>(occupied));
                EvaluateAndDisplay();
                audio?.PlayPlace();
                return true;
            }

            if (origin.HasValue)
            {
                occupied.Add(origin.Value);
                SpawnCube(origin.Value);
            }
            EvaluateAndDisplay();
            SetStatus("未命中合法落点，方块已返回");
            audio?.PlayReject();
            return false;
        }

        public void CancelDrag()
        {
            if (!dragging) return;
            var origin = dragOrigin;
            StopDragVisuals();
            if (origin.HasValue && !occupied.Contains(origin.Value))
            {
                occupied.Add(origin.Value);
                SpawnCube(origin.Value);
            }
            EvaluateAndDisplay();
            SetStatus("已取消拖动");
            audio?.PlayReject();
        }

        private void BeginDrag(Vector3Int? origin, Vector3 start, Ray ray)
        {
            dragging = true;
            dragOrigin = origin;
            dragCell = null;
            dragSnapshot = new List<Vector3Int>(occupied);
            dragFollowDistance = Mathf.Clamp(Vector3.Distance(ray.origin, start), 0.65f, 12f);
            CreateDragVisual(start);
            UpdateDrag(ray);
        }

        private bool CanPlaceDraggedCell(Vector3Int cell)
        {
            return level != null && !levelCompleted && InBounds(cell) && !occupied.Contains(cell) &&
                   occupied.Count < level.cubeLimit && ProjectionRules.IsSupported(cell, occupied);
        }

        private void SetStatus(string message)
        {
            CurrentStatusText = message;
            if (statusLabel != null) statusLabel.text = message;
        }

        private void EvaluateAndDisplay()
        {
            evaluation = ProjectionRules.Evaluate(occupied, level);
            if (cubeCountLabel) cubeCountLabel.text = $"方块 {occupied.Count} / {level.cubeLimit}";
            if (statusLabel)
            {
                CurrentStatusText = evaluation.complete
                    ? "正面与侧面投影已吻合 · 结构完成"
                    : $"正面 {evaluation.front.matched}/{evaluation.front.targetCount} · 侧面 {evaluation.side.matched}/{evaluation.side.targetCount}";
                statusLabel.text = CurrentStatusText;
            }
            else
            {
                CurrentStatusText = evaluation.complete
                    ? "正面与侧面投影已吻合 · 结构完成"
                    : $"正面 {evaluation.front.matched}/{evaluation.front.targetCount} · 侧面 {evaluation.side.matched}/{evaluation.side.targetCount}";
            }
            UpdateProjectionPanel(frontPanel, evaluation.frontCurrent, level.frontHeights, level.width);
            UpdateProjectionPanel(sidePanel, evaluation.sideCurrent, level.sideHeights, level.depth);
            if (evaluation.complete && !levelCompleted)
            {
                levelCompleted = true;
                audio?.PlayComplete();
                onLevelComplete?.Invoke();
            }
        }

        private void UpdateProjectionPanel(Transform panel, bool[,] current, int[] targetHeights, int columns)
        {
            if (panel == null) return;
            for (var y = 0; y < level.height; y++) for (var column = 0; column < columns; column++)
            {
                var tile = panel.Find($"ProjectionTile_{column}_{y}");
                if (tile == null) continue;
                var renderer = tile.GetComponent<Renderer>();
                var material = GetProjectionMaterial(renderer);
                if (material == null) continue;
                var wants = targetHeights != null && column < targetHeights.Length && y < targetHeights[column];
                var has = current[y, column];
                var color = wants && has
                    ? new Color(0.12f, 1.00f, 0.70f, 1f)
                    : wants
                        ? new Color(1.00f, 0.43f, 0.05f, 1f)
                        : has
                            ? new Color(1.00f, 0.12f, 0.04f, 1f)
                            : new Color(0.015f, 0.07f, 0.09f, 1f);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * (wants || has ? 1.4f : 0.45f));
                }
            }
        }

        private void HandleDesktopInput()
        {
            if (level == null) return;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame && desktopOwnsDrag)
                {
                    CancelDrag();
                    desktopOwnsDrag = false;
                }
                if (Keyboard.current.zKey.wasPressedThisFrame) Undo();
                if (Keyboard.current.rKey.wasPressedThisFrame) ResetCurrentLevel();
                if (Keyboard.current.hKey.wasPressedThisFrame) Hint();
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame) keyboardColumn.x = Mathf.Max(0, keyboardColumn.x - 1);
                if (Keyboard.current.rightArrowKey.wasPressedThisFrame) keyboardColumn.x = Mathf.Min(level.width - 1, keyboardColumn.x + 1);
                if (Keyboard.current.upArrowKey.wasPressedThisFrame) keyboardColumn.z = Mathf.Max(0, keyboardColumn.z - 1);
                if (Keyboard.current.downArrowKey.wasPressedThisFrame) keyboardColumn.z = Mathf.Min(level.depth - 1, keyboardColumn.z + 1);
                if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame) TryPlaceAt(new Vector3Int(keyboardColumn.x, ProjectionRules.ColumnTop(occupied, keyboardColumn.x, keyboardColumn.z, level.height), keyboardColumn.z));
                if (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame) RemoveTopAt(keyboardColumn.x, keyboardColumn.z);
            }
            if (Mouse.current == null || playerCamera == null) return;
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                var ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out var hit, 100f))
                {
                    var source = hit.collider.GetComponentInParent<CubeSourceInteractable>();
                    var cube = hit.collider.GetComponentInParent<PuzzleCubeInteractable>();
                    desktopOwnsDrag = source != null ? BeginDragFromSource(ray) : BeginDragFromCube(cube, ray);
                }
            }
            if (desktopOwnsDrag && Mouse.current.leftButton.isPressed)
            {
                var ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                UpdateDrag(ray);
            }
            if (desktopOwnsDrag && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                var ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                CommitDrag(ray);
                desktopOwnsDrag = false;
            }
            if (!dragging && Mouse.current.rightButton.wasPressedThisFrame)
            {
                var ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out var hit, 100f)) { var cube = hit.collider.GetComponentInParent<PuzzleCubeInteractable>(); if (cube != null) RemoveTopAt(cube.cell.x, cube.cell.z); }
            }
        }

        public bool TryGetDropCell(Ray ray, out Vector3Int cell)
        {
            cell = default;
            if (level == null || gridRoot == null) return false;

            if (TryGetDirectHitColumn(ray, out var directColumn) &&
                TryGetTopCell(directColumn, out var directCell) && CanPlaceDraggedCell(directCell))
                return StabilizeDropCell(ray, directCell, out cell);

            var planeOrigin = GroundBounds.size != Vector3.zero
                ? new Vector3(GroundBounds.center.x, GroundBounds.max.y, GroundBounds.center.z)
                : gridRoot.TransformPoint(Vector3.up * gridBaseHeight);
            var plane = new Plane(gridRoot.up, planeOrigin);
            if (plane.Raycast(ray, out var distance) && distance >= 0f && distance <= maxSnapRayDistance * 2f)
            {
                if (TryGetNearestValidPlaneCell(ray.GetPoint(distance), out var planeCell, out var nearestDistanceSqr))
                {
                    var radius = GetSnapRadius(new Vector2Int(planeCell.x, planeCell.z));
                    if (nearestDistanceSqr <= radius * radius) return StabilizeDropCell(ray, planeCell, out cell);
                }
            }

            return TryGetNearestRayCell(ray, out var rayCell) && StabilizeDropCell(ray, rayCell, out cell);
        }

        private bool TryGetNearestValidPlaneCell(Vector3 point, out Vector3Int cell, out float distanceSqr)
        {
            cell = default;
            distanceSqr = float.PositiveInfinity;
            var found = false;
            for (var x = 0; x < level.width; x++)
            for (var z = 0; z < level.depth; z++)
            {
                var column = new Vector2Int(x, z);
                if (!TryGetTopCell(column, out var candidate) || !CanPlaceDraggedCell(candidate)) continue;
                var delta = point - GetGridSurfacePosition(x, z);
                var planarDistanceSqr = Mathf.Pow(Vector3.Dot(delta, gridRoot.right), 2f) +
                                        Mathf.Pow(Vector3.Dot(delta, gridRoot.forward), 2f);
                if (planarDistanceSqr >= distanceSqr) continue;
                distanceSqr = planarDistanceSqr;
                cell = candidate;
                found = true;
            }
            return found;
        }

        private bool StabilizeDropCell(Ray ray, Vector3Int candidate, out Vector3Int cell)
        {
            cell = candidate;
            if (!dragCell.HasValue || dragCell.Value == candidate || !CanPlaceDraggedCell(dragCell.Value)) return true;

            var current = dragCell.Value;
            var currentDistance = DistanceFromRay(ray, CellToWorld(current));
            var candidateDistance = DistanceFromRay(ray, CellToWorld(candidate));
            var currentRadius = GetSnapRadius(new Vector2Int(current.x, current.z));
            var switchingMargin = Mathf.Max(0.02f, cellSize * 0.16f);
            if (currentDistance <= currentRadius && currentDistance <= candidateDistance + switchingMargin) cell = current;
            return true;
        }

        private static float DistanceFromRay(Ray ray, Vector3 point)
        {
            var alongRay = Mathf.Max(0f, Vector3.Dot(point - ray.origin, ray.direction));
            return Vector3.Distance(point, ray.GetPoint(alongRay));
        }

        private bool TryGetDirectHitColumn(Ray ray, out Vector2Int column)
        {
            column = default;
            var hits = Physics.RaycastAll(ray, maxSnapRayDistance)
                .OrderBy(hit => hit.distance);
            foreach (var hit in hits)
            {
                var cube = hit.collider.GetComponentInParent<PuzzleCubeInteractable>();
                if (cube != null)
                {
                    column = new Vector2Int(cube.cell.x, cube.cell.z);
                    return true;
                }

                var current = hit.collider.transform;
                while (current != null && !current.name.StartsWith(groundCellNamePrefix, StringComparison.Ordinal)) current = current.parent;
                if (current == null) continue;
                column = FindNearestGridColumn(hit.point, out _);
                return true;
            }
            return false;
        }

        private bool TryGetNearestRayCell(Ray ray, out Vector3Int cell)
        {
            cell = default;
            var found = false;
            var bestScore = float.PositiveInfinity;
            for (var x = 0; x < level.width; x++)
            for (var z = 0; z < level.depth; z++)
            {
                var column = new Vector2Int(x, z);
                if (!TryGetTopCell(column, out var candidate) || !CanPlaceDraggedCell(candidate)) continue;
                var center = CellToWorld(candidate);
                var alongRay = Vector3.Dot(center - ray.origin, ray.direction);
                if (alongRay < 0.08f || alongRay > maxSnapRayDistance) continue;
                var perpendicular = Vector3.Distance(center, ray.GetPoint(alongRay));
                var radius = GetSnapRadius(column);
                if (perpendicular > radius) continue;
                var score = perpendicular + alongRay * 0.0025f;
                if (dragCell.HasValue && dragCell.Value == candidate) score /= snapHysteresisMultiplier;
                if (score >= bestScore) continue;
                bestScore = score;
                cell = candidate;
                found = true;
            }
            return found;
        }

        private bool TryGetTopCell(Vector2Int column, out Vector3Int cell)
        {
            cell = default;
            if (column.x < 0 || column.x >= level.width || column.y < 0 || column.y >= level.depth) return false;
            var y = ProjectionRules.ColumnTop(occupied, column.x, column.y, level.height);
            if (y < 0) return false;
            cell = new Vector3Int(column.x, y, column.y);
            return true;
        }

        private float GetSnapRadius(Vector2Int column)
        {
            var radius = Mathf.Max(cellSize, Mathf.Max(cubeVisualWidth, cubeVisualDepth)) * snapRadiusMultiplier;
            if (dragCell.HasValue && dragCell.Value.x == column.x && dragCell.Value.z == column.y) radius *= snapHysteresisMultiplier;
            return radius;
        }

        private void UpdateWorldLabels() { }
        private bool InBounds(Vector3Int c) => c.x >= 0 && c.x < level.width && c.y >= 0 && c.y < level.height && c.z >= 0 && c.z < level.depth;
        private void PushHistory() { history.Push(new List<Vector3Int>(occupied)); }
        private void ClearCells() { foreach (var pair in cubeObjects) Destroy(pair.Value); cubeObjects.Clear(); occupied.Clear(); }
        private void SpawnCube(Vector3Int cell)
        {
            var cube = Instantiate(cubePrefab, CellToWorld(cell), gridRoot.rotation, gridRoot);
            cube.transform.localScale *= cubeVisualScale;
            cube.SetActive(true); cube.name = $"PuzzleCube_{cell.x}_{cell.y}_{cell.z}";
            var interactable = cube.GetComponent<PuzzleCubeInteractable>() ?? cube.AddComponent<PuzzleCubeInteractable>(); interactable.cell = cell;
            cubeObjects[cell] = cube;
        }
        private void DestroyCube(Vector3Int cell)
        {
            if (cubeObjects.TryGetValue(cell, out var cube))
            {
                cube.SetActive(false);
                Destroy(cube);
            }
            cubeObjects.Remove(cell);
        }
        public Vector3 CellToWorld(Vector3Int cell)
        {
            var surface = GetGridSurfacePosition(cell.x, cell.z);
            return surface + gridRoot.up * (cell.y * cubeVisualHeight + cubeVisualHeight * 0.5f - cubeVisualCenterY)
                - gridRoot.right * cubeVisualCenterX - gridRoot.forward * cubeVisualCenterZ;
        }

        private void CreateDragVisual(Vector3 start)
        {
            preview = Instantiate(cubePrefab, start, gridRoot.rotation, gridRoot);
            preview.transform.localScale *= cubeVisualScale;
            preview.name = "DraggedCubePreview_Runtime";
            preview.SetActive(true);
            foreach (var collider in preview.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var interactable in preview.GetComponentsInChildren<PuzzleCubeInteractable>(true)) interactable.enabled = false;

            previewMaterial ??= CreateUnlitMaterial("DraggedCube_Hologram", new Color(0.18f, 1f, 0.88f, 1f));
            foreach (var renderer in preview.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
                for (var index = 0; index < materials.Length; index++) materials[index] = previewMaterial;
                renderer.sharedMaterials = materials;
            }
            CreateDropIndicator();
        }

        private void CreateDropIndicator()
        {
            if (dropIndicator != null) return;
            var root = new GameObject("DropIndicator_Runtime");
            dropIndicator = root.transform;
            dropIndicator.SetParent(transform, true);
            dropIndicatorMaterial ??= CreateUnlitMaterial("DropIndicator_Hologram", new Color(0.08f, 1f, 0.72f, 1f));

            var width = Mathf.Max(cubeVisualWidth, cubeSize) * 1.08f;
            var depth = Mathf.Max(cubeVisualDepth, cubeSize) * 1.08f;
            CreateIndicatorBar("Front", new Vector3(0f, 0f, depth * 0.5f), new Vector3(width, 0.012f, 0.018f));
            CreateIndicatorBar("Back", new Vector3(0f, 0f, -depth * 0.5f), new Vector3(width, 0.012f, 0.018f));
            CreateIndicatorBar("Left", new Vector3(-width * 0.5f, 0f, 0f), new Vector3(0.018f, 0.012f, depth));
            CreateIndicatorBar("Right", new Vector3(width * 0.5f, 0f, 0f), new Vector3(0.018f, 0.012f, depth));
            dropIndicator.gameObject.SetActive(false);
        }

        private void CreateIndicatorBar(string name, Vector3 localPosition, Vector3 localScale)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(dropIndicator, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localScale = localScale;
            var collider = bar.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            bar.GetComponent<Renderer>().sharedMaterial = dropIndicatorMaterial;
        }

        private void StopDragVisuals()
        {
            if (preview != null)
            {
                preview.SetActive(false);
                Destroy(preview);
            }
            if (dropIndicator != null) dropIndicator.gameObject.SetActive(false);
            preview = null;
            dragging = false;
            desktopOwnsDrag = false;
            dragCell = null;
            dragOrigin = null;
            dragSnapshot = null;
        }

        private void CachePlacedGridMarkers()
        {
            gridAnchors.Clear();
            GroundBounds = default;
            if (gridRoot == null) return;

            if (usePlacedGroundCells)
            {
                var searchRoot = gridRoot.parent != null ? gridRoot.parent : gridRoot;
                var grounds = searchRoot.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name.StartsWith(groundCellNamePrefix, StringComparison.Ordinal))
                    .Select(item => new { transform = item, index = GetGroundCellIndex(item.name) })
                    .Where(item => item.index >= 0 && item.index < 16)
                    .OrderBy(item => item.index)
                    .ToArray();

                foreach (var ground in grounds)
                {
                    if (!TryGetRendererBounds(ground.transform.gameObject, out var bounds)) continue;
                    var key = new Vector2Int(ground.index / 4, ground.index % 4);
                    gridAnchors[key] = new GridAnchor(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
                    ConfigureSurfaceVisibility(ground.transform.gameObject);
                    if (GroundBounds.size == Vector3.zero) GroundBounds = bounds;
                    else GroundBounds.Encapsulate(bounds);
                }
            }

            if (gridAnchors.Count != 16 && usePlacedGridMarkers)
            {
                gridAnchors.Clear();
                for (var x = 0; x < 4; x++) for (var z = 0; z < 4; z++)
                {
                    var marker = gridRoot.Find($"GridMarker_{x}_{z}");
                    if (marker == null) continue;
                    gridAnchors[new Vector2Int(x, z)] = new GridAnchor(marker.position + gridRoot.up * gridBaseHeight);
                }
            }

            if (gridAnchors.TryGetValue(new Vector2Int(0, 0), out var origin) && gridAnchors.TryGetValue(new Vector2Int(1, 0), out var xStep))
            {
                var measured = Vector3.Distance(origin.surfaceCenter, xStep.surfaceCenter);
                if (measured > 0.001f) cellSize = measured;
            }
        }

        private void CalibrateCubePrefab()
        {
            cubeVisualHeight = Mathf.Max(cubeSize, 0.01f);
            cubeVisualWidth = cubeVisualHeight;
            cubeVisualDepth = cubeVisualHeight;
            cubeVisualCenterY = 0f;
            cubeVisualCenterX = 0f;
            cubeVisualCenterZ = 0f;
            cubeVisualScale = 1f;
            if (!fitCubeToPrefabBounds || cubePrefab == null) return;

            var sample = Instantiate(cubePrefab);
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.SetActive(true);
            var foundRenderer = false;
            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var minZ = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;
            var maxZ = float.NegativeInfinity;
            foreach (var renderer in sample.GetComponentsInChildren<Renderer>(true))
            {
                var bounds = renderer.bounds;
                var corners = new[]
                {
                    new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                    new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                    new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                    new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                    new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
                };
                foreach (var corner in corners)
                {
                    var local = sample.transform.InverseTransformPoint(corner);
                    minX = Mathf.Min(minX, local.x);
                    minY = Mathf.Min(minY, local.y);
                    minZ = Mathf.Min(minZ, local.z);
                    maxX = Mathf.Max(maxX, local.x);
                    maxY = Mathf.Max(maxY, local.y);
                    maxZ = Mathf.Max(maxZ, local.z);
                }
                foundRenderer = true;
            }

            sample.SetActive(false);
            Destroy(sample);
            if (!foundRenderer || float.IsInfinity(minY) || maxY - minY < 0.001f) return;
            cubeVisualWidth = Mathf.Max(maxX - minX, 0.01f);
            cubeVisualHeight = maxY - minY;
            cubeVisualDepth = Mathf.Max(maxZ - minZ, 0.01f);
            cubeVisualCenterX = (minX + maxX) * 0.5f;
            cubeVisualCenterY = (minY + maxY) * 0.5f;
            cubeVisualCenterZ = (minZ + maxZ) * 0.5f;
            if (cellSize > 0.001f)
            {
                cubeVisualScale = Mathf.Min(cellSize * gridCubeFill / cubeVisualWidth, cellSize * gridCubeFill / cubeVisualDepth);
                cubeVisualScale = Mathf.Clamp(cubeVisualScale, 0.5f, 6f);
                cubeVisualWidth *= cubeVisualScale;
                cubeVisualHeight *= cubeVisualScale;
                cubeVisualDepth *= cubeVisualScale;
                cubeVisualCenterX *= cubeVisualScale;
                cubeVisualCenterY *= cubeVisualScale;
                cubeVisualCenterZ *= cubeVisualScale;
            }
            cubeSize = cubeVisualHeight;
        }

        private Vector3 GetGridSurfacePosition(int x, int z)
        {
            if (gridAnchors.TryGetValue(new Vector2Int(x, z), out var anchor)) return anchor.surfaceCenter;
            return gridRoot.TransformPoint(new Vector3((x - 1.5f) * cellSize, gridBaseHeight, (z - 1.5f) * cellSize));
        }

        private Vector2Int FindNearestGridColumn(Vector3 world, out float bestDistance)
        {
            var local = gridRoot.InverseTransformPoint(world);
            var nearest = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(local.x / Mathf.Max(cellSize, 0.001f) + 1.5f), 0, 3),
                Mathf.Clamp(Mathf.RoundToInt(local.z / Mathf.Max(cellSize, 0.001f) + 1.5f), 0, 3));
            bestDistance = float.PositiveInfinity;
            foreach (var pair in gridAnchors)
            {
                var delta = Vector3.ProjectOnPlane(world - pair.Value.surfaceCenter, gridRoot.up);
                var distance = delta.sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; nearest = pair.Key; }
            }
            return nearest;
        }

        private int GetGroundCellIndex(string name)
        {
            if (name == groundCellNamePrefix) return 0;
            var open = name.LastIndexOf('(');
            var close = name.LastIndexOf(')');
            return open >= 0 && close > open && int.TryParse(name.Substring(open + 1, close - open - 1), out var index) ? index : -1;
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { bounds = default; return false; }
            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return true;
        }

        private static Material CreateUnlitMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.8f);
            }
            return material;
        }

        private Material GetProjectionMaterial(Renderer renderer)
        {
            if (renderer == null) return null;
            if (projectionMaterials.TryGetValue(renderer, out var material) && material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader == null) return renderer.material;
            material = new Material(shader) { name = "ProjectionTile_RuntimeUnlit" };
            renderer.material = material;
            projectionMaterials[renderer] = material;
            return material;
        }

        private static void ConfigureSurfaceVisibility(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                foreach (var material in materials)
                {
                    if (material == null || !material.HasProperty("_EmissionColor")) continue;
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(0.012f, 0.075f, 0.09f, 1f));
                }
                renderer.materials = materials;
            }
        }

        private GameObject CreateFallbackCubePrefab()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = "PuzzleCube_RuntimeFallback"; cube.transform.localScale = Vector3.one * cubeSize;
            var renderer = cube.GetComponent<Renderer>();
            if (cubeMaterial != null)
            {
                renderer.sharedMaterial = cubeMaterial;
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                if (shader != null) renderer.sharedMaterial = new Material(shader) { color = new Color(1f, 0.46f, 0.05f) };
            }
            cube.SetActive(false); return cube;
        }
    }
}
