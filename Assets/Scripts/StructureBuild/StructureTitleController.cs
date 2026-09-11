using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace StructureBuild
{
    /// <summary>
    /// Owns the isolated boot stage. Loading the gameplay scene in Single mode
    /// destroys this entire TitleRoot, so no title renderer, collider, audio or
    /// input component can remain over the world-space puzzle.
    /// </summary>
    public sealed class StructureTitleController : MonoBehaviour
    {
        public const string GameplaySceneName = "StructureBuild";

        public DesignPlayerStart designStart;
        public DesktopCameraOrbit desktopOrbit;
        public string gameplaySceneName = GameplaySceneName;

        private bool loading;
        private StructureTitleButton hovered;
        public bool IsTransitioning => loading;

        private void Awake()
        {
            if (designStart == null) designStart = FindAnyObjectByType<DesignPlayerStart>();
            if (desktopOrbit == null) desktopOrbit = FindAnyObjectByType<DesktopCameraOrbit>();
        }

        private void Start()
        {
            // Desktop and PICO use this stage's authored starting landmark.
            designStart?.ApplyDesktopPose(desktopOrbit);
        }

        private void Update()
        {
            // Desktop parity for the physical title button. PICO uses
            // TitleWorldInteraction on each tracked controller instead.
            if (loading || Mouse.current == null) return;
            var camera = Camera.main;
            if (camera == null) return;
            var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var button = Physics.Raycast(ray, out var hit, 20f)
                ? hit.collider.GetComponentInParent<StructureTitleButton>() : null;
            if (hovered != button)
            {
                if (hovered != null) hovered.SetHighlighted(this, false);
                hovered = button;
                if (hovered != null) hovered.SetHighlighted(this, true);
            }
            if (Mouse.current.leftButton.wasPressedThisFrame) button?.Execute();
        }

        private void OnDisable()
        {
            if (hovered != null) hovered.SetHighlighted(this, false);
        }

        public void BeginGame()
        {
            if (loading) return;
            loading = true;
            StartCoroutine(Transition(false));
        }

        private IEnumerator Transition(bool exit)
        {
            // Let the button's press flash register before removing the scene.
            yield return new WaitForSecondsRealtime(0.18f);
            if (exit)
            {
                Debug.Log("STRUCTURE_TITLE_EXIT: exit requested from isolated title stage.");
                Application.Quit();
                yield break;
            }
            Debug.Log("STRUCTURE_TITLE_START: isolated TitleRoot is unloading before the physical gameplay table loads.");
            SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
        }

        public void ExitGame()
        {
            if (loading) return;
            loading = true;
            StartCoroutine(Transition(true));
        }
    }
}
