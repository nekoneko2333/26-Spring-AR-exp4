# AR Spirit Adventure Book 项目 README（当前更新版）

> 本文档基于当前 Unity + Vuforia 项目进度整理，用于记录项目定位、已完成功能、当前场景结构、核心脚本说明、第一章与第二章配置、后续开发路线以及大作业交付建议。
>
> 当前项目已经从“多 ImageTarget 模型展示 Demo”升级为“AR 冒险书可玩原型”。

---

## 1. 项目定位

本项目是一款基于 Unity 与 Vuforia 的移动端 AR 冒险书应用。

用户通过手机摄像头识别实体书封面和章节页面。封面用于展示开场提示；不同章节页作为不同 AR 地图的空间锚点。每章中，玩家角色可以沿节点路线移动，在指定节点与 NPC 或精灵交互，阅读碎片化叙事对白，收服主线精灵，并在章节终点完成章节任务。

项目核心体验可以概括为：

```text
扫描封面
  ↓
封面出现粒子特效与 Open Chapter 1 提示
  ↓
翻开实体书页
  ↓
识别章节页面
  ↓
AR 地图从书页上出现
  ↓
玩家沿节点移动
  ↓
与导师或精灵交互
  ↓
收服章节主线精灵
  ↓
到达终点完成章节
  ↓
继续翻页进入下一章
  ↓
最终修复裂隙并通关
```

本项目不是简单的“扫图出模型”，而是将实体书、翻页、节点地图、角色移动、精灵交互、收服系统和章节进度结合起来，形成一个完整的 AR 冒险书应用。

---

## 2. 当前项目状态总览

到目前为止，项目已经完成：

```text
1. Git 忽略规则整理
2. Vuforia 书页识别目标导入
3. 封面识别与粒子提示
4. 第一章 AR 地图基础搭建
5. 节点移动系统
6. 多章节 PlayerMover 适配
7. 点击/触摸射线系统
8. 多句对白系统
9. NPC/精灵通用交互系统
10. UI 交互按钮系统
11. 精灵收服系统
12. PlayerPrefs 收集记录
13. 章节进度系统
14. 章节终点完成检测
15. 章节完成粒子效果
16. 第二章多章节适配修复
```

当前已经具备第一章 AR 冒险书的基础玩法闭环，并且第二章可以复用同一套 Managers 和脚本系统。

---

## 3. Git 与资源管理

### 3.1 `.gitignore` 已更新

当前 `.gitignore` 已忽略大文件和不适合进入 Git 的资源，包括：

```text
*.onnx
*.pt
*.pth
*.safetensors
*.gguf
*.bin
*.zip
*.rar
*.7z
*.tgz
/Assets/models/
```

这样可以避免模型、权重和压缩包等大体积资源进入 Git。

### 3.2 最终提交注意事项

虽然 `/Assets/models/` 被 Git 忽略，但最终提交大作业时必须包含模型资源。

最终提交 Unity 项目时建议直接压缩完整项目文件夹，至少保留：

```text
Assets
Packages
ProjectSettings
```

并确认：

```text
Assets/models/
```

已经包含在最终提交包中。否则老师打开项目时会出现模型丢失问题。

---

## 4. Vuforia 识别目标

当前 Vuforia Database 中已经导入以下图片：

```text
cover.jpg
chapter1.jpg
chapter2.jpg
chapter3.jpg
chapter4.jpg
chapter5.jpg
```

当前识别目标规划：

| 图片 | Unity Target | 当前作用 |
|---|---|---|
| `cover.jpg` | `CoverTarget` | 显示封面粒子特效和打开第一章提示 |
| `chapter1.jpg` | `Chapter01Target` | 第一章地图与教程章节 |
| `chapter2.jpg` | `Chapter02Target` | 第二章森林章节 |
| `chapter3.jpg` | `Chapter03Target` | 第三章火山章节，后续补全 |
| `chapter4.jpg` | `Chapter04Target` | 第四章湖泊章节，后续补全 |
| `chapter5.jpg` | `Chapter05Target` | 第五章裂隙遗迹终章，后续补全 |

---

## 5. 当前交互设计原则

当前项目采用以下交互分工：

