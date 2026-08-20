# 结构构建 · PICO XR

这是 Three.js 空间投影解谜玩法的 Unity/PICO XR 移植项目，包含 12 个按顺序解锁的关卡、桌面测试入口和可编辑的未来科技室内场景。

## 打开场景

1. 使用官方 Unity `6000.5.5f1` 打开项目。
2. 打开 `Assets/Scenes/StructureBuild.unity`。
3. 在 Hierarchy 展开 `STRUCTURE_BUILD_SCENE`。

场景已生成完成。菜单 `Structure Build > Create Future Tech Scene` 用于重新生成整张场景，会覆盖现有 `StructureBuild.unity`；手工调整模型后不要再次生成，除非确认要重置。

## 可编辑结构

- `Environment_Editable`：地面环替换位、左右墙、后墙、顶棚、灯光和装饰肋。
- `PuzzleTable_Editable`：1/2 号工作台、正面/侧面投影板、5 号方块、6 号发射台、网格与装饰模型。
- `Assets/Prefabs/PuzzleCube.prefab`：运行时方块模板。替换方块模型时打开此 Prefab；场景中的 `CubeSource_Editable` 可直接预览同一套 5 号方块外观。
- `FrontProjectionPanel` / `SideProjectionPanel`：严格 4:3 的全息投影板。
- `GameSystems`：关卡、判定和 HUD 引用。
- `XR Rig`：PICO 头显、左右手柄射线与输入。

所有带 `Editable` 后缀的对象都可以直接在 Scene 窗口移动、旋转、缩放、替换或删除。

用户提供的 7 号 FBX 在 macOS Player 中会触发 Unity 场景反序列化越界，因此没有直接实例化；场景中的 `Floor_Base_Editable` 是可替换地面位。7 号源文件仍完整保留在 `Assets/Models/7`。

## 玩法

- 在 `4 × 3 × 4` 网格中放置有限数量的方块。
- 正面按 X 轴投影，侧面按 Z 轴投影；方块必须连续支撑，不能悬空。
- 琥珀色为目标，青绿色为吻合，红色为多余投影。
- 每关完成后自动进入下一关，不能选择或跳过关卡；第 12 关完成后显示全部通关。

桌面测试：鼠标从发射台方块拖到网格，右键移除；方向键选择列，回车/空格放置，Delete/Backspace 移除，`Z` 撤销，`R` 重置，`H` 提示。中键拖动旋转视角，滚轮缩放，`F` 回到默认视角。

PICO：扳机抓取/放置，握把删除；右手 A 撤销，左手 X 提示，B/Y 重置，右摇杆按下重置 XR 朝向。成功操作带手柄震动反馈。

太空背景不创建 Light：640 颗星点错峰闪烁，14 条普通流星循环出现；每约 18 秒触发一波 32 条彗尾的流星雨。

## PICO 设置

- PICO Unity SDK `6.0.0`
- Android / ARM64 / IL2CPP / `UnityPlayerActivity`
- Minimum API 29，Target API 36
- PICO Loader、Multiview、OpenGLES3 + Vulkan
- Unity Input System-only

## 构建输出

- PICO APK：`Builds/结构构建-PICO.apk`

电脑端请直接在 Unity Editor 中运行，使用桌面相机和键鼠回退控制。PICO SDK 的 XR 自动加载与 macOS 独立 Player 存在冲突，因此不交付 macOS 独立包。

Unity 的 Android 工具不接受项目路径中的中文字符。当前中文项目可正常编辑和运行；以后重新构建 APK 时，需要先把项目复制到纯英文临时路径再构建。现有 APK 已通过英文临时路径完成构建验证。
