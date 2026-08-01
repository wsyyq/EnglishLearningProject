# M0-T02 Codex 执行指令

## 任务名称

```text
M0-T02：初始化现有 Godot 4.7.1 .NET/C# 工程与基础主场景
```

本文件用于直接覆盖：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M0-T02_CODEX_INSTRUCTION.md
```

本任务只执行 M0-T02，不执行 M0-T03，不实现任何后续业务功能。

---

# 1. 固定路径

## 1.1 仓库根目录

```text
D:\UGit\EnglishLearningProject
```

## 1.2 现有 Godot 工程目录

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

## 1.3 根解决方案

```text
D:\UGit\EnglishLearningProject\GameLexicon.sln
```

## 1.4 Godot 4.7.1 .NET 安装目录

```text
E:\SteamLibrary\steamapps\common\Godot Engine
```

## 1.5 Godot 4.7.1 .NET 主程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe
```

## 1.6 Godot 4.7.1 .NET 控制台程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe
```

## 1.7 Steam 启动兼容程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe
```

说明：

- `.NET` 主程序是 M0-T02 的主要 Godot 编辑器。
- `.NET` 控制台程序用于命令行、构建和 headless 验证。
- `godot.windows.opt.tools.64.exe` 只作为 Steam 启动兼容程序进行额外验证。
- Codex 不得修改 Godot 安装目录或 Steam 设置。

---

# 2. 必须阅读的项目文件

开始前必须完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/MT_INSTRUCTION/M0-T02_CODEX_INSTRUCTION.md
```

如果以下文件存在，也必须读取与当前任务有关的内容：

```text
docs/AGENT_SYSTEM.md
docs/SKILLS_CATALOG.md
docs/AGENT_HANDOFF.md
.agents/skills/project-routing/SKILL.md
.agents/skills/godot-workflow/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

开始前应按项目规则完成任务路由：

- 主领域：Godot
- 主要专业 Agent：`godot_specialist`
- 辅助专业 Agent：`milestone_architect`
- 默认唯一写入者：主 Agent
- 专业 Agent 只读分析，不并行修改同一工作区

---

# 3. 当前已知状态

M0-T01 已完成：

- 根解决方案存在。
- 根解决方案当前包含 7 个项目。
- 构建成功。
- 测试 3/3 通过。
- 现有 Godot 工程未加入根解决方案。
- 现有 Godot 工程尚未生成 Godot C# `.csproj`。

当前 Godot 4.7.1 .NET 环境已准备：

```text
Godot 主程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe

Godot 控制台程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe

GodotSharp：
E:\SteamLibrary\steamapps\common\Godot Engine\GodotSharp
```

当前已安装的 .NET SDK 至少包括：

```text
8.0.423
10.0.301
```

当前仓库可能存在：

```text
M docs/IMPLEMENTATION_STATUS.md
M english-learning-project/project.godot
?? docs/MT_INSTRUCTION/M0-T02_CODEX_INSTRUCTION.md
```

其中 `project.godot` 中的 `[dotnet]` 修改可能是用户此前使用 Godot .NET 编辑器产生的既有修改。

---

# 4. 阶段 0：任务前置基线检查

任何工程修改前，必须先执行本阶段。