```text
Vuforia ImageTarget：识别封面和章节页
Unity Node：定义真实移动路径
PlayerAvatar：在节点之间移动
InteractButton：玩家到达对应节点后触发 NPC/精灵对白
CaptureButton：当前目标可收服时执行收服
DialoguePanel：显示导师、精灵、章节完成等对白
PlayerPrefs：保存收服状态和章节进度
```

当前已经取消封面虚拟按钮。封面不需要用户按下书上机关，只负责显示粒子特效和提示。

封面流程为：

```text
识别 cover.jpg
  ↓
CoverRoot 显示
  ↓
封面粒子特效显示
  ↓
提示 Open Chapter 1
  ↓
用户真实翻开书到 chapter1.jpg
```

这样更符合真实书本的使用逻辑。

---

## 6. 推荐场景层级结构

当前推荐层级结构如下：

```text
PokemonGame_ARBook
├── ARCamera
├── Directional Light
├── Canvas
│   ├── DialoguePanel
│   │   ├── SpeakerNameText
│   │   ├── DialogueText
│   │   └── ContinueButton
│   ├── ActionButtons
│   │   ├── InteractButton
│   │   └── CaptureButton
│   └── OtherPanels
│
├── Managers
│   ├── ARTapRaycaster
│   ├── DialogueManager
│   ├── ARBookInteractionButton
│   ├── ARBookCollectionManager
│   ├── ARBookCaptureController
│   ├── ARBookChapterProgress
│   └── ARBookChapterCompletionTrigger
│
└── Books
    ├── CoverTarget
    │   └── CoverRoot
    │       ├── CoverMagicEffect
    │       └── OpenChapterHint
    │
    ├── Chapter01Target
    │   └── Chapter01Root
    │       ├── RouteVisual
    │       ├── PlayerAvatar
    │       │   └── Cynthia_Renagade_20
    │       ├── GroundBase
    │       ├── Nodes
    │       │   ├── Node_01
    │       │   ├── Node_02
    │       │   ├── ...
    │       │   └── Node_16
    │       ├── Environment
    │       └── Creatures
    │           ├── Giovanni_Sygna_10
    │           └── pikachu_navidad_-_pokemon
    │
    ├── Chapter02Target
    │   └── Chapter02Root
    │       ├── RouteVisual
    │       ├── PlayerAvatar
    │       ├── GroundBase
    │       ├── Nodes
    │       ├── Environment
    │       └── Creatures
    │
    ├── Chapter03Target
    ├── Chapter04Target
    └── Chapter05Target
```

核心规则：

```text
1. 每个 ImageTarget 下只放一个 Root。
2. DefaultObserverEventHandler 只负责 Root 显示和隐藏。
3. 每章拥有自己的 ChapterRoot、Nodes、PlayerAvatar、Creatures。
4. Managers 全局共享，但运行时要能识别当前激活章节。
5. 不同章节的 PlayerMover 和 Nodes 不应互相干扰。
```

---

## 7. 当前脚本目录

AR 冒险书相关脚本位于：

```text
Assets/Scripts/ARBook/
```

当前已实现脚本：

```text
ARBookMapNode.cs
ARBookPlayerMover.cs
ARTapRaycaster.cs
DialogueManager.cs
ARBookInteractable.cs
ARBookInteractionButton.cs
BillboardToCamera.cs
ARBookCollectionManager.cs
ARBookCaptureController.cs
ARBookChapterProgress.cs
ARBookChapterCompletionTrigger.cs
```

---

## 8. 核心脚本说明

### 8.1 `ARBookMapNode`

用途：章节地图节点脚本。

挂载位置：

```text
Node_01 到 Node_16
```

主要功能：

```text
1. 保存节点编号 nodeIndex
2. 保存节点解锁状态
3. 支持 onNodeReached 到达事件
4. 被点击后请求当前章节 PlayerAvatar 移动
```

当前关键节点：

| 节点 | 功能 |
|---|---|
| `Node_06` | 第一章导师交互节点 |
| `Node_09` | 第一章 Pikachu 出现和交互节点 |
| 章节终点 Node | 调用章节完成检测 |

---

### 8.2 `ARBookPlayerMover`

用途：玩家移动脚本。

挂载位置：

```text
每章自己的 PlayerAvatar
```

主要功能：

