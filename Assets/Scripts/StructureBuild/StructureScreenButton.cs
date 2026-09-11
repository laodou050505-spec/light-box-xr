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
        private bool hovered;
        private float pressedUntil;
        private float emphasis;

        private void Awake()
        {
            if (background == null) background = GetComponent<Image>();
            if (background != null) background.color = normalColor;
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
            emphasis = Mathf.Lerp(emphasis, hovered || Time.unscaledTime < pressedUntil ? 1 : 0,
                1 - Mathf.Exp(-16 * Time.unscaledDeltaTime));
            if (background != null) background.color = Color.Lerp(normalColor, hoverColor, emphasis);
            if (focusGraphic != null) focusGraphic.color = new Color(0.94f, 0.82f, 0.61f, emphasis);
        }
        private void OnDisable()
        {
            hovered = false; emphasis = 0; pressedUntil = 0;
            if (background != null) background.color = normalColor;
            if (focusGraphic != null) focusGraphic.color = Color.clear;
        }
    }
}
