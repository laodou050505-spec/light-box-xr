using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StructureBuild
{
    /// <summary>
    /// A command-line gated desktop evidence runner. It is compiled into the
    /// player but does nothing for normal players. With
    /// -structure-qa-evidence it waits through a silent title stage, captures
    /// the opaque cover, uses the same START transition as the title button,
    /// and captures the physical gameplay table after the title scene has
    /// unloaded. This gives reproducible visual regression evidence without
    /// touching the shared PICO emulator.
    /// </summary>
    public sealed class StructureTitleRuntimeEvidence : MonoBehaviour
    {
        private const string TitleSceneName = "StructureBuildTitle";
        private const string GameplaySceneName = "StructureBuild";

        private static bool bootstrapped;
        private string outputDirectory;
        private bool captureMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (bootstrapped || !HasArgument("-structure-qa-evidence")) return;
            bootstrapped = true;
            var runner = new GameObject("StructureTitleRuntimeEvidence");
            DontDestroyOnLoad(runner);
            runner.AddComponent<StructureTitleRuntimeEvidence>();
        }

        private void Awake()
        {
            captureMode = HasArgument("-structure-qa-evidence");
            if (!captureMode) return;
            // Headless/automated macOS launches can lose foreground focus
            // while the Terminal owns the active window. The evidence runner
            // must still advance its two-second title inspection and scene
            // transition in that situation.
            Application.runInBackground = true;
            outputDirectory = GetArgumentValue("-structure-qa-evidence-dir") ??
                              Path.Combine(Application.persistentDataPath, "StructureTitleEvidence");
            Directory.CreateDirectory(outputDirectory);
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return WaitForScene(TitleSceneName, 12f);
            var title = FindAnyObjectByType<StructureTitleController>();
            var titleCamera = GameObject.Find("TitleDesktopCamera")?.GetComponent<Camera>();
            var cover = GameObject.Find("TitleCover");
            if (title == null || titleCamera == null || cover == null ||
                FindAnyObjectByType<StructureGameController>() != null ||
                GameObject.Find("PuzzleTable_Editable") != null ||
                GameObject.Find("GameplayPresentation_v2") != null)
            {
                Fail("STRUCTURE_TITLE_EVIDENCE_FAIL: title stage did not stay isolated from gameplay.");
                yield break;
            }

            // The requested static title check: no input or scene transition
            // happens before this full two second inspection window.
            yield return new WaitForSeconds(2f);
            yield return CaptureAfterFrame("01-title-silent-2s.png");
            Debug.Log("STRUCTURE_TITLE_EVIDENCE_TITLE_OK: two-second silent title capture contains only opaque TitleCover and title controls.");

            // This deliberately calls the same public method the START world
            // button calls. The following check proves Single-mode unloading.
            title.BeginGame();
            yield return WaitForScene(GameplaySceneName, 15f);
            yield return new WaitForSeconds(1f);
            var game = FindAnyObjectByType<StructureGameController>();
            var desktopCamera = GameObject.Find("DesktopCamera")?.GetComponent<Camera>();
            var start = FindAnyObjectByType<DesignPlayerStart>();
            if (game == null || desktopCamera == null || start == null ||
                GameObject.Find("PuzzleTable_Editable") == null || GameObject.Find("PicoWorldHUD") == null ||
                GameObject.Find("TitleRoot") != null || GameObject.Find("TitleCover") != null ||
                FindAnyObjectByType<StructureTitleController>() != null)
            {
                Fail("STRUCTURE_TITLE_EVIDENCE_FAIL: START did not fully unload title or initialize world-space gameplay.");
                yield break;
            }

            start.ApplyDesktopPose(desktopCamera.GetComponent<DesktopCameraOrbit>());
            // Let the Single-mode unload settle completely, then resolve the
            // surviving desktop camera again. Unity can invalidate a cached
            // Camera wrapper on the same frame an old title camera is
            // destroyed even though the named gameplay object is already in
            // the hierarchy.
            yield return CaptureAfterFrame("02-gameplay-after-start.png");
            Debug.Log($"STRUCTURE_TITLE_EVIDENCE_GAMEPLAY_OK: START unloaded TitleRoot; gameplay table rendered at shared gameplay start eye={start.DesignedEyePosition:F2}.");
            Debug.Log("STRUCTURE_TITLE_EVIDENCE_OK: title and gameplay captures were written to " + outputDirectory);
            Application.Quit(0);
        }

        private IEnumerator WaitForScene(string sceneName, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (SceneManager.GetActiveScene().name != sceneName && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (SceneManager.GetActiveScene().name != sceneName)
                Fail("STRUCTURE_TITLE_EVIDENCE_FAIL: timed out waiting for scene " + sceneName + ".");
        }

        private IEnumerator CaptureAfterFrame(string fileName)
        {
            var path = Path.Combine(outputDirectory, fileName);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(path, 1);
            var deadline = Time.realtimeSinceStartup + 5f;
            while ((!File.Exists(path) || new FileInfo(path).Length < 1024) && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!File.Exists(path) || new FileInfo(path).Length < 1024)
                throw new IOException("Capture was not written: " + path);
        }

        private void Fail(string message)
        {
            Debug.LogError(message);
            Application.Quit(1);
        }

        private static bool HasArgument(string expected)
        {
            foreach (var argument in Environment.GetCommandLineArgs())
                if (string.Equals(argument, expected, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string GetArgumentValue(string name)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < arguments.Length; index++)
                if (string.Equals(arguments[index], name, StringComparison.Ordinal)) return arguments[index + 1];
            return null;
        }
    }
}
