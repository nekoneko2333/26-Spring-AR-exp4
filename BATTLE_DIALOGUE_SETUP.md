# 战斗与 3D 对话系统配置

## 当前只需要做什么

基础舞台可以由编辑器工具直接创建，不需要再手动搭空物体和 UI：

```text
ARBook > 演出系统 > 创建或修复战斗与对话舞台
```

工具会复用场景中已有的同名物体，不会重复创建。它会自动完成：

- 补齐 `PresentationSystem`、战斗舞台和对话舞台的组件与引用
- 配置两台演出摄像机、背景平面、环绕镜头和 `Presentation` 层
- 创建战斗血条、提示文字、攻击按钮和退出按钮
- 创建对话框、角色名、正文、继续按钮和左右说话方高亮
- 创建战斗控制器、对话控制器、演出会话并绑定按钮事件
- 将两个舞台设为初始隐藏，将两台演出摄像机设为初始关闭

角色模型现在由 `ARBookPresentationDirector` 自动选择和复制：

- 与谁互动，对话左侧就使用谁的模型。
- 对话右侧自动使用当前章节正在操作的人物模型。
- 捕捉哪只精灵，战斗左侧就使用哪只精灵的模型。
- 战斗右侧自动使用当前章节正在操作的人物模型。
- 演出结束后自动销毁复制体，不会移动主场景中的原模型。
- 点击捕捉后先进入战斗，只有胜利才执行收服和图鉴解锁。

四个 Anchor 必须保持为空，不要手动放固定角色模型。

你只需要确认：

1. 运行后确认自动复制出来的人物大小、朝向和站位。
2. 确认镜头高度和环绕速度。
3. 确认各模型动画控制器中的状态名；没有同名状态时模型仍会显示，
   但对应演出动画不会播放。
4. 在两个 `ARBookPresentationSession` 的“演出时隐藏”列表中加入任务栏、
   互动按钮和旧对话框。
5. 修改每个 `ARBookInteractable` 自己的对话文本。演出系统会自动读取当前
   互动对象的文本，不需要在 `DialogueController` 重复配置。

下面保留完整结构说明，供需要调整单个设置时查询。

本文只说明新战斗演出和双角色 3D 对话的配置。所有对象都由你手动创建和绑定，
脚本不会自动复制模型或修改现有 AR 摄像机的位置。

## 一、系统结构

运行流程：

1. 隐藏原来的任务栏、按钮和对话框。
2. 截取当前 AR 画面作为固定背景。
3. 暂时关闭 `ARCamera` 的摄像机渲染。
4. 开启独立的演出摄像机和角色副本。
5. 播放环绕入场、战斗或半身对话。
6. 演出结束后恢复 AR 摄像机和原界面。

不要移动 Vuforia 的 `ARCamera`。Vuforia 每帧都会更新它的变换组件。

## 二、新增脚本

脚本都在：

```text
Assets/Scripts/ARBook/Presentation/
```

- `ARBookPresentationSession`：进入和退出演出模式。
- `ARBookFrozenBackground`：截取并显示固定 AR 背景。
- `ARBookPresentationCameraRig`：让演出摄像机环绕角色。
- `ARBookPresentationActor`：按动画状态机中的状态名播放角色动画。
- `ARBookBattleCombatant`：角色 HP、攻击力和受击状态。
- `ARBookBattleController`：基础回合战斗。
- `ARBookCinematicDialogueController`：左右双角色 3D 对话。

## 三、创建演出层

1. 打开 `编辑 > 项目设置 > 标签和层`。
2. 新建一个层：

```text
Presentation
```

3. 后面创建的演出摄像机、角色副本和背景平面都使用这个层。
4. 地图里的原角色不要修改层。

## 四、创建总演出结构

在场景根节点创建：

