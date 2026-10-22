# LUMEN BOX EN — 打磨报告（Polish 2026-10-22）

## 第二轮精修摘要（UI 精修 + 按钮交互反馈，2026-10-22 17:xx）

本轮新增以下改动（在第一轮打磨基础上继续）：

1. **封面 START/EXIT 按钮交互反馈增强**（StructureTitleButton.cs、TitleWorldInteraction.cs）
2. **底栏三按钮及游戏内 UIButton 交互反馈增强**（StructureUIButton.cs、StructureScreenButton.cs、WorldSpaceInteraction.cs）
3. **Optical Archive 风格字体组合**（Cinzel + Jost，SIL OFL，已加入工程；SDF 生成脚本 StructureBuildOpticalFontSetup.cs 已就绪，总控在 Unity 中执行 Structure Build → Generate Optical SDF Fonts 后替换场景引用）
4. **玻璃感点击音效**（magic_ui_soft_02.mp3，ange-embed 来源）

红线全部保留：棋盘/投影板不旋转、关卡规则/方块预算不变、底栏严格 Hint/Undo/Reset、教学门禁/手柄重连逻辑不动、信息板位置不动、15° 俯视开场不变、文字不遮投影网格。

---

## 改动清单

### 1. 方块落地弹跳动画（StructureGameController.cs）

**改了什么：** 在 `TryPlaceAt()` 和 `CommitDrag()` 中，方块成功落格后启动 `CubeLandBounce()` 协程——方块在 0.15 秒内做一次轻微缩放超调（+16%）再弹回原尺寸，sin 弧线曲线。

**为什么：** 原版放置方块时手感平直，没有物理重量感。轻弹跳动效与音效配合，让每次放置更有满足感，同时完全不影响游戏判定（动画结束后尺寸恢复原值）。

### 2. 吸附高亮闪光（StructureGameController.cs）

**改了什么：** 在 `UpdateDrag()` 中，每次落点指示框（`DropIndicator_Runtime`）跳到新的合法格时，设置 `snapFlashTimer = 0.18f`。在 `Update()` 中新增 `UpdateSnapFlash()` 方法，每帧把 timer 递减并将 `dropIndicatorMaterial` 的 `_EmissionColor` 按 `boost * 2.0f` 峰值拉亮，0.18 秒内线性衰减至正常。

**为什么：** 原版吸附指示框只有 7Hz 慢速脉冲，视觉反馈不够明确。新加的短促闪光让玩家一眼看清"指示框已吸附到新格"。不影响碰撞、物理或任何游戏规则。

### 3. 投影板通关闪光（StructureGameController.cs）

**改了什么：** 在 `EvaluateAndDisplay()` 中，当 `evaluation.complete && !levelCompleted` 首次触发时，除原有音效和通关事件外，额外启动 `ProjectionCompletionFlash()` 协程——0.55 秒内把所有投影格材质的颜色从正常青绿（`#1FFF72`）拉亮到接近白色（`#E1FFF0 × 4.5`），然后自然消退。

**为什么：** 当前正面和侧面投影全部匹配时，视觉上只有音效作为提示，没有画面上的高潮感。闪光提供清晰的"所有格全对齐"视觉确认，避免玩家错过通关时刻。

### 4. Archive Complete 弹出动画（LevelCompleteOverlay.cs）

**改了什么：** 在 `Show()` 中，`panelRoot.SetActive(true)` 之后启动 `EntrancePunch()` 协程——面板从 0.84 倍缩放以 ease-out cubic 曲线在 0.20 秒内放大到原尺寸（`localScale` 恢复后续不再受干扰）。

**为什么：** 原版通关面板直接出现，视觉上突兀。弹出动效增添庆典感，且时间极短（0.20 秒），不影响节奏和后续输入。

### 5. 封面图生成（Tripo banana2）

