# AR Spirit Adventure Book 项目规划 README

## 1. 项目定位

本项目是一个基于 Unity 与 Vuforia 的移动端 AR 冒险书应用。项目不再以“多张图片识别后展示单个模型”为最终目标，而是改造为一本可交互的 AR 精灵冒险书。

用户首先扫描实体书封面 `cover.jpg`，激活冒险书；随后翻开实体书页，依次识别 `chapter1.jpg` 到 `chapter5.jpg`。每一页代表一个章节地图，Unity 会在对应书页上叠加显示 3D 弹出式地图、路径节点、精灵模型、剧情碎片、交互机关和章节任务。

最终体验目标是：

> 用户像翻阅一本真实冒险书一样，通过扫描封面、翻页、点击精灵、触发书页机关、阅读记忆碎片和收服精灵，逐步探索书中世界并完成最终裂隙修复。

---

## 2. 当前项目状态

当前核心场景为：

```text
Assets/Scenes/PokemonGame.unity
```

当前项目已经完成：

```text
1. Vuforia ARCamera 配置
2. Vuforia Database 导入
3. 多 ImageTarget 识别
4. ImageTarget 识别后显示模型
5. ImageTarget 丢失后隐藏模型
6. DefaultObserverEventHandler 批量绑定
7. StatusFilter 设置为 Tracked
8. 模型缩放与适配工具
9. 角色/宠物变体切换脚本
10. Vuforia 本地包依赖接入
```

当前新导入的 AR 冒险书识别图包括：

```text
cover.jpg
chapter1.jpg
chapter2.jpg
chapter3.jpg
chapter4.jpg
chapter5.jpg
```

这些图片已经导入 Vuforia Database，并且已经导入 Unity 项目。

---

## 3. 最终应用结构

### 3.1 整体流程

```text
启动应用
  ↓
扫描 cover.jpg
  ↓
封面出现 AR 激活动画
  ↓
提示用户打开第一章
  ↓
扫描 chapter1.jpg
  ↓
第一章地图弹出
  ↓
小人沿地图路径移动
  ↓
点击精灵触发记忆碎片对话
  ↓
收服主线精灵
  ↓
获得章节记忆碎片
  ↓
翻页进入下一章
  ↓
重复章节探索
  ↓
扫描 chapter5.jpg
  ↓
触发最终裂隙修复
  ↓
显示通关结果和图鉴
```

### 3.2 核心交互方式

本项目采用三类交互方式：

| 交互类型                   | 作用           | 使用场景                                        |
| ---------------------- | ------------ | ------------------------------------------- |
| Vuforia ImageTarget    | 识别封面和章节页     | `cover.jpg`、`chapter1.jpg` 到 `chapter5.jpg` |
| Vuforia Virtual Button | 书页上的实体机关     | 激活封面、打开封印、修复裂隙                              |
| Unity Raycast 点击 AR 模型 | 点击精灵、节点、记忆碎片 | 精灵对话、节点移动、收集物触发                             |
| Unity 屏幕 UI 按钮         | 稳定执行核心操作     | Continue、Capture、Back、Collection            |

设计原则：

```text
1. 书页识别负责“进入章节”
2. 虚拟按钮负责“按下书中的机关”
3. AR 点击负责“与书中世界互动”
4. 屏幕按钮负责“保证核心流程稳定”
```

---

## 4. 项目改造总原则

当前项目有大量以宝可梦名称命名的 ImageTarget，但最终版本不再让每只精灵都对应一张单独识别图。

新的逻辑是：

```text
旧逻辑：
Pikachu ImageTarget → Pikachu 模型
Mew ImageTarget → Mew 模型
Squirtle ImageTarget → Squirtle 模型

新逻辑：
chapter1.jpg → 第一章地图 → Pikachu / Meowth / Squirtle 作为章节内精灵
chapter2.jpg → 第二章地图 → Bulbasaur / Celebi / Zarude / Zorua 作为章节内精灵
...
```

也就是说：

> ImageTarget 从“角色卡片”升级为“章节书页”，精灵模型变成章节地图中的可交互对象。

---

## 5. 推荐场景改造方式

不要直接在原始场景上大改，先复制一份场景：

```text
Assets/Scenes/PokemonGame.unity
复制为：
Assets/Scenes/PokemonGame_ARBook.unity
```

后续所有 AR 冒险书相关修改都在：

```text
PokemonGame_ARBook.unity
```