```text
PresentationSystem
├── BattleStage
│   ├── FrozenBackground
│   ├── CameraLookTarget
│   ├── BattleCamera
│   ├── LeftCreatureAnchor
│   ├── RightTrainerAnchor
│   └── BattleCanvas
└── DialogueStage
    ├── FrozenBackground
    ├── DialogueCamera
    ├── LeftActorAnchor
    ├── RightActorAnchor
    └── DialogueCanvas
```

`BattleStage` 和 `DialogueStage` 初始都取消勾选激活状态。

`PresentationSystem` 本身保持激活。战斗、对话控制器和演出会话组件建议挂在
`PresentationSystem`，不要挂在初始未激活的舞台上。

## 五、配置固定背景

### 1. 创建背景平面

分别在 `BattleStage` 和 `DialogueStage` 下创建：

```text
游戏对象 > 3D 对象 > 四边形
```

部分 Unity 中文版菜单仍可能显示 `Quad`。这里指的是一块单面的背景平面，不是带很多
网格的地面 `Plane`。命名为 `FrozenBackground`。

配置：

- 层：`Presentation`
- 删除 Collider
- 创建一个材质，着色器选择 `无光照/纹理（Unlit/Texture）`
- 将材质拖给背景平面

背景平面的位置和缩放不用手调，`ARBookFrozenBackground` 会根据摄像机自动调整。

### 2. 添加 ARBookFrozenBackground

在 `PresentationSystem` 创建空物体：

```text
BattleBackgroundController
```

添加 `ARBookFrozenBackground`：

- `背景渲染器（Background Renderer）`：BattleStage 背景平面的网格渲染器
- `演出摄像机（Presentation Camera）`：BattleCamera
- `背景距离（Background Distance）= 20`
- 勾选 `适配摄像机（Fit Quad To Camera）`

对 DialogueStage 再建立一个 `DialogueBackgroundController`，绑定对话摄像机和
对话背景平面。

背景平面会放在摄像机前方 20 单位。角色必须放在摄像机与背景之间。

## 六、创建战斗摄像机

### BattleCamera

1. 在 `BattleStage` 下创建摄像机，命名 `BattleCamera`。
2. 删除它的音频监听器，场景只能保留一个音频监听器。
3. 配置：

```text
剔除遮罩（Culling Mask）= Presentation
深度（Depth）= 高于 ARCamera
取消勾选摄像机组件
```

4. 给 `PresentationSystem` 下新建的 `BattleCameraRig` 添加
   `ARBookPresentationCameraRig`。
5. 绑定：

- `舞台摄像机（Stage Camera）`：BattleCamera
- `观察目标（Look Target）`：CameraLookTarget
- `半径（Radius）= 5`
- `高度（Height）= 1.5`
- `起始角度（Start Angle）= -180`
- `结束角度（End Angle）= 0`
- `环绕时间（Orbit Duration）= 2.5`

`CameraLookTarget` 放在左右角色中间，Y 位置大约在胸口。

如果只想绕半圈就使用 `-180 -> 0`。完整一圈可以使用 `0 -> 360`，但完整一圈通常
比半圈更拖沓。

## 七、准备演出角色副本

地图角色与演出角色分开。

不要把地图中的 `PlayerAvatar` 移进 BattleStage。将人物模型 Prefab 再拖一份作为
演出副本。

战斗布局：

```text
LeftCreatureAnchor
└── CreatureModel

RightTrainerAnchor
└── HildaModel
```

全部设置为 `Presentation` 层。

建议位置先用：

```text
LeftCreatureAnchor  = (-1.5, 0, 0)
RightTrainerAnchor  = ( 1.5, 0, 0)
```

让两个模型略微朝向画面中心。

### 角色演出组件

给人物和精灵模型根节点分别添加 `ARBookPresentationActor`。

- `动画器（Animator）`：该模型的动画器组件
- `交叉淡化时间（Cross Fade Duration）= 0.15`
- 状态名填写动画控制器中的状态名

人物推荐：