```text
1. 支持直接移动到点击节点
2. 支持按 nodeIndex 顺序沿路径移动
3. 支持移动中忽略重复点击
4. 使用 localPosition 修复坐标系问题
5. 多章节下只收集当前 ChapterRoot 下的 Nodes
```

关键配置建议：

```text
Route Root = 当前章节 ChapterRoot
Move Directly To Tapped Node = 调试时可开，正式演示建议关闭
Use Nearest Node As Start = true
Ignore Move Requests While Moving = true
```

说明：

```text
当前不使用 NavMesh，也不做自由移动。
角色沿节点路径移动，稳定性更高，更适合 AR 书本地图。
```

---

### 8.3 `ARTapRaycaster`

用途：点击/触摸射线检测。

挂载位置：

```text
Managers
```

主要功能：

```text
1. 支持 Unity Editor 鼠标点击
2. 支持移动端触摸
3. 支持点击节点
4. 支持点击交互物
```

当前说明：

```text
虽然脚本支持点击交互物，但当前主要通过 InteractButton 触发 NPC/精灵交互。
这样可以避免手机 AR 中点击模型不稳定的问题。
```

---

### 8.4 `DialogueManager`

用途：对白 UI 管理。

挂载位置：

```text
Managers
```

支持：

```text
Legacy Text
TextMeshPro TMP_Text
多句对白
Continue 翻页
最后一句后关闭 DialoguePanel
```

当前用途：

```text
导师对白
精灵记忆碎片对白
收服对白
章节完成提示
封面提示
终章结局对白
```

---

### 8.5 `ARBookInteractable`

用途：NPC 和精灵通用交互脚本。

挂载位置：

```text
导师模型
精灵模型
其他可交互对象
```

支持字段：

```text
displayName
dialogueFragments
animationTriggerName
faceCameraOnInteract
interactionNode
interactionNodeIndex
interactionRadius
requirePlayerAtInteractionNode
canBeCaptured
isCaptured
captureId
captureDialogue
onCaptured
```

当前用途：

```text
Giovanni：导师 NPC，不可收服
Pikachu：第一章主线精灵，可收服
Celebi：第二章主线精灵，可收服
```

---

### 8.6 `ARBookInteractionButton`

用途：控制 UI 交互按钮。

挂载位置：

```text
Managers
```

主要功能：

```text
1. 检测当前章节中玩家是否到达可交互对象节点
2. 到达后显示或启用 InteractButton
3. 点击 InteractButton 后触发当前对象对白
4. 与 CaptureController 同步当前可收服目标
5. 多章节下自动选择当前激活章节的 PlayerMover
```

设计原因：

```text
移动端 AR 中直接点击 3D 模型容易受抖动、遮挡和视角影响。
因此当前采用“到达节点后显示 UI 交互按钮”的方式，稳定性更高。
```

---

### 8.7 `BillboardToCamera`

用途：让物体朝向 Camera.main。

当前状态：可选。

适合用于：

```text
导师对话
精灵对话
重要提示牌
```

---

### 8.8 `ARBookCollectionManager`

用途：收服记录管理。

挂载位置：

```text
Managers
```

主要功能：

```text
1. 使用 PlayerPrefs 保存收服状态
2. Key 格式：Captured_ + captureId
3. CaptureCreature(string captureId)
4. IsCaptured(string captureId)
5. ClearCollection()
6. 支持 clearOnStartForDebug
```

测试阶段：

```text
clearOnStartForDebug = true
```

正式演示前：

```text
clearOnStartForDebug = false
```

---

### 8.9 `ARBookCaptureController`

用途：控制 CaptureButton。

挂载位置：

```text
Managers 或 Canvas
```

主要功能：

```text
1. 管理当前可收服对象
2. 当前对象可收服且未收服时显示 CaptureButton
3. 点击 CaptureButton 后保存收服状态
4. 设置目标 isCaptured
5. 显示 captureDialogue
6. 调用目标 onCaptured
7. 收服后隐藏 CaptureButton
```

---

### 8.10 `ARBookChapterProgress`

用途：章节进度与记忆碎片管理。

挂载位置：

```text
Managers
```

保存 Key：

```text
ChapterCompleted_1
MemoryFragment_1
ChapterCompleted_2
MemoryFragment_2
...
```

主要方法：