中完成。

原场景保留，作为旧版展示 Demo 备份。

---

## 6. 推荐 Unity 层级结构

最终场景建议整理为：

```text
PokemonGame_ARBook
├── ARCamera
├── Directional Light
├── Canvas
│   ├── DialoguePanel
│   ├── TaskPanel
│   ├── ActionButtons
│   ├── CollectionPanel
│   └── DebugPanel
│
├── ImageTargets
│   ├── CoverTarget
│   │   └── CoverRoot
│   │       ├── CoverMagicEffect
│   │       ├── CoverTitleEffect
│   │       └── CoverVirtualButtons
│   │
│   ├── Chapter01Target
│   │   └── Chapter01Root
│   │       ├── GroundBase
│   │       ├── Nodes
│   │       ├── RouteVisual
│   │       ├── PlayerAvatar
│   │       ├── Environment
│   │       ├── Creatures
│   │       ├── ChapterEffects
│   │       └── VirtualButtons
│   │
│   ├── Chapter02Target
│   │   └── Chapter02Root
│   │
│   ├── Chapter03Target
│   │   └── Chapter03Root
│   │
│   ├── Chapter04Target
│   │   └── Chapter04Root
│   │
│   └── Chapter05Target
│       └── Chapter05Root
│
└── Managers
    ├── ARBookGameManager
    ├── DialogueManager
    ├── CollectionManager
    ├── UIManager
    └── ARTapRaycaster
```

注意：

```text
每个 ImageTarget 下最好只有一个章节根节点 Root。
DefaultObserverEventHandler 只负责显示/隐藏这个 Root。
Root 内部再管理该章节的地图、节点、精灵和机关。
```

这样可以继续复用你已经完成的 ImageTarget 显示/隐藏绑定逻辑。

---

## 7. 章节规划

### 7.1 封面：Book Cover

识别图：

```text
cover.jpg
```

功能：

```text
1. 识别封面
2. 显示魔法阵或发光特效
3. 通过 Vuforia Virtual Button 激活冒险书
4. 显示提示：Open Chapter 1
```

封面虚拟按钮建议：

```text
Cover_ActivateBook
```

触发效果：

```text
1. CoverMagicEffect.SetActive(true)
2. 播放激活动画
3. 显示提示文本
4. 允许用户翻到第一章
```

---

### 7.2 第一章：Torn Camp

识别图：

```text
chapter1.jpg
```

章节主题：

```text
残破营地 / 新手教程 / 冒险开始
```

精灵分配：

```text
Pikachu      主线精灵
Meowth       支线记忆精灵
Squirtle     支线记忆精灵
```

主要功能：

```text
1. 地图弹出
2. 小人出现在起点
3. 显示基础教程
4. 玩家点击节点，小人沿路线移动
5. 到达指定节点后，Pikachu 出现
6. 点击 Pikachu 显示记忆碎片
7. 收服 Pikachu
8. 获得第一枚 Memory Fragment
9. 提示翻到第二章
```

章节虚拟按钮建议：

```text
Camp_SummonGuide
```

触发效果：

```text
1. 唤醒引导残影
2. 显示第一段教程对话
3. 激活第一个可移动节点
```

---

### 7.3 第二章：Silent Forest

识别图：

```text
chapter2.jpg
```

章节主题：

```text
静默森林 / 自然契约 / 失落记忆
```

精灵分配：

```text
Bulbasaur    支线记忆精灵
Celebi       主线精灵
Zarude       支线记忆精灵
Zorua        支线记忆精灵
```

主要功能：

```text
1. 森林地图弹出
2. 玩家沿路径探索森林节点
3. 点击支线精灵读取碎片化对白
4. 按下树叶机关解除森林封印
5. Celebi 出现
6. 收服 Celebi
7. 获得森林 Memory Fragment
```

章节虚拟按钮建议：

```text
Forest_OpenSeal
```

---

### 7.4 第三章：Ashen Volcano

识别图：

```text
chapter3.jpg
```

章节主题：

```text
灰烬火山 / 暴走能量 / 誓言残响
```

精灵分配：

```text
Infernape    主线精灵
Toxtricity   支线记忆精灵
Sneasler     支线记忆精灵
Axew         支线记忆精灵
```

主要功能：

```text
1. 火山地图弹出
2. 玩家通过岩浆路径节点
3. 支线精灵提供火山区域记忆碎片
4. 按下火焰机关稳定裂隙
5. Infernape 从火山核心附近出现
6. 安抚并收服 Infernape
7. 获得火山 Memory Fragment
```