```text
Idle        = Idle
Entry       = BattleEntry
Attack      = BattleCommand
Hit         = Hit
Victory     = BattleVictory
Defeat      = Defeat
Speak       = Speak
Greeting    = Greeting
```

精灵按照自己的动画资源填写。没有某个动画时可以填 `Idle`，或者留空。

状态名必须与动画器控制器里的状态名称完全一致，不是 FBX 文件名。

## 八、配置 Hilda 动画器控制器

### 统一状态名称

所有可操作人物的演出动画控制器统一使用以下状态名。状态名必须完全一致，
不要直接使用很长的 FBX 动画片段名称。

| 固定状态名 | 用途 | Hilda 推荐动画片段 | 是否循环 |
|---|---|---|---|
| `Idle` | 默认待机 | `idle` | 是 |
| `Greeting` | 对话首次打招呼 | `greeting_1` | 否 |
| `Speak` | 普通说话 | `speak_1` | 否 |
| `Happy` | 开心、肯定 | 从 `pose_01` 到 `pose_07` 中预览选择 | 否 |
| `Serious` | 严肃、警觉 | 从 `pose_01` 到 `pose_07` 中预览选择 | 否 |
| `BattleEntry` | 战斗入场 | `appearance_1` 或 `appearance_2` | 否 |
| `Attack` | 发出攻击指令 | `direct` 或 `direct_ace` | 否 |
| `Hit` | 受击反应 | 暂时可使用 `land_1` | 否 |
| `Victory` | 战斗胜利 | `pose_03_battlefin` | 否 |
| `Defeat` | 战斗失败 | 暂时可使用 `land_2` | 否 |
| `Wave` | 挥手 | `wave_1` | 否 |
| `Search` | 调查、观察 | `search_1` | 否 |

## 人物完整状态机设计

第一版不要创建子状态机。将所有状态直接平铺在 `Base Layer`：

```text
Base Layer
├── Idle_A
├── Idle_B
├── Walk
├── Run
├── TurnLeft
├── TurnRight
├── BattleEntry
├── CaptureSuccess
├── Greeting
├── Speak
├── Reaction_01
├── Reaction_02
└── Reaction_03
```

可以在动画器窗口中把移动状态放在左侧、演出状态放在右侧，仅通过位置分组，不创建
`Locomotion` 和 `Presentation` 子状态机。

原因：

- 脚本当前按简单状态名播放，例如 `Greeting` 和 `Reaction_01`。
- 子状态机中的状态通常需要完整路径，容易出现“状态不存在”的警告。
- 跨子状态机返回需要配置 `Exit`、父级过渡和默认入口，当前没有必要增加这层复杂度。
- 平铺状态不影响最终效果，后续状态很多时再整理即可。

### 参数

在动画器的“参数”页创建：

| 参数名 | 类型 | 用途 |
|---|---|---|
| `Speed` | Float | `0` 待机，约 `0.5` 走路，约 `1` 跑步 |
| `Turn` | Float | 预留；当前地图移动脚本不驱动 |
| `IdleVariant` | Int | 预留；当前地图移动脚本不驱动 |
| `BattleEntryTrigger` | Trigger | 播放入场动画 |
| `CaptureSuccessTrigger` | Trigger | 播放收服成功动画 |
| `GreetingTrigger` | Trigger | 进入对话时打招呼 |
| `SpeakTrigger` | Trigger | 播放说话动作 |
| `Reaction` | Int | `0` 无反应，`1-3` 播放三个反应动作 |

参数名称区分大小写，必须完全一致。

### 移动状态

#### Idle_A

- 设为控制器默认状态。
- 使用第一个待机动画。
- 动画导入设置勾选“循环时间”。

切换：

```text
Idle_A -> Idle_B：IdleVariant 等于 1
Idle_A -> Walk：Speed 大于 0.1
```

取消“有退出时间”，过渡持续时间设为 `0.12`。

#### Idle_B

- 使用第二个待机动画。
- 勾选“循环时间”。

