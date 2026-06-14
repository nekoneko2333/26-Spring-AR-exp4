# mcfAR 操作说明

本说明按当前 `Assets/Scenes/PokemonGame_ARBook.unity` 的实际状态整理。后续配置前先看“当前场景状态”，不要把还没做的东西当成已经绑定好的东西。

## 当前场景状态

已确认存在：

- 主场景：`Assets/Scenes/PokemonGame_ARBook.unity`
- 章节目标：`Chapter01Target` 到 `Chapter05Target`
- 章节根物体：`Chapter01Root` 到 `Chapter05Root`
- 图鉴页目标：`pokemonpage1` 到 `pokemonpage4`、`characterpage1`
- 图鉴页 `OnTargetFound` 已经能调用对应 `ARBookPokedexPage.Refresh()`
- 第一章已有 `Chapter01Objective`
- 第一章已有三个闪电碎片：`LightningFragment01/02/03`
- 第一章已有 `ARSequenceTapChallenge`
- 第二章已有 `ARViewAlignmentChallenge`
- 第三、四、五章已经有 Target、Root、PlayerAvatar、WalkableGround、Creatures 等基础结构
- Managers 上已有全局交互、捕捉、章节进度、调试清档等组件

还需要你手动配置的重点：

- 第二章三条文字的实际摆放和正确观察角度
- 第三章火山机关的具体物体、顺序、特效、桥或门
- 第四章湖中央宝可梦的起点、岸边终点、触发条件
- 第五章遗迹细节、石碑/符号/最终门/最终特效
- 每章主线宝可梦的对话、捕捉 ID、章节完成触发点

## 新增通用脚本

这些脚本都在 `Assets/Scripts/ARBook/`。

### `ARBookCondition`

单个条件。支持：

- `CapturedCreature`：检查 `Captured_<id>`
- `ChapterCompleted`：检查 `ChapterCompleted_<chapterId>`
- `ChallengeCompleted`：检查 `ChallengeCompleted_<chapterId>_<id>`
- `PlayerPrefsIntAtLeast`：检查自定义 PlayerPrefs int
- `PlayerPrefsKeyEqualsOne`：检查自定义 key 是否等于 1
- `FinaleCompleted`：检查最终结局是否完成

### `ARBookConditionGroup`

一组条件。

- `All`：全部满足
- `Any`：任意一个满足

### `ARBookConditionalActivator`

条件满足后开关物体或组件。

常用场景：

- 火山机关完成后显示桥
- 遗迹符号集齐后打开门
- 湖泊条件满足后显示岸边提示
- 条件不满足时隐藏某个精灵

### `ARBookConditionalMover`

条件满足后把一个物体从当前位置或起点移动到终点。

常用场景：

- 第四章湖中央宝可梦从湖中走到岸边
- 火山机关完成后移动石桥
- 遗迹门缓慢打开

### `ARBookConditionalDialogue`

根据条件显示成功或失败对话。

常用场景：

- 点石碑时提示“还缺少符号”
- 条件满足后提示“机关回应了”

### `ARBookProgressMarker`

写入一个自定义 PlayerPrefs key。

常用场景：

- 某个机关完成后写入 `Chapter3_VolcanoCooled = 1`
- 某个湖泊谜题完成后写入 `Chapter4_LakePathOpened = 1`

## 通用条件配置方法

所有带 `ARBookConditionGroup` 的脚本都按这个方式配：

1. 找到 `Conditions`。
2. `Match Mode` 选 `All` 或 `Any`。
3. 设置 `Conditions` 数组长度。
4. 每个元素选择 `Type`。
5. 填对应字段：
   - 捕捉条件：`Type = CapturedCreature`，`Id = Pikachu`
   - 章节条件：`Type = ChapterCompleted`，`Chapter Id = 1`
   - 挑战条件：`Type = ChallengeCompleted`，`Chapter Id = 2`，`Id = CelebiViewAlignment`
   - 自定义 key：`Type = PlayerPrefsKeyEqualsOne`，`Id = Chapter4_LakePathOpened`

右键组件菜单可以点 `Log Conditions` 查看当前条件是否满足。

## 第一章：闪电碎片和皮卡丘

当前场景已有：

- `Chapter01Objective`
- `LightningFragment01/02/03`
- `ARSequenceTapChallenge`
- `Pikachu`
- 章节完成触发器

目标流程：

1. 找到三个闪电碎片。
2. 按正确顺序触发碎片。
3. 解锁皮卡丘捕捉。
4. 捕捉皮卡丘。
5. 到终点完成第一章。
6. 场景里其他宝可梦可以自由对话和捕捉，不影响主线。

检查配置：

