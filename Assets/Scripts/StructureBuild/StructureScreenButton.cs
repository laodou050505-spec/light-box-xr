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
        // Keep generated artwork at full strength; the hover state uses only a
        // light cyan tint so the texture is never replaced by a dark fill.
        public Color normalColor = Color.white;
        public Color hoverColor = new Color(0.78f, 1f, 1f, 1f);

        private void Awake()
        {
            if (background == null) background = GetComponent<Image>();
            if (background != null) background.color = normalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            hud?.audio?.PlayUiClick();
            hud?.Execute(action);
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null) background.color = hoverColor;
            hud?.audio?.PlayUiHover();
        }
        public void OnPointerExit(PointerEventData eventData) { if (background != null) background.color = normalColor; }
    }
}
