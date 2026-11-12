# BearPi Tilt Maze · 开发记录

## 工程基线

工作区初始只有空 Git 仓库。本机已安装 Unity 6000.6.3f1（45d8eee7de74），使用该版本通过编辑器命令行创建工程。检查 `ProjectVersion.txt`、`GraphicsSettings.asset`、`ProjectSettings.asset` 和 Package 清单后，选用 Built-in Render Pipeline、PhysX、旧版 Input Manager 与内置 UI Toolkit。保留编辑器生成的 Package 清单与输入模式，注册了原型启动场景。

## 1 · 三维场景、物理和输入

- 新增 `Assets/Scenes/TiltMaze.unity`、`Assets/Art/Materials/` 和 `Assets/Editor/TiltMazeSceneBuilder.cs`。场景包含三维墙体、平台、光源、独立刚体小球、出生点、终点和断崖。
- 新增 `Assets/Scripts/Input/`、`Physics/MazeTiltController.cs`、`Physics/BallController.cs`、`Game/Core/GameManager.cs` 和 `Game/Level/GoalTrigger.cs`。
- 小球依靠刚体和重力滚动，平台采用运动学刚体，在 FixedUpdate 更新双轴倾角。键盘与 Mock 使用相同姿态数据。模拟输入回正时，待方向键松开再接受新的倾斜。重试使用平台刚体姿态计算出生点。
- Unity 批处理编译、场景序列化和 `Assets/Editor/TiltMazeSmoke.cs` 的初版 Play Mode 检查通过，覆盖双轴限幅、滚动、墙壁约束、坠落与重置。
- 当时待办是替换终点和坠落后的临时重置流程。

## 2 · 游戏状态、关卡和视角

- 新增 `Game/Core/GameState.cs`、`Game/Level/LevelDefinition.cs`、`Game/Level/LevelManager.cs` 和 `Game/Camera/CameraManager.cs`。更新 `GameManager.cs`、场景和构建脚本。
- 游戏状态统一处理主菜单、准备、游玩、暂停、失败、通关。关卡清单控制重试和下一关。正交相机可在全景、跟随和近距离间平滑过渡，遇到墙体遮挡时抬高视角。
- Unity 批处理编译与实际 Play Mode 检查通过。测试覆盖 Ready、暂停和恢复、失败原因、重试时出生点、视角切换、终点触发和返回主菜单。
- 当时待办是接入地形、机关、技能、耐久与反馈。

## 3 · 地形、机关、技能与耐久

- 新增 `Terrain/`、`Game/Mechanisms/`、`Game/Skills/SkillManager.cs`、`Physics/BallDamageSystem.cs` 和 `Feedback/`。更新平台、关卡管理器、游戏管理器、测试、场景和材质。
- 地形格独立配置接触材质、阻力和侧滑。机关周期曲线与运动学执行器分离。耐久伤害使用相对速度在接触法线上的分量，接触与陷阱分别限频。制动和护盾共享手势输入。
- Unity 批处理编译与 Play Mode 回归通过，覆盖水域阻力、自动门关闭、陷阱伤害、护盾免伤、制动和冷却。
- 当时待办是完成操作界面、反馈音效和整局路线验证。

## 4 · UI、本地反馈与调优

- 新增 `Assets/UI/`、`Assets/Scripts/UI/MazeHud.cs` 和 `Feedback/GoalBeacon.cs`。更新 `LocalFeedbackSink.cs`、场景、材质、平台限速、伤害参数和烟雾测试。
- UI Toolkit 提供主菜单、HUD、暂停、失败、通关和输入诊断。本地反馈适配器播放合成提示音并写入日志。第二块 BearPi 可使用同一反馈接口接收事件。UI 采用英文。
- 最终 Play Mode 回归通过。Mock 从出生点完整滚到终点，计时 22.6 秒，剩余耐久 100。回归还覆盖校准、坠落失败、重试、暂停、视角切换、自动门、扫障伤害、制动、护盾和水域减速。
- Unity 6000.6.3f1 批处理 Play Mode 启动日志记录了 Unity Editor Search 索引异常，场景测试继续执行并以退出码 0 完成。没有 C# 编译错误。
- Windows 独立程序中实际检查了 Start game、V、Q、Space、Esc、输入诊断、返回主菜单和退出。完整路线由 Play Mode 的 Mock 输入验证。
- 当前有一张实际关卡。硬件 UART/UDP、第二块 BearPi 输出及后续关卡属于后续开发范围。

## 复测

关闭正在占用同一工程的 Unity 编辑器后，可在工程目录运行以下 PowerShell。无图形 Play Mode 测试会自行退出，并在日志中打印 `TILT MAZE SMOKE PASS` 或失败原因。测试临时使用 Mock，完整路线用与 WASD 相同的姿态范围从出生点行驶至终点。独立的触发和伤害检查会直接摆放测试小球。

```powershell
$unity = 'D:/MyWorksoft/Unity Hub/Editor/6000.6.3f1/Editor/Unity.exe'
[IO.Directory]::CreateDirectory((Join-Path $PWD.Path 'Logs')) > $null
$p = Start-Process -FilePath $unity -ArgumentList (
    '-batchmode -nographics -projectPath "' + $PWD.Path +
    '" -executeMethod BearPi.TiltMaze.Editor.TiltMazeSmoke.Run -logFile "' +
    (Join-Path $PWD.Path 'Logs/tiltmaze-smoke.log') + '"'
) -PassThru -WindowStyle Hidden
$p.WaitForExit()
$p.ExitCode
```

人工复测可依照 README 通关路线开始一局。打开诊断面板观察 WASD 和 R 响应，按 V 循环三个视角，测试两种技能和冷却。沿出生点右侧长廊滚出断崖，检查失败原因和重试。成功抵达终点后，检查用时、剩余耐久和重试按钮。

