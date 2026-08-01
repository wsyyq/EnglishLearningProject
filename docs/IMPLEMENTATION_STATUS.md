# GameLexicon 实施状态

更新日期：2026-08-01

> 本文件记录当前仓库状态、已完成任务、正在等待执行的任务、环境限制与后续前置条件。  
> Codex 每次开始工作前必须先阅读：
>
> - `AGENTS.md`
> - `docs/PRODUCT_SPEC.md`
> - `docs/IMPLEMENTATION_STATUS.md`
>
> 每次只执行一个任务。未完成当前任务的构建、测试、人工验收和状态更新前，不得进入下一任务。

---

## 1. 仓库基线

### 1.1 仓库根目录

```text
D:\UGit\EnglishLearningProject
```

### 1.2 现有 Godot 工程目录

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

约束：

- `english-learning-project/` 是当前已有的 Godot 工程目录。
- 未经明确任务授权，不得移动或重命名该目录。
- 不得在 Godot 编辑器安装目录中创建项目文件。
- 不得创建第二个重复的 Godot 工程。
- 当前 Git 仓库根目录为外层 `EnglishLearningProject/`。

---

## 2. 最近完成任务

### PREP-01：项目现状检查与实施文档初始化

- Task ID：`PREP-01`
- 名称：项目现状检查与实施文档初始化
- 状态：`Done`
- 完成日期：2026-08-01

### 2.1 已完成内容

- 已确认 Git 仓库根目录。
- 已确认现有 Godot 工程目录。
- 已检查仓库目录结构。
- 已检查是否存在嵌套 Git 仓库。
- 已记录当前 Godot 工程状态。
- 已记录本机可用的 `.NET SDK` 与 Git 版本。
- 已创建或确认以下项目文档：
  - `AGENTS.md`
  - `docs/PRODUCT_SPEC.md`
  - `docs/IMPLEMENTATION_STATUS.md`
- 未安装第三方依赖。
- 未实现任何业务功能。
- 未移动或重命名现有 Godot 工程。

### 2.2 自动化测试

不适用。

本任务仅用于环境检查和文档初始化，当前仓库尚无可构建的解决方案和测试项目。

### 2.3 人工验收

已完成：

- [x] 确认仓库根目录为 `D:\UGit\EnglishLearningProject`
- [x] 确认 Godot 工程位于 `english-learning-project/`
- [x] 确认未发现嵌套 `.git` 仓库
- [x] 确认当前工程尚无 `.sln`
- [x] 确认当前工程尚无 `.csproj`
- [x] 确认当前工程尚无 C# 脚本
- [x] 确认当前工程尚无 GDScript 脚本
- [x] 记录 `.NET SDK` 版本
- [x] 记录 Git 版本
- [x] 记录 Godot 工程声明的版本特性和渲染配置

### 2.4 已知限制

- Godot 命令当前不在 `PATH`。
- 尚未通过命令行确认 Godot 编辑器的精确版本。
- 尚未确认当前使用的 Godot 编辑器是否为要求的 Godot 4.7.1 .NET 版本。
- 当前运行进程名为 `godot.windows.opt.tools.64`，但尚未确认其完整可执行路径和文件版本。
- 当前 Godot 工程尚未生成 `.csproj`。
- 当前 Godot 工程尚未生成 `.sln`。
- 当前 Godot 工程尚未初始化 C# 脚本环境。
- 当前仓库尚无可构建的根解决方案。
- 当前仓库尚无自动化测试项目。

### 2.5 后续任务

```text
M0-T01：创建解决方案与基础分层项目
```

---

## 3. 最近完成任务详情

### M0-T01：创建解决方案与基础分层项目

- Task ID：`M0-T01`
- 名称：创建解决方案与基础分层项目
- 状态：`Done`
- 开始时间：2026-08-01 16:20 +08:00
- 完成时间：2026-08-01 16:30 +08:00
- 负责人：Codex
- 前置任务：`PREP-01`
- 阻塞状态：无
- 是否允许执行后续任务：否