章节虚拟按钮建议：

```text
Volcano_StabilizeRift
```

---

### 7.5 第四章：Sunken Lake

识别图：

```text
chapter4.jpg
```

章节主题：

```text
沉没湖畔 / 记忆回声 / 治愈与愿望
```

精灵分配：

```text
Manaphy      主线精灵
Jirachi      支线记忆精灵
Electrode    支线机关精灵
```

主要功能：

```text
1. 湖泊地图弹出
2. 玩家沿水上节点移动
3. 点击湖面记忆点触发对白
4. 按下水滴机关唤醒湖中记忆
5. Manaphy 出现
6. 通过 Feed / Heal 交互恢复 Manaphy
7. 收服 Manaphy
8. 获得湖泊 Memory Fragment
```

章节虚拟按钮建议：

```text
Lake_AwakenMemory
```

---

### 7.6 第五章：Rift Ruins

识别图：

```text
chapter5.jpg
```

章节主题：

```text
裂隙遗迹 / 最终修复 / 世界记忆
```

精灵分配：

```text
Zekrom            主线精灵
Mew               结局记忆精灵
Mismagius         支线记忆精灵
Scizor            遗迹守卫精灵
Dragapult         支线记忆精灵
GalarianZapdos    支线记忆精灵
```

主要功能：

```text
1. 遗迹地图弹出
2. 玩家探索遗迹路径
3. 点击支线精灵读取最终记忆碎片
4. 检查前四章 Memory Fragment 是否已获得
5. 按下裂隙核心机关
6. Zekrom 出现
7. 完成最终收服或稳定
8. 所有碎片汇聚到 Rift Core
9. 裂隙关闭
10. 显示 Ending Panel
```

章节虚拟按钮建议：

```text
Ruins_RepairCore
```

---

## 8. 地图与 3D 场景实现原则

不要尝试把生成的平面地图图片完整 3D 化。这样会导致重新建模，工作量过大。

正确做法是：

```text
书页图片：负责识别、背景氛围、视觉底图
Unity 3D 物体：负责真实可交互地图、节点、路径、精灵、机关
```

也就是说：

```text
chapter1.jpg 是 Vuforia 识别目标和视觉背景
Chapter01Root 里的 3D Nodes / Props / Creatures 才是真正可玩的内容
```

推荐每章制作小型 3D 弹出式棋盘：

```text
1. 一个低矮 GroundBase
2. 10-12 个路径节点
3. 1 条主路径
4. 1-2 个支线路径
5. 3-5 个精灵
6. 1 个章节虚拟按钮机关
7. 1 个章节终点
```

---

## 9. 每章 3D 资源需求

资源原则：

```text
不从零建模
优先使用现成低模资源
必要时用 Unity 基础几何体搭建
风格保持卡通、低模、明亮、治愈
```

### 9.1 通用资源

所有章节都可以共用：

```text
PathNode 圆形节点
RouteLine 路径线
PlayerAvatar 小人
DialogueBubble 对话框
MemoryFragment 记忆碎片
SpiritWisp 灵光
RuneStone 符文石
CrystalTotem 水晶柱
PortalGate 传送门
```

### 9.2 第一章资源

```text
Tent
Campfire
WoodenCrate
WoodenFence
PineTree
Rock
BrokenCart
SmallBridge
PortalGate
```

### 9.3 第二章资源

```text
Tree
Mushroom
TreeStump
ForestTotem
LeafShrine
StoneArch
Stream
ForestHut
```

### 9.4 第三章资源

```text
RockPlatform
LavaPlane
CrystalCluster
ForgeDoor
WoodenBridge
FireParticle
FlameShrine
```

### 9.5 第四章资源

```text
WaterPlane
LilyPad
Reed
Dock
SteppingStone
WillowTree
WaterGate
LotusShrine
```

### 9.6 第五章资源

```text
BrokenColumn
RuinsPlatform
CrystalCluster
RiftPortal
RuneCircle
FloatingRock
RopeBridge
AncientGate
```

---

## 10. 推荐新增脚本目录

新增脚本建议放在：

```text
Assets/Scripts/ARBook/
```

推荐脚本：

