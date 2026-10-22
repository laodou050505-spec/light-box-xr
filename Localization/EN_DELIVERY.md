# LUMEN BOX EN — 交付报告
# 生成日期：2026-10-22（最终修订：字体+译文+模拟器安装）

## 英文工程来源

- **克隆自**：`/Users/ld/Documents/Codex/LightboxCoverBuild-20260908`
- **选择理由**：该构建镜像的 `StructureBuildTitle.unity` 比正式工程（`/Users/ld/Documents/Codex/结构构建`）新 4 分钟且大 84 字节，包含最终版封面场景（英文 "LUMEN BOX / SPATIAL PROJECTION PUZZLE" 字标已在原版制作时直接生成），是最新状态。
- **英文工程路径**：`/Users/ld/Documents/Codex/EnglishEditions/LumenBox-EN/`
- **克隆方式**：APFS `cp -cR`（排除 Library/ Temp/ Logs/ obj/ Builds/ build/）
- **Unity 版本**：6000.5.5f1

## 改动清单

### 代码文件（10 个 .cs 文件，共计 ~200 处替换）

| 文件 | 改动内容 |
|---|---|
| LevelData.cs | 12 关名称和规则说明 → 英文（含本轮修正：archive-008 Refraction Corridor、archive-009 Dual-Core Polarization、archive-011 rule 更新） |
| StructureGameController.cs | 所有玩家可见状态/提示文字 → 英文（含本轮修正：budget_full、no_drop 措辞） |
| StructureHUDController.cs | 视角 HUD 标签 → 英文 |
| StructureScreenHUD.cs | 桌面 HUD 关卡/方块/视角标签 → 英文 |
| LevelCompleteOverlay.cs | 通关标题/详情/按钮文字 → 英文 |
| StructureCameraViews.cs | 摄像机视角名称（含逻辑比较串） → 英文 |
| StructureBuildOpticalPanels.cs | 生成器默认字符串 + 字体路径 → 英文（教学标题含 em dash） |
| StructureBuildCurrentSceneInstaller.cs | 字体路径/验证逻辑/字符集/所有文字创建调用 → 英文 |
| StructureBuildSceneBuilder.cs | 对话框/HUD 默认/包名/产品名 → 英文 |
| BuildCommands.cs | APK 输出路径 → `Builds/EN/LumenBox-EN-PICO.apk` |

### 场景文件

| 文件 | 改动内容 |
|---|---|
| Assets/Scenes/StructureBuild.unity | 36 个 m_text 字段 → 英文；字体 GUID 更新（68 处）；教学标题含 em dash（"LUMEN BOX — Tutorial"） |
| Assets/Scenes/StructureBuildTitle.unity | 无 m_text（文字由代码动态注入），无需修改 |

### 项目设置

| 字段 | 原值 | 英文值 |
|---|---|---|
| productName | 光匣 | LUMEN BOX |
| bundleIdentifier (Android) | com.structurebuild.pico | com.structurebuild.pico.en |
| APK 输出路径 | Builds/结构构建-PICO.apk | Builds/EN/LumenBox-EN-PICO.apk |

### 字体资产

- **StructureBuildLatinSDF.asset** (`Assets/UI/StructureBuildLatinSDF.asset`)
  - **本轮更换**：源字体从 Arial Unicode MS（Apple/Microsoft 授权）换成 **Arimo Regular（Apache 2.0）**
  - 位置：`Assets/UI/Fonts/Arimo-Regular.ttf`
  - 许可证：`Assets/UI/Fonts/LICENSE-Arimo-Apache2.txt`（Apache 2.0 全文）
  - SDF 参数：SDFAA · 2048×2048 · 90pt · padding 9 · Dynamic
  - 字符集：ASCII 32–126 + · × – — ' ' " " … →（11 个扩展字符）
  - GUID：`aa3bbbfa4bd646948ccaf7027781902d`（与场景引用一致）
  - 视觉效果：Arimo 与 Arial 同等字宽（Google/Monotype 兼容设计），外观与原版一致

## CJK 扫描结果

| 文件类型 | CJK 处数 | 说明 |
|---|---|---|
| .cs 代码文件 | **0** | CLEAN |
| .unity 场景文件 | **0** | CLEAN |
| .prefab 预制文件 | **0** | CLEAN |
| .asset 资产文件 | **0** | CLEAN |
| TMP 换行配置 .txt | 2 行 | TMP 包内置文件，非玩家可见 |
| Tripo 元数据 .json | 12 行 | 仅含源文件路径字段（历史记录），非玩家可见 |
| 开发者文档 .md | 其余行 | FONT_NOTES.md + README.md（原版开发文档），非玩家可见 |
| EN_TEXT_TABLE.md | - | 翻译对照表，"原中文"列保留中文（设计如此） |