### 3.1 本任务目标

在仓库根目录创建 GameLexicon 的基础 C# 解决方案结构，但不初始化 Godot C# 工程，不实现任何业务功能。

应创建：

```text
GameLexicon.sln

src/
├─ GameLexicon.Domain/
├─ GameLexicon.Application/
└─ GameLexicon.Infrastructure/

tools/
└─ GameLexicon.CaptureBridge/

tests/
├─ GameLexicon.Domain.Tests/
├─ GameLexicon.Application.Tests/
└─ GameLexicon.Infrastructure.Tests/
```

### 3.2 预计涉及文件

```text
GameLexicon.sln

src/GameLexicon.Domain/
├─ GameLexicon.Domain.csproj
└─ ...

src/GameLexicon.Application/
├─ GameLexicon.Application.csproj
└─ ...

src/GameLexicon.Infrastructure/
├─ GameLexicon.Infrastructure.csproj
└─ ...

tools/GameLexicon.CaptureBridge/
├─ GameLexicon.CaptureBridge.csproj
└─ ...

tests/GameLexicon.Domain.Tests/
├─ GameLexicon.Domain.Tests.csproj
└─ ...

tests/GameLexicon.Application.Tests/
├─ GameLexicon.Application.Tests.csproj
└─ ...

tests/GameLexicon.Infrastructure.Tests/
├─ GameLexicon.Infrastructure.Tests.csproj
└─ ...

docs/IMPLEMENTATION_STATUS.md
```

实际文件应在任务开始前根据仓库现状再次确认。

### 3.3 依赖

- 可用的 `.NET SDK`
- 可用的 Git
- 已存在并可读取的：
  - `AGENTS.md`
  - `docs/PRODUCT_SPEC.md`
  - `docs/IMPLEMENTATION_STATUS.md`

本任务不依赖：

- Godot C# 工程文件
- Tesseract
- SQLite
- Windows Graphics Capture
- 系统 TTS
- 第三方词典服务

### 3.4 项目引用规则

生产项目引用方向应为：

```text
GameLexicon.Domain
    ↑
GameLexicon.Application
    ↑
GameLexicon.Infrastructure
```

具体规则：

- `GameLexicon.Domain`
  - 不引用任何其他生产项目。
  - 不引用 Godot。
  - 不引用 SQLite。
  - 不引用 Windows API。
  - 不引用 OCR 实现。

- `GameLexicon.Application`
  - 可以引用 `GameLexicon.Domain`。
  - 不直接引用 Godot UI。
  - 不直接引用 Windows API。
  - 不直接引用具体 SQLite 实现。

- `GameLexicon.Infrastructure`
  - 可以引用 `GameLexicon.Application`。
  - 可以引用 `GameLexicon.Domain`。
  - 后续负责 SQLite、OCR、配置、导出等实现。
  - 本任务不添加实际基础设施依赖。

- `GameLexicon.CaptureBridge`
  - 本任务只创建基础项目。
  - 不实现全局快捷键。
  - 不实现截图。
  - 如无明确需要，不添加对其他生产项目的引用。

- 测试项目
  - 只引用各自需要测试的项目。
  - 不引用无关生产项目。

### 3.5 执行计划

1. 阅读：
   - `AGENTS.md`
   - `docs/PRODUCT_SPEC.md`
   - `docs/IMPLEMENTATION_STATUS.md`
2. 检查当前 Git 状态。
3. 确认仓库根目录。
4. 确认现有 Godot 工程仍位于 `english-learning-project/`。
5. 创建根解决方案 `GameLexicon.sln`。
6. 创建 Domain、Application、Infrastructure 项目。
7. 创建 CaptureBridge 项目。
8. 创建三个测试项目。
9. 配置项目引用。
10. 添加至少一个最小 smoke test。
11. 运行解决方案构建。
12. 运行全部当前测试。
13. 检查项目引用方向。
14. 检查 Git diff。
15. 更新本文件。
16. 将当前任务状态更新为 `Done`。
17. 将下一任务设置为 `M0-T02`。
18. 不自动执行 `M0-T02`。