```text
ARBookGameManager.cs
BookPageController.cs
ChapterController.cs
ARBookMapNode.cs
ARBookPathController.cs
ARBookPlayerMover.cs
ARBookNodeEvent.cs
CreatureProfile.cs
CreatureInteraction.cs
ARTapRaycaster.cs
DialogueManager.cs
CollectionManager.cs
BookVirtualButtonTrigger.cs
BillboardToCamera.cs
```

---

## 11. 核心脚本职责

### 11.1 ARBookGameManager.cs

负责全局流程：

```text
1. 当前章节
2. 当前任务状态
3. 是否已激活封面
4. 已获得的 Memory Fragment
5. 是否允许进入最终章节
6. 游戏结束状态
```

---

### 11.2 BookPageController.cs

挂在每个章节 Root 上。

负责：

```text
1. 页面被识别后初始化章节
2. 页面丢失后暂停章节显示
3. 管理 ChapterRoot 的显示/隐藏
4. 调用 ChapterController
```

---

### 11.3 ChapterController.cs

每一章一个。

负责：

```text
1. 章节编号
2. 章节标题
3. 本章精灵列表
4. 本章路径节点
5. 本章主线精灵
6. 本章是否完成
7. 本章虚拟按钮事件响应
```

---

### 11.4 ARBookMapNode.cs

挂在每个路径节点上。

字段建议：

```text
nodeIndex
nodeName
isUnlocked
isSideNode
OnNodeReached
```

---

### 11.5 ARBookPathController.cs

负责路径顺序。

功能：

```text
1. 保存节点列表
2. 获取当前节点
3. 获取目标节点
4. 返回从当前节点到目标节点的移动路径
```

不需要复杂寻路。
目前只做线性路径或简单分支即可。

---

### 11.6 ARBookPlayerMover.cs

负责小人沿节点移动。

功能：

```text
1. 点击节点后移动
2. 从一个节点平滑移动到下一个节点
3. 移动时朝向前进方向
4. 到达节点后触发节点事件
```

---

### 11.7 CreatureProfile.cs

用于配置精灵信息。

字段建议：

```text
creatureName
elementType
chapterId
isMainCreature
canBeCaptured
dialogueFragments
capturedDialogue
animationTriggerName
```

---

### 11.8 CreatureInteraction.cs

挂在每个精灵模型上。

功能：

```text
1. 被点击时触发 Interact()
2. 面向摄像机
3. 播放互动动画
4. 显示下一句记忆碎片对白
5. 可收服时显示 Capture 按钮
6. 收服后写入 CollectionManager
```

---

### 11.9 ARTapRaycaster.cs

挂在全局 Manager 上。

功能：

```text
1. 检测屏幕点击
2. 从 Camera.main 发射 Raycast
3. 判断是否点中 CreatureInteraction
4. 判断是否点中 ARBookMapNode
5. 调用对应交互方法
```

---

### 11.10 DialogueManager.cs

负责显示对白。

功能：

```text
1. 显示普通对白
2. 显示精灵记忆碎片
3. 显示章节提示
4. 控制 Continue 按钮
```

---

### 11.11 CollectionManager.cs

负责图鉴和收服记录。

功能：

```text
1. 保存已收服精灵
2. 使用 PlayerPrefs 记录状态
3. 查询某精灵是否已收服
4. 统计收服数量
5. 最终结局展示已收服精灵
```

---

### 11.12 BookVirtualButtonTrigger.cs

负责 Vuforia Virtual Button。

功能：

```text
1. 监听虚拟按钮按下
2. 根据 eventId 调用章节事件
3. 支持 UnityEvent 绑定
4. 用于封面激活、章节机关、最终修复
```

---

### 11.13 BillboardToCamera.cs

用于对话和聚焦模式。

功能：

```text
1. 让精灵或 NPC 在对话时朝向摄像机
2. 避免俯拍时只看到模型背面或顶部
3. 提高移动端观看体验
```

---

## 12. UI 系统规划

Canvas 建议使用 Screen Space - Overlay。

推荐 UI：

```text
Canvas
├── DialoguePanel
│   ├── SpeakerNameText
│   ├── DialogueText
│   └── ContinueButton
│
├── TaskPanel
│   ├── ChapterTitleText
│   └── TaskHintText
│
├── ActionButtons
│   ├── CalmButton
│   ├── FeedButton
│   ├── CaptureButton
│   └── BackButton
│
├── CollectionPanel
│   ├── CollectionTitle
│   └── CreatureList
│
└── DebugPanel
    └── StatusText
```

核心按钮：

