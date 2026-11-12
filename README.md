# BearPi Tilt Maze

用键盘倾斜三维迷宫，控制球在重力作用下滚动，穿过冰面、水域、湿滑路段、升降门和移动陷阱，抵达薄荷绿终点。原型内含一张关卡 **Switchback**。

## 在 Unity 中运行

1. 在 Unity Hub 里将此文件夹添加为本地工程，使用 **Unity 6000.6.3f1** 打开。
2. 在 Project 窗口双击 `Assets/Scenes/TiltMaze.unity`，然后按 Play。
3. 在 Game 窗口选择 **Start game**。出现 **Get ready** 提示后，使用 WASD 开始操控。
4. 如需 Windows 可执行版本，在 **File > Build Profiles** 选择 Windows 并构建。场景已加入构建列表。编辑器中的 **Quit** 对应独立程序的退出动作。

## 操作

| 按键 | 操作 |
| --- | --- |
| W / S | 向场景上方 / 下方倾斜 |
| A / D | 向场景左方 / 右方倾斜 |
| Space | 紧急制动 0.75 秒，结束后冷却 3.5 秒 |
| Q | 护盾 2.2 秒，结束后冷却 8 秒 |
| V | 依次切换全局、跟随和近距离视角 |
| R | 平台回正。模拟输入时，松开 WASD 后可再次倾斜 |
| Esc | 暂停 / 恢复 |

按键集中配置在场景中 **Game systems > Keyboard Input Source**。在 HUD 右下角点击 **Input diagnostics** 可以查看输入 Roll、Pitch、实际 X/Z 倾角和输入来源状态。

从金色出生点先向上，在第一处横向通道向右，经过冰面、红色扫障和紫色湿滑地面。到升降门后向上，沿蓝色水道右转，再向上抵达绿色终点。出生点右侧的长廊通向断崖。转弯前减速，接近红色扫障时可用护盾抵挡伤害。

## 调试与扩展

- 在 **Game systems > Game Manager** 的 **Input Source** 里，把默认键盘组件切换为同物体上的 `MockInputSource`，即可在 Inspector 设置姿态，并用该组件的上下文菜单发送技能或校准事件。
- 输入适配器实现 `IMazeInputSource` 并输出以度为单位的 `MazeInput`。Roll 正值使球向右滚动，Pitch 正值使球向前滚动。适配器在读取时消费待处理的手势和按键边沿。
- `SerialInputSource` 和 `UdpInputSource` 为预留适配器。第二块 BearPi 的反馈适配器可实现 `IGameFeedbackSink` 并添加至 `GameFeedback` 的 **Sinks** 列表。
- 新关卡用独立平台根节点及 `LevelDefinition` 配置出生点、终点、平台尺寸和机关，再添加到 `LevelManager` 的 **Levels** 列表。通关后按列表顺序进入下一关。
- 实际场景、材质和 UI 配置均已保存在 `Assets`。编辑器菜单 **Tilt Maze > Rebuild Prototype Scene** 可从代码中的布局重建该场景，并覆盖原型场景和生成材质。
- 阶段改动、验证命令和复测步骤见 [开发记录](DEVELOPMENT.md)。