### 3.6 本任务明确不做

- 不创建第二个 Godot 工程。
- 不移动 `english-learning-project/`。
- 不重命名 `english-learning-project/`。
- 不手工创建 Godot `.csproj`。
- 不将当前 Godot 工程加入根解决方案。
- 不创建 Godot 场景。
- 不创建 Godot C# 脚本。
- 不实现导航 UI。
- 不实现日志系统。
- 不实现配置系统。
- 不实现 SQLite。
- 不实现数据库迁移。
- 不实现截图。
- 不实现 CaptureBridge 平台功能。
- 不实现 OCR。
- 不实现 TTS。
- 不实现词条。
- 不实现复习。
- 不添加本任务不需要的生产依赖。
- 不修改 Godot 安装目录。
- 不删除用户已有文件。

### 3.7 自动化验收

必须完成：

- [x] `dotnet build GameLexicon.sln` 成功
- [x] 全部当前测试通过
- [x] 至少存在一个通过的 smoke test
- [x] Domain 项目无平台依赖
- [x] 项目引用方向符合本节规则
- [x] 不存在循环项目引用
- [x] 未创建第二个 Godot 工程
- [x] 未移动现有 Godot 工程
- [x] 未实现后续里程碑功能

### 3.8 人工验收

必须确认：

- [x] `GameLexicon.sln` 位于仓库根目录
- [x] `src/`、`tools/`、`tests/` 位于仓库根目录
- [x] `english-learning-project/` 路径未变化
- [x] 现有 `project.godot` 未被不必要修改
- [x] 未将生成缓存提交到 Git
- [x] `.gitignore` 能排除 `.godot/`、`bin/` 和 `obj/`
- [x] `docs/IMPLEMENTATION_STATUS.md` 与实际实现一致
- [x] Codex 最终报告包含：
  - 创建文件
  - 修改文件
  - 执行命令
  - 构建结果
  - 测试结果
  - 人工验收结果
  - 已知限制
  - 建议的下一任务

### 3.9 完成结果

- 创建文件：
  - `GameLexicon.sln`
  - 三个生产类库项目及其空占位类型
  - 一个 CaptureBridge 控制台项目骨架
  - 三个 xUnit 测试项目及程序集加载 smoke test
- 修改文件：
  - `.gitignore`
  - `docs/IMPLEMENTATION_STATUS.md`
- 项目引用：
  - Application → Domain
  - Infrastructure → Application、Domain
  - CaptureBridge → 无生产项目引用
  - 各测试项目 → 各自对应的生产项目
- 构建结果：成功，0 个警告，0 个错误。
- 测试结果：成功，3 个测试通过，0 个失败，0 个跳过。
- 已知限制：普通 C# 项目暂以本机唯一可用 targeting pack 对应的 `net10.0` 为目标；M0-T02 必须以 Godot 自动生成的 `.csproj` 为准重新核对兼容目标框架。
- 后续任务：`M0-T02：初始化 Godot .NET/C# 工程与基础主场景`（当前因 Godot 4.7.1 .NET 环境未验证而阻塞）。

---

## 4. 最近完成任务

### M0-T02：初始化 Godot .NET/C# 工程与基础主场景

- Task ID：`M0-T02`
- 名称：初始化 Godot .NET/C# 工程与基础主场景
- 状态：`Done`
- 开始时间：2026-08-01 17:50 +08:00
- 完成时间：2026-08-01 18:25 +08:00
- 阻塞状态：无
- 前置任务：
  - `M0-T01` 完成
  - Godot .NET 环境验证完成

### 4.1 M0-T02 前置条件

开始 M0-T02 前，必须完成：

