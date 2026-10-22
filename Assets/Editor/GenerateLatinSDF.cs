// 将 StructureBuildLatinSDF.asset 的源字体换成 Arimo（Apache 2.0）并生成 SDF atlas
// 入口：StructureBuild.Editor.GenerateLatinSDF.Generate
// 字符集：ASCII + em dash, en dash, ×, →, · (U+00B7), 弯引号, 省略号
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace StructureBuild.Editor
{
    public static class GenerateLatinSDF
    {
        private const string ArimoTtfPath = "Assets/UI/Fonts/Arimo-Regular.ttf";
        private const string LatinSdfPath = "Assets/UI/StructureBuildLatinSDF.asset";

        // ASCII 32–126 + specialchars
        private const string CharacterSet =
            " !\"#$%&'()*+,-./0123456789:;<=>?@" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~" +
            "·"  // · middle dot
          + "×"  // × multiplication
          + "–"  // – en dash
          + "—"  // — em dash
          + "‘"  // ' left single quote
          + "’"  // ' right single quote
          + "“"  // " left double quote
          + "”"  // " right double quote
          + "…"  // … ellipsis
          + "→"; // → right arrow

        public static void Generate()
        {
            // 导入 Arimo TTF
            AssetDatabase.ImportAsset(ArimoTtfPath, ImportAssetOptions.ForceSynchronousImport);
            var font = AssetDatabase.LoadAssetAtPath<Font>(ArimoTtfPath);
            if (font == null)
            {
                Debug.LogError($"[GenerateLatinSDF] Arimo TTF not found: {ArimoTtfPath}");
                EditorApplication.Exit(1);
                return;
            }

            // 创建 TMP_FontAsset (Dynamic SDFAA, 2048×2048)
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 2048,
                atlasHeight: 2048,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: false
            );

            if (fontAsset == null)
            {
                Debug.LogError("[GenerateLatinSDF] CreateFontAsset returned null");
                EditorApplication.Exit(1);
                return;
            }

            fontAsset.name = "StructureBuildLatinSDF";

            // 预载字符集（触发 atlas 内部初始化）
            fontAsset.TryAddCharacters(CharacterSet);

            // 拿到内部 atlas texture 和 material
            var atlasTexture = fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0
                ? fontAsset.atlasTextures[0] : null;
            var mat = fontAsset.material;

            // 删除旧 .asset 文件（保留 .meta → 保留 GUID）
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absPath = Path.Combine(projectRoot, LatinSdfPath);
            if (File.Exists(absPath))
            {
                File.Delete(absPath);
                Debug.Log("[GenerateLatinSDF] Deleted old .asset (meta preserved → GUID unchanged).");
            }

            // 保存主资产
            AssetDatabase.CreateAsset(fontAsset, LatinSdfPath);

            // 把 atlas texture 和 material 加为子资产（否则 m_AtlasTextures 序列化为 null）
            if (atlasTexture != null)
            {
                atlasTexture.name = "Arimo - Regular Atlas";
                AssetDatabase.AddObjectToAsset(atlasTexture, LatinSdfPath);
            }
            if (mat != null)
            {
                mat.name = "Arimo - Regular Material";
                AssetDatabase.AddObjectToAsset(mat, LatinSdfPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GenerateLatinSDF] Done. Arimo Regular (Apache 2.0) · 2048x2048 SDFAA Dynamic.");
            EditorApplication.Exit(0);
        }
    }
}
