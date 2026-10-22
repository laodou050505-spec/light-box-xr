# LUMEN BOX EN — 交付报告
# 生成日期：2026-10-22

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
| LevelData.cs | 12 关名称和规则说明 → 英文 |
| StructureGameController.cs | 所有玩家可见状态/提示文字 → 英文（13 处） |
| StructureHUDController.cs | 视角 HUD 标签 → 英文 |
| StructureScreenHUD.cs | 桌面 HUD 关卡/方块/视角标签 → 英文 |
| LevelCompleteOverlay.cs | 通关标题/详情/按钮文字 → 英文 |
| StructureCameraViews.cs | 摄像机视角名称（含逻辑比较串） → 英文 |
| StructureBuildOpticalPanels.cs | 生成器默认字符串 + 字体路径 → 英文 |
| StructureBuildCurrentSceneInstaller.cs | 字体路径/验证逻辑/字符集/所有文字创建调用 → 英文 |
| StructureBuildSceneBuilder.cs | 对话框/HUD 默认/包名/产品名 → 英文 |
| BuildCommands.cs | APK 输出路径 → `Builds/EN/LumenBox-EN-PICO.apk` |

### 场景文件

| 文件 | 改动内容 |
|---|---|
| Assets/Scenes/StructureBuild.unity | 36 个 m_text 字段 → 英文；字体 GUID 更新（68 处） |
| Assets/Scenes/StructureBuildTitle.unity | 无 m_text（文字由代码动态注入），无需修改 |

### 项目设置

| 字段 | 原值 | 英文值 |
|---|---|---|
| productName | 光匣 | LUMEN BOX |
| bundleIdentifier (Android) | com.structurebuild.pico | com.structurebuild.pico.en |
| APK 输出路径 | Builds/结构构建-PICO.apk | Builds/EN/LumenBox-EN-PICO.apk |

### 字体资产

- **StructureBuildLatinSDF.asset** (`Assets/UI/StructureBuildLatinSDF.asset`)
  - 来源字体：Arial Unicode MS（macOS 系统字体 `/System/Library/Fonts/Supplemental/Arial Unicode.ttf`）
  - 策略：从 StructureBuildChineseSDF.asset 复制（同一源字体，已含 A-Z / a-z / 0-9 / · 等所有英文游戏用字）
  - 视觉效果：与原中文版完全一致（相同字体、相同 SDF 参数）
  - 待优化：在 Unity 槽位可用时，用 TMP Generator 重新生成精简 Latin atlas（仅含 ASCII + 游戏实际用字），可减小文件体积

## 字体许可

- Arial Unicode MS 随 macOS 附带，字体文件未打包进 APK
- SDF atlas 图集由引擎从字形光栅化生成，不受字体分发协议约束
- 字体相关说明：`Assets/UI/Fonts/FONT_NOTES.md`

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

## 截图清单

存放目录：`VisualQA/EN-20261022/`

| 截图 | 内容 | 获取方式 |
|---|---|---|
| 01_title_cover_EN.png | 封面 — LUMEN BOX 英文字标 | 手动截图（待填入） |
| 02_main_game_HUD.png | 游戏主 HUD — Archive/Blocks/View 标签 | 待 Unity 槽位后截图 |
| 03_hint_button.png | 底栏 Hint/Undo/Reset 三按钮 | 待截图 |
| 04_completion_card.png | 通关卡 — Archive Complete | 待截图 |
| 05_final_complete.png | 全通 — All Archives Complete | 待截图 |
| 06_tutorial_card.png | 教学卡 — LUMEN BOX - Tutorial | 待截图 |

## APK 信息

| 字段 | 值 |
|---|---|
| APK 路径 | `Builds/EN/LumenBox-EN-PICO.apk` |
| 产品名 | LUMEN BOX |
| 包名 | com.structurebuild.pico.en |
| SHA-256 | （待构建后填入） |
| 大小 | （待构建后填入） |
| Unity 版本 | 6000.5.5f1 |

## 已验证项

- [x] 10 个 .cs 文件汉化完成，CJK 扫描 CLEAN
- [x] StructureBuild.unity 36 个 m_text 字段全部更新为英文
- [x] 字体 GUID 在场景中替换（68 处）
- [x] ProjectSettings productName / bundleIdentifier 已更新
- [x] 封面图片已含英文字标（无需 Tripo 重新生成）
- [x] 所有 UI 底图无烘焙中文
- [x] StructureCameraViews 的"视角名称=逻辑字符串"一致性替换

## 未验证边界

1. **APK 未构建**：Unity 槽位被其他代理占用（QuantumEcho-EN + TombDuel-EN 正在 IL2CPP 编译）。构建完成后需补充 SHA-256 和文件大小。
2. **字体未在头显上验证**：StructureBuildLatinSDF.asset 是复用的 Arial Unicode SDF，所有英文字符在图集中已确认存在，但未在实体 PICO 头显上测试。
3. **Unity Editor 未启动**：场景生成器（StructureBuildCurrentSceneInstaller）的验证逻辑已更新为英文，但未运行验证（需 Editor 模式）。
4. **桌面截图限制**：由于没有 Unity Editor 启动，VisualQA 截图仅含封面静态图，游戏画面截图待 Unity 槽位后补充。

## Git 提交历史

| 提交 | 内容 |
|---|---|
| 基线 | APFS 克隆 LightboxCoverBuild-20260908，排除 Library/ 等 |
| 阶段一 | 10 个 .cs 文件翻译、字体资产、ProjectSettings、EN_TEXT_TABLE.md |
| 阶段二 | StructureBuild.unity 场景文字汉化 + 字体 GUID 更新 |
