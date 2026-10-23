using System.Collections.Generic;
using TMPro;
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

        [Tooltip("Pre-placed Image sized slightly larger than the button (placed behind it in the hierarchy). " +
                 "Fades in as amber on hover to create a visible outer-glow effect in the headset. " +
                 "Assign via StructureBuildTitleSceneInstaller; must be a sibling of this object's Transform.")]
        public Graphic glowImage;

        [Tooltip("TMP text label overlaid on the button (e.g. START, EXIT in Cinzel). " +
                 "The engraved silver frame keeps a dark navy interior in every state, so the label " +
                 "stays light: ice-white at rest, warm gold-white on hover/press.")]
        public TextMeshProUGUI labelText;

        // 按钮底图是深藏青内衬的雕花银框，各状态文字都保持浅色：常态冰白，悬停/按下暖金白。
        public static readonly Color LabelNormalColor = new Color(0.87f, 0.94f, 1.00f, 1f); // ice white
        public static readonly Color LabelHoverColor  = new Color(1.00f, 0.91f, 0.70f, 1f); // warm gold-white
        // 按下瞬间整块提亮到接近原图亮度（常态略压暗，所以这里能看出闪亮）
        private static readonly Color PressFlashColor = new Color(1.00f, 0.96f, 0.86f, 1f);

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
            if (glowImage != null) glowImage.color = Color.clear;
            if (labelText != null) labelText.color = LabelNormalColor;
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

            var pressT = pressed ? Mathf.Clamp01((Time.unscaledTime - (pressedUntil - 0.18f)) / 0.18f) : 1f;
            ApplyVisual(emphasis, pressed, pressT);

            wasHovered = hovered;
        }

        /// <summary>
        /// Applies the visual state. emphasis: 0 = idle, 1 = hovered. pressT: 0 at the moment of the
        /// press, 1 when the flash has settled. Also used by editor screenshot tools so captures match runtime.
        /// </summary>
        public void ApplyVisual(float emphasisValue, bool pressed, float pressT)
        {
            if (baseLocalScale == Vector3.zero) baseLocalScale = transform.localScale;

            // Colour: press flash → near full brightness, hover → highlighted (+25% brightness), idle → normal.
            if (graphic != null)
            {
                var brightHighlight = new Color(
                    Mathf.Min(highlightedColor.r * 1.25f, 1f),
                    Mathf.Min(highlightedColor.g * 1.25f, 1f),
                    Mathf.Min(highlightedColor.b * 1.25f, 1f),
                    highlightedColor.a);
                var settled = Color.Lerp(normalColor, brightHighlight, emphasisValue);
                graphic.color = pressed ? Color.Lerp(PressFlashColor, settled, pressT) : settled;
            }

            // Outer glow image: soft amber halo sprite, fades in on hover, peaks on press.
            if (glowImage != null)
                glowImage.color = new Color(focusColor.r, focusColor.g, focusColor.b,
                    Mathf.Clamp01(emphasisValue * 0.55f + (pressed ? (1f - pressT) * 0.35f : 0f)));

            if (labelText != null)
                labelText.color = Color.Lerp(LabelNormalColor, LabelHoverColor, pressed ? 1f : emphasisValue);

            // Focus underline — amber, brightened during hover.
            if (focusGraphic != null)
            {
                var glowAlpha = emphasisValue * (pressed ? 1.6f : 1f);
                focusGraphic.color = new Color(focusColor.r, focusColor.g, focusColor.b, Mathf.Min(glowAlpha, 1f));
            }

            // Focus underline scale sweep.
            if (focusLine != null)
                focusLine.localScale = new Vector3(Mathf.Lerp(0.16f, 1f, emphasisValue), 1f, 1f);

            // Scale feedback: hover grows, press sinks back.
            float targetScale = pressed
                ? Mathf.Lerp(pressScale, hoverScale, Mathf.Clamp01(pressT * 3f))
                : Mathf.Lerp(1f, hoverScale, emphasisValue);
            transform.localScale = baseLocalScale * targetScale;
        }

        private void OnDisable()
        {
            pointers.Clear();
            emphasis = 0f;
            wasHovered = false;
            if (graphic != null) graphic.color = normalColor;
            if (focusGraphic != null) focusGraphic.color = Color.clear;
            if (glowImage != null) glowImage.color = Color.clear;
            if (labelText != null) labelText.color = LabelNormalColor;
            transform.localScale = baseLocalScale;
        }
    }
}