## 4.1 检查 Git 状态

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git log -5 --oneline
git status --short --untracked-files=all
git diff -- english-learning-project/project.godot
git diff -- docs/IMPLEMENTATION_STATUS.md
git diff --check
```

## 4.2 检查 M0-T01 提交

必须确认：

- M0-T01 已有本地 Git 提交。
- 该提交包含：
  - `GameLexicon.sln`
  - `src/`
  - `tests/`
  - `tools/`
  - M0-T01 状态更新

如果 M0-T01 尚未提交：

1. 停止 M0-T02。
2. 不修改 Godot 工程。
3. 报告需要先创建 M0-T01 本地检查点提交。
4. 不得由 Codex 自动提交，除非用户明确授权。

## 4.3 检查 `project.godot` 既有修改

必须解释：

- 当前 `[dotnet]` 差异具体是什么。
- 是否属于有效的 Godot .NET 工程配置。
- 是否可能由用户此前使用 .NET 编辑器打开工程后自动写入。
- 是否需要作为 M0-T02 前置基线保留。

如果 `project.godot` 中存在非本任务预期、来源不明或可能被覆盖的修改：

1. 停止。
2. 不恢复。
3. 不覆盖。
4. 不继续 M0-T02。
5. 报告差异与建议处理方式。

## 4.4 检查前置 Git 检查点

理想状态：

```text
working tree clean
```

如果当前变更全部属于用户已确认保留的 M0-T02 前置准备，但尚未提交：

1. 停止任务。
2. 建议用户先创建本地 Git 检查点。
3. 不自动提交，除非用户明确授权。

## 4.5 检查 Godot 编辑器进程

确认当前没有另一个 Godot 编辑器实例正在打开：

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

可以检查相关进程：

```powershell
Get-Process |
  Where-Object {
    $_.ProcessName -match "godot"
  } |
  Select-Object ProcessName, Id, Path
```

如果发现 Godot 编辑器正在打开该项目：

1. 停止。
2. 要求用户保存并关闭编辑器。
3. 不修改场景、脚本、`.csproj` 或 `project.godot`。

## 4.6 阶段 0 停止条件

出现以下任意情况时停止：

- M0-T01 没有本地提交。
- Git 中存在未确认的用户修改。
- `project.godot` 既有差异来源不明。
- 当前没有可恢复的 Git 检查点。
- Godot 编辑器正在打开同一项目。
- 仓库中出现多个 `project.godot`。
- 仓库根目录或 Godot 工程目录与文档不一致。

停止时只报告：

- 阻塞原因
- 证据
- 用户需要执行的处理步骤

不得继续后续阶段。

---

# 5. 阶段 1：验证 Godot 4.7.1 .NET 环境

只有阶段 0 通过后才能执行。

## 5.1 验证文件存在

执行：

```powershell
$GodotDir = "E:\SteamLibrary\steamapps\common\Godot Engine"
$GodotExe = Join-Path $GodotDir "Godot_v4.7.1-stable_mono_win64.exe"
$GodotConsoleExe = Join-Path $GodotDir "Godot_v4.7.1-stable_mono_win64_console.exe"
$SteamGodotExe = Join-Path $GodotDir "godot.windows.opt.tools.64.exe"
$GodotSharpDir = Join-Path $GodotDir "GodotSharp"

Test-Path $GodotExe
Test-Path $GodotConsoleExe
Test-Path $SteamGodotExe
Test-Path $GodotSharpDir
```

四项必须均返回：

```text
True
```

## 5.2 验证 Godot 版本

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" --version

& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" --version

& "E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe" --version
```

必须记录完整输出，并确认：

- 版本为 Godot 4.7.1。
- 主程序和控制台程序属于 mono/.NET 发行版。
- Steam 启动兼容程序当前也指向 4.7.1 .NET/mono 版本。
- 架构为 64 位。

## 5.3 验证 `GodotSharp`

执行：

```powershell
Get-ChildItem `
  "E:\SteamLibrary\steamapps\common\Godot Engine\GodotSharp" `
  -Force

Get-ChildItem `
  "E:\SteamLibrary\steamapps\common\Godot Engine\GodotSharp" `
  -Recurse `
  -File |
  Select-Object -First 100 FullName