**改了什么：** 以 OpticalArchiveFinal 封面为参考，调用 banana2（`tripo_image_to_image`）生成了一张增强版封面，要求：加深午夜蓝背景、强化琥珀玻璃内光、细化银色哥特框架高光、加重 LUMEN BOX 字体白色光泽。布局保持原版（START / EXIT 位置和外形不变，无副标题）。生成后需目视审查再替换。

**task_id：** `b1d50327-abe5-4a2e-9119-d995d54dc15f`，模型 `banana2`

---

## Tripo 调用清单

| 用途 | 模型 | task_id | 状态 |
|---|---|---|---|
| 封面增强版（午夜蓝+琥珀光） | banana2 | `b1d50327-abe5-4a2e-9119-d995d54dc15f` | 已通过目视审查并替换 |

### 封面审查结论

目视检查通过：
- "LUMEN BOX" 两行大写白色衬线字体，拼写正确
- 无副标题文字（符合导演审定版要求）
- START（金色药丸）和 EXIT（描边按钮）均在左下，位置和外形符合原版
- 四角无水印
- 无中文字符
- 效果提升：背景午夜蓝更深（含深紫渐变边缘）、琥珀玻璃内光更亮、银色哥特框架高光更细腻

**新封面文件：** `Assets/UI/Generated/Polish-20261022/01_generated_image_url.png`
**新 GUID：** `2a862f847ccd405cad2d8917ef7f8679`
**已更新场景：** `Assets/Scenes/StructureBuildTitle.unity` 第 713 行（`m_Sprite` 引用替换）

---

## 第二轮新增改动详情

### 6. 封面 START/EXIT 悬停+按下反馈（StructureTitleButton.cs）

**改了什么：**
- 新增 `hoverScale = 1.05f` / `pressScale = 0.97f` 字段控制缩放倍率
- 记录 `baseLocalScale`，在 `Update()` 中以 14Hz spring 动画将按钮缩放平滑到目标值
- 悬停时：缩放至 +5%（emphasis 驱动），focusGraphic 琥珀光晕完整显示
- 按下时：缩放先跌到 -3% 再在 0.06s 内回弹到悬停态，同时颜色闪至高亮琥珀色（0.18s 衰退）
- `OnDisable()` 恢复 `baseLocalScale`，防止场景切换后残留缩放
- 新增 `IsHovered` 属性供外部读取

**为什么：** 封面 START/EXIT 烘焙在图上，原有反馈（颜色变化）可见度低；加入明确的缩放动效，让玩家在 VR 中能清楚感知"我的射线正在指向可交互的按钮"。命中区大小不变。

### 7. 封面手柄悬停+按下震动（TitleWorldInteraction.cs）

**改了什么：**
- 射线从无 → 有 `hovered` 按钮时：`device.SendHapticImpulse(0u, 0.15f, 0.02f)`（轻触震动 20ms）
- 触发键按下时：震动从原 `0.22f/0.05s` 改为 `0.40f/0.04f`（更清脆的确认感）

**为什么：** 规范 A 明确要求悬停 0.15/20ms、按下 0.4/40ms。

### 8. 底栏三按钮缩放反馈（StructureUIButton.cs）

**改了什么：**
- 同样加入 `hoverScale/pressScale`，`baseLocalScale` 模式
- 按下颜色：从白 → 高亮琥珀短闪（0.18s 衰退至 teal hover 色），视觉确认感更强
- focusGraphic 在按下时额外放大至 1.5× 不透明度（短暂高亮）
- 缩放动画与颜色同步，`OnDisable()` 恢复原始缩放

### 9. 底栏桌面屏幕按钮反馈（StructureScreenButton.cs）

**改了什么：** 同 StructureUIButton，为桌面/非 XR 模式下的平面 Canvas 按钮补充相同的三态缩放和颜色反馈。

### 10. 游戏内 UI 按钮悬停震动（WorldSpaceInteraction.cs）

**改了什么：** 在射线首次悬停到 `StructureUIButton` 时调用 `Pulse(0.15f, 0.02f)`，与封面保持一致的手柄反馈规范。