- [x] 确认 Godot 可执行文件的完整路径
- [x] 确认精确版本为 Godot 4.7.1
- [x] 确认使用支持 C# 的 Godot .NET 版本
- [x] 确认 Godot 与 `.NET SDK` 均为 64 位
- [x] 能使用指定 Godot 编辑器打开：
  - `english-learning-project/project.godot`
- [x] 能在该工程中创建第一个 C# 脚本
- [x] 能由 Godot 自动生成 `.csproj`
- [x] 能使用 `.NET SDK` 构建 Godot 自动生成的 `.csproj`
- [x] 确认 Godot 自动生成的目标框架
- [x] 确认 Godot C# 项目可以加入根解决方案

### 4.2 M0-T02 预期结果

M0-T02 预计负责：

- 验证 Godot 4.7.1 .NET。
- 初始化现有 Godot 工程的 C# 项目文件。
- 保留 Godot 自动生成的 `.csproj` 配置。
- 创建基础 `App.tscn`。
- 创建基础 `AppRoot.cs`。
- 将 Godot C# 项目加入 `GameLexicon.sln`。
- 配置 Godot 项目对 Application、Domain 和 Infrastructure 的引用。
- 验证 Godot 工程可编译、可打开、可运行。

不得在 M0-T01 中提前实施以上内容。

### 4.3 M0-T02 完成结果

- Godot 版本：`4.7.1.stable.mono.official.a13da4feb`，x64。
- GodotSharp：`Api/` 与 `Tools/` 存在且非空。
- 已安装 SDK：`.NET SDK 8.0.423`、`10.0.301`，主机架构 x64。
- Godot 自动生成项目：`english-learning-project/EnglishLearningProject.csproj`。
- Godot 本地解决方案：`english-learning-project/EnglishLearningProject.sln`。
- Project SDK：`Godot.NET.Sdk/4.7.1`。
- 桌面目标框架：`net8.0`；Android 条件目标框架：`net9.0`。
- 经用户明确授权，将 Domain、Application、Infrastructure 三个生产类库从 `net10.0` 调整为 `net8.0`，未批量修改测试项目或 CaptureBridge。
- 创建 `scripts/AppRoot.cs`、对应 Godot UID 文件和 `scenes/App.tscn`。
- 主场景设置为 `res://scenes/App.tscn`。
- Godot 项目引用 Application、Domain、Infrastructure；根解决方案共 8 个项目，无循环引用。
- Godot C# 项目构建：成功，0 警告，0 错误。
- 根解决方案 restore/build：成功，0 警告，0 错误。
- 自动化测试：3/3 通过，0 失败，0 跳过。
- Godot headless 编辑器构建与主场景加载：成功；输出 `GameLexicon AppRoot initialized.`。
- GUI 人工验收：应用窗口、深色背景、左侧约 220px 占位区、右侧内容区、初始化输出均通过；无 C# 异常或资源错误，应用正常关闭且无残留 Godot 进程。
- Git：`.godot/`、`bin/`、`obj/` 均被忽略；仅保留 M0-T02 源文件和必要项目配置变更；`git diff --check` 通过。
- Skill Impact Review：无需更新 Skills、catalog 或 changelog。
- 已知限制：根解决方案的 Release 配置未在本任务单独验证；本任务验证的是默认 Debug 构建。Godot GUI 视觉结果来自用户人工验收。
- 执行的主要命令：Godot `--version`、`--help`、`--headless --editor --build-solutions --quit`、headless 主场景运行、`dotnet build`、`dotnet sln add/list`、`dotnet restore/build/test`、Git status/diff/diff-check。

---

## 5. 当前实际目录结构