- 三个碎片挂 `ARBookCollectible`
- 三个碎片挂 `ARSequenceTapStep`
- `ARSequenceTapStep.challenge` 指向 `Chapter01Objective` 上的 `ARSequenceTapChallenge`
- `stepIndex` 分别是 `0/1/2`
- Pikachu 挂 `ARBookInteractable`
- Pikachu 的 `captureId = Pikachu`
- Pikachu 的 `canBeCaptured = true`
- Pikachu 挂 `ARBookCaptureRequirement`
- `ARBookCaptureRequirement.requiredChallenge` 指向第一章的 `ARSequenceTapChallenge`
- 第一章终点调用 `ARBookChapterCompletionTrigger.TryCompleteChapter()`
- 第一章完成触发器：
  - `chapterId = 1`
  - `requiredCaptureId = Pikachu`

## 第二章：视角文字拼合

当前场景已有：

- `Chapter02Root`
- `ARViewAlignmentChallenge`

目标流程：

1. 你手动摆三条 3D TMP 文字。
2. 玩家移动手机，找到正确视角。
3. 三条字从该视角拼成一句有意义的话。
4. 稳定观察指定秒数后完成挑战。
5. 可以解锁主线精灵捕捉或直接作为章节完成条件。

配置步骤：

1. 选中挂 `ARViewAlignmentChallenge` 的物体。
2. 添加 `ARViewAlignmentCalibrator`。
3. `Challenge` 拖同一个物体上的 `ARViewAlignmentChallenge`。
4. `Alignment Target` 拖观察中心点，通常是文字组合的父物体。
5. 在 Scene 视图移动到你认为正确的观察角度。
6. 点 `Use Scene View Camera As Expected`。
7. `ARViewAlignmentChallenge.angleTolerance` 建议先用 `12-18`。
8. `requiredStableSeconds` 建议 `1.0-2.0`。

如果第二章完成条件是“视角挑战完成”：

- 章节完成触发器 `requiredCaptureId` 可以留空。
- `Extra Conditions` 添加：
  - `Type = ChallengeCompleted`
  - `Chapter Id = 2`
  - `Id = CelebiViewAlignment`

如果第二章完成条件是“完成视角挑战后捕捉 Celebi”：

- Celebi 挂 `ARBookCaptureRequirement`
- `requiredChallenge` 指向 `ARViewAlignmentChallenge`
- 章节完成触发器：
  - `chapterId = 2`
  - `requiredCaptureId = Celebi`

## 第三章：火山

当前场景已有：

- `Chapter03Target`
- `Chapter03Root`
- `PlayerAvatar`
- `WalkableGround`
- `Creatures`

推荐玩法：冷却火山机关。

流程：

1. 玩家在火山地图里找到三个火山符号或火山核心。
2. 按正确顺序触发。
3. 火山冷却，桥、平台或封印门出现。
4. Infernape 变为可捕捉。
5. 捕捉 Infernape 后完成第三章。

配置方法：

1. 在 `Chapter03Root` 下创建 `Chapter03VolcanoPuzzle`。
2. 添加 `ARSequenceTapChallenge`。
3. 设置：
   - `chapterId = 3`
   - `challengeId = VolcanoSeal`
   - `requiredSteps = 3`
4. 三个火山符号分别添加：
   - Collider
   - `ARSequenceTapStep`
5. 三个 `stepIndex` 配成 `0/1/2`。
6. 在桥或门的父物体上添加 `ARBookConditionalActivator`。
7. `Conditions` 添加：
   - `Type = ChallengeCompleted`
   - `Chapter Id = 3`
   - `Id = VolcanoSeal`
8. 把桥放进 `Activate When Met`。
9. 把封印门放进 `Deactivate When Met`。
10. Infernape 挂 `ARBookCaptureRequirement`。
11. `requiredChallenge` 指向 `Chapter03VolcanoPuzzle` 的 `ARSequenceTapChallenge`。
12. 第三章终点的 `ARBookChapterCompletionTrigger`：
    - `chapterId = 3`
    - `requiredCaptureId = Infernape`

## 第四章：湖泊

当前场景已有：

- `Chapter04Target`
- `Chapter04Root`
- 湖泊地图基础物体
- 你已经放了一个湖中央宝可梦

推荐玩法：打开湖面通路，让宝可梦自己走到岸边。

流程：

1. 湖中央宝可梦初始不可达，`canBeCaptured` 可以先关掉，或者交互距离设小。
2. 玩家完成湖边机关。
3. 宝可梦从湖中央移动到岸边。
4. 移动完成后开启捕捉。
5. 捕捉 Manaphy 后完成第四章。

配置方法：

1. 在湖边放 3 个机关点，例如 `LakeRune01/02/03`。
2. 用 `ARSequenceTapChallenge` 或三个 `ARBookProgressMarker` 做条件。
3. 推荐最简单版本：
   - 创建 `Chapter04LakePuzzle`
   - 添加 `ARSequenceTapChallenge`
   - `chapterId = 4`
   - `challengeId = LakePath`
4. 给三个机关点添加 `ARSequenceTapStep`，stepIndex 为 `0/1/2`。
5. 创建两个空物体：
   - `ManaphyLakeStart`
   - `ManaphyShoreEnd`