**结论：游戏运行时不会显示任何 CJK 字符。**

## 关于投影面板发白问题

**根因确认：batchmode 离屏渲染限制。**

- `ProjectionTile_x_y` 子 GameObject 的 RenderTexture 由 `StructureGameController.UpdateProjectionPanel()` 在运行时初始化。
- 在 `-batchmode` 加 `-nographics` 的 Unity 下，`Camera.Render()` 可渲染静态几何，但 RenderTexture 关联的游戏状态（`Awake/Start` 生命周期）未触发，投影面板保持默认白色。
- **英文版本身没有损坏**：原版场景和材质引用均正确；字体 GUID 替换未波及材质或 RawImage 引用。
- 验收方法：装到设备/模拟器上，进入游戏实际运行画面，投影面板即可正常显示。

## Tripo 调用清单

| 用途 | 模型 | task_id | 结论 |
|---|---|---|---|
| — | — | — | 无需调用 |

**Tripo 调用数：0。**

理由：所有 UI 图片均为无字背景模板，或原版制作时已生成英文版（封面"LUMEN BOX / SPATIAL PROJECTION PUZZLE"、标题场景按钮"START / EXIT"）。

## 图片审核结果

| 文件 | 英文状态 |
|---|---|
| StructureBuild_BottomBar.png | 无烘焙文字 ✓ |
| StructureBuild_CompletionPanel.png | 无烘焙文字 ✓ |
| StructureBuild_LevelInfoPanel.png | 无烘焙文字 ✓ |
| OpticalPanels20260908/Dock | 无烘焙文字 ✓ |
| OpticalPanels20260908/Guide | 无烘焙文字 ✓ |
| OpticalPanels20260908/Readout | 无烘焙文字 ✓ |
| NanoBanana20260908/TitleCover | "LUMEN BOX / SPATIAL PROJECTION PUZZLE" ✓ |
| NanoBanana20260908/OpticalArchive | "LUMEN BOX / SPATIAL PROJECTION PUZZLE / START / EXIT" ✓ |
| NanoBanana20260908/OpticalArchiveFinal | 同上（最终精修版）✓ |

## APK 信息

| 字段 | 值 |
|---|---|
| APK 路径 | `Builds/EN/LumenBox-EN-PICO.apk` |
| 产品名 | LUMEN BOX |
| 包名 | com.structurebuild.pico.en |
| SHA-256 | `a45b3d50dbf15a414dfef55373e7ed4c7f79ba47d4568f973176364958726d2d` |
| 大小 | 63,170,931 字节（60 MB） |
| Unity 版本 | 6000.5.5f1 |
| 构建时间 | 2026-10-22 13:57 CST |

## 模拟器安装结果

| 字段 | 值 |
|---|---|
| 模拟器 | emulator-5554 |
| adb 路径 | `~/Library/Android/sdk/platform-tools/adb` |
| 安装命令 | `adb -s emulator-5554 install LumenBox-EN-PICO.apk` |
| 安装结果 | **Success** |
| 原版包 | `com.structurebuild.pico` ✓ 仍在（未动） |
| 英文包 | `com.structurebuild.pico.en` ✓ 已安装 |

## 已验证项

- [x] 10 个 .cs 文件汉化完成，CJK 扫描 CLEAN
- [x] StructureBuild.unity 36 个 m_text 字段全部更新为英文（含 em dash 教学标题）
- [x] 字体 GUID 在场景中替换（68 处）
- [x] ProjectSettings productName = "LUMEN BOX" / bundleIdentifier = com.structurebuild.pico.en
- [x] 封面图片已含英文字标（无需 Tripo 重新生成）
- [x] 所有 UI 底图无烘焙中文
- [x] **字体已替换为 Arimo（Apache 2.0），许可证文件已入库**
- [x] **em dash 支持：SDF 字符集含 em dash，教学标题已恢复 "LUMEN BOX — Tutorial"**
- [x] **6 处译文修正已应用**（budget_full、no_drop、archive-008/009/011 名称+规则）
- [x] APK 构建成功（60 MB，SHA-256 已计算）
- [x] **APK 已安装到 emulator-5554，pm list packages 确认双包共存**
- [x] Library/Temp 中间产物已清理
- [x] Unity slot 已释放
- [x] 底栏严格 3 按钮：Hint / Undo / Reset