```text
Continue
Calm
Feed
Capture
Collection
Back
```

UI 原则：

```text
1. 大段剧情不要放在 AR 空间里
2. 精灵名字和短提示可以跟随模型
3. 正式对白放在屏幕 DialoguePanel 中
4. 核心操作按钮固定在屏幕下方或右侧
```

---

## 13. 碎片化叙事设计

本项目采用碎片化叙事，不做长篇连续剧情。

故事背景：

```text
这本冒险书曾经连接着一个完整的精灵世界。
某次裂隙灾变后，书页中的世界破碎成不同章节。
每只精灵都保留了一段残缺记忆。
用户通过翻页、触摸机关、点击精灵和收服主线精灵，逐步恢复书中世界。
```

叙事来源：

```text
1. 精灵对话碎片
2. 章节机关提示
3. 节点触发文本
4. 收服后的记忆奖励
5. 最终裂隙修复文本
```

每只精灵建议配置 2-3 句短对白：

```text
第一次点击：环境记忆
第二次点击：灾变线索
收服后：关键提示或祝福
```

示例风格：

```text
“The path remembers every footstep, even the ones that never returned.”

“The forest did not fall. It only learned to whisper.”

“Take this fragment. It still knows the way home.”
```

注意：

```text
对白保持简短、神秘、温和。
不要写成长篇解释。
不要一次性讲完整故事。
```

---

## 14. 每章主线目标

| 章节        | 主线目标                    |
| --------- | ----------------------- |
| Cover     | 激活冒险书                   |
| Chapter 1 | 学会移动、点击、读取对白、收服 Pikachu |
| Chapter 2 | 打开森林封印，收服 Celebi        |
| Chapter 3 | 稳定火山裂隙，收服 Infernape     |
| Chapter 4 | 唤醒湖中记忆，收服 Manaphy       |
| Chapter 5 | 集齐碎片，稳定 Zekrom，修复最终裂隙   |

---

## 15. 具体实施阶段

### 阶段 0：备份与整理

任务：

```text
1. 复制 PokemonGame.unity 为 PokemonGame_ARBook.unity
2. 保留旧 ImageTarget，不删除
3. 新建空物体 ImageTargets，统一管理书页 ImageTarget
4. 新建 Managers 空物体
5. 新建 Assets/Scripts/ARBook/
6. 新建 Assets/Prefabs/ARBook/
7. 新建 Assets/Materials/ARBook/
```

检查点：

```text
打开 PokemonGame_ARBook.unity 不报错。
旧 Demo 仍然保留。
新目录结构清晰。
```

---

### 阶段 1：确认 Vuforia 书页识别

任务：

```text
1. 在场景中创建 CoverTarget
2. 绑定 cover.jpg 对应的 Vuforia Image Target
3. 创建 Chapter01Target 到 Chapter05Target
4. 分别绑定 chapter1.jpg 到 chapter5.jpg
5. 每个 Target 下创建一个 Root 子物体
6. 使用 DefaultObserverEventHandler 控制 Root 显示/隐藏
7. 确保 StatusFilter 为 Tracked
```

检查点：

```text
扫描 cover.jpg → CoverRoot 显示
移开封面 → CoverRoot 隐藏

扫描 chapter1.jpg → Chapter01Root 显示
移开页面 → Chapter01Root 隐藏
```

---

### 阶段 2：完成封面激活

任务：

```text
1. 在 CoverRoot 下添加简单魔法阵或粒子特效
2. 添加 Cover_ActivateBook 虚拟按钮
3. 按下虚拟按钮后显示激活提示
4. UI 提示用户打开第一章
```

检查点：

```text
扫描封面后不会直接进入游戏。
必须按下封面机关后，才显示 “Open Chapter 1”。
```

---

### 阶段 3：完成第一章 3D 地图样板

任务：

```text
1. 在 Chapter01Root 下创建 GroundBase
2. 创建 10-12 个节点 Node_01 到 Node_12
3. 创建 1-2 个支线节点 SideNode_01 / SideNode_02
4. 创建 RouteVisual
5. 放置 PlayerAvatar
6. 放置少量营地资源：帐篷、篝火、树、箱子、传送门
7. 放置 Pikachu / Meowth / Squirtle
8. 初始隐藏精灵，节点触发后显示
```

检查点：

```text
第一章识别后，书页上出现一个小型 3D 营地棋盘。
路线节点清楚。
小人位置清楚。
精灵不会一开始全部挤在屏幕里。
```