```text
CompleteChapter(int chapterId)
IsChapterCompleted(int chapterId)
SetMemoryFragmentCollected(int chapterId)
HasMemoryFragment(int chapterId)
```

---

### 8.11 `ARBookChapterCompletionTrigger`

用途：章节完成检测。

当前逻辑：

```text
不在收服精灵后立刻完成章节。
而是：
1. 先收服主线精灵
2. 继续走到指定终点 Node
3. 终点 Node 的 onNodeReached 调用 TryCompleteChapter
4. 如果 requiredCaptureId 已收服，则完成章节
```

支持：

```text
chapterId
requiredCaptureId
completeDialogue
transitionEffectRoot
transitionEffect
```

当前示例：

```text
Chapter 1:
chapterId = 1
requiredCaptureId = Pikachu
completeDialogue = Chapter 1 is complete. Open Chapter 2.

Chapter 2:
chapterId = 2
requiredCaptureId = Celebi
completeDialogue = Chapter 2 is complete. Open Chapter 3.
```

---

## 9. 第一章当前配置

### 9.1 第一章主题

```text
Chapter 1: Torn Camp
```

### 9.2 第一章完整流程

```text
识别 Chapter01Target
  ↓
Chapter01Root 显示
  ↓
玩家从 Node_01 出发
  ↓
移动到 Node_06
  ↓
InteractButton 出现
  ↓
与 Giovanni 导师对话
  ↓
继续移动到 Node_09
  ↓
Pikachu 出现
  ↓
与 Pikachu 对话
  ↓
CaptureButton 出现
  ↓
收服 Pikachu
  ↓
继续移动到终点 Node
  ↓
触发 Chapter 1 completed
  ↓
显示 Chapter 1 is complete. Open Chapter 2.
```

### 9.3 第一章关键节点

| 节点 | 功能 |
|---|---|
| `Node_01` | 起点 |
| `Node_06` | 导师 Giovanni 交互节点 |
| `Node_09` | Pikachu 出现与交互节点 |
| 终点 Node | 第一章完成检测节点 |

### 9.4 第一章角色配置

#### PlayerAvatar

```text
PlayerAvatar
└── Cynthia_Renagade_20
```

说明：

```text
PlayerAvatar 挂 ARBookPlayerMover。
Cynthia_Renagade_20 是可见玩家模型。
```

#### Giovanni 导师

```text
Giovanni_Sygna_10
```

推荐配置：

```text
displayName = Mentor
interactionNode = Node_06
interactionNodeIndex = 6
requirePlayerAtInteractionNode = true
canBeCaptured = false
```

#### Pikachu

```text
pikachu_navidad_-_pokemon
```

推荐配置：

```text
Active 初始为 false
displayName = Pikachu
interactionNode = Node_09
interactionNodeIndex = 9
requirePlayerAtInteractionNode = true
canBeCaptured = true
captureId = Pikachu
captureDialogue = Pikachu joined the memory book.
```

### 9.5 第一章事件绑定

```text
Node_09.onNodeReached
  → pikachu_navidad_-_pokemon.SetActive(true)
```

终点 Node：

```text
onNodeReached
  → ARBookChapterCompletionTrigger.TryCompleteChapter()
```

---

## 10. 第二章当前适配

### 10.1 第二章主题

```text
Chapter 2: Silent Forest
```

当前状态：

```text
第二章已经开始适配。
第二章移动和第一章共用同一套脚本系统。
多章节下 PlayerMover、InteractionButton 和 Nodes 串章的问题已经修复。
```

### 10.2 已修复多章节问题

之前可能出现的问题：

```text
1. 节点找错 PlayerMover
2. PlayerMover 收集到其他章节 Nodes
3. InteractionButton 只引用某一章 PlayerMover
4. 第二章正常后第一章交互按钮不显示
```

当前修复结果：

```text
1. 每章 PlayerMover 只收集当前 ChapterRoot 下的 Nodes
2. InteractionButton 会自动选择当前激活章节的 PlayerMover
3. 第一章和第二章可以共用 Managers
4. 每章独立使用自己的 ChapterRoot、Nodes、PlayerAvatar 和 Interactable
```

### 10.3 第二章建议主线

主线精灵：

```text
Celebi
```

支线精灵：

```text
Bulbasaur
Zarude
Zorua
```

最小闭环：