```

必须确认：

- `GodotSharp/` 存在。
- 目录非空。
- 包含 Godot C#/.NET 支持所需文件。
- 不存在明显版本混用迹象。

不得修改或重新生成 Godot 安装目录中的任何文件。

## 5.4 验证 .NET SDK

执行：

```powershell
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
```

必须记录：

- 默认 SDK。
- 已安装 SDK。
- 已安装 Runtime。
- 系统架构。

预期至少存在：

```text
8.0.423
10.0.301
```

要求：

- 架构为 x64。
- Godot 与 .NET SDK 架构一致。
- 不因为默认 SDK 是 .NET 10，就手工把 Godot 项目改成 `net10.0`。

## 5.5 查看实际命令行能力

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" --help
```

记录与以下能力相关的实际参数：

- `--editor`
- `--path`
- `--headless`
- `--quit`
- `--build-solutions`
- C# 构建相关参数

后续命令必须以该版本实际 `--help` 输出为准。

## 5.6 阶段 1 强制停止条件

出现以下任意情况时立即停止：

- 主程序不存在。
- 控制台程序不存在。
- `GodotSharp/` 不存在或为空。
- 版本不是 4.7.1。
- 无法确认是 .NET/mono 版本。
- Steam 启动兼容程序仍为标准版。
- Godot 与 .NET SDK 架构不一致。
- .NET 8 SDK 不可用。
- Godot 启动或版本检查失败。

停止时：

- 不修改 Godot 工程。
- 不创建 `.csproj`。
- 不创建 `.cs`。
- 不创建 `.tscn`。
- 不修改根解决方案。
- 不修改 Godot 安装目录。

---

# 6. 阶段 2：初始化现有 Godot C# 工程

只有阶段 1 通过后才能执行。

## 6.1 初始化规则

必须在现有工程中初始化：

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

要求：

- 保留现有 `project.godot`。
- 保留现有 `icon.svg`。
- 保留现有 `icon.svg.import`。
- 不创建第二个 Godot 工程。
- 不移动或重命名工程目录。
- 不手工伪造 Godot `.csproj`。
- 不手工伪造 Godot 本地 `.sln`。
- 不提前将 Godot 项目目标框架改为 `net10.0`。
- 不修改 Godot 安装目录。
- 不修改 Steam 设置。

## 6.2 创建目录

创建：

```text
english-learning-project/scripts/
english-learning-project/scenes/
```

不得创建本任务无关目录。

## 6.3 创建最小 C# 脚本

创建：

```text
english-learning-project/scripts/AppRoot.cs
```

内容：

```csharp
using Godot;

public partial class AppRoot : Control
{
    public override void _Ready()
    {
        GD.Print("GameLexicon AppRoot initialized.");
    }
}
```

要求：

- 文件名与类名一致。
- 继承 `Control`。
- 使用 `partial`。
- 只包含最小启动逻辑。
- 不连接数据库。
- 不执行 OCR。
- 不调用 Win32。
- 不实现导航。
- 不实现业务功能。
- 不添加第三方依赖。

## 6.4 让 Godot 生成 C# 项目文件

优先使用控制台程序进行可观察的命令行操作。

根据实际 `--help` 输出，使用 Godot 4.7.1 支持的方式打开工程并初始化 C# 项目。