---

### 阶段 4：实现节点移动

任务：

```text
1. 创建 ARBookMapNode.cs
2. 创建 ARBookPathController.cs
3. 创建 ARBookPlayerMover.cs
4. 创建 ARBookNodeEvent.cs
5. 点击节点后，小人移动到该节点
6. 到达节点后触发事件
```

检查点：

```text
点击 Node_03 → 小人从当前节点移动到 Node_03
到达 Node_03 → 触发 Node_03 的 UnityEvent
```

---

### 阶段 5：实现精灵点击交互

任务：

```text
1. 创建 CreatureProfile.cs
2. 创建 CreatureInteraction.cs
3. 创建 ARTapRaycaster.cs
4. 给每只精灵添加 Collider
5. 点击精灵后显示对白
6. 精灵对话时朝向摄像机
7. 精灵播放互动动画
```

检查点：

```text
点击 Pikachu → 显示 Pikachu 的第一句记忆碎片
再次点击 Pikachu → 显示第二句记忆碎片
点击 Meowth / Squirtle → 分别显示自己的对白
```

---

### 阶段 6：实现收服与图鉴

任务：

```text
1. 创建 CollectionManager.cs
2. 实现 Capture 按钮
3. 主线精灵可以被收服
4. 收服后写入 PlayerPrefs
5. 收服后显示 Captured 状态
6. 图鉴界面显示已收服精灵
```

检查点：

```text
点击 Pikachu → 出现 Capture 按钮
点击 Capture → Pikachu 标记为已收服
打开 Collection → 能看到 Pikachu 已解锁
```

---

### 阶段 7：复制章节模板

在第一章功能跑通后，再复制结构到其他章节。

顺序：

```text
1. Chapter02Root
2. Chapter03Root
3. Chapter04Root
4. Chapter05Root
```

不要一开始同时做五章。
先让第一章完整闭环，再复制模板。

---

### 阶段 8：完善最终结局

任务：

```text
1. Chapter05Root 中放置 RiftCore
2. 检查前四章 Memory Fragment
3. 按下 Ruins_RepairCore 虚拟按钮
4. 触发能量汇聚动画
5. 显示 EndingPanel
6. 展示已收服精灵数量
```

检查点：

```text
前四章完成后，遗迹页可以触发最终修复。
最终修复后显示通关文字。
```

---

### 阶段 9：移动端测试

任务：

```text
1. Android Build Settings
2. Camera Permission
3. Vuforia License Key
4. Minimum API Level
5. 横屏/竖屏方向确认
6. 真机测试识别稳定性
7. 测试遮挡、晃动、光线变化
```

建议：

```text
本项目更适合横屏演示。
因为章节地图是横版打开书页结构。
```

检查点：

```text
APK 安装后能打开摄像头。
扫描封面和章节页能识别。
点击 UI 和 AR 精灵正常。
```

---

## 16. 开发优先级

### 必须完成

```text
1. 封面识别
2. 第一章识别
3. 第一章 3D 地图
4. 小人节点移动
5. 精灵点击对白
6. Pikachu 收服
7. 至少 Chapter 2 和 Chapter 5 可识别展示
8. 最终结局页面
9. Android APK
10. 演示视频
```

### 重要增强

```text
1. 五个章节全部可识别
2. 所有精灵可点击对白
3. 每章主线精灵可收服
4. Vuforia 虚拟按钮机关
5. 图鉴系统
```

### 可选加分

```text
1. 粒子特效
2. 背景音乐
3. 节点解锁动画
4. 精灵聚焦模式
5. 拍照模式
6. 收服失败与再次尝试
```

### 不建议做

```text
1. 大范围现实空间走动
2. SLAM 地面检测
3. 复杂战斗系统
4. 背包系统
5. 联网功能
6. 每只精灵独立任务线
7. 重新完整建模地图
```

---

## 17. Agent 工作规范

后续需要 agent 协助时，不要一次性要求它完成整个游戏。
每次只让它完成一个小模块。

要求 agent 遵守：

```text
1. 不修改 Vuforia 包源码
2. 不删除现有 ImageTarget
3. 新脚本统一放在 Assets/Scripts/ARBook/
4. 不引入第三方依赖
5. 代码变量、注释、日志、UI 字符串使用英文
6. 每次说明新增/修改了哪些文件
7. 每次说明 Unity Inspector 如何挂载脚本
8. 每次说明如何测试
9. 保持实现简单，不做过度设计
```