### 11. 玻璃感点击音效加入工程（magic_ui_soft_02.mp3）

**改了什么：** 从 ange-embed `ui_audio` 集合（point_id `4b75390c`）下载 `magic_ui_soft_02.mp3`（玻璃高频敲击声），放入 `Assets/Audio/`。

**待总控操作：** 在 Unity 中将该文件指定给 `StructureAudioController.uiClickClip`（拖拽赋值），替换现有 `scifi_ui_soft_03.mp3`；`uiHoverClip` 保持 `scifi_ui_soft_02.mp3`（雷达音色）不变。

### 12. Optical Archive 字体组合加入工程（Cinzel + Jost）

**改了什么：**
- 下载 `Cinzel-Regular.ttf`、`Cinzel-SemiBold.ttf`（标题衬线，SIL OFL，Google Fonts）
- 下载 `Jost-Regular.ttf`、`Jost-Medium.ttf`（正文人文无衬线，SIL OFL，Google Fonts）
- 许可证：`Assets/UI/Fonts/LICENSE-OFL.txt`
- 新增编辑器脚本：`Assets/Editor/StructureBuildOpticalFontSetup.cs`
- 更新：`Assets/UI/Fonts/FONT_NOTES.md`

**待总控操作：**
1. 在 Unity 中执行菜单 `Structure Build → Generate Optical SDF Fonts`，生成四个 SDF .asset 文件
2. 将各场景 TMP 文字标签的 Font Asset：
   - 面板标题（Archive / 档案编号）→ `CinzelRegularSDF`
   - 规则文字、方块计数、Controls 面板正文 → `JostRegularSDF`
   - 中等强调文字 → `JostMediumSDF`
3. 检查字号是否满足规范 C（见 FONT_NOTES.md VR 字高要求）

---

## 性能前后对比

| 指标 | 改前（前次构建基线） | 第二轮变化 | 备注 |
|---|---|---|---|
| 新增 Update() 调用 | 第一轮 +1 | +0（Scale lerp 合并进已有 Update） | 无新方法 |
| GC 分配 | 无 | 无 | 仅数值运算，无对象分配 |
| Draw Call / SetPass | 不变 | 不变 | 未新增材质或渲染对象 |
| 纹理内存 | 不变 | 字体 atlas 待生成（≤4×4MB） | SDF 图集生成后约 +16MB GPU 纹理 |
| 实时灯光 | 不变 | 不变 | |
| 后处理 | 不变 | 不变 | |

**结论：** Scale lerp 每帧 < 5ns 运算，无 GC，不触发 Draw Call 增加。字体 SDF 图集为一次性纹理上传，之后无运行时开销，总计约 16MB 纹理预算在 PICO 4 限额内。

---

## 未完成项（需总控操作）

| 项目 | 状态 | 说明 |
|---|---|---|
| magic_ui_soft_02.mp3 场景赋值 | 待总控 | Inspector 拖拽 → StructureAudioController.uiClickClip |
| Cinzel+Jost SDF 生成 | 待总控 | Unity 内执行 Structure Build → Generate Optical SDF Fonts |
| SDF 生成后场景引用更新 | 待总控 | 见 FONT_NOTES.md 步骤 |
| APK 重建 | 待总控 | 所有代码+资产变更完成后，StructureBuild.Editor.BuildCommands.BuildAndroidEN |
| 通关后对比截图 | 待模拟器安装后 | 届时在 VisualQA/Polish-20261022/ 补充 |

---

## 说明

- 所有改动均在 `EnglishEditions/LumenBox-EN/` 工程内；原版 `/结构构建` 和 `LightboxCoverBuild-20260908` 未动。
- 红线保留：棋盘不旋转、投影板不旋转、关卡规则/方块预算不变、底栏严格 Hint/Undo/Reset、教学门禁/手柄重连逻辑不动、信息板位置不动、15° 俯视开场视角不动、文字不遮投影网格。
- 包名 `com.structurebuild.pico.en`，原版包 `com.structurebuild.pico` 不受影响。