```text
D:\UGit\EnglishLearningProject\
├─ .git\
├─ .gitignore
├─ AGENTS.md
├─ GameLexicon.sln
├─ docs\
│  ├─ PRODUCT_SPEC.md
│  └─ IMPLEMENTATION_STATUS.md
├─ src\
│  ├─ GameLexicon.Domain\
│  ├─ GameLexicon.Application\
│  └─ GameLexicon.Infrastructure\
├─ tools\
│  └─ GameLexicon.CaptureBridge\
├─ tests\
│  ├─ GameLexicon.Domain.Tests\
│  ├─ GameLexicon.Application.Tests\
│  └─ GameLexicon.Infrastructure.Tests\
└─ english-learning-project\
   ├─ .editorconfig
   ├─ .gitattributes
   ├─ .gitignore
   ├─ .godot\
   │  ├─ editor\
   │  ├─ imported\
   │  ├─ mono\
   │  └─ shader_cache\
   ├─ scenes\
   │  └─ App.tscn
   ├─ scripts\
   │  ├─ AppRoot.cs
   │  └─ AppRoot.cs.uid
   ├─ EnglishLearningProject.csproj
   ├─ EnglishLearningProject.sln
   ├─ icon.svg
   ├─ icon.svg.import
   └─ project.godot
```

说明：

- Git 仓库根目录为：

```text
D:\UGit\EnglishLearningProject
```

- 现有 Godot 工程位于：

```text
english-learning-project/
```

- 未发现嵌套 `.git` 仓库。
- 根解决方案为 `GameLexicon.sln`，包含 8 个项目。
- 当前有 Domain、Application、Infrastructure 三个普通 C# 类库项目。
- 当前有三个 xUnit 测试项目，每个项目包含一个通过的 smoke test。
- CaptureBridge 当前只有控制台项目骨架，没有实现平台功能。
- 当前有 M0-T02 创建的基础场景和最小 C# 主脚本。
- 当前没有插件目录。
- 当前 Godot 工程已初始化为可构建、可运行的 Godot C# 工程。

---

## 6. Godot 工程状态

### 6.1 工程信息

- 工程文件：

```text
english-learning-project/project.godot
```

- 工程名称：

```text
EnglishLearningProject
```

- 配置格式：

```text
config_version=5
```

- 声明的 Godot 特性：

```text
4.7
C#
Mobile
```

- Windows 渲染驱动：

```text
d3d12
```

- 渲染方式：

```text
mobile
```

- 3D 物理引擎：

```text
Jolt Physics
```

### 6.2 脚本与项目文件状态

| 项目 | 当前状态 |
|---|---|
| Godot `.csproj` | 已由 Godot 4.7.1 .NET 生成 |
| 根 `.sln` | 已生成；包含 Godot 工程，共 8 个项目 |
| Godot 本地 `.sln` | 已由 Godot 生成并保留 |
| Godot C# 脚本 | `scripts/AppRoot.cs` |
| GDScript 脚本 | 尚未创建 |
| 场景目录 | `scenes/App.tscn` |
| 脚本目录 | 已创建 |
| 插件目录 | 尚未创建 |

### 6.3 工程类型结论

当前是一个已完成最小 C# 初始化的 Godot 4.7.1 .NET 工程。

更准确的当前结论是：

- Godot 自动生成 `EnglishLearningProject.csproj` 与本地解决方案。
- Project SDK 为 `Godot.NET.Sdk/4.7.1`，桌面目标框架为 `net8.0`。
- 基础 C# 项目、主场景、根解决方案集成、构建、headless 与 GUI 验证均已完成。

---

## 7. 本机工具状态

| 工具 | 当前状态 |
|---|---|
| Godot | 命令不在 `PATH` |
| Godot 安装目录 | `E:\SteamLibrary\steamapps\common\Godot Engine`（已验证） |
| Godot .NET 主程序 | `E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe`（已验证） |
| Godot .NET 控制台程序 | `E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe`（已验证） |
| Steam 启动兼容程序 | `E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe`（已验证） |
| Godot 精确版本 | `4.7.1.stable.mono.official.a13da4feb` |
| Godot .NET 支持 | 已由 Mono 标识、GodotSharp、C# 生成与构建验证 |
| `.NET SDK` | `8.0.423`、`10.0.301` |
| Git | `2.55.0.windows.3` |