---

## 18. Agent Prompt 模板一：创建 ARBook 核心脚本

```text
You are working on an existing Unity + Vuforia AR project.

Project context:
- Main scene: Assets/Scenes/PokemonGame_ARBook.unity
- Vuforia is already installed and working.
- The project already has ARCamera, ImageTargets, DefaultObserverEventHandler, and Pokémon models.
- The new design is an AR adventure book.
- cover.jpg and chapter1.jpg to chapter5.jpg have already been imported into a Vuforia database.
- Each chapter page is a Vuforia ImageTarget.
- The printed image is only the recognition target and background.
- The playable map should be built in Unity as a 3D pop-up board map on top of each page.

Task:
Create the core runtime framework under:
Assets/Scripts/ARBook/

Required scripts:
1. ARBookGameManager.cs
2. BookPageController.cs
3. ChapterController.cs
4. ARBookMapNode.cs
5. ARBookPathController.cs
6. ARBookPlayerMover.cs
7. ARBookNodeEvent.cs

Functional requirements:
- BookPageController manages a page root object.
- ChapterController stores chapter id, title, completion state, and chapter events.
- ARBookMapNode represents one route node.
- ARBookPathController stores ordered nodes.
- ARBookPlayerMover moves a player avatar smoothly from node to node.
- ARBookNodeEvent exposes UnityEvents for OnNodeReached.
- Keep the system simple and Inspector-friendly.
- Do not use NavMesh.
- Do not introduce external packages.
- Do not modify Vuforia package files.

After coding, explain:
1. Which files were created.
2. How to attach the scripts in Unity.
3. How to create 10 nodes manually.
4. How to test node movement.
```

---

## 19. Agent Prompt 模板二：实现精灵点击与对白

```text
You are working on an existing Unity + Vuforia AR adventure book project.

Task:
Create a reusable creature interaction system.

Create scripts under:
Assets/Scripts/ARBook/

Required scripts:
1. CreatureProfile.cs
2. CreatureInteraction.cs
3. ARTapRaycaster.cs
4. DialogueManager.cs
5. BillboardToCamera.cs

Functional requirements:
- CreatureProfile stores creatureName, elementType, chapterId, dialogueFragments, capturedDialogue, isMainCreature, canBeCaptured, and animationTriggerName.
- CreatureInteraction is attached to each creature model.
- When tapped, the creature should face the camera, optionally trigger an Animator parameter, and show the next dialogue fragment.
- ARTapRaycaster uses Physics.Raycast from Camera.main to detect tapped creatures.
- DialogueManager displays dialogue using Unity UI Text. Do not require TextMeshPro unless already present.
- BillboardToCamera makes the creature face the main camera during dialogue.

Constraints:
- Do not modify existing Vuforia scripts.
- Do not delete existing ImageTargets.
- Use English code names, comments, logs, and UI strings.
- Keep the system simple and easy to configure in Inspector.

After implementation, explain:
1. How to add Collider to each creature.
2. How to configure dialogue fragments.
3. How to test by tapping Pikachu.
```

---

## 20. Agent Prompt 模板三：实现 Vuforia 虚拟按钮机关

```text
You are working on a Unity + Vuforia AR adventure book project.

Vuforia version:
- Vuforia Engine 11.4.4

Task:
Add support for Vuforia Virtual Buttons used as physical book-page mechanisms.

Create:
Assets/Scripts/ARBook/BookVirtualButtonTrigger.cs

Functional requirements:
- The script should work with the installed Vuforia version.
- It should expose eventId in the Inspector.
- It should support UnityEvents for OnPressed and OnReleased.
- It should log virtual button name and eventId.
- It should call ChapterController or ARBookGameManager when pressed.
- It should be safe if optional references are missing.

Example eventIds:
- Cover_ActivateBook
- Camp_SummonGuide
- Forest_OpenSeal
- Volcano_StabilizeRift
- Lake_AwakenMemory
- Ruins_RepairCore

Constraints:
- Do not modify Vuforia package files.
- Do not replace DefaultObserverEventHandler.
- Do not change current ImageTarget found/lost behavior.
- Keep the implementation minimal and readable.

After coding, explain:
1. How to create a Virtual Button under an ImageTarget.
2. How to attach BookVirtualButtonTrigger.
3. How to bind a UnityEvent to activate an object or play an effect.
4. How to test the button on a printed page.
```

