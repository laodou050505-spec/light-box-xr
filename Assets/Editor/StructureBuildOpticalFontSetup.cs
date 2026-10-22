using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace StructureBuild.Editor
{
    /// <summary>
    /// 为 LUMEN BOX EN 的 Optical Archive 风格字体组合生成 TMP SDF 资产。
    /// 调用 StructureBuild.Editor.StructureBuildOpticalFontSetup.GenerateOpticalSDF
    ///
    /// 字体组合（SIL OFL）：
    ///   标题：Cinzel-Regular / Cinzel-SemiBold（精致古典衬线体）
    ///   正文：Jost-Regular / Jost-Medium（清晰几何人文无衬线）
    ///
    /// 生成的 .asset 文件存放在 Assets/UI/Fonts/ 目录下。
    /// 生成后手动替换 StructureBuildLatinSDF.asset 的引用，或用新 SDF 配置面板。
    /// </summary>
    public static class StructureBuildOpticalFontSetup
    {
        private const string FontsDir = "Assets/UI/Fonts";
        private const string CinzelRegularPath = "Assets/UI/Fonts/Cinzel-Regular.ttf";
        private const string CinzelSemiBoldPath = "Assets/UI/Fonts/Cinzel-SemiBold.ttf";
        private const string JostRegularPath = "Assets/UI/Fonts/Jost-Regular.ttf";
        private const string JostMediumPath = "Assets/UI/Fonts/Jost-Medium.ttf";

        // 游戏用字符集：ASCII + 中间点 · + 常见符号
        private const string GameCharacterSet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 " +
            "!?.,;:'-/()[]·×→±°%#@&*<>\"'";

        [MenuItem("Structure Build/Generate Optical SDF Fonts")]
        public static void GenerateOpticalSDF()
        {
            GenerateSDF(CinzelRegularPath, "CinzelRegularSDF", 2048, 32);
            GenerateSDF(CinzelSemiBoldPath, "CinzelSemiBoldSDF", 2048, 32);
            GenerateSDF(JostRegularPath, "JostRegularSDF", 2048, 28);
            GenerateSDF(JostMediumPath, "JostMediumSDF", 2048, 28);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LumenBox-EN] Optical SDF font assets generated in Assets/UI/Fonts/. " +
                      "Assign CinzelRegularSDF to title labels, JostRegularSDF to body labels.");
        }

        private static void GenerateSDF(string fontPath, string assetName, int atlasSize, int samplingPointSize)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError($"[LumenBox-EN] Font not found at {fontPath}. Run menu again after importing fonts.");
                return;
            }

            var outputPath = $"{FontsDir}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
            if (existing != null)
            {
                Debug.Log($"[LumenBox-EN] {assetName} already exists, skipping. Delete it manually to regenerate.");
                return;
            }

            // TMP_FontAsset.CreateFontAsset() is the public API for generating SDF font assets.
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize,
                atlasSize,
                atlasSize,
                GlyphRenderMode.SDFAA,
                atlasSize,
                atlasSize
            );

            if (fontAsset == null)
            {
                Debug.LogError($"[LumenBox-EN] Failed to create SDF for {fontPath}.");
                return;
            }

            fontAsset.name = assetName;

            // 填充游戏字符集
            fontAsset.TryAddCharacters(GameCharacterSet, out _);

            AssetDatabase.CreateAsset(fontAsset, outputPath);
            Debug.Log($"[LumenBox-EN] Generated {outputPath}");
        }
    }
}
