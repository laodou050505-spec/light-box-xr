# Font Notes — LUMEN BOX EN

## 当前已启用字体

### Arimo-Regular.ttf（当前 SDF 基础）

- 来源：Google Fonts (Apache 2.0)
- 许可证：LICENSE-Arimo-Apache2.txt
- 现状：StructureBuildLatinSDF.asset 和 StructureBuildChineseSDF.asset 均引用此字体的 SDF 图集。

## Optical Archive 风格字体组合（2026-10-22 新增，待在 Unity 中生成 SDF）

### Cinzel-Regular.ttf / Cinzel-SemiBold.ttf（标题精致衬线体）

- 来源：Google Fonts — https://fonts.google.com/specimen/Cinzel
- 作者：The Cinzel Project Authors
- 许可证：SIL OFL 1.1（LICENSE-OFL.txt）
- 适用：面板标题、档案编号（Archive 01）、通关标题文字
- 风格特点：源自罗马古典雕刻铭文风格，大写字母比例优雅，与 Optical Archive 的琥珀玻璃、钢铁框架美学高度吻合

### Jost-Regular.ttf / Jost-Medium.ttf（正文清晰人文无衬线）

- 来源：Google Fonts — https://fonts.google.com/specimen/Jost
- 作者：The Jost Project Authors (indestructible-type)
- 许可证：SIL OFL 1.1（LICENSE-OFL.txt）
- 适用：规则说明、方块计数、状态文字、Controls 面板、底栏按钮标签
- 风格特点：几何人文无衬线，兼具 Futura 的简洁与 Gill Sans 的可读性，远距 VR 可读性优秀

## SDF 生成说明

编辑器脚本：`Assets/Editor/StructureBuildOpticalFontSetup.cs`

在 Unity 中执行菜单 **Structure Build → Generate Optical SDF Fonts**，将在本目录生成：

- `CinzelRegularSDF.asset`（标题用，atlas 2048×2048，采样 32pt）
- `CinzelSemiBoldSDF.asset`（强调标题，atlas 2048×2048，采样 32pt）
- `JostRegularSDF.asset`（正文，atlas 2048×2048，采样 28pt）
- `JostMediumSDF.asset`（中等强调正文，atlas 2048×2048，采样 28pt）

生成后，在各场景中将相关 TMP 文字组件的 Font Asset 字段从 StructureBuildLatinSDF 替换为对应的新 SDF。

## VR 字高要求（规范 C）

以信息板视距 1.0m 为基准：
- 标题（Archive 档案编号）：cap height ≥ 28mm（目标 ≥ d × 0.028）
- 正文（规则、方块计数）：cap height ≥ 17mm（目标 ≥ d × 0.017）
- 次要文字（说明、单位）：cap height ≥ 12mm（目标 ≥ d × 0.012）

底栏按钮标签（视距约 1.2m）：cap height ≥ 20mm（目标 ≥ 1.2 × 0.017 ≈ 20mm）
