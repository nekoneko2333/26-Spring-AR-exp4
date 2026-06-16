# 当前 UI 绑定修复说明

现在不要再运行会重建或重新排版 UI 的旧工具。当前只保留一个安全工具：

`ARBook > Tools > Repair Current UI Bindings`

这个工具不会移动 UI、不会改锚点、不会套样式、不会重建 `ARBookGameShellGeneratedRoot`。它只做这些事：

- 绑定 `ARBookGameShellController` 的 Home、HUD、Backpack、Companion、Dialogue、Battle 引用
- 支持 `DialoguePanel`、`DialogueCanvas` 或 `DialogueBox`
- 支持 `BattlePanel`、`BattleCanvas` 或 `BattleControls`
- 绑定首页、背包、陪伴、返回等按钮到外壳控制器
- 绑定普通对话 `DialogueManager`
- 绑定电影对话 `ARBookCinematicDialogueController`
- 绑定战斗 `ARBookBattleController`
- 绑定战斗血条、战斗消息、攻击/退出按钮
- 默认隐藏 Backpack、CompanionMode、DialoguePanel/DialogueBox、BattlePanel/BattleControls
- 隐藏并清空旧章节 HUD 的 `QuestText / ChapterProgressText / ChallengeText` 引用

## 你现在要做

1. 等 Unity 编译完成。
2. 点 `ARBook > Tools > Repair Current UI Bindings`。
3. 选中 `ARBookGameShell`，看 Inspector 里的引用是否都自动填上。
4. 运行游戏检查首页、HUD、背包、陪伴、对话、战斗。

## 默认显隐

运行前默认：

- `Home`：显示，除非 `Show Cover On Start` 被关掉
- `HUD`：跟 `Home` 相反
- `Backpack`：隐藏
- `CompanionMode`：隐藏
- `DialoguePanel` / `DialogueCanvas` / `DialogueBox`：隐藏
- `BattlePanel` / `BattleCanvas` / `BattleControls`：隐藏

## 不要再用的旧工具

旧的 UI 生成、合并 Canvas、套 Home 风格、演出舞台重建工具已经从当前菜单里移除，避免误点覆盖你手调好的 UI。