```text
识别 Chapter02Target
  ↓
Chapter02Root 显示
  ↓
玩家沿节点移动
  ↓
到指定节点后 Celebi 出现
  ↓
与 Celebi 对话
  ↓
收服 Celebi
  ↓
到终点 Node
  ↓
Chapter 2 completed
  ↓
显示 Chapter 2 is complete. Open Chapter 3.
```

推荐配置：

```text
Celebi:
displayName = Celebi
canBeCaptured = true
captureId = Celebi
requirePlayerAtInteractionNode = true
captureDialogue = Celebi joined the memory book.
```

第二章完成检测：

```text
chapterId = 2
requiredCaptureId = Celebi
completeDialogue = Chapter 2 is complete. Open Chapter 3.
```

---

## 11. 封面当前配置

封面当前不做按键、不做虚拟按钮。

当前流程：

```text
识别 cover.jpg
  ↓
CoverRoot 显示
  ↓
粒子特效出现
  ↓
提示用户打开 Chapter 1
```

推荐结构：

```text
CoverTarget
└── CoverRoot
    ├── CoverMagicEffect
    └── OpenChapterHint
```

推荐提示文本：

```text
Open Chapter 1
```

或者：

```text
The book is awake.
Open Chapter 1.
```

已取消内容：

```text
Vuforia Virtual Button
Cover_ActivateBook
BookActivated 状态
按下封面机关
```

取消原因：

```text
封面的自然现实操作是“看见封面后打开书”，不需要额外按下虚拟按钮。
封面只做入口提示更符合真实书本交互。
```

---

## 12. UI 系统当前状态

当前主要 UI：

```text
DialoguePanel
ContinueButton
InteractButton
CaptureButton
```

### 12.1 DialoguePanel

用途：

```text
显示对白、提示和章节完成信息。
```

支持：

```text
Legacy Text
TextMeshPro
多句对白
Continue 翻页
```

### 12.2 InteractButton

用途：

```text
玩家到达可交互对象对应节点后，用按钮触发对话。
```

当前不依赖直接点击模型触发对白。

### 12.3 CaptureButton

用途：

```text
当前交互对象可收服且未收服时显示。
点击后完成收服。
```

---

## 13. 当前存档与调试设置

当前使用 PlayerPrefs 保存：

```text
Captured_Pikachu
ChapterCompleted_1
MemoryFragment_1
Captured_Celebi
ChapterCompleted_2
MemoryFragment_2
...
```

测试阶段：

```text
ARBookCollectionManager.clearOnStartForDebug = true
```

正式演示前必须改为：

```text
ARBookCollectionManager.clearOnStartForDebug = false
```


---

## 14. 当前已验证内容

当前已经验证或可运行的内容包括：

```text
1. CoverTarget 识别后显示封面效果和提示
2. Chapter01Target 识别后显示第一章 Root
3. Node_01 到 Node_16 可以作为路径节点
4. PlayerAvatar 可以按节点移动
5. DialogueManager 可以显示多句对白
6. Giovanni 可以绑定 Node_06 交互
7. Pikachu 可以绑定 Node_09 交互
8. Node_09 可以触发 Pikachu 出现
9. CaptureButton 可以收服可收服精灵
10. PlayerPrefs 可以保存收服状态
11. 章节终点可以在满足条件后完成章节
12. 章节完成时可以播放粒子效果
13. 第二章可以共用 Managers，不再串到第一章
```

---

## 15. 后续开发任务

当前不建议再继续新增大型系统。下一步重点是把已有系统复制到完整章节结构中。

### 15.1 优先任务一：稳定第二章

目标：完成 Chapter 2 的完整闭环。

任务清单：

```text
[ ] 确认 Chapter02Target 可稳定识别
[ ] 确认 Chapter02Root 显示/隐藏正常
[ ] 确认 Chapter02Root 下有自己的 PlayerAvatar
[ ] 确认 Chapter02Root 下有自己的 Nodes
[ ] 确认第二章 PlayerMover 的 Route Root 指向 Chapter02Root
[ ] 配置 Celebi 初始隐藏
[ ] 设置 Celebi 的 interactionNode
[ ] 设置 Celebi 的 captureId = Celebi
[ ] 设置 Celebi canBeCaptured = true
[ ] 设置指定 Node.onNodeReached → Celebi.SetActive(true)
[ ] 设置第二章终点 Node.onNodeReached → TryCompleteChapter()
[ ] 配置 ChapterCompletionTrigger：
    chapterId = 2
    requiredCaptureId = Celebi
    completeDialogue = Chapter 2 is complete. Open Chapter 3.
[ ] 测试第二章完整流程
```

