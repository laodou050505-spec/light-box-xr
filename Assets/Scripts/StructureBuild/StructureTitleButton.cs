using UnityEngine;
using UnityEngine.UI;

namespace StructureBuild
{
    public enum StructureTitleButtonAction
    {
        Start,
        Exit,
    }

    /// <summary>Physical title-scene button, usable by mouse or a PICO controller ray.</summary>
    public sealed class StructureTitleButton : MonoBehaviour
    {
        public StructureTitleController title;
        public StructureTitleButtonAction action;
        public Graphic graphic;
        public Color normalColor = new Color(0.035f, 0.24f, 0.30f, 1f);
        public Color highlightedColor = new Color(0.20f, 0.92f, 0.88f, 1f);

        private bool highlighted;

        private void Awake()
        {
            if (title == null) title = FindAnyObjectByType<StructureTitleController>();
            if (graphic == null) graphic = GetComponent<Graphic>();
            SetHighlighted(false);
        }

        public void Execute()
        {
            if (title == null) return;
            if (action == StructureTitleButtonAction.Start) title.BeginGame();
            else title.ExitGame();
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
            if (graphic != null) graphic.color = value ? highlightedColor : normalColor;
        }
    }
}