切换：

```text
Idle_B -> Idle_A：IdleVariant 等于 0
Idle_B -> Walk：Speed 大于 0.1
```

取消“有退出时间”，过渡持续时间设为 `0.12`。

当前先不要建立 `Idle_A <-> Idle_B` 的自动过渡。地图移动稳定后，再单独增加待机
随机脚本，否则待机条件可能在移动时抢走 `Walk`。

#### Walk

- 使用走路动画。
- 勾选“循环时间”。

切换：

```text
Walk -> Idle_A：Speed 小于 0.1，并且 IdleVariant 等于 0
Walk -> Idle_B：Speed 小于 0.1，并且 IdleVariant 等于 1
Walk -> Run：Speed 大于 0.75
```

全部取消“有退出时间”，过渡持续时间设为 `0.08-0.12`。

当前地图移动脚本只稳定写入：

```text
IsWalking = true / false
Speed = 0.5 / 0
```

因此暂时不要创建 `Walk -> TurnLeft/TurnRight` 条件。

#### Run

- 使用跑步动画。
- 勾选“循环时间”。

切换：

```text
Run -> Walk：Speed 小于 0.65
```

取消“有退出时间”，过渡持续时间设为 `0.08`。

#### TurnLeft

- 使用向左转的中间动作。

切换：

```text
TurnLeft -> Walk：Turn 大于 -0.15，并且 Speed 小于 0.75
TurnLeft -> Run：Turn 大于 -0.15，并且 Speed 大于 0.75
```

取消“有退出时间”，过渡持续时间设为 `0.08`。

#### TurnRight

- 使用向右转的中间动作。

切换：

```text
TurnRight -> Walk：Turn 小于 0.15，并且 Speed 小于 0.75
TurnRight -> Run：Turn 小于 0.15，并且 Speed 大于 0.75
```

取消“有退出时间”，过渡持续时间设为 `0.08`。

如果转身动画本身会让模型产生位移，取消动画器的“应用根运动”。实际方向仍由
`ARBookPlayerMover` 控制。

### 演出状态

从 `Any State` 分别建立以下过渡：

```text
Any State -> BattleEntry
条件：BattleEntryTrigger

Any State -> CaptureSuccess
条件：CaptureSuccessTrigger

Any State -> Greeting
条件：GreetingTrigger

Any State -> Speak
条件：SpeakTrigger

Any State -> Reaction_01
条件：Reaction 等于 1

Any State -> Reaction_02
条件：Reaction 等于 2

Any State -> Reaction_03
条件：Reaction 等于 3
```

这些进入过渡统一设置：

- 取消“有退出时间”
- 过渡持续时间 `0.08`
- “可以过渡到自身”取消勾选

每个一次性演出状态都直接建立返回默认待机的过渡：

```text
BattleEntry -> Idle_A
CaptureSuccess -> Idle_A
Greeting -> Idle_A
Speak -> Idle_A
Reaction_01 -> Idle_A
Reaction_02 -> Idle_A
Reaction_03 -> Idle_A
```

返回过渡统一设置：

- 勾选“有退出时间”
- 退出时间 `0.9`
- 过渡持续时间 `0.1`
- 不添加条件

`Reaction_01/02/03` 播放完成后，脚本会把 `Reaction` 重置为 `0`。否则下次进入
动画器时可能立即重复播放。

### 动作分配建议

按你当前 Hilda 动画资源，可以先这样分配：

| 状态 | 动画片段建议 |
|---|---|
| `BattleEntry` | `appearance_1` 或 `appearance_2` |
| `CaptureSuccess` | `join` 或 `unique_poke_1`，预览后选择 |
| `Greeting` | `greeting_1` |
| `Speak` | `speak_1` |
| `Reaction_01` | `pose_01` |
| `Reaction_02` | `pose_02` |
| `Reaction_03` | `pose_04` |
| `Idle_A` | `idle` |
| `Idle_B` | `pose_base`，如果不能自然循环就换另一个待机片段 |
| `Walk` | `walk_1` |
| `Run` | `run_1` |
| `TurnLeft` | `turn_l` |
| `TurnRight` | `turn_r` |

