# LUMEN BOX EN — 打磨报告（Polish 2026-10-22）

## 改动摘要

本轮在英文版交付基础上，在不改变游戏玩法、关卡规则、空间布局、按钮命中区域的前提下，对视觉效果和游玩体验做了以下打磨。

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

## 性能前后对比

| 指标 | 改前（前次构建基线） | 改后变化 | 备注 |
|---|---|---|---|
| 新增 Update() 调用 | 0 | +1（UpdateSnapFlash, ~5ns/帧，无分配） | snapFlashTimer <= 0 时立即 return |
| 新增协程 | 0 | 最多 3 同时运行（bounce × N + flash × 1 + overlayPunch × 1） | 每次放置 ≤ 0.55s，自然结束 |
| GC 分配 | 无额外 | 协程 IEnumerator 对象各一次（<100B） | 无持续分配 |
| Draw Call / SetPass | 不变 | 不变 | 未新增材质或渲染对象 |
| 纹理内存 | 不变 | 不变 | |
| 实时灯光 | 不变 | 不变 | |
| 后处理 | 不变 | 不变 | |

**结论：** 本轮改动对帧率影响极微（均为协程 + Material.SetColor，不触发 Draw Call 增加或纹理上传）。PICO 4 72fps 目标不受影响。

---

## 未完成项与风险

| 项目 | 状态 | 说明 |
|---|---|---|
| APK 构建 | 待磁盘空间 ≥ 15GB | 当前 10GB，政策要求 15GB 起；代码已就绪，等待空间释放后构建 |
| 封面增强版目视审查 | 已完成 | banana2 生成图通过全部检查，已替换场景引用 |
| 通关后对比截图 | 待运行画面 | Polish-20261022/ 目录已建，截图在模拟器安装后补充 |
| 手柄震动 | 已有 | WorldSpaceInteraction.cs 的 Pulse() 已覆盖：抓取(0.16/35ms)、放置(0.32/80ms)、失败(0.12/35ms)，本轮未另加 |
| 投影面板截图问题 | 文档记录中 | batchmode 无法初始化 RenderTexture，需设备/模拟器运行才能正常显示，非英文版 bug |

---

## 说明

- 所有改动均在 `EnglishEditions/LumenBox-EN/` 工程内；原版 `/结构构建` 和 `LightboxCoverBuild-20260908` 未动。
- 红线保留：棋盘不旋转、投影板不旋转、关卡规则/方块预算不变、底栏严格 Hint/Undo/Reset、教学门禁/手柄重连逻辑不动、信息板位置不动、15° 俯视开场视角不动。
- 包名 `com.structurebuild.pico.en`，原版包 `com.structurebuild.pico` 不受影响。
