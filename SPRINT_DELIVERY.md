# 四天冲刺交付说明

## 已完成的代码能力

- `PokemonGame_ARBook.unity` 已加入 Build Settings。
- Chapter 3/4/5 的完成条件已改为 `Infernape`、`Manaphy`、`Zekrom`。
- 第一章可使用 `ARBookChapterObjectiveManager`、`ARBookCollectible`、`ARSequenceTapChallenge`、`ARSequenceTapStep` 和 `ARBookCaptureRequirement` 做“收集 + 顺序点击 + 收服前置”。
- 第二章可使用 `ARViewAlignmentChallenge` 做“移动手机寻找正确观察角度”的 AR 谜题。
- 第五章可使用 `ARBookFinaleController` 或 `ARBookChapterCompletionTrigger.requiredCompletedChapterIds` 检查 Chapter 1-4 完成状态。
- 宠物状态支持 `PlayerPrefs` 存档和离线变化。
- 章节终点条件不足时会显示中文提示，不再静默失败。

## 标准 Unity 配置流程

### 通用 UI

1. 在 `Canvas` 下创建三个 TMP 文本：
   - `QuestText`：任务侧栏。
   - `ChallengeText`：挑战进度。
   - `ChapterProgressText`：章节进度或演示说明。
2. 三个 TMP 文本都使用中文 TMP 字体。
3. 可选：给文本外层加半透明 Image 面板，放在屏幕左侧。

### 第一章

1. 在 `Chapter01Root` 下创建或选择 `Chapter01Objective`。
2. 给 `Chapter01Objective` 添加：
   - `ARBookChapterObjectiveManager`
   - `ARSequenceTapChallenge`
3. `ARBookChapterObjectiveManager` 设置：
   - `chapterId = 1`
   - `objectiveTitle = 收集闪电碎片`
   - `requiredCollectibleCount = 3`
   - `objectiveTMPText = ChallengeText`
4. `ARSequenceTapChallenge` 设置：
   - `chapterId = 1`
   - `challengeId = PikachuSequence`
   - `requiredSteps = 3`
   - `progressTMPText = ChallengeText`
5. 三个碎片对象分别添加：
   - Collider
   - `ARBookCollectible`
   - `ARSequenceTapStep`
6. 三个碎片的 `ARBookCollectible.objectiveManager` 都拖 `Chapter01Objective`。
7. 三个碎片的 `collectibleId` 分别为：
   - `Fragment_01`
   - `Fragment_02`
   - `Fragment_03`
8. 三个碎片的 `ARSequenceTapStep.challenge` 都拖 `Chapter01Objective` 上的 `ARSequenceTapChallenge`。
9. 三个碎片的 `stepIndex` 分别为 `0`、`1`、`2`。
10. Pikachu 对象添加或检查：
    - `ARBookInteractable`
    - `ARBookCaptureRequirement`
11. Pikachu 的 `ARBookCaptureRequirement` 设置：
    - `objectiveManager = Chapter01Objective`
    - `requiredCollectibleCount = 3`
    - `requiredChallenge = Chapter01Objective.ARSequenceTapChallenge`
    - `lockedSpeaker = Pikachu`
12. `Chapter01Root` 添加 `ARBookQuestTracker`：
    - `chapterId = 1`
    - `mentor = 第一章导师 ARBookInteractable`
    - `creature = Pikachu ARBookInteractable`
    - `objectiveManager = Chapter01Objective`
    - `questTMPText = QuestText`

### 第二章

1. 在 Celebi 或其父对象上添加：
   - `ARViewAlignmentChallenge`
   - `ARBookCaptureRequirement`
2. `ARViewAlignmentChallenge` 设置：
   - `chapterId = 2`
   - `challengeId = CelebiViewAlignment`
   - `alignmentTarget = Celebi 或一个专门的观察目标点`
   - `expectedViewDirection = (0, 0, 1)` 起步
   - `angleTolerance = 15`
   - `requiredStableSeconds = 1`
   - `progressTMPText = ChallengeText`
3. Celebi 的 `ARBookCaptureRequirement` 设置：
   - `requiredCollectibleCount = 0`
   - `requiredChallenge = Celebi.ARViewAlignmentChallenge`
   - `lockedSpeaker = Celebi`
   - `challengeLockedDialogue = 移动手机，从正确方向观察 Celebi 的时间碎片。`
4. Celebi 的 `ARBookInteractable` 设置：
   - `canBeCaptured = true`
   - `captureId = Celebi`

### 第三、四、五章

1. 第三章主线精灵 Infernape 添加 `ARBookInteractable`：
   - `displayName = Infernape`
   - `canBeCaptured = true`
   - `captureId = Infernape`
2. 第四章主线精灵 Manaphy 添加 `ARBookInteractable`：
   - `displayName = Manaphy`
   - `canBeCaptured = true`
   - `captureId = Manaphy`
3. 第五章主线精灵 Zekrom 添加 `ARBookInteractable`：
   - `displayName = Zekrom`
   - `canBeCaptured = true`
   - `captureId = Zekrom`
4. 每章终点调用对应 `ARBookChapterCompletionTrigger.TryCompleteChapter()`。
5. 第五章的 `ARBookChapterCompletionTrigger` 设置：
   - `chapterId = 5`
   - `requiredCaptureId = Zekrom`
   - `requiredCompletedChapterIds = [1, 2, 3, 4]`
   - `completeDialogue = Adventure Complete.`

## 演示前检查

- `ARBookCollectionManager.clearOnStartForDebug` 必须关闭。
- TMP 中文字体要绑定到任务、对白和按钮文本。
- `PokemonGame_ARBook.unity` 已在 Build Settings 中启用。
- 第一章和第二章作为重点演示；第三、四章作为快速过渡；第五章展示最终结局。