先在模型导入器的“动画”页逐个预览。`join` 和 `unique_poke_1` 哪个更像收服成功，
以实际预览为准。

### 当前移动配置

为了不影响现有地图行走，当前先只保留：

```text
Idle_A -> Walk：Speed 大于 0.1
Walk -> Idle_A：Speed 小于 0.1
```

如果暂时没有跑步速度切换，就不要创建 `Walk -> Run`。`Idle_B`、`TurnLeft` 和
`TurnRight` 可以先把状态和动画片段放好，但不要连过渡。

地图行走稳定后，再增加真实转向角速度和随机待机驱动。

### 对话反应规则

对话时使用以下节奏：

1. 对话开始：对方播放 `Greeting`。
2. 当前说话者播放 `Speak`。
3. 每经过随机 `1-2` 句话，未说话的一方随机播放
   `Reaction_01/02/03`。
4. 同一个反应动作不能连续播放两次。
5. 对话结束：双方回到 `Idle_A` 或 `Idle_B`。

这部分不是只配置动画器就能完成，还需要对话控制脚本负责计数和随机选择。

第一版必须配置的只有：

```text
Idle
Greeting
Speak
BattleEntry
Attack
Hit
Victory
Defeat
```

`Happy`、`Serious`、`Wave` 和 `Search` 是后续对话表现需要时再添加。

### 创建人物演出控制器

1. 在项目窗口复制 `Assets/Animations/Hilda_Regular_00.controller`。
2. 重命名为 `Hilda_Regular_00_Cinematic.controller`。
3. 双击新控制器打开“动画器”窗口。
4. 将上表对应的 FBX 动画片段拖进动画器窗口。
5. 逐个将状态重命名为表中的固定状态名。
6. 右键 `Idle`，选择“设为层默认状态”。
7. 选中 `PresentationSystem`，把新控制器拖到
   `ARBookPresentationDirector > Player Presentation Controller`。
8. 取消勾选“应用根运动”。

不要修改地图行走人物正在使用的原控制器。演出系统复制人物后，会为复制体切换到
`Player Presentation Controller`，地图中的原人物仍然继续使用行走控制器。

导师或精灵需要单独演出控制器时，在它自己的：

```text
ARBookInteractable > Presentation Animator Controller
```

绑定对应控制器。留空则继续使用模型原来的控制器。

### 状态过渡

脚本使用 `CrossFade` 按状态名直接播放，因此不需要从 `Any State` 建触发器过渡。
只需要给每个一次性状态添加返回 `Idle` 的过渡：

```text
Greeting   -> Idle
Speak      -> Idle
Happy      -> Idle
Serious    -> Idle
BattleEntry -> Idle
Attack     -> Idle
Hit        -> Idle
Victory    -> Idle
Defeat     -> Idle
Wave       -> Idle
Search     -> Idle
```

每条返回过渡设置：

- 勾选“有退出时间”
- “退出时间”设为 `0.9`
- “过渡持续时间”设为 `0.1`
- 不添加任何条件

动画片段导入设置：

- `idle` 勾选“循环时间”
- 其他一次性动画取消“循环时间”
- 所有演出人物的动画器取消“应用根运动”

### 脚本字段对应

`ARBookPresentationActor` 使用以下字段：

```text
Idle State      = Idle
Entry State     = BattleEntry
Attack State    = Attack
Hit State       = Hit
Victory State   = Victory
Defeat State    = Defeat
Speak State     = Speak
Greeting State  = Greeting
```

以后换人物模型时，优先保持这些状态名不变，只替换每个状态内部使用的动画片段。

当前 `Hilda_Regular_00.controller` 只正式接入了：