---

## 21. 最小可演示版本目标

最小可演示版本不要求五章全部完整，但必须有完整闭环。

最低完成标准：

```text
1. 扫描 cover.jpg
2. 按下封面虚拟按钮
3. 显示冒险书激活
4. 翻到 chapter1.jpg
5. 第一章 3D 地图出现
6. 小人可以沿节点移动
7. 点击 Pikachu 显示对白
8. Pikachu 可以收服
9. 打开 Collection 能看到 Pikachu
10. 翻到 chapter5.jpg
11. 触发最终裂隙修复
12. 显示 EndingPanel
```

完整版本目标：

```text
1. cover + chapter1-5 全部可识别
2. 每章都有 3D 弹出地图
3. 每章都有 10-12 个节点
4. 所有精灵都能点击对话
5. 每章主线精灵可以收服
6. 每章有一个虚拟按钮机关
7. 最后一章可以修复裂隙
8. 图鉴记录所有已收服精灵
```

---

## 22. 演示视频建议流程

视频建议控制在 2-4 分钟。

演示顺序：

```text
1. 展示实体书封面
2. 手机打开 APK
3. 扫描 cover.jpg
4. 封面出现激活动画
5. 按下封面虚拟按钮
6. 提示打开第一章
7. 翻到 chapter1.jpg
8. 第一章地图弹出
9. 点击节点，小人移动
10. 点击 Pikachu，显示记忆碎片对白
11. 收服 Pikachu
12. 翻到 chapter2.jpg，展示森林地图
13. 翻到 chapter3.jpg，展示火山地图
14. 翻到 chapter4.jpg，展示湖泊地图
15. 翻到 chapter5.jpg
16. 按下裂隙核心虚拟按钮
17. 显示最终修复
18. 展示图鉴或通关界面
```

---

## 23. 实验报告重点

报告中应突出：

```text
1. 实体书作为 AR 交互载体
2. 封面识别作为应用入口
3. 翻页作为章节切换方式
4. 书页地图作为 AR 空间锚点
5. Vuforia 虚拟按钮作为书页机关
6. 精灵点击与碎片化叙事
7. 3D 弹出式地图增强沉浸感
8. 移动端手持相机下的显示与交互设计
```

可以强调：

> 本项目不是简单的图片识别模型展示，而是将实体书、章节地图、AR 角色、路径节点、机关按钮和碎片化剧情结合起来，形成具有完整体验流程的 AR 冒险书应用。

---

## 24. 当前下一步任务清单

现在从这里开始做：

```text
[ ] 1. 复制 PokemonGame.unity 为 PokemonGame_ARBook.unity
[ ] 2. 在新场景中创建 CoverTarget 和 Chapter01Target- Chapter05Target
[ ] 3. 给每个 Target 绑定对应的 cover/chapter 图片
[ ] 4. 每个 Target 下创建唯一 Root 子物体
[ ] 5. 确认识别后 Root 显示，丢失后 Root 隐藏
[ ] 6. 先完成 CoverRoot 的激活动画
[ ] 7. 先完成 Chapter01Root 的 3D 地图样板
[ ] 8. 给 Chapter01Root 添加 10-12 个节点
[ ] 9. 放置 PlayerAvatar
[ ] 10. 实现节点点击移动
[ ] 11. 放置 Pikachu / Meowth / Squirtle
[ ] 12. 实现点击精灵显示对白
[ ] 13. 实现 Pikachu 收服
[ ] 14. 实现 CollectionManager
[ ] 15. 复制第一章模板到其他章节
[ ] 16. 接入 Vuforia Virtual Button
[ ] 17. 完成最终 Rift 修复
[ ] 18. 真机测试
[ ] 19. 打包 APK
[ ] 20. 录制演示视频
```

---

## 25. 最终目标总结

最终项目应从原来的：

```text
扫描图片 → 显示单个模型
```

升级为：

```text
扫描封面 → 激活冒险书
翻开章节 → 地图从书页中弹出
点击节点 → 小人在地图中移动
点击精灵 → 阅读记忆碎片
收服精灵 → 修复章节记忆
翻到终章 → 汇聚碎片修复裂隙
```

项目核心卖点是：

```text
AR 冒险书
实体翻页交互
3D 弹出式地图
Vuforia 虚拟按钮机关
所有精灵可点击对话
碎片化叙事
章节式探索
移动端完整演示
```
