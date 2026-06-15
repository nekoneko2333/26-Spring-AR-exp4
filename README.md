# mcfAR 当前待办

这里只记录当前还需要你配置的内容。已经做完的部分不再重复。

## 第三章：火山

目标流程：

1. 完成 `VolcanoSeal` 三步机关。
2. `SmokeRoot1` 消失。
3. `SmokeRoot2` 消失。
4. `MagmaRoot1` 消失。
5. `MagmaRoot2` 消失。
6. 阶梯出现，人物走到山顶收服 Infernape。

这里的“五步”是机关完成后的五段场景变化，不需要把
`Chapter03VolcanoPuzzle.requiredSteps` 从 `3` 改成 `5`。

### 1. 补阶梯物体

场景中已经有：

- `SmokeRoot1`
- `SmokeRoot2`
- `MagmaRoot1`
- `MagmaRoot2`

它们位于第三章火山模型的层级下，不需要重新创建。

现在只补下面三个物体：

1. 在第三章火山附近建立空物体 `StairRoot`。
2. 把阶梯的可见模型放进 `StairRoot`，并取消 `StairRoot` 的 Active。
3. 建立 `RouteBlocker` 放在阶梯入口。
4. 给 `RouteBlocker` 添加 `NavMeshObstacle` 并勾选 `Carve`。

### 2. 配置分阶段变化

1. 在 `Chapter03Root` 下建立空物体 `VolcanoResultSequence`。
2. 给它添加 `ARBookActivationSequence`。
3. `Steps` 数量设为 `5`。
4. Step 0：
   - `Delay = 0`
   - `Deactivate` 数量设为 `1`
   - 放入 `SmokeRoot1`
5. Step 1：
   - `Delay = 0.8`
   - `Deactivate` 数量设为 `1`
   - 放入 `SmokeRoot2`
6. Step 2：
   - `Delay = 1`
   - `Deactivate` 数量设为 `1`
   - 放入 `MagmaRoot1`
7. Step 3：
   - `Delay = 0.8`
   - `Deactivate` 数量设为 `1`
   - 放入 `MagmaRoot2`
8. Step 4：
   - `Delay = 0.8`
   - `Activate` 数量设为 `1`
   - 放入 `StairRoot`
   - `Deactivate` 数量设为 `1`
   - 放入 `RouteBlocker`
9. 选中 `Chapter03VolcanoPuzzle`。
10. 在 `ARSequenceTapChallenge > On Challenge Completed` 添加事件。
11. 拖入 `VolcanoResultSequence`，选择：

```text
ARBookActivationSequence -> Play()
```

进入 Play 模式后，可右键组件执行 `Play Sequence` 单独测试这段演出。

### 3. 上山路线

不需要制作真正的梯子。

先打开 Navigation 的 NavMesh 显示：

- 如果蓝色可行走区域已经从山脚连续到山顶，不需要增加任何路线模型。
- 如果蓝色区域在山脚断开，需要增加一条坡道。

最稳定的做法：

1. 用 Cube 或简单模型制作阶梯外观，并放进 `StairRoot`。
2. Bake 时临时启用 `StairRoot`。
3. 阶梯保留 Collider，并使用第三章 `NavMeshSurface` 收集的地面 Layer。
4. 重新 Bake。
5. 确认蓝色 NavMesh 从山脚沿阶梯连续覆盖到山顶。
6. Bake 完成后可以再次取消 `StairRoot` 的 Active。
7. 给 `RouteBlocker` 添加 `NavMeshObstacle`，勾选 `Carve`，让它在运行时切断阶梯入口。
8. 第五段演出显示 `StairRoot`，同时关闭 `RouteBlocker`。

注意：Bake 结果保存在 `NavMeshSurface` 的 NavMesh 数据里。禁用
`StairRoot` 只会隐藏阶梯模型和 Collider，不会删除已经 Bake 的蓝色可行走面。
因此不能只依赖禁用阶梯来阻止人物上山，必须由 `RouteBlocker` 在机关完成前
切断路线。

如果阶梯 Bake 后蓝色区域连续，就不需要平滑坡面。只有蓝色区域在台阶间断开时，
才在阶梯下面增加一个隐藏的平滑坡面作为备用行走面。

### 4. 限制 Infernape 捕捉

1. 选中第三章场景中的 Infernape。
2. 添加 `ARBookCaptureRequirement`。
3. `Required Challenge` 拖入 `Chapter03VolcanoPuzzle` 上的 `ARSequenceTapChallenge`。
4. 保持 `ARBookInteractable.canBeCaptured = true`。

这样机关完成前会阻止捕捉，完成后才允许捕捉。

## 其他仍需配置

### 第二章

1. 摆好组成图案的 3D 实体笔画。
2. 用 `ARViewAlignmentCalibrator` 保存正确观察角度。
3. `progressTMPText` 需要显示时绑定 Canvas 下的 `ChallengeText`。
4. 真机测试角度容差和稳定时间。

### 第四章

1. `LakeRune01/02/03` 的 `ARSequenceTapStep.challenge` 分别绑定 `Chapter04LakePuzzle`。
2. Manaphy 的 `ARBookInteractable.canBeCaptured` 改为 `true`。
3. 测试完成 `LakePath` 后 Manaphy 是否移动到岸边并允许捕捉。

### 第五章

1. 放置需要调查的遗迹细节和符号。
2. 决定最终机关的触发条件。
3. 配置遗迹门、Zekrom 捕捉条件和最终演出。

## 当前测试顺序

1. 保持调试清档开启，进入 Play。
2. 识别第三章。
3. 按顺序触发三个火山机关。
4. 检查 `SmokeRoot1`、`SmokeRoot2`、`MagmaRoot1`、`MagmaRoot2` 是否依次消失。
5. 检查第五步是否显示 `StairRoot` 并关闭 `RouteBlocker`。
6. 点击山顶，确认人物能沿 NavMesh 到达。
7. 机关前后分别测试 Infernape 捕捉按钮。
