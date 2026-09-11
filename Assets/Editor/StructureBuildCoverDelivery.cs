using UnityEditor;
using UnityEngine;

namespace StructureBuild.Editor
{
    public static class StructureBuildCoverDelivery
    {
        public static void PreparePanelsAndBuildDesktop()
        {
            StructureBuildOpticalPanels.ApplyAndSave();
            BuildCommands.ValidateXrWorldSpacePackage();
            BuildCommands.BuildDesktop();
            Debug.Log("STRUCTURE_OPTICAL_DESKTOP_BUILD_OK");
        }

        public static void PrepareAndBuildAndroid()
        {
            StructureBuildTitleSceneInstaller.CreateOrRefreshTitleScene();
            BuildCommands.EnablePicoXRForAndroid();
            BuildCommands.ValidateXrWorldSpacePackage();
            BuildCommands.ValidateCampaignAndScene();
            BuildCommands.BuildAndroidAsciiPath();
            Debug.Log("STRUCTURE_COVER_ANDROID_BUILD_OK");
        }

        public static void PrepareAndBuildDesktop()
        {
            StructureBuildTitleSceneInstaller.CreateOrRefreshTitleScene();
            BuildCommands.EnablePicoXRForAndroid();
            BuildCommands.ValidateXrWorldSpacePackage();
            BuildCommands.ValidateCampaignAndScene();
            BuildCommands.BuildDesktop();
            Debug.Log("STRUCTURE_COVER_DESKTOP_BUILD_OK");
        }
    }
}
