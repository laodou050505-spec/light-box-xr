# LUMEN BOX — English Text Table
# 生成日期：2026-10-22

## 术语表（Glossary）

| 中文术语 | 英文术语 | 说明 |
|---|---|---|
| 档案 | Archive | 关卡的 Optical Archive 主题命名 |
| 结构单元 / 方块 | Block(s) | 可放置的方块单元 |
| 结构档案 | Archive | 关卡/档案 |
| 正面投影 | Front projection | 正面投影面板 |
| 侧面投影 | Side projection | 侧面投影面板 |
| 提示 | Hint | 底栏按钮 |
| 撤回 | Undo | 底栏按钮 |
| 重置 | Reset | 底栏按钮 |
| 概览 | Overview | 摄像机视角 |
| 正面（视角） | Front | 摄像机视角 |
| 侧面（视角） | Side | 摄像机视角 |
| 视角 | View | 摄像机视角标签 |

---

## 文本表

| key | 原中文 | 英文 | 来源文件:行 | 使用场景 | 状态 |
|---|---|---|---|---|---|
| status.idle | 等待结构输入 | Awaiting structure input | StructureGameController.cs:97 | 游戏初始状态文本 | 已翻译 |
| status.complete | 正面与侧面投影已吻合 · 结构完成 | Front & side projections matched · Structure complete | StructureGameController.cs:378,385 | 结构完成时的状态文本 | 已翻译 |
| status.progress | 正面 {f.matched}/{f.targetCount} · 侧面 {s.matched}/{s.targetCount} | Front {f.matched}/{f.targetCount} · Side {s.matched}/{s.targetCount} | StructureGameController.cs:379,386 | 投影进度状态 | 已翻译 |
| status.hint_placed | 提示: 已放置 {x},{y},{z} | Hint: placed {x},{y},{z} | StructureGameController.cs:209 | 提示按钮放置后状态 | 已翻译 |
| status.budget_full | 结构单元已用完，请先移动或撤回方块 | Block budget reached, move or undo first | StructureGameController.cs:221 | 方块预算耗尽时提示 | 已翻译（em dash→逗号，避免字体缺字）|
| status.remove_above | 请先移走上方方块 | Remove the block above first | StructureGameController.cs:248 | 拖取被压住的方块时提示 | 已翻译 |
| status.drag_started | 拖动方块到高亮落点 | Drag block to a highlighted cell | StructureGameController.cs:258 | 开始拖动时状态 | 已翻译 |
| status.drag_valid | 释放以放置 {x},{y},{z} | Release to place at {x},{y},{z} | StructureGameController.cs:291 | 拖动悬停在有效落点时 | 已翻译 |
| status.drag_invalid | 将方块移动到棋盘高亮区域 | Move block over a highlighted grid cell | StructureGameController.cs:291 | 拖动悬停在无效区域时 | 已翻译 |
| status.no_drop | 未命中合法落点，方块已返回 | No valid drop target, block returned | StructureGameController.cs:322 | 释放时没有落点 | 已翻译（em dash→逗号，避免字体缺字）|
| status.drag_cancel | 已取消拖动 | Drag cancelled | StructureGameController.cs:338 | 取消拖动时 | 已翻译 |
| label.level | 档案 {n:00} / {total} · {name} | Archive {n:00} / {total} · {name} | StructureGameController.cs:143 | 关卡标题标签 | 已翻译 |
| label.cubes | 方块 {count} / {limit} | Blocks {count} / {limit} | StructureGameController.cs:374 | 方块计数标签 | 已翻译 |
| hud.view | 视角 · {viewName} | View · {viewName} | StructureHUDController.cs:63 | 视角标签 | 已翻译 |
| hud.level | 结构档案 {n:00} / {total}  ·  {name} | Archive {n:00} / {total}  ·  {name} | StructureScreenHUD.cs:37 | 桌面HUD关卡标签 | 已翻译 |
| hud.cubes | 结构单元  {count} / {budget} | Blocks  {count} / {budget} | StructureScreenHUD.cs:40 | 桌面HUD方块计数 | 已翻译 |
| hud.view_full | 视角  {viewName} | View  {viewName} | StructureScreenHUD.cs:41 | 桌面HUD视角标签 | 已翻译 |
| cam.overview | 概览 | Overview | StructureCameraViews.cs:19,46,55 | 摄像机视角名称 | 已翻译 |
| cam.front | 正面 | Front | StructureCameraViews.cs:62 | 摄像机视角名称 | 已翻译 |
| cam.side | 侧面 | Side | StructureCameraViews.cs:69 | 摄像机视角名称 | 已翻译 |
| complete.title.final | 全部结构已归档 | All Archives Complete | LevelCompleteOverlay.cs:57 | 最终关通关标题 | 已翻译 |
| complete.title | 结构档案完成 | Archive Complete | LevelCompleteOverlay.cs:57 | 普通关通关标题 | 已翻译 |
| complete.detail.final | 12 个空间结构均已通过双面投影校验 | All 12 spatial structures passed dual-projection verification | LevelCompleteOverlay.cs:59 | 最终关详情 | 已翻译 |
| complete.detail | 档案 {n:00} 已通过双面投影校验 | Archive {n:00} passed dual-projection verification | LevelCompleteOverlay.cs:60 | 普通关详情 | 已翻译 |
| complete.progress.final | 本轮探索完成 | Exploration complete | LevelCompleteOverlay.cs:62 | 最终关进度提示 | 已翻译 |
| complete.progress | 准备进入档案 {n:00} | Ready for Archive {n:00} | LevelCompleteOverlay.cs:63 | 进入下一关提示 | 已翻译 |
| btn.next | 下一关 | Next | LevelCompleteOverlay.cs:84 | 通关继续按钮 | 已翻译 |
| btn.restart | 重新开始 | Restart | LevelCompleteOverlay.cs:84 | 重新开始按钮 | 已翻译 |
| btn.hint | 提示 | Hint | StructureBuildOpticalPanels.cs:136 | 底栏提示按钮 | 已翻译 |
| btn.undo | 撤回 | Undo | StructureBuildOpticalPanels.cs:137 | 底栏撤回按钮 | 已翻译 |
| btn.reset | 重置 | Reset | StructureBuildOpticalPanels.cs:138 | 底栏重置按钮 | 已翻译 |
| btn.start_practice | 开始练习 | Start Practice | StructureBuildOpticalPanels.cs:192 | 教学开始练习按钮 | 已翻译 |
| panel.guide_title | 操作提示 | Controls | StructureBuildOpticalPanels.cs:156 | 右侧操作提示面板标题 | 已翻译 |
| panel.guide_body | 1 按扳机抓取左侧方块\n2 拖到橙色格，松开后吸附\n3 让两面投影都变成青绿色\n\n完成后点击下一关 | 1  Pull trigger to grab a block from the left\n2  Drag to an orange cell; release to snap\n3  Turn both projections teal\n\nTap Next when done | StructureBuildOpticalPanels.cs:159 | 右侧操作提示面板正文 | 已翻译 |
| teaching.title | 光匣教学关 | LUMEN BOX - Tutorial | StructureBuildOpticalPanels.cs:184 | 教学卡标题 | 已翻译（em dash→连字符，避免字体缺字）|
| teaching.body | 教学关只需 2 块地面方块\n\n1 指向左侧方块源，按扳机抓取\n2 拖到橙色高亮格，松开后自动吸附\n3 让正面与侧面投影都变成青绿色 | The tutorial requires only 2 ground blocks.\n\n1  Aim at the block source on the left; pull trigger to grab\n2  Drag to an orange highlighted cell; release to snap\n3  Turn both the front and side projections teal | StructureBuildOpticalPanels.cs:187 | 教学卡正文 | 已翻译 |
| teaching.note | 完成后点击下一关，继续后面的档案 | Tap Next when done to continue through the archives | StructureBuildOpticalPanels.cs:191 | 教学卡附注 | 已翻译 |
| generator.level_label | 结构档案 01 / 12  ·  首次校准 | Archive 01 / 12  ·  First Calibration | StructureBuildOpticalPanels.cs:101 | 生成器默认文本 | 已翻译 |
| generator.rule_label | 先放两块地面方块，让正面与侧面同时亮起 | Place two ground blocks to illuminate both projections at once | StructureBuildOpticalPanels.cs:103 | 生成器默认文本 | 已翻译 |
| generator.status_label | 正面 0/2 · 侧面 0/2 | Front 0/2 · Side 0/2 | StructureBuildOpticalPanels.cs:106 | 生成器默认文本 | 已翻译 |
| generator.cube_count | 结构单元  0 / 2 | Blocks  0 / 2 | StructureBuildOpticalPanels.cs:108 | 生成器默认文本 | 已翻译 |