可使用：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --editor `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

如果需要 GUI 初始化，可使用：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" `
  --editor `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

目标是由 Godot 生成：

```text
english-learning-project/*.csproj
```

Godot 如同时生成本地 `.sln`：

- 可以保留。
- 根解决方案仍以：

  ```text
  D:\UGit\EnglishLearningProject\GameLexicon.sln
  ```

  为主。

必须记录：

- 生成的 `.csproj` 文件名。
- 是否生成本地 `.sln`。
- `TargetFramework`。
- Godot SDK 配置。
- `RootNamespace`。
- 其他关键构建属性。
- 是否生成 `.godot/mono/`。

## 6.5 目标框架规则

当前普通 C# 项目暂时使用：

```text
net10.0
```

Godot 项目的目标框架必须以 Godot 自动生成结果为准。

不得：

- 将 Godot 项目手工改为 `net10.0`。
- 直接批量修改所有普通 C# 项目。
- 删除 Godot 自动生成的 SDK 设置。
- 忽略 TargetFramework 不兼容错误。

如果 Godot 项目不能引用当前普通类库：

1. 停止后续集成。
2. 报告：
   - Godot 项目实际 TargetFramework
   - Domain/Application/Infrastructure 实际 TargetFramework
   - 完整构建错误
   - 推荐的最小调整方案
3. 不自行批量修改目标框架。
4. 等待用户确认。

---

# 7. 阶段 3：创建基础主场景

只有阶段 2 成功生成并构建 Godot C# 项目后才能执行。

## 7.1 创建场景文件

创建：

```text
english-learning-project/scenes/App.tscn
```

基础节点结构：

```text
AppRoot (Control)
├─ Background (ColorRect)
├─ AppLayout (HBoxContainer)
│  ├─ Sidebar (PanelContainer)
│  └─ ContentHost (MarginContainer)
├─ ToastLayer (CanvasLayer)
├─ ModalLayer (CanvasLayer)
└─ GlobalLoadingOverlay (CanvasLayer)
```

## 7.2 节点要求

### AppRoot

- 类型：`Control`
- 脚本：

  ```text
  res://scripts/AppRoot.cs
  ```

- Layout：Full Rect
- 作为根节点

### Background

- 类型：`ColorRect`
- Layout：Full Rect
- 作为背景
- 不实现正式主题

### AppLayout

- 类型：`HBoxContainer`
- Layout：Full Rect
- 承载侧栏和内容区

### Sidebar

- 类型：`PanelContainer`
- 只创建占位结构
- 建议最小宽度约 220 像素
- 不创建正式导航按钮

### ContentHost

- 类型：`MarginContainer`
- 填满剩余空间
- 只创建占位结构

### ToastLayer

- 类型：`CanvasLayer`
- 当前为空

### ModalLayer

- 类型：`CanvasLayer`
- 当前为空

### GlobalLoadingOverlay

- 类型：`CanvasLayer`
- 当前为空

## 7.3 设置主场景

设置：

```text
res://scenes/App.tscn
```

为项目主场景。

对 `project.godot` 只允许：

- 设置主场景。
- 保留或写入 Godot C#/.NET 必需配置。
- 接受 Godot 自动生成的必要修改。

不得修改：

- 渲染器。
- 物理引擎。
- 输入映射。
- 插件。
- 与本任务无关的窗口配置。
- 与本任务无关的项目特性。

## 7.4 最小界面要求

运行后只需显示：

- 基础应用窗口。
- 背景。
- 左侧占位区。
- 主内容占位区。

不实现：

- 正式视觉设计。
- 路由。
- 导航按钮。
- 动画。
- OCR。
- 词条。
- 复习。
- 截图收件箱。

---

# 8. 阶段 4：加入根解决方案

## 8.1 将 Godot 项目加入根解决方案

使用实际生成的 `.csproj` 路径：

```powershell
dotnet sln `
  "D:\UGit\EnglishLearningProject\GameLexicon.sln" `
  add `
  "D:\UGit\EnglishLearningProject\english-learning-project\<实际文件名>.csproj"
```

不得猜测文件名。

## 8.2 配置项目引用

Godot 项目可以引用：

```text
GameLexicon.Domain
GameLexicon.Application
GameLexicon.Infrastructure
```

引用方向：

```text
Godot UI
  ├─ Application
  ├─ Domain
  └─ Infrastructure
```

禁止：

```text
Domain → Godot
Application → Godot
Infrastructure → Godot
```

## 8.3 检查引用结构

必须确认：

```text
Domain
  └─ 无生产项目引用

Application
  └─ Domain

Infrastructure
  ├─ Application
  └─ Domain

Godot
  ├─ Application
  ├─ Domain
  └─ Infrastructure

CaptureBridge
  └─ 无生产项目引用
```

不得存在循环依赖。

## 8.4 解决方案项目数量

M0-T01 完成后根解决方案包含 7 个项目。

M0-T02 完成后应包含 8 个项目：

```text
1. GameLexicon.Domain
2. GameLexicon.Application
3. GameLexicon.Infrastructure
4. GameLexicon.CaptureBridge
5. GameLexicon.Domain.Tests
6. GameLexicon.Application.Tests
7. GameLexicon.Infrastructure.Tests
8. Godot C# 项目
```

---

# 9. 本任务明确不做

M0-T02 不实现：

- 正式导航。
- ViewModel。
- 日志系统。
- 配置系统。
- SQLite。
- 数据库迁移。
- CaptureBridge 平台逻辑。
- 全局快捷键。
- Windows Graphics Capture。
- OCR。
- Tesseract。
- TTS。
- 词条管理。
- 词条库。
- 复习系统。
- 导入导出。
- 云服务。
- 在线词典。
- LLM 功能。
- Steam 更新策略。
- Godot 安装目录修改。
- M0-T03。

---

# 10. 阶段 5：构建与验证

## 10.1 构建 Godot C# 项目

优先使用 Godot 4.7.1 控制台程序和实际支持的参数。

可尝试：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --editor `
  --quit
```

如该命令不能触发 C# 构建：

1. 查看 `--help`。
2. 使用 Godot 4.7.1 实际支持的等价参数。
3. 记录最终实际命令。

必要时执行：

```powershell
dotnet build `
  "D:\UGit\EnglishLearningProject\english-learning-project\<实际文件名>.csproj"
```

要求：

- Godot C# 项目构建成功。
- `AppRoot.cs` 编译成功。
- 不存在 GodotSharp SDK 缺失错误。
- 不存在场景脚本类型错误。

## 10.2 构建根解决方案

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet restore GameLexicon.sln
dotnet build GameLexicon.sln --no-restore
```

要求：

- 构建成功。
- 记录警告数。
- 记录错误数。
- 不忽略失败项目。

## 10.3 运行测试

执行：

```powershell
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 现有测试全部通过。
- 记录各测试项目结果。
- 不跳过失败测试。

## 10.4 Godot headless 验证

执行适用于 Godot 4.7.1 的最小 headless 验证命令。

推荐起点：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --editor `
  --quit
```

要求：

- 项目能加载。
- C# 脚本可解析。
- 主场景可加载。
- 无脚本编译错误。
- 无场景资源错误。

不得通过删除脚本或场景规避错误。

## 10.5 Godot GUI 验证

运行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

确认：

- 应用窗口可以启动。
- 主场景正确加载。
- 左侧占位区可见。
- 主内容占位区可见。
- 输出出现：

  ```text
  GameLexicon AppRoot initialized.
  ```

- 没有 C# 异常。
- 没有资源加载错误。

如果 Codex 无法观察 GUI：

- 不得声称已完成 GUI 验收。
- 将 GUI 验收明确列为用户人工确认项。
- 仍需完成 headless 和构建验证。

## 10.6 验证 Steam 兼容程序

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe" --version
```

只验证：

- 当前版本为 4.7.1。
- 当前属于 .NET/mono 版本。

本任务不验证 Steam 自动更新行为。

---

# 11. 阶段 6：Git、状态和 Skill 影响检查

## 11.1 Git 检查

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff
git diff --check
```

确认：

- 未创建第二个 `project.godot`。
- 未移动 `english-learning-project/`。
- 未修改 Godot 安装目录。
- `.godot/`、`bin/` 和 `obj/` 被忽略。
- `icon.svg.import` 未被错误忽略。
- `project.godot` 修改仅限本任务必要内容。
- 变更范围只属于 M0-T02。

## 11.2 更新实施状态

更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

必须记录：

- Task ID：`M0-T02`
- 名称
- 状态
- 开始时间
- 完成时间
- Godot 主程序路径
- Godot 控制台程序路径
- Steam 兼容程序路径
- Godot 完整版本
- `GodotSharp/` 验证结果
- `.NET SDK`
- 生成的 Godot `.csproj`
- Godot TargetFramework
- 创建文件
- 修改文件
- 执行命令
- Godot C# 构建结果
- 根解决方案构建结果
- 测试结果
- headless 验证结果
- GUI 人工验收状态
- Git diff
- 已知限制

完成后将下一任务设置为：

```text
M0-T03：实现基础导航
```

状态：

```text
Not Started
```

不得自动执行 M0-T03。

## 11.3 更新环境文档

如果环境文档存在，更新：

```text
docs/ENVIRONMENT.md
```

仅记录已验证的实际事实：

- Godot 安装目录
- Godot 主程序
- 控制台程序
- Steam 兼容程序
- GodotSharp
- Godot 版本
- .NET SDK
- Godot 项目 TargetFramework

不得记录：

- 密码
- Token
- 私密代理凭证

## 11.4 Agent 交接

如果存在：

```text
docs/AGENT_HANDOFF.md
```

更新最近一次任务交接摘要：

- 当前任务
- 主领域
- 使用的 Agent
- 证据
- 决策
- 变更
- 验证
- 阻塞
- 下一允许动作

不得复制完整终端日志。

## 11.5 Skill Impact Review

应用：

```text
.agents/skills/skill-maintenance/SKILL.md
```

必须报告：

- 本任务实际使用的 Skills
- 是否改变了可复用的 Godot 工作流
- 是否需要更新 `godot-workflow`
- 是否需要更新 `milestone-workflow`
- 是否需要更新 `project-routing`
- 是否需要更新 `SKILLS_CATALOG.md`
- 是否需要更新 `SKILL_CHANGELOG.md`

规则：

- 普通代码和场景创建不自动触发 Skill 修改。
- 只有工作流、路径来源、命令、前置条件、停止条件或验收标准发生可复用变化时，才修改 Skill。
- 如 Skill 被修改，最终报告必须提示重新启动或新开 Codex 会话。
- 不得为记录一次性日志而修改 Skill。

---

# 12. 自动化验收清单

- [ ] M0-T01 有本地 Git 提交
- [ ] M0-T02 开始前工作区基线已确认
- [ ] `project.godot` 既有差异已解释
- [ ] 当前没有其他 Godot 实例编辑该项目
- [ ] Godot .NET 主程序存在
- [ ] Godot .NET 控制台程序存在
- [ ] Steam 兼容程序存在
- [ ] `GodotSharp/` 存在且非空
- [ ] Godot 版本确认为 4.7.1
- [ ] Godot 确认为 .NET/mono 版本
- [ ] .NET SDK 架构为 x64
- [ ] .NET 8 SDK 可用
- [ ] 现有 Godot 工程已生成 `.csproj`
- [ ] `.csproj` 由 Godot 生成
- [ ] 已记录 Godot TargetFramework
- [ ] 已创建 `scripts/AppRoot.cs`
- [ ] 已创建 `scenes/App.tscn`
- [ ] `App.tscn` 已设置为主场景
- [ ] Godot 项目已加入根解决方案
- [ ] 根解决方案包含 8 个项目
- [ ] 项目引用方向正确
- [ ] 不存在循环引用
- [ ] Godot C# 项目构建成功
- [ ] 根解决方案构建成功
- [ ] 全部现有测试通过
- [ ] Godot headless 验证通过
- [ ] 未创建第二个 Godot 工程
- [ ] 未修改 Godot 安装目录
- [ ] 未提前实现后续功能
- [ ] `git diff --check` 通过
- [ ] 实施状态已更新
- [ ] Skill Impact Review 已完成

---

# 13. 人工验收清单

- [ ] `english-learning-project/` 路径未变化
- [ ] 仓库中仍只有一个 `project.godot`
- [ ] Godot 安装目录未被修改
- [ ] 根解决方案仍在仓库根目录
- [ ] Godot 主场景可在 .NET 编辑器中打开
- [ ] 应用启动后显示基础窗口
- [ ] 左侧占位区域可见
- [ ] 主内容占位区域可见
- [ ] 输出中出现 `GameLexicon AppRoot initialized.`
- [ ] 没有 C# 编译错误
- [ ] 没有场景资源错误
- [ ] 没有同时使用两个 Godot 实例编辑项目
- [ ] 没有实现 OCR、SQLite、截图、TTS、词条或复习
- [ ] `IMPLEMENTATION_STATUS.md` 与实际结果一致

---

# 14. 全局强制停止条件

出现以下任意情况时，停止并报告，不自行扩大修改：

- Git 工作区存在未确认用户修改。
- M0-T01 没有本地提交。
- `project.godot` 既有差异来源不明。
- Godot 编辑器正在打开同一项目。
- Godot 版本或 .NET 支持验证失败。
- GodotSharp 缺失。
- .NET SDK 架构不匹配。
- Godot `.csproj` 无法由 Godot 生成。
- Godot TargetFramework 与普通类库不兼容。
- 需要批量修改所有项目目标框架。
- 根解决方案构建失败且原因超出本任务。
- 发现第二个 Godot 工程。
- 必须修改 Godot 安装目录才能继续。
- 必须修改 Steam 设置才能继续。

停止后不得：

- 批量修改目标框架。
- 删除用户修改。
- `git reset --hard`。
- `git clean -fd`。
- 强制提交。
- 自动执行 M0-T03。

---

# 15. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M0-T02
- 名称:
- 状态:
- 是否执行 M0-T03: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M0-T01 commit:
- Initial Git status:
- Existing project.godot diff:
- Baseline conclusion:

## 环境验证

- Godot installation directory:
- Godot main executable:
- Godot console executable:
- Steam compatibility executable:
- Godot version:
- Godot .NET/mono:
- GodotSharp:
- .NET SDK:
- Architecture:

## Godot 项目

- Generated .csproj:
- Generated local .sln:
- TargetFramework:
- Main scene:
- Main script:

## 创建的文件

- ...

## 修改的文件

- ...

## 项目引用

```text
...
```

## 执行的命令

```text
...
```

## 构建结果

```text
...
```

## 测试结果

```text
...
```

## Godot 验证

- C# project build:
- Headless:
- GUI:
- User manual verification required:

## Git diff

```text
...
```

## Skill Impact Review

- Skills used:
- Update required:
- Skills updated:
- Documentation updated:
- Restart required:

## 人工验收

- ...

## 警告和已知限制

- ...

## 下一任务

- M0-T03
- Status: Not Started
- Not automatically executed
```

---

# 16. 可直接执行的总指令

请执行：

```text
M0-T02：初始化现有 Godot 4.7.1 .NET/C# 工程与基础主场景
```

固定路径：

```text
仓库根目录：
D:\UGit\EnglishLearningProject

Godot 工程目录：
D:\UGit\EnglishLearningProject\english-learning-project

根解决方案：
D:\UGit\EnglishLearningProject\GameLexicon.sln

Godot .NET 安装目录：
E:\SteamLibrary\steamapps\common\Godot Engine

Godot .NET 主程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe

Godot .NET 控制台程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe

Steam 启动兼容程序：
E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe
```

严格按照本文件执行。

特别要求：

1. 先完成 Git 和 `project.godot` 基线检查。
2. 如果工作区不是已确认的安全基线，立即停止。
3. 只执行 M0-T02。
4. 不执行 M0-T03。
5. 不修改 Godot 安装目录。
6. 不修改 Steam 设置。
7. 不批量修改项目目标框架。
8. 修改完成后执行构建、测试、Git 检查和 Skill Impact Review。
