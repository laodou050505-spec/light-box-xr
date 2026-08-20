using UnityEngine;
using UnityEngine.UI;

namespace StructureBuild
{
    public sealed class StructureUIButton : MonoBehaviour
    {
        public StructureHUDController hud;
        public StructureUIButtonAction action;
        public Renderer background;
        public Graphic backgroundGraphic;
        public Color normalColor = Color.white;
        public Color highlightedColor = new Color(0.78f, 1f, 1f, 1f);
        private bool highlighted;

        private void Awake()
        {
            if (background == null) background = GetComponent<Renderer>();
            if (backgroundGraphic == null) backgroundGraphic = GetComponent<Graphic>();
            ApplyColor(normalColor);
        }

        public void Execute()
        {
            hud?.audio?.PlayUiClick();
            hud?.Execute(action);
            ApplyColor(highlightedColor);
            CancelInvoke(nameof(ResetColor));
            Invoke(nameof(ResetColor), 0.12f);
        }

        public void SetHighlighted(bool value)
        {
            if (highlighted == value) return;
            highlighted = value;
            ApplyColor(value ? highlightedColor : normalColor);
        }

        private void ResetColor() => ApplyColor(highlighted ? highlightedColor : normalColor);

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