6. 在 Manaphy 或它的父物体上添加 `ARBookConditionalMover`。
7. 配置：
   - `Target = Manaphy` 或 Manaphy 的父物体
   - `Start Point = ManaphyLakeStart`
   - `End Point = ManaphyShoreEnd`
   - `Snap To Start On Start = true`
   - `Auto Move When Conditions Met = true`
   - `Evaluate Repeatedly = true`
8. `Conditions` 添加：
   - `Type = ChallengeCompleted`
   - `Chapter Id = 4`
   - `Id = LakePath`
9. 如果要移动完成后才允许捕捉：
   - Manaphy 初始 `canBeCaptured = false`
   - 在 `ARBookConditionalMover.onMoveCompleted` 里把 Manaphy 的 `ARBookInteractable.canBeCaptured` 改不了，因为 UnityEvent 不能直接改字段。
   - 更简单做法：Manaphy 挂 `ARBookCaptureRequirement`，`requiredChallenge` 指向 `Chapter04LakePuzzle`。
10. 第四章终点的 `ARBookChapterCompletionTrigger`：
    - `chapterId = 4`
    - `requiredCaptureId = Manaphy`

## 第五章：遗迹

当前场景已有：

- `Chapter05Target`
- `Chapter05Root`
- 大地图基础结构
- `ARBookFinaleController` 可用于最终结局

推荐玩法：遗迹细节探索。

流程：

1. 地图里放多个石碑、壁画、遗迹碎片。
2. 玩家调查细节，得到 4 个符号。
3. 4 个符号代表前四章记忆。
4. 条件满足后遗迹中心门打开。
5. 捕捉或唤醒 Zekrom。
6. 完成第五章或触发最终结局。

配置方法：

1. 每个石碑挂 `ARBookInteractable`，只做对话。
2. 每个关键石碑额外挂 `ARBookProgressMarker`。
3. 示例 key：
   - `Chapter5_Rune_Thunder`
   - `Chapter5_Rune_Time`
   - `Chapter5_Rune_Fire`
   - `Chapter5_Rune_Water`
4. 在石碑的 `ARBookInteractable.onCaptured` 不适合用，因为石碑不是捕捉物。
5. 更推荐用 `ARBookProximityTrigger` 或按钮事件调用 `ARBookProgressMarker.SetProgressKey()`。
6. 遗迹门父物体添加 `ARBookConditionalActivator`。
7. `Conditions` 添加四个：
   - `PlayerPrefsKeyEqualsOne / Chapter5_Rune_Thunder`
   - `PlayerPrefsKeyEqualsOne / Chapter5_Rune_Time`
   - `PlayerPrefsKeyEqualsOne / Chapter5_Rune_Fire`
   - `PlayerPrefsKeyEqualsOne / Chapter5_Rune_Water`
8. 门放入 `Deactivate When Met`。
9. 最终特效放入 `Activate When Met`。
10. Zekrom 挂 `ARBookCaptureRequirement`，也可以用同样四个条件控制。
11. 第五章终点：
    - 如果只是章节完成：用 `ARBookChapterCompletionTrigger`
    - 如果是最终结局：用 `ARBookFinaleController.TryCompleteFinale()`

第五章最终结局推荐：

- `ARBookFinaleController.requiredChapterIds = [1, 2, 3, 4]`
- `finaleEffectRoot` 拖最终遗迹特效
- 终点触发 `TryCompleteFinale()`

## 调试方法

### 清空进度

Managers 上有 `ARBookDebugProgressResetter`。

- `Clear On Awake = true`：每次 Play 自动清进度，适合从头测试。
- `Clear On Awake = false`：保留进度，适合测试图鉴和后续章节。

### 查看图鉴是否解锁

选中图鉴页 PageRoot，点：

```text
ARBookPokedexPage -> Refresh And Log Pokedex Page
```

重点看：

- `unlocked`
- `captured`
- `chapterDone`
- `unlockedRootActive`

### 查看条件是否满足

带 `ARBookConditionalActivator`、`ARBookConditionalMover`、`ARBookConditionalDialogue` 的物体都可以右键点：

```text
Log Conditions
```

### 测试自定义 key

带 `ARBookProgressMarker` 的物体可以右键点：

```text
Set Progress Key
Log Progress Key
Clear Progress Key
```

## 演示前检查

- `ARBookDebugProgressResetter.clearOnAwake` 根据演示需要决定是否关闭。
- 图鉴页 ImageTarget 的 `OnTargetFound` 要调用对应 `ARBookPokedexPage.Refresh()`。
- 每章终点要调用对应章节的 `ARBookChapterCompletionTrigger.TryCompleteChapter()` 或 `ARBookFinaleController.TryCompleteFinale()`。
- 每章主线宝可梦的 `captureId` 必须唯一：
  - `Pikachu`
  - `Celebi`
  - `Infernape`
  - `Manaphy`
  - `Zekrom`
- 第三、四、五章如果使用新条件脚本，先在 Play 模式里用 `Log Conditions` 确认条件读到的是 `true`。
