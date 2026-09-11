using System.Collections.Generic;
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
        public RectTransform focusLine;
        public Graphic focusGraphic;
        public Color focusColor = new Color(1f, 0.75f, 0.34f, 1f);

        private readonly HashSet<Object> pointers = new HashSet<Object>();
        private float emphasis;
        private float pressedUntil;
        private bool executed;

        private void Awake()
        {
            if (title == null) title = FindAnyObjectByType<StructureTitleController>();
            if (graphic == null) graphic = GetComponent<Graphic>();
            if (graphic != null) graphic.color = normalColor;
            if (focusGraphic != null) focusGraphic.color = Color.clear;
        }

        public void Execute()
        {
            if (title == null || executed || title.IsTransitioning) return;
            executed = true;
            pressedUntil = Time.unscaledTime + 0.18f;
            if (action == StructureTitleButtonAction.Start) title.BeginGame();
            else title.ExitGame();
        }

        public void SetHighlighted(bool value) => SetHighlighted(this, value);

        // Mouse, keyboard and both hands can independently focus one button.
        public void SetHighlighted(Object pointer, bool value)
        {
            if (pointer == null) return;
            if (value) pointers.Add(pointer);
            else pointers.Remove(pointer);
        }

        private void Update()
        {
            var pressed = Time.unscaledTime < pressedUntil;
            emphasis = Mathf.Lerp(emphasis, pointers.Count > 0 || pressed ? 1f : 0f,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            if (graphic != null)
                graphic.color = pressed ? new Color(1f, 0.88f, 0.63f, 0.28f) : Color.Lerp(normalColor, highlightedColor, emphasis);
            if (focusGraphic != null)
                focusGraphic.color = new Color(focusColor.r, focusColor.g, focusColor.b, emphasis);
            if (focusLine != null)
                focusLine.localScale = new Vector3(Mathf.Lerp(0.16f, 1f, emphasis), 1f, 1f);
        }

        private void OnDisable()
        {
            pointers.Clear();
            emphasis = 0f;
            if (graphic != null) graphic.color = normalColor;
            if (focusGraphic != null) focusGraphic.color = Color.clear;
        }
    }
}