补充说明：

- Godot 自动生成项目使用 `Godot.NET.Sdk/4.7.1` 和桌面 `net8.0`。
- Domain、Application、Infrastructure 经用户授权调整为 `net8.0` 并完成兼容构建。
- 测试项目和 CaptureBridge 保持 `net10.0`。

---

## 8. Git 与资源文件规则

### 8.1 应忽略

以下内容属于生成缓存、构建输出或用户本地数据，不应提交：

```text
.godot/
bin/
obj/
logs/
capture_inbox/
screenshots/
*.db
*.db-shm
*.db-wal
.local/
```

实际规则以根目录 `.gitignore` 为准。

### 8.2 应提交

以下内容属于项目源文件或资源导入配置，应提交：

```text
project.godot
icon.svg
icon.svg.import
AGENTS.md
docs/
*.sln
*.csproj
*.cs
*.tscn
```

特别说明：

- `.godot/` 是 Godot 生成的缓存目录，不应提交。
- `icon.svg` 是项目源资源，应提交。
- `icon.svg.import` 保存资源导入配置，应提交。
- 根目录 `.gitignore` 不应使用会排除全部 `*.import` 文件的规则。

### 8.3 当前 Git 约束

- 不覆盖用户已有未提交修改。
- 不删除现有文件。
- 不创建嵌套 Git 仓库。
- 不提交：
  - 用户数据库
  - 用户截图
  - OCR 文本缓存
  - 日志
  - API 密钥
  - 本机 Godot 路径
  - 构建输出
  - Godot 缓存
- 每个任务完成后必须检查 Git diff。

---

## 9. 项目准备约束

### 9.1 技术基线

- Godot：4.7.1 .NET
- 主要语言：C#
- MVP 平台：Windows 10/11 x64
- 架构：
  - Godot 桌面应用
  - Windows CaptureBridge
  - 本地 Tesseract OCR
  - SQLite
- 默认离线优先

### 9.2 安全约束

- 不向游戏进程注入代码。
- 不读取游戏进程内存。
- 不实现反作弊绕过。
- 不绕过 DRM。
- 不实现受保护内容捕获绕过。
- 不在日志中记录用户截图和完整 OCR 文本。
- 不将 API Key 提交到 Git。

### 9.3 架构约束

- UI 不直接执行 SQL。
- UI 不直接启动 OCR 子进程。
- UI 不直接调用 Win32。
- Domain 不依赖 Godot。
- Domain 不依赖 SQLite。
- Domain 不依赖 Windows API。
- 外部能力通过接口隔离。
- 每个任务只实现当前范围。
- 不提前实现后续里程碑。

### 9.4 当前阶段明确禁止

当前处于 Milestone 0 初期。

在 M0-T01 完成前，不得实现：

- OCR
- SQLite 业务表
- 截图
- 全局快捷键
- CaptureBridge 平台逻辑
- TTS
- 词条编辑
- 复习
- 导入导出
- 云服务
- 在线词典
- LLM 功能

---

## 10. 风险与待确认事项

### 10.1 Godot 编辑器版本

状态：已在 M0-T02 验证完成

M0-T02 应使用：

```text
主验证程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe

命令行及 headless 验证程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe

Steam 启动兼容程序（仅额外版本验证）：
E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe
```

三个程序均确认为 `4.7.1.stable.mono.official.a13da4feb` x64；GodotSharp、C# 项目生成、构建、headless 和 GUI 均已验证。

处理阶段：

```text
M0-T02（Done）
```

### 10.2 Godot 目标框架

状态：已确认

规则：

- 不在 Godot `.csproj` 生成前猜测目标框架。
- 不手工创建 Godot `.csproj`。
- 不手工固定为 `net10.0`。
- 以 Godot 自动生成结果为准。
- 再调整普通 C# 项目以保证兼容。