---

## 12 关数据

| 关卡 ID | 中文名 | 英文名 | 原规则说明 | 英文规则说明 |
|---|---|---|---|---|
| archive-001 | 首次校准 | First Calibration | 先放两块地面方块，让正面与侧面同时亮起 | Place two ground blocks to illuminate both projections at once |
| archive-002 | 初层阶梯 | First Ascent | 第一次使用垂直堆叠 | Use vertical stacking for the first time |
| archive-003 | 双峰折线 | Twin Peaks | 让两座高点在侧面错位对应 | Offset two high points so they align in the side view |
| archive-004 | 阶梯信标 | Staircase Beacon | 匹配四列不同高度 | Match four columns of different heights |
| archive-005 | 三峰棱镜 | Three-Peak Prism | 辨认三层塔与双层塔的对应 | Map the three-tier tower against the two-tier tower |
| archive-006 | 双塔回声 | Twin Tower Echo | 两座三层塔需要正确换位 | Two three-tier towers must swap positions correctly |
| archive-007 | 终端光门 | Terminal Gate | 完成双塔组合，进入深层档案 | Complete the twin-tower pair to unlock the deep archive |
| archive-008 | 折光回廊 | Refraction Hall | 让三座双层塔在侧影中共用深度 | Three two-tier towers must share depth in the side silhouette |
| archive-009 | 双核偏振 | Dual-Core Shift | 同一正面高塔必须在深处留下第二核心 | A single front-facing tower must leave a second core deeper in |
| archive-010 | 暗面列阵 | Dark Side Array | 把三座高塔藏进同一条侧面光柱 | Hide three tall towers within a single side-projection column |
| archive-011 | 四壁共振 | Four-Wall Resonance | 用一座主塔同时遮住三面深层回声 | Use one master tower to block three layers of deep echoes |
| archive-012 | 终极光栅 | Final Grating | 让四座满高光塔在侧面压缩成一条主脊 | Compress four full-height towers into a single ridge in the side view |

---

## PlayerSettings

| 字段 | 原值 | 英文值 |
|---|---|---|
| productName | 光匣 | LUMEN BOX |
| bundleIdentifier | com.structurebuild.pico | com.structurebuild.pico.en |
| APK 输出路径 | Builds/结构构建-PICO.apk | Builds/EN/LumenBox-EN-PICO.apk |
