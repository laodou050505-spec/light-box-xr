using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 英文版 APK 构建命令（batchmode 专用）
/// 用法：Unity -batchmode -executeMethod BuildCommands.BuildAndroidEN -quit
/// </summary>
public static class BuildCommands
{
    private const string ApkOutputPath = "Builds/EN/LumenBox-EN-PICO.apk";

    [MenuItem("Structure Build/Build Android EN APK")]
    public static void BuildAndroidEN()
    {
        // 确保输出目录存在
        var outputDir = Path.GetDirectoryName(ApkOutputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        // 设置包名（冗余保险）
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.structurebuild.pico.en");
        PlayerSettings.productName = "LUMEN BOX";

        var scenes = new[]
        {
            "Assets/Scenes/StructureBuildTitle.unity",
            "Assets/Scenes/StructureBuild.unity"
        };

        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = ApkOutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[LumenBox-EN] APK 构建成功: {ApkOutputPath} ({summary.totalSize / 1024 / 1024} MB)");
        }
        else
        {
            Debug.LogError($"[LumenBox-EN] APK 构建失败: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