---

### 15.2 优先任务二：实现第五章最小结局

为了让大作业演示具有完整闭环，建议在补第三、四章前先完成第五章最小结局。

最小结局流程：

```text
识别 Chapter05Target
  ↓
Chapter05Root 显示
  ↓
玩家移动到 RiftCore 终点
  ↓
检查是否完成前置章节
  ↓
播放最终粒子效果
  ↓
显示最终对白
  ↓
显示 Adventure Complete
```

最小前置条件可以先设置为：

```text
ChapterCompleted_1
ChapterCompleted_2
```

后续完整版本再改成检查：

```text
ChapterCompleted_1
ChapterCompleted_2
ChapterCompleted_3
ChapterCompleted_4
```

建议结局对白：

```text
The lost fragments are gathered.
The rift is healed.
The book remembers its path again.
```

---

### 15.3 后续任务三：补全第三章和第四章

在第二章和第五章跑通后，再补：

```text
Chapter 3: Ashen Volcano
Chapter 4: Sunken Lake
```

每章复用第二章模板：

```text
识别章节页
  ↓
章节 Root 显示
  ↓
玩家节点移动
  ↓
主线精灵出现
  ↓
对白
  ↓
收服
  ↓
到终点完成章节
```

推荐主线精灵：

| 章节 | 主线精灵 | 完成条件 |
|---|---|---|
| Chapter 3 | Infernape | `Captured_Infernape` |
| Chapter 4 | Manaphy | `Captured_Manaphy` |
| Chapter 5 | Zekrom | 可作为最终章节主线或结局展示 |

---

## 16. 后续章节配置建议

### 16.1 Chapter 3: Ashen Volcano

```text
主线精灵：Infernape
支线精灵：Toxtricity / Sneasler / Axew
完成条件：Captured_Infernape
完成提示：Chapter 3 is complete. Open Chapter 4.
```

### 16.2 Chapter 4: Sunken Lake

```text
主线精灵：Manaphy
支线精灵：Jirachi / Electrode
完成条件：Captured_Manaphy
完成提示：Chapter 4 is complete. Open Chapter 5.
```

### 16.3 Chapter 5: Rift Ruins

```text
主线精灵：Zekrom
支线精灵：Mew / Mismagius / Scizor / Dragapult / GalarianZapdos
最小结局条件：完成 Chapter 1 和 Chapter 2
完整结局条件：完成 Chapter 1 到 Chapter 4
结局提示：Adventure Complete.
```

---

## 17. 当前不建议继续扩展的功能

为了保证项目按时完成，不建议当前继续做：

```text
1. NavMesh 自动寻路
2. 自由移动
3. 大范围现实空间行走
4. 战斗系统
5. 背包系统
6. 技能系统
7. 多结局系统
8. 联网系统
9. 每只精灵独立任务线
10. 复杂图鉴界面
11. 复杂 Vuforia Virtual Button 机关
```

当前开发重点应是：

```text
稳定第一章
完成第二章
完成第五章最小结局
复制模板补第三、四章
真机测试
打包 APK
录制视频
写报告
```

---

## 18. 后续 Agent 工作规范

继续让 agent 协助时，需要遵守：

```text
1. 不修改 Vuforia 包源码
2. 不修改 DefaultObserverEventHandler
3. 不删除或重命名现有 ImageTarget
4. 不重写已有节点移动系统
5. 不重写已有对白系统
6. 不重写已有收服系统
7. 不引入第三方依赖
8. 不加入 NavMesh
9. 不加入战斗系统
10. 新脚本放在 Assets/Scripts/ARBook/
11. 代码变量、注释、日志、UI 字符串尽量使用英文
12. 每次只完成一个明确模块
13. 每次说明 Unity Inspector 如何配置和测试
```

---

## 19. Agent Prompt：完成第二章完整闭环

