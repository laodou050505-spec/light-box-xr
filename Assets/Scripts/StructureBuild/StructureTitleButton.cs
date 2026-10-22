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

    /// <summary>
    /// Physical title-scene button (START / EXIT baked in cover image).
    /// Supports mouse and PICO controller ray. Hover: amber glow + scale up 5%.
    /// Press: scale down 3% + bright flash. Commands are executed on trigger-down;
    /// audio and haptic feedback are coordinated by TitleWorldInteraction.
    /// </summary>
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

        [Header("Scale Feedback (Optical Archive)")]
        [Tooltip("Scale multiplier while hovered (e.g. 1.05 = +5%).")]
        public float hoverScale = 1.05f;
        [Tooltip("Scale multiplier at peak press (e.g. 0.97 = -3%).")]
        public float pressScale = 0.97f;

        private readonly HashSet<Object> pointers = new HashSet<Object>();
        private float emphasis;
        private float pressedUntil;
        private bool executed;
        private Vector3 baseLocalScale;
        // Tracks whether this was hovered on the previous frame to detect edge.
        private bool wasHovered;

        private void Awake()
        {
            if (title == null) title = FindAnyObjectByType<StructureTitleController>();
            if (graphic == null) graphic = GetComponent<Graphic>();
            if (graphic != null) graphic.color = normalColor;
            if (focusGraphic != null) focusGraphic.color = Color.clear;
            baseLocalScale = transform.localScale;
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

        /// <summary>True when at least one pointer is hovering this button.</summary>
        public bool IsHovered => pointers.Count > 0;

        private void Update()
        {
            var pressed = Time.unscaledTime < pressedUntil;
            var hovered = pointers.Count > 0;
            // Smooth emphasis spring (14 Hz = ~0.09s settle).
            emphasis = Mathf.Lerp(emphasis, hovered || pressed ? 1f : 0f,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));

            // Colour: press flash → amber, hover → teal, idle → deep blue.
            if (graphic != null)
            {
                if (pressed)
                    graphic.color = Color.Lerp(new Color(1f, 0.88f, 0.63f, 0.35f), normalColor, Mathf.Clamp01((Time.unscaledTime - (pressedUntil - 0.18f)) / 0.18f));
                else
                    graphic.color = Color.Lerp(normalColor, highlightedColor, emphasis);
            }

            // Focus glow — amber, brightened during hover.
            if (focusGraphic != null)
            {
                var glowAlpha = emphasis * (pressed ? 1.6f : 1f);
                focusGraphic.color = new Color(focusColor.r, focusColor.g, focusColor.b, Mathf.Min(glowAlpha, 1f));
            }

            // Focus underline scale sweep.
            if (focusLine != null)
                focusLine.localScale = new Vector3(Mathf.Lerp(0.16f, 1f, emphasis), 1f, 1f);

            // Scale feedback: hover grows, press sinks back.
            float targetScale;
            if (pressed)
                targetScale = Mathf.Lerp(pressScale, hoverScale, Mathf.Clamp01((Time.unscaledTime - (pressedUntil - 0.18f)) / 0.06f));
            else
                targetScale = Mathf.Lerp(1f, hoverScale, emphasis);
            transform.localScale = baseLocalScale * targetScale;

            wasHovered = hovered;
        }

        private void OnDisable()
        {
            pointers.Clear();
            emphasis = 0f;
            wasHovered = false;
            if (graphic != null) graphic.color = normalColor;
            if (focusGraphic != null) focusGraphic.color = Color.clear;
            transform.localScale = baseLocalScale;
        }
    }
}
