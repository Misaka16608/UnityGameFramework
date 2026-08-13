# opencode.md

> 本文档为 opencode 专属版本，与 `CLAUDE.md` 内容保持一致，修改时需同步更新。

## 项目概述

**UnityGameFramework** 是基于 Unity 引擎的游戏框架 [Game Framework](https://gameframework.cn/) 的 Unity 集成层，作者 Jiang Yin (Ellan)，MIT 协议开源。

本项目将纯 .NET 的 `GameFramework` 核心库封装为 Unity MonoBehaviour 组件，向 Unity 项目提供 19 个内置游戏模块。项目以 Unity Package 形式分发（`com.jiangyin.gameframework`），当前版本 **2021.5.31**，要求 Unity **5.3+**。

## 项目结构

```
UnityGameFramework/
├── GameFramework/                  # Git submodule：纯 .NET 3.5 核心库
│   ├── GameFramework/              # C# 源码，无 Unity 依赖 (noEngineReferences: true)
│   │   ├── Base/                   # 基础架构：Entry、Module、EventPool、ReferencePool、TaskPool、Variable、Serializer
│   │   ├── Config/ DataNode/ DataTable/ Debugger/ Download/ Entity/ Event/
│   │   ├── FileSystem/ Fsm/ Localization/ Network/ ObjectPool/ Procedure/
│   │   ├── Resource/ Scene/ Setting/ Sound/ UI/ WebRequest/ Utility/
│   │   ├── GameFramework.csproj    # VS 工程，目标 .NET Framework 3.5
│   │   └── Properties/AssemblyInfo.cs
│   ├── GameFramework.asmdef        # Unity 程序集定义，name: "GameFramework"
│   └── GameFramework.sln
├── Scripts/
│   ├── Runtime/                    # UnityGameFramework.Runtime 程序集
│   │   ├── Base/                   # GameEntry, BaseComponent, GameFrameworkComponent, ShutdownType
│   │   ├── Config/ DataNode/ DataTable/ Debugger/ Download/ Entity/
│   │   ├── Event/ FileSystem/ Fsm/ Localization/ Network/ ObjectPool/
│   │   ├── Procedure/ ReferencePool/ Resource/ Scene/ Setting/ Sound/
│   │   ├── UI/ Utility/ Variable/ WebRequest/
│   │   └── UnityGameFramework.Runtime.asmdef
│   └── Editor/                     # UnityGameFramework.Editor 程序集（仅 Editor 平台）
│       ├── Inspector/              # 各组件的 Inspector 面板（23 个 *ComponentInspector.cs）
│       ├── Misc/                   # BuildSettings, LogRedirection, ScriptingDefineSymbols 等
│       ├── ResourceAnalyzer/       # 资源依赖分析与循环引用检测
│       ├── ResourceBuilder/        # AssetBundle 构建流水线
│       ├── ResourceCollection/     # 资源配置数据模型
│       ├── ResourceEditor/         # 资源编辑器窗口
│       ├── ResourcePackBuilder/    # 资源包构建
│       ├── ResourceSyncTools/      # 资源同步工具
│       └── UnityGameFramework.Editor.asmdef
├── Libraries/
│   ├── ICSharpCode.SharpZipLib.dll  # 压缩库依赖
│   └── link.xml                     # IL2CPP 代码剥离保护配置
├── GameFramework.prefab             # 框架预制体（挂载 BaseComponent 及所有模块 Component）
├── package.json                     # UPM 包描述
├── README.md                        # 中英双语项目介绍
└── LICENSE.md                       # MIT 协议
```

## 三个程序集的依赖关系

```
GameFramework (纯 .NET 3.5, 无 Unity 依赖)
    ↑ 引用
UnityGameFramework.Runtime (MonoBehaviour 封装层)
    ↑ 引用
UnityGameFramework.Editor (编辑器工具, 仅 Editor 平台)
```

## 核心架构

### 模块管理器：GameFrameworkEntry

`GameFrameworkEntry` 是 GameFramework 核心库的入口静态类，负责所有模块的创建、注册和生命周期管理：

- `GameFrameworkEntry.Update(elapseSeconds, realElapseSeconds)` — 每帧轮询所有已注册模块
- `GameFrameworkEntry.GetModule<T>()` — 懒加载获取模块（按接口获取，模块自动创建并以 Priority 排序插入链表）
- `GameFrameworkEntry.Shutdown()` — 逆序关闭所有模块

所有核心模块（`EventManager`, `FsmManager`, `ProcedureManager` 等）都继承自 `GameFrameworkModule` 基类。

### Unity 组件层：GameEntry

`GameEntry` 是 Unity 侧的入口静态类，管理所有 `GameFrameworkComponent`（MonoBehaviour）的注册与查找：

- `GameEntry.GetComponent<T>()` — 获取已注册的框架组件
- `GameEntry.Shutdown(shutdownType)` — 关闭框架（支持 None / Restart / Quit 三种模式）

### 核心组件：BaseComponent

`BaseComponent` 是所有组件中最重要的，挂载于 `GameFramework.prefab`：

- 在 `Awake()` 中初始化 TextHelper、VersionHelper、LogHelper、CompressionHelper、JsonHelper
- 在 `Update()` 中驱动 `GameFrameworkEntry.Update(deltaTime, unscaledDeltaTime)`
- 控制帧率、游戏速度、后台运行、休眠策略
- 监听低内存事件并自动释放对象池与资源

### 组件模式

每个内置模块有两层实现：

1. **核心层**（`GameFramework` 程序集）：接口 + Manager 类，纯逻辑，无 Unity 依赖
   - 例：`IEventManager` 接口 -> `EventManager` 实现
2. **Unity 封装层**（`UnityGameFramework.Runtime` 程序集）：Component 类，MonoBehaviour
   - 例：`EventComponent : GameFrameworkComponent`，在 `Awake()` 中通过 `GameFrameworkEntry.GetModule<T>()` 获取对应模块，暴露 Unity 友好的 API

### Helper / 可替换机制

框架大量使用 Helper 接口模式，允许用户替换默认实现：

- 每类 Helper 都有一个接口（如 `IJsonHelper`, `ITextHelper`, `IResourceHelper`）
- 框架提供默认实现（如 `DefaultJsonHelper`, `DefaultTextHelper`）
- 用户在 `BaseComponent` 的 Inspector 中配置 Helper 类型名即可替换
- 核心模块也有对应 Helper（如 `IEntityGroupHelper`, `IUIFormHelper`），用于桥接 Unity 特定功能

### 事件系统

- 事件以 int ID 标识，通过 `EventPool<T>` 实现
- `Fire()` 线程安全，事件在主线程的下一帧分发
- `FireNow()` 立即模式，非线程安全
- 每个模块在操作完成时抛出内置事件（如 `LoadDataTableSuccessEventArgs`, `OpenUIFormSuccessEventArgs`）

### 流程系统（Procedure）

- 基于 FSM 的游戏生命周期管理
- 继承 `ProcedureBase` 定义流程，在 `ProcedureComponent` 中配置可用流程列表和入口流程
- `ProcedureRegistry` 提供零反射的流程工厂注册（优先），失败时回退到反射

## 19 个内置模块

| 模块 | Component | 功能 |
|------|-----------|------|
| Base | `BaseComponent` | 框架入口、帧循环驱动、Helper 初始化 |
| Config | `ConfigComponent` | 全局只读配置 |
| DataNode | `DataNodeComponent` | 树状数据结构存储 |
| DataTable | `DataTableComponent` | Excel 表格数据读取 |
| Debugger | `DebuggerComponent` | 运行时调试器窗口 |
| Download | `DownloadComponent` | 文件下载，支持断点续传 |
| Entity | `EntityComponent` | 实体管理、显示隐藏、挂接 |
| Event | `EventComponent` | 事件订阅与分发 |
| FileSystem | `FileSystemComponent` | 虚拟文件系统 |
| Fsm | `FsmComponent` | 有限状态机 |
| Localization | `LocalizationComponent` | 多语言本地化 |
| Network | `NetworkComponent` | TCP Socket 长连接 |
| ObjectPool | `ObjectPoolComponent` | GameObject 对象池 |
| Procedure | `ProcedureComponent` | 游戏流程控制 |
| ReferencePool | `ReferencePoolComponent` | C# 引用对象池 |
| Resource | `ResourceComponent` | 异步资源加载（AssetBundle） |
| Scene | `SceneComponent` | 场景加载/卸载 |
| Setting | `SettingComponent` | 键值对配置存储 |
| Sound | `SoundComponent` | 音频管理 |
| UI | `UIComponent` | UI 界面管理 |
| WebRequest | `WebRequestComponent` | HTTP 短连接 |

## 编译/构建

本项目为 Unity Package，**没有独立的构建命令**。通过在 Unity 项目中引用此 Package 来使用：

- 在 Unity 项目的 `Packages/manifest.json` 中添加 Git 依赖，或直接放入 `Assets/` 目录
- Unity 编辑器自动检测 `.asmdef` 文件并按依赖顺序编译三个程序集
- `GameFramework.sln` 可用 Visual Studio / Rider 打开进行核心库的独立开发与编译

### 日志预编译宏

日志系统使用 `[Conditional]` 特性控制编译，在 Unity 的 `Scripting Define Symbols` 中配置：

- `ENABLE_LOG` — 启用所有日志
- `ENABLE_DEBUG_LOG` / `ENABLE_INFO_LOG` / `ENABLE_WARNING_LOG` / `ENABLE_ERROR_LOG` / `ENABLE_FATAL_LOG` — 单级别启用
- `ENABLE_DEBUG_AND_ABOVE_LOG` / `ENABLE_INFO_AND_ABOVE_LOG` / etc. — 按级别以上启用

Editor 中通过 `Game Framework/Log Scripting Define Symbols` 菜单快速切换。

## 关键约定

- **命名空间**：核心库用 `GameFramework`，Runtime 层用 `UnityGameFramework.Runtime`，Editor 层用 `UnityGameFramework.Editor`
- **Component 命名**：`{ModuleName}Component`，继承 `GameFrameworkComponent`
- **事件参数命名**：`{Action}{ModuleName}{Result}EventArgs`，如 `LoadDataTableSuccessEventArgs`
- **Helper 命名**：接口 `I{Name}Helper`，默认实现 `Default{Name}Helper`，基类 `{Name}HelperBase`
- **源码文件头**：所有 `.cs` 文件使用统一的版权头（Copyright (c) 2013-2021 Jiang Yin）
- **代码注释**：所有公开 API 使用中文 XML 文档注释
- **Submodule**：`GameFramework` 子模块指向 `git@github.com:Misaka16608/GameFramework.git`