```text
IsWalking
Capture
```

虽然 FBX 中动画很多，但没有放进动画器控制器的状态无法由脚本直接播放。

### 1. 复制控制器

复制：

```text
Assets/Animations/Hilda_Regular_00.controller
```

重命名：

```text
Hilda_Regular_00_Cinematic.controller
```

不要直接大改原控制器，否则地图行走也会受影响。

将新控制器绑定给 BattleStage 和 DialogueStage 中的 Hilda 副本。

### 2. 创建战斗状态

从 Hilda FBX 展开动画，将下面动画拖入动画器窗口：

| 动画状态名 | 推荐动画 |
|---|---|
| `Idle` | `idle` |
| `BattleEntry` | `appearance_1` 或 `appearance_2` |
| `BattleStart` | `start_battle` |
| `BattleCommand` | `direct`、`direct_ace` 或 `shot_battle` |
| `BattleVictory` | `pose_03_battlefin` |
| `Greeting` | `greeting_1` 或 `wave_1` |
| `Speak` | `speak_1` |
| `Search` | `search_1` |

`shot_battle` 当前只有约 2 帧，更像一个短促姿态或同步片段，不适合单独作为长演出。
优先预览 `direct`、`direct_ace` 和 `start_battle`。

### 3. 状态切换方式

新的演出脚本使用交叉淡化，直接按状态名切换，因此不用给每个演出状态建立触发器。

每个一次性状态建议建立返回 Idle 的过渡：

```text
BattleEntry -> Idle
BattleCommand -> Idle
BattleVictory -> Idle
Greeting -> Idle
Speak -> Idle
```

配置：

- 勾选 `有退出时间（Has Exit Time）`
- `退出时间（Exit Time）= 0.9`
- 不添加过渡条件
- `过渡持续时间（Transition Duration）` 约为 `0.1`

`Idle` 动画需要在动画导入设置中勾选 `循环时间（Loop Time）`。一次性动画不要勾选。

所有演出副本的动画器组件：

```text
取消勾选 应用根运动（Apply Root Motion）
```

否则动画位移会破坏角色锚点布局。

## 九、创建战斗 UI

在 `BattleStage` 下创建画布：

```text
BattleCanvas
```

配置：

```text
渲染模式（Render Mode）= 屏幕空间-摄像机
渲染摄像机（Render Camera）= BattleCamera
排序顺序（Sorting Order）= 20
```

建议结构：

```text
BattleCanvas
├── TopMessage
├── LeftStatus
│   ├── NameAndHP
│   └── HPSlider
├── RightStatus
│   ├── NameAndHP
│   └── HPSlider
└── BattleControls
    ├── AttackButton
    └── ExitButton
```

使用 TextMeshPro 文本组件。

### 左右状态 UI

分别给左右角色模型根节点添加 `ARBookBattleCombatant`：

- `显示名称（Display Name）`：角色名
- `最大生命值（Max HP）`：例如 100
- `攻击力（Attack Power）`：例如 20
- `演出角色（Actor）`：同物体的 ARBookPresentationActor
- `生命值滑动条（HP Slider）`：对应滑动条
- `生命值文本（HP Text）`：对应 TMP 文本

如果右边人物是玩家，则将右边的战斗角色绑定到 `BattleController` 的 `Player`；
左边精灵绑定为 `Enemy`。

## 十、配置战斗控制器

在 `PresentationSystem` 创建 `BattleController`，添加：

```text
ARBookPresentationSession
ARBookBattleController
```

### 演出会话组件

- `AR 摄像机（AR Camera）`：场景中 Vuforia `ARCamera` 的摄像机组件
- `演出摄像机（Presentation Camera）`：BattleCamera
- `演出根物体（Presentation Root）`：BattleStage
- `冻结背景（Frozen Background）`：BattleBackgroundController 上的 ARBookFrozenBackground
- `演出时隐藏（Hide During Presentation）`：
  - 原来的主画布中不想冻结进背景的界面
  - 任务栏
  - 互动按钮
  - 捕捉按钮
  - 原来的对话面板

