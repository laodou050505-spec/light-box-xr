#if UNITY_EDITOR
using UnityEditor;

namespace StructureBuild.Editor
{
    // Keep the existing menu entry on the current optical panel system.
    // Reapplying layout must not restore the retired six-button cyan HUD.
    public static class StructureBuildUiLayoutApplier
    {
        [MenuItem("Structure Build/Apply Current PICO UI Layout")]
        public static void ApplyCurrentPicoUiLayout() => StructureBuildOpticalPanels.ApplyAndSave();
    }
}
#endif
