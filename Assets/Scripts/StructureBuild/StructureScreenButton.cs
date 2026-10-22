using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StructureBuild
{
    public sealed class StructureScreenButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public StructureHUDController hud;
        public StructureUIButtonAction action;
        public Image background;
        public Graphic focusGraphic;
        public Color normalColor = Color.white;
        public Color hoverColor = new Color(0.78f, 1f, 1f, 1f);

        [Header("Scale Feedback")]
        public float hoverScale = 1.05f;
        public float pressScale = 0.97f;

        private bool hovered;
        private float pressedUntil;
        private float emphasis;
        private Vector3 baseLocalScale;

        private void Awake()
        {
            if (background == null) background = GetComponent<Image>();
            if (background != null) background.color = normalColor;
            baseLocalScale = transform.localScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            hud?.audio?.PlayUiClick();
            hud?.Execute(action);
            pressedUntil = Time.unscaledTime + 0.18f;
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            hud?.audio?.PlayUiHover();
        }
        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        private void Update()
        {
            var pressed = Time.unscaledTime < pressedUntil;
            emphasis = Mathf.Lerp(emphasis, hovered || pressed ? 1 : 0,
                1 - Mathf.Exp(-16 * Time.unscaledDeltaTime));

            // 颜色：按下琥珀闪 → 悬停青色 → 常态白
            if (pressed)
                background.color = Color.Lerp(new Color(1f, 0.88f, 0.63f, 1f), hoverColor,
                    Mathf.Clamp01((Time.unscaledTime - (pressedUntil - 0.18f)) / 0.18f));
            else if (background != null)
                background.color = Color.Lerp(normalColor, hoverColor, emphasis);

            // 琥珀焦点光晕（更明显）
            if (focusGraphic != null)
                focusGraphic.color = new Color(0.94f, 0.82f, 0.61f, emphasis * (pressed ? 1.5f : 1f));

            // 缩放反馈
            float targetScale;
            if (pressed)
                targetScale = Mathf.Lerp(pressScale, hoverScale,
                    Mathf.Clamp01((Time.unscaledTime - (pressedUntil - 0.18f)) / 0.06f));
            else
                targetScale = Mathf.Lerp(1f, hoverScale, emphasis);
            transform.localScale = baseLocalScale * targetScale;
        }

        private void OnDisable()
        {
            hovered = false; emphasis = 0; pressedUntil = 0;
            if (background != null) background.color = normalColor;
            if (focusGraphic != null) focusGraphic.color = Color.clear;
            transform.localScale = baseLocalScale;
        }
    }
}