不要把当前图像目标或整个章节根物体放进隐藏列表，否则截图中地图也会消失。

### 战斗控制器组件

- `Session`：上面的 Session
- `摄像机支架（Camera Rig）`：BattleCameraRig
- `玩家（Player）`：右边人物的战斗角色组件
- `敌人（Enemy）`：左边精灵的战斗角色组件
- `战斗操作根物体（Battle Controls Root）`：BattleControls
- `消息文本（Message Text）`：TopMessage

按钮绑定：

```text
AttackButton 的 点击事件（On Click）
-> ARBookBattleController.PlayerAttack()

ExitButton 的 点击事件（On Click）
-> ARBookBattleController.ExitBattle()
```

开始战斗：

```text
ARBookBattleController.BeginBattle()
```

可以把它绑到精灵 `ARBookInteractable.onInteracted`，或单独的“战斗”按钮。

第一版战斗规则是：

1. 玩家攻击。
2. 敌人扣血并播放 Hit。
3. 未击败时敌人反击。
4. 任意一方 HP 为 0 后播放 Victory/Defeat。

这是演出与回合框架，不包含属性克制、技能列表、能量和状态效果。后续在这个 Controller
上扩展，不需要重做摄像机和 UI。

## 十一、创建 3D 半身对话

### 1. 对话摄像机

在 `DialogueStage` 创建摄像机：

```text
剔除遮罩（Culling Mask）= Presentation
取消勾选摄像机组件
```

删除 Audio Listener。

对话不需要环绕时可以直接固定摄像机。

### 2. 左右半身角色

在两个锚点下各放一个模型副本：

- 左边：NPC 或精灵
- 右边：当前操作人物

通过位置和摄像机裁切，只显示腰部或胸部以上。不要真的删除下半身模型。

模型都挂 `ARBookPresentationActor`，并使用刚才复制的演出动画控制器。

### 3. 对话画布

创建画布，并配置：

```text
渲染模式（Render Mode）= 屏幕空间-摄像机
渲染摄像机（Render Camera）= DialogueCamera
排序顺序（Sorting Order）= 20
```

结构：

```text
DialogueCanvas
├── LeftSpeakerHighlight
├── RightSpeakerHighlight
├── DialogueBox
│   ├── SpeakerNameText
│   ├── DialogueText
│   └── ContinueButton
```

左右高亮可以各用一个半透明图片组件。当前说话方保持白色，另一边降低透明度。

### 4. 对话控制器

在 `PresentationSystem` 创建 `DialogueController`，添加：

```text
ARBookPresentationSession
ARBookCinematicDialogueController
```

对话演出会话绑定 DialogueStage、DialogueCamera 和 DialogueBackgroundController。

`ARBookCinematicDialogueController`：

- `演出会话（Session）`：对话演出会话
- `左侧角色（Left Actor）`：左侧 ARBookPresentationActor
- `右侧角色（Right Actor）`：右侧 ARBookPresentationActor
- `对话界面根物体（Dialogue UI Root）`：DialogueCanvas 或 DialogueBox
- `说话者名字文本（Speaker Name Text）`：名字文本
- `对话正文文本（Dialogue Text）`：正文文本
- `左右说话者高亮（Left/Right Speaker Highlight）`：左右高亮图片

ContinueButton：

```text
点击事件（On Click）
-> ARBookCinematicDialogueController.ContinueDialogue()
```

## 十二、逐句配置情绪动画

`Lines` 数组每个元素代表一句话：

- `说话方（Speaker Side）`：左侧或右侧
- `说话者名字（Speaker Name）`：显示名字
- `文本（Text）`：正文
- `左侧角色状态（Left Actor State）`：本句左侧角色动画
- `右侧角色状态（Right Actor State）`：本句右侧角色动画

示例：