```text
你正在处理一个 Unity + Vuforia AR 冒险书项目。

当前第一章已经完成基础闭环：
- Chapter01Target 可以识别
- Chapter01Root 下有 PlayerAvatar、Nodes、Creatures
- PlayerAvatar 可以沿 Node_01 到 Node_16 移动
- Giovanni 绑定 Node_06
- Pikachu 绑定 Node_09
- Pikachu 可收服
- 收服 Pikachu 后，走到终点 Node 可以完成 Chapter 1
- Managers 上已有：
  - ARTapRaycaster
  - DialogueManager
  - ARBookInteractionButton
  - ARBookCollectionManager
  - ARBookCaptureController
  - ARBookChapterProgress
  - ARBookChapterCompletionTrigger

现在需要完成 Chapter 2: Silent Forest 的完整闭环。

请不要重写第一章系统。
请不要修改 Vuforia 包源码。
请不要引入新系统。
请只检查并适配多章节配置。

任务：
1. 确保 Chapter02Root 拥有自己的 PlayerAvatar、Nodes、Environment、Creatures。
2. 确保 Chapter02Root 下的 PlayerAvatar 使用 ARBookPlayerMover。
3. 确保第二章 PlayerMover 只收集 Chapter02Root 下的 Nodes。
4. 配置 Celebi 作为第二章主线精灵。
5. Celebi 初始 inactive。
6. 某个指定 Node 到达后触发 Celebi.SetActive(true)。
7. Celebi 使用 ARBookInteractable：
   - displayName = Celebi
   - captureId = Celebi
   - canBeCaptured = true
   - requirePlayerAtInteractionNode = true
8. 第二章终点 Node 调用 ARBookChapterCompletionTrigger.TryCompleteChapter。
9. ChapterCompletionTrigger 应支持：
   - chapterId = 2
   - requiredCaptureId = Celebi
   - completeDialogue = Chapter 2 is complete. Open Chapter 3.
10. 测试第二章流程：
   - 识别 chapter2
   - 移动到 Celebi 节点
   - Celebi 出现
   - 点击 InteractButton 显示 Celebi 对话
   - CaptureButton 出现
   - 收服 Celebi
   - 走到终点
   - 显示 Chapter 2 is complete. Open Chapter 3.
```

---

## 20. Agent Prompt：实现第五章最小结局

```text
你正在处理一个 Unity + Vuforia AR 冒险书项目。

当前项目已经有：
- 第一章收服 Pikachu 并完成章节
- 第二章计划收服 Celebi 并完成章节
- ARBookChapterProgress 可以保存 ChapterCompleted_X 和 MemoryFragment_X
- DialogueManager 可以显示多句对白
- ChapterCompletionTrigger 可以播放章节完成粒子效果

现在需要实现 Chapter 5: Rift Ruins 的最小结局闭环。

要求：
1. 不加入战斗系统。
2. 不加入 Boss 战。
3. 不加入自动寻路。
4. 不修改 Vuforia 脚本。
5. 不重写已有系统。

任务：
创建或扩展一个简单的 ARBookEndingController。

功能：
- 检查前置章节是否完成。
- 最小版本先检查：
  - ChapterCompleted_1
  - ChapterCompleted_2
- 如果未满足条件，显示：
  "The rift is still unstable. More memories are missing."
- 如果满足条件：
  - 播放 endingEffect
  - 显示 EndingPanel
  - 通过 DialogueManager 显示：
    "The lost fragments are gathered."
    "The rift is healed."
    "The book remembers its path again."
- 提供公共方法 TryPlayEnding()，用于绑定到 Chapter05 的终点 Node.onNodeReached。
- 使用英文变量名、注释、日志和 UI 字符串。
- 保持实现简单。

完成后说明：
1. 新增或修改了哪些脚本。
2. EndingPanel 如何配置。
3. endingEffect 如何配置。
4. Chapter05 终点 Node 如何绑定 TryPlayEnding。
5. 如何测试有/没有完成前置章节的两种情况。
```

---

## 21. 最小可演示版本标准

当前最小可演示版本目标为：

