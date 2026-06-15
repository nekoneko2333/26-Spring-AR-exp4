# 完整游戏外壳 UI 配置

## 一键创建

在 Unity 顶部菜单执行：

`ARBook > 工具 > 创建或更新完整游戏UI外壳`

场景中会出现 `ARBookGameShell`，并且菜单会直接生成真实的场景 UI：

- `ARBookGameShellCanvas`
- `ARBookGameShellGeneratedRoot`
- `Home`
- `HUD`
- `Backpack`
- `CompanionMode`

这些都是 Hierarchy 里的真实 GameObject，可以直接调 `RectTransform`、`Image`、`TextMeshProUGUI`、`LayoutGroup` 等组件。运行时脚本只负责绑定按钮事件和刷新文本，不会每次重新临时生成整套外壳。

这套真实 UI 包含：

- 封面页
- 开始游戏 / 继续游戏
- 清空存档 / 重新开始
- 陪伴模式入口
- 常驻 HUD
- 背包面板
- 已收服精灵陪伴面板

如果你不需要章节顺序，在 Unity 顶部菜单继续执行：

`ARBook > 工具 > 转换为地图独立收服流程`

这个工具会关闭所有地图完成触发器里的前置章节要求。每张地图可以独立识别、独立解密、独立收服对应宝可梦。

## 需要你确认的绑定

选中 `ARBookGameShell`，检查 `ARBookGameShellController`：

- `Collection Manager`：场景里的 `ARBookCollectionManager`
- `Chapter Progress`：场景里的 `ARBookChapterProgress`
- `Progress Resetter`：调试清档用的 `ARBookDebugProgressResetter`
- `Chapter Hud Controller`：现有章节 HUD 控制器
- `Chinese Font`：你的中文 TMP 字体
- `Player Avatar Sprite`：玩家头像 2D 图
- `Companion Placement Root`：陪伴模式模型生成位置。默认会用主摄像机前方

## 陪伴模式配置

`Companions` 数组已经按你的图鉴顺序预填了 ID 和中文名。

再次执行 `ARBook > 工具 > 创建或更新完整游戏UI外壳` 时，工具会自动尝试补：

- `Portrait Texture`：从 `Assets/Editor/Vuforia/ImageTargetTextures/mcfAR` 里的 jpg 自动匹配
- `Scene Object`：从场景里 `ARBookInteractable.captureId` 对应的可收服对象自动匹配

逻辑规则：

- 只有 `Captured_<ID>` 已保存的精灵才会出现在陪伴模式。
- 可以多选，点击 `放置选中` 生成模型。
- 放置时会克隆 `Companion Prefab` 或 `Scene Object`，不会移动地图里的原模型。
- 点击 `互动 + 好感` 会给选中的精灵增加好感度。
- 好感度保存在 `PlayerPrefs`，清空存档会一起清掉。

如果自动匹配错了，再手动改对应条目的 `Portrait Texture`、`Companion Prefab` 或 `Scene Object`。

## 调整真实 UI

如果你已经手动调过 `Home`、`HUD`、`Backpack`、`CompanionMode` 的位置和样式，不要反复执行“创建或更新完整游戏UI外壳 / 生成真实游戏UI外壳”，因为这个菜单会重建 `ARBookGameShellGeneratedRoot` 下面的外壳 UI。

正常调整流程：
- 先执行一次菜单生成真实 UI。
- 在 Hierarchy 里展开 `ARBookGameShellCanvas > ARBookGameShellGeneratedRoot`。
- 直接调整里面的组件。
- 之后只运行游戏测试，不需要再生成。

## 现有 UI

新外壳会隐藏旧的章节任务文字，改用新的常驻 HUD 显示。

对话 UI 和战斗 UI 不会被重建，只会在运行时统一按钮和文字风格，避免破坏你已经调好的对话/战斗镜头。

## 注意

这套外壳不改变 ImageTarget、地图识别、战斗、对话、收服逻辑。它只是把现有流程包成完整游戏入口、HUD、图鉴陪伴模式。