```text
Line 0
Speaker Side = Left
Speaker Name = 导师
Text = 你终于来到这里了。
Left Actor State = Greeting
Right Actor State = Idle

Line 1
Speaker Side = Right
Speaker Name = Hilda
Text = 我已经准备好了。
Left Actor State = Idle
Right Actor State = Speak

Line 2
Speaker Side = Left
Speaker Name = 导师
Text = 那就开始吧。
Left Actor State = BattleCommand
Right Actor State = pose_02
```

状态字段填写动画器控制器中的状态名。

Hilda 对话推荐先建立：

| 状态名 | 动画 |
|---|---|
| `Greeting` | `greeting_1` |
| `Wave` | `wave_1` |
| `Speak` | `speak_1` |
| `Happy` | 预览后选择 `pose_01` 到 `pose_07` |
| `Serious` | 预览后选择合适 pose |
| `BattleCommand` | `direct` |

`mouth_01` 到 `mouth_07` 不要直接按名字判断情绪。先在模型 Importer 的 Animation
页面逐个 Preview，确认它们修改的是嘴型、脸部还是全身。

如果希望身体姿势和嘴型同时播放，需要额外建立动画器层和角色遮罩。
第一版先使用完整的 `speak_1` 和 pose 动画，避免不同动画层互相覆盖骨骼。

## 十三、从 NPC 互动启动新对话

选中地图里的 NPC，其 `ARBookInteractable`：

1. 取消勾选 `使用默认对话（Use Default Dialogue）`。
2. 展开 `互动时事件（On Interacted）`。
3. 拖入 `DialogueController`。
4. 选择：

```text
ARBookCinematicDialogueController.BeginDialogue()
```

这样点击 NPC 后不会再弹旧对话面板，而是进入冻结背景的双角色 3D 对话。

如果仍需要旧对话框，保持勾选 `使用默认对话`，不要绑定新控制器。

## 十四、从精灵互动启动战斗

选中地图中的精灵：

1. 在 `ARBookInteractable` 的 `互动时事件` 中拖入 BattleController。
2. 选择：

```text
ARBookBattleController.BeginBattle()
```

如果希望先对话再战斗：

1. 先启动 3D 对话。
2. 在对话控制器的 `对话完成事件（On Dialogue Completed）` 中调用：

```text
ARBookBattleController.BeginBattle()
```

不要让对话演出会话和战斗演出会话同时处于激活状态。

## 十五、测试顺序

1. BattleStage 和 DialogueStage 初始取消激活。
2. BattleCamera 和 DialogueCamera 的摄像机组件初始取消勾选。
3. 进入 Play，确认普通 AR 场景正常。
4. 手动从组件菜单执行 `Begin Battle`。
5. 检查背景是否冻结且不含任务 HUD。
6. 检查演出摄像机是否绕角色。
7. 检查入场动画是否播放。
8. 点击 Attack，检查攻击、受击、扣血和反击。
9. 退出后确认 ARCamera 和原界面恢复。
10. 再测试 3D 对话。

## 十六、常见问题

### 角色完全不显示

- 模型所在层不是 `Presentation`
- 演出摄像机的剔除遮罩没有包含 `Presentation`
- 模型在背景平面后面
- 演出舞台仍处于未激活状态

### 背景盖住角色

将背景距离调大，例如 `20`，角色保持在摄像机前方约 `3-8` 单位。

### 提示动画器不存在某个状态

脚本填写的是动画器状态名，不是 FBX 动画片段名。检查控制器中是否真的建立了同名状态。

### 动画播放后角色跑出锚点

取消动画器组件的 `应用根运动（Apply Root Motion）`。

### 截图里包含旧任务栏

把任务栏游戏对象放进演出会话的 `演出时隐藏（Hide During Presentation）`。

### 退出演出后识图位置变化

确认没有移动 `ARCamera`、图像目标或章节根物体。这里只切换摄像机组件是否启用。
