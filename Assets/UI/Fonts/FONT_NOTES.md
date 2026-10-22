# Font Notes — LUMEN BOX EN

## StructureBuildLatinSDF.asset

- Source font: Arial Unicode MS (Monotype，macOS 系统字体：/System/Library/Fonts/Supplemental/Arial Unicode.ttf)
- Atlas: 从 StructureBuildChineseSDF.asset 复制（同一源字体 Arial Unicode MS，相同 SDF 参数）
- 包含字符：A-Z, a-z, 0-9, 空格，以及常用标点，含 · (U+00B7)
- Atlas 中缺少：! ? ; & — （这些字符未出现在英文游戏文本中）
- 说明：正式发布前，请在有 Unity 槽位时，用 Unity TMP Generator 从 ArialUnicode.ttf
  重新生成 StructureBuildLatinSDF.asset（仅含 ASCII + 游戏实际用字），以获得精简的 Latin 图集。

## Arial Unicode MS 字体说明

Arial Unicode MS 是 Microsoft 授权的 Unicode 多语言字体，随 macOS 附带（系统组件）。
该字体不可商用分发。游戏 APK 中不包含该字体文件；仅通过 Unity TMP Generator
提取所需字形生成 SDF 图集（SDF atlas 本身不受字体分发许可约束）。

## 未来优化

在 Unity 槽位可用时：
1. 将 ArialUnicode.ttf 导入 Unity
2. 用 Assets/Editor/StructureBuildFontGenerator.cs (待创建) 生成 StructureBuildLatinSDF.asset
3. 字符集：ASCII + · % × / — 等游戏用符号