实际结果：Godot 桌面目标为 `net8.0`；三个被引用的生产类库经用户授权最小调整为 `net8.0`。

处理阶段：

```text
M0-T02（Done）
```

### 10.3 根 `.gitignore`

状态：已验证

重点确认：

- 忽略 `.godot/`
- 忽略 `bin/`
- 忽略 `obj/`
- 不忽略 `icon.svg.import`
- 不忽略 `project.godot`
- 不忽略 `.csproj`
- 不忽略 `.sln`
- 不忽略 `docs/`

### 10.4 Godot 工程目录名称

状态：保留现状

当前名称：

```text
english-learning-project/
```

规则：

- 现阶段不移动。
- 现阶段不重命名。
- 如后续需要迁移到 `app/GameLexicon.Godot/`，必须创建独立任务并先获得明确授权。

---

## 11. 当前任务完成后的更新要求

M0-T01 完成后，Codex 必须更新本文件：

1. 将：

```text
M0-T01 状态：Done
```

改为：

```text
M0-T01 状态：Done
```

2. 记录：

- 开始时间
- 完成时间
- 创建文件
- 修改文件
- 执行命令
- 构建结果
- 测试结果
- 人工验收结果
- 已知限制

3. 将 `M0-T01` 移入“最近完成任务”。

4. 将当前任务更新为：

```text
M0-T02：初始化 Godot .NET/C# 工程与基础主场景
```

5. 如果 Godot 4.7.1 .NET 尚未验证，则：

```text
M0-T02 状态：Blocked
```

6. 不得自动执行 M0-T02。

---

## 12. Codex 当前执行摘要

当前已完成：

```text
M0-T01：创建解决方案与基础分层项目
```

当前已完成：

```text
M0-T02：初始化 Godot .NET/C# 工程与基础主场景
```

M0-T02 已完成环境验证、Godot C# 初始化、基础场景、解决方案集成、构建、测试、headless 验证和 GUI 人工验收。

下一任务：

```text
M0-T03：实现基础导航
状态：Not Started
```

不得自动执行 M0-T03。

---

## 13. META 基础设施任务

### META-T01：部署项目级多 Agent、任务路由与 Skills 自维护系统

- Task ID：`META-T01`
- 状态：`Done`
- 完成日期：2026-08-01
- 范围：仅项目级 Codex Agent/Skill 配置与共享文档
- 创建：
  - `.codex/config.toml`
  - `.codex/agents/` 下四个只读专业 Agent
  - `.agents/skills/` 下五个项目 Skill
  - `docs/AGENT_SYSTEM.md`
  - `docs/SKILLS_CATALOG.md`
  - `docs/AGENT_HANDOFF.md`
  - `docs/SKILL_CHANGELOG.md`
  - `docs/ENVIRONMENT.md`
  - `docs/DECISIONS.md`
- 修改：
  - `AGENTS.md`：加入协调器、任务路由、单写入者和 Skill Impact Review 规则
  - `docs/IMPLEMENTATION_STATUS.md`：记录 META-T01，不改变 M0-T02 业务状态
- 验证：
  - 5 个 TOML 文件可由标准 `tomllib` 解析
  - 4 个 Agent 名称唯一，均为 `read-only`
  - 5 个 Skill frontmatter、名称、描述和正文结构通过等价检查
  - Agent、Skill、catalog 和 AGENTS 引用一致
  - 未发现凭证或密钥赋值
- Skill Impact Review：`Yes`；初始创建五个可复用项目 Skills，并更新 catalog/changelog
- 已知限制：`skill-creator` 的 `quick_validate.py` 因现有运行环境缺少 `PyYAML` 无法执行；未安装依赖，改用 Python 标准库完成等价结构检查
- 会话要求：修改了 `AGENTS.md`、Agent TOML 和 Skills；必须重启或新开 Codex 会话后再验证自动发现与路由
- M0-T02：保持原状态，本任务未执行
- 下一步：新会话中执行只读路由验证，不自动开始 M0-T02
