using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StructureBuild
{
    public sealed class StructureUIButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public StructureHUDController hud;
        public StructureUIButtonAction action;
        public Renderer background;
        public Graphic backgroundGraphic;
        public Graphic focusGraphic;
        public Color normalColor = Color.white;
        public Color highlightedColor = new Color(0.78f, 1f, 1f, 1f);

        [Header("Scale Feedback")]
        [Tooltip("Scale multiplier while hovered (+5%).")]
        public float hoverScale = 1.05f;
        [Tooltip("Scale multiplier at peak press (-3%).")]
        public float pressScale = 0.97f;

        private bool highlighted;
        private float pressedUntil;
        private float emphasis;
        private int executedFrame = -1;
        private Canvas ownerCanvas;
        private Collider[] buttonColliders;
        private Vector3 baseLocalScale;

        public bool IsInteractable => isActiveAndEnabled && gameObject.activeInHierarchy &&
            (ownerCanvas == null || ownerCanvas.isActiveAndEnabled);

        private void Awake()
        {
            if (background == null) background = GetComponent<Renderer>();
            if (backgroundGraphic == null) backgroundGraphic = GetComponent<Graphic>();
            ownerCanvas = GetComponentInParent<Canvas>();
            buttonColliders = GetComponents<Collider>();
            baseLocalScale = transform.localScale;
            ApplyColor(normalColor);
            RefreshColliders();
        }

        public void Execute()
        {
            // The mouse UI module and tracked controller path may report the
            // same input frame. Each physical button performs its action once.
            if (!IsInteractable || executedFrame == Time.frameCount) return;
            executedFrame = Time.frameCount;
            hud?.audio?.PlayUiClick();
            hud?.Execute(action);
            pressedUntil = Time.unscaledTime + 0.18f;
        }

        public void SetHighlighted(bool value)
        {
            value &= IsInteractable;
            if (highlighted == value) return;
            highlighted = value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // XR executes on trigger-down in WorldSpaceInteraction. Ignore
            // its UI-module trigger-up event, which arrives in a later frame.
            if (IsTrackedPointer(eventData)) return;
            if (eventData.button == PointerEventData.InputButton.Left) Execute();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable || IsTrackedPointer(eventData)) return;
            SetHighlighted(true);
            hud?.audio?.PlayUiHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsTrackedPointer(eventData)) SetHighlighted(false);
        }

        private static bool IsTrackedPointer(PointerEventData eventData) =>
            eventData is ExtendedPointerEventData extended && extended.pointerType == UIPointerType.Tracked;

        private void Update()
        {
            if (!IsInteractable) highlighted = false;
            var pressed = Time.unscaledTime < pressedUntil;
            emphasis = Mathf.Lerp(emphasis, highlighted || pressed ? 1 : 0,
                1 - Mathf.Exp(-16 * Time.unscaledDeltaTime));

            // 颜色：按下闪亮琥珀 → 悬停青色 → 常态白
            if (pressed)
                ApplyColor(Color.Lerp(new Color(1f, 0.88f, 0.63f, 1f), highlightedColor,
                    Mathf.Clamp01((Time.unscaledTime - (pressedUntil - 0.18f)) / 0.18f)));
            else
                ApplyColor(Color.Lerp(normalColor, highlightedColor, emphasis));

            // 琥珀焦点光晕
            if (focusGraphic != null)
                focusGraphic.color = new Color(0.94f, 0.82f, 0.61f, emphasis * (pressed ? 1.5f : 1f));

            // 缩放反馈：悬停 +5%，按下 -3%
            float targetScale;
            if (pressed)
                targetScale = Mathf.Lerp(pressScale, hoverScale,
                    Mathf.Clamp01((Time.unscaledTime - (pressedUntil - 0.18f)) / 0.06f));
            else
                targetScale = Mathf.Lerp(1f, hoverScale, emphasis);
            transform.localScale = baseLocalScale * targetScale;
        }

        private void LateUpdate() => RefreshColliders();

        private void RefreshColliders()
        {
            if (buttonColliders == null) return;
            // Disabling a Canvas only hides its graphics; it does not disable
            // 3D hit volumes. Hidden tutorial/HUD buttons must not catch rays.
            foreach (var collider in buttonColliders)
                if (collider != null) collider.enabled = IsInteractable;
        }

        private void OnDisable()
        {
            highlighted = false; emphasis = 0; pressedUntil = 0;
            RefreshColliders();
            ApplyColor(normalColor);
            if (focusGraphic != null) focusGraphic.color = Color.clear;
            transform.localScale = baseLocalScale;
        }

        private void ApplyColor(Color color)
        {
            if (backgroundGraphic != null) backgroundGraphic.color = color;
            if (background != null)
            {
                var material = background.material;
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            }
        }
    }
}