```text
1. 扫描 cover.jpg
2. 封面出现粒子特效和 Open Chapter 1 提示
3. 翻到 chapter1.jpg
4. 第一章地图出现
5. 玩家移动到 Node_06 与导师对话
6. 玩家移动到 Node_09 触发 Pikachu 出现
7. 与 Pikachu 对话并收服
8. 玩家移动到终点完成 Chapter 1
9. 翻到 chapter2.jpg
10. 第二章地图出现
11. 收服 Celebi
12. 完成 Chapter 2
13. 翻到 chapter5.jpg
14. 触发最终裂隙修复
15. 显示 Adventure Complete
```

这个版本已经足够作为大作业演示基础。

---

## 22. 完整版本目标

完整版本可以继续补：

```text
1. Chapter 1 到 Chapter 5 全部有独立地图
2. 每章都有主线精灵
3. 每章都有支线精灵对白
4. 每章完成后获得 MemoryFragment
5. Chapter 5 检查 Chapter 1 到 Chapter 4 全部完成后再结局
6. 最后展示收服过的精灵列表
7. 加入背景音乐和粒子反馈
8. 优化 UI 风格
9. 真机横屏演示稳定
```

---

## 23. 演示视频建议流程

视频建议控制在 2 到 4 分钟。

推荐流程：

```text
1. 展示实体书封面
2. 打开 APK
3. 扫描 cover.jpg
4. 封面出现粒子特效和 Open Chapter 1
5. 翻到 chapter1.jpg
6. 第一章地图出现
7. 点击节点移动
8. 到 Node_06，与导师对话
9. 到 Node_09，Pikachu 出现
10. 与 Pikachu 对话并收服
11. 到终点，Chapter 1 complete
12. 翻到 chapter2.jpg
13. 展示第二章地图和 Celebi 收服
14. 翻到 chapter5.jpg
15. 触发最终修复
16. 显示 Adventure Complete
```

---

## 24. 实验报告可强调内容

报告中可以重点写：

```text
1. 项目从普通 AR 模型展示 Demo 改造为 AR 冒险书应用。
2. 实体书封面和书页作为现实识别目标。
3. 封面只负责开场提示，翻页负责章节切换。
4. 章节页不只是显示模型，而是承载可移动节点地图。
5. 玩家移动逻辑由 Unity 中的 Node 控制，不依赖平面图片。
6. NPC 和精灵共用 ARBookInteractable 交互脚本。
7. 对话采用碎片化叙事方式。
8. 收服状态和章节进度通过 PlayerPrefs 保存。
9. 多章节共用 Managers，但每章拥有独立 ChapterRoot、Nodes 和 PlayerAvatar。
10. 项目兼顾了 AR 识别稳定性、手机屏幕可读性和课程项目工作量。
```

---

## 25. 当前下一步任务清单

```text
[ ] 1. 完整跑通 Chapter 1
[ ] 2. 完整跑通 Chapter 2
[ ] 3. 实现 Chapter 5 最小结局
[ ] 4. 复制模板补 Chapter 3
[ ] 5. 复制模板补 Chapter 4
[ ] 6. 每章补充主线精灵对白
[ ] 7. 每章补充主线精灵收服
[ ] 8. 每章终点绑定章节完成检测
[ ] 9. 检查所有章节的 PlayerMover Route Root
[ ] 10. 检查所有章节的 Interactable interactionNode
[ ] 11. 检查所有 captureId 是否唯一
[ ] 12. 正式演示前关闭 clearOnStartForDebug
[ ] 13. 真机测试 Vuforia 识别稳定性
[ ] 14. 真机测试 UI 按钮点击
[ ] 15. 真机测试章节完成和收服保存
[ ] 16. Android 打包
[ ] 17. 录制演示视频
[ ] 18. 编写实验报告
```

---

## 26. 当前项目状态总结

当前项目已经具备：

```text
封面识别
封面粒子提示
第一章 AR 地图
第二章多章节适配基础
节点移动
导师交互
精灵对白
收服系统
章节进度
章节完成检测
章节完成粒子效果
多章节共享 Managers
```

下一阶段核心目标是：

```text
把第一章原型复制成完整章节结构，
至少完成 Chapter 1、Chapter 2 和 Chapter 5，
让项目具备“封面入口—章节探索—最终结局”的完整演示闭环。
```

最终项目应呈现为：

```text
一本真实实体书
一套 AR 章节地图
一个可移动玩家角色
多个可交互精灵
一套碎片化叙事对白
一个收服与记忆修复系统
一个最终裂隙修复结局
```
