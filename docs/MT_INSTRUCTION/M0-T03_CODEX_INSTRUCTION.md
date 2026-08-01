# M0-T03 Codex 执行指令

## 任务名称

```text
M0-T03：实现基础导航
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M0-T03_CODEX_INSTRUCTION.md
```

本任务仅实现应用壳层的基础导航，不实现六个页面的业务功能，不执行 M0-T04。

---

# 1. 任务目标

根据 `docs/PRODUCT_SPEC.md` 完成：

- Sidebar
- RouteHost
- 六个占位页面
- `NavigationService`

六个主导航页面：

```text
首页
截图收件箱
词条库
今日复习
统计
设置
```

核心验收：

1. 点击导航不重新创建整个 `AppRoot`。
2. 当前页面对应的导航按钮有明确选中状态。
3. 页面只在 `RouteHost` 内切换。
4. 六个导航项均可到达对应占位页面。
5. 默认进入“首页”。

---

# 2. 固定路径

```text
仓库根目录：
D:\UGit\EnglishLearningProject

Godot 工程目录：
D:\UGit\EnglishLearningProject\english-learning-project

根解决方案：
D:\UGit\EnglishLearningProject\GameLexicon.sln

Godot 项目：
D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj

Godot 主场景：
D:\UGit\EnglishLearningProject\english-learning-project\scenes\App.tscn

Godot 主脚本：
D:\UGit\EnglishLearningProject\english-learning-project\scripts\AppRoot.cs

Godot .NET 主程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe

Godot .NET 控制台程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe
```

---

# 3. 必须阅读

开始前完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/AGENT_HANDOFF.md
docs/MT_INSTRUCTION/M0-T03_CODEX_INSTRUCTION.md
```

如存在以下文件，也必须读取：

```text
docs/AGENT_SYSTEM.md
docs/SKILLS_CATALOG.md
.agents/skills/project-routing/SKILL.md
.agents/skills/godot-workflow/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain: Godot
Primary writer: 主协调 Agent
Supporting agents:
- godot_specialist
- milestone_architect
- skill_curator（仅在收尾影响审查需要时）
```

专业 Agent 默认只读。主协调 Agent 是同一工作区唯一默认写入者。

---

# 4. 前置基线检查

## 4.1 Git

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git log -3 --oneline
git diff --check
```

必须确认：

- 用户已通过 UGit 提交 M0-T02。
- 当前工作区干净。
- 不存在未确认修改。
- `docs/IMPLEMENTATION_STATUS.md` 中 M0-T02 为 `Done`。
- M0-T03 为 `Not Started`。

如工作区不干净：

1. 立即停止。
2. 列出所有修改和未跟踪文件。
3. 不覆盖、不恢复、不暂存、不提交。
4. 等待用户确认。

## 4.2 Godot 进程

执行：

```powershell
Get-Process -ErrorAction SilentlyContinue |
  Where-Object { $_.ProcessName -match "godot" } |
  Select-Object ProcessName, Id, Path
```

若 Godot 编辑器正在打开该项目：

1. 停止修改。
2. 要求用户保存并关闭编辑器。
3. 不直接结束不确定用途的进程。

## 4.3 M0-T02 产物

必须确认存在：

```text
GameLexicon.sln
english-learning-project/project.godot
english-learning-project/EnglishLearningProject.csproj
english-learning-project/EnglishLearningProject.sln
english-learning-project/scenes/App.tscn
english-learning-project/scripts/AppRoot.cs
```

检查解决方案：

```powershell
dotnet sln "D:\UGit\EnglishLearningProject\GameLexicon.sln" list
```

必须包含 8 个项目。

检查 Godot 项目配置：

```powershell
Select-String `
  -Path "D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj" `
  -Pattern "Project Sdk|TargetFramework"
```

预期：

```text
Godot.NET.Sdk/4.7.1
net8.0
```

M0-T03 不得修改目标框架或项目引用结构。

## 4.4 基线构建

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet restore GameLexicon.sln
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 根解决方案构建成功。
- 所有现有测试通过。
- 基线失败时停止，不把既有问题混入 M0-T03。

---

# 5. 允许的文件范围

建议创建：

```text
english-learning-project/scripts/AppRoute.cs
english-learning-project/scripts/NavigationService.cs

english-learning-project/scenes/dashboard/DashboardView.tscn
english-learning-project/scenes/capture_inbox/CaptureInboxView.tscn
english-learning-project/scenes/library/LibraryView.tscn
english-learning-project/scenes/review/ReviewView.tscn
english-learning-project/scenes/statistics/StatisticsView.tscn
english-learning-project/scenes/settings/SettingsView.tscn
```

Godot 自动生成的对应 `.uid` 文件可以保留。

允许修改：

```text
english-learning-project/scenes/App.tscn
english-learning-project/scripts/AppRoot.cs
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/ENVIRONMENT.md（仅当环境事实变化）
docs/SKILLS_CATALOG.md（仅当 Skill 影响审查要求）
docs/SKILL_CHANGELOG.md（仅当 Skill 实际更新）
.agents/skills/*/SKILL.md（仅当可复用工作流变化）
```

要求：

- 优先增量修改 M0-T02 的 `App.tscn`。
- 不移动或重建 Godot 工程。
- 不修改 Godot 安装目录或 Steam 设置。
- 不修改普通类库目标框架。
- 不修改项目引用方向。

---

# 6. 路由模型

## 6.1 `AppRoute`

创建：

```text
english-learning-project/scripts/AppRoute.cs
```

建议内容：

```csharp
public enum AppRoute
{
    Dashboard,
    CaptureInbox,
    Library,
    Review,
    Statistics,
    Settings
}
```

要求：

- 内部键使用稳定英文名。
- UI 显示文本使用中文。
- 当前不实现动态路由、URL、深链接或历史栈。

## 6.2 `NavigationService`

创建：

```text
english-learning-project/scripts/NavigationService.cs
```

职责：

1. 持有 `RouteHost` 引用。
2. 维护 `AppRoute → PackedScene` 映射。
3. 维护当前路由。
4. 切换当前占位页面。
5. 允许 `AppRoot` 更新按钮选中状态。
6. 防止重复点击当前路由时重复创建页面。
7. 保证不重新创建 `AppRoot`。

建议最小 API：

```csharp
public AppRoute CurrentRoute { get; }

public void Register(AppRoute route, PackedScene scene);

public Control Navigate(AppRoute route);
```

可使用等价设计，但不得扩大职责。

## 6.3 页面实例策略

优先采用“懒加载并缓存”：

- 首次导航时实例化。
- 添加到 `RouteHost`。
- 后续切换通过 `Visible` 或等价方式显示/隐藏。
- 重复点击当前路由不重新实例化。
- 每个路由最多一个页面实例。

导航错误必须清晰报告：

- 未注册路由。
- 空 `PackedScene`。
- 场景根节点不是 `Control`。
- `RouteHost` 或按钮缺失。

不得静默失败。

---

# 7. 六个占位页面

创建：

```text
res://scenes/dashboard/DashboardView.tscn
res://scenes/capture_inbox/CaptureInboxView.tscn
res://scenes/library/LibraryView.tscn
res://scenes/review/ReviewView.tscn
res://scenes/statistics/StatisticsView.tscn
res://scenes/settings/SettingsView.tscn
```

每个场景：

- 根节点为 `Control`、`MarginContainer` 或 `ScrollContainer`。
- Layout 为 Full Rect。
- 至少显示标题和一行占位说明。
- 不实现数据库、OCR、截图、词条或复习业务。
- 不添加第三方资源。
- 不创建无必要的页面脚本。

推荐结构：

```text
<PageName>View (MarginContainer)
└─ Content (VBoxContainer)
   ├─ Title (Label)
   └─ PlaceholderMessage (Label)
```

标题与占位说明：

| Route | 标题 | 占位说明 |
|---|---|---|
| Dashboard | 首页 | 仪表盘功能将在后续里程碑实现。 |
| CaptureInbox | 截图收件箱 | 截图收件箱功能将在后续里程碑实现。 |
| Library | 词条库 | 词条库功能将在后续里程碑实现。 |
| Review | 今日复习 | 复习队列与题型将在后续里程碑实现。 |
| Statistics | 统计 | 学习统计功能将在后续里程碑实现。 |
| Settings | 设置 | 配置与设置功能将在后续里程碑实现。 |

这些页面只证明导航结构，不代表业务功能完成。

---

# 8. 更新 `App.tscn`

目标结构：

```text
AppRoot (Control)
├─ Background (ColorRect)
├─ AppLayout (HBoxContainer)
│  ├─ Sidebar (PanelContainer)
│  │  └─ SidebarMargin (MarginContainer)
│  │     └─ SidebarContent (VBoxContainer)
│  │        ├─ AppTitle (Label)
│  │        ├─ NavigationList (VBoxContainer)
│  │        │  ├─ DashboardButton (Button)
│  │        │  ├─ CaptureInboxButton (Button)
│  │        │  ├─ LibraryButton (Button)
│  │        │  ├─ ReviewButton (Button)
│  │        │  ├─ StatisticsButton (Button)
│  │        │  └─ SettingsButton (Button)
│  │        └─ SidebarSpacer (Control，可选)
│  └─ ContentHost (MarginContainer)
│     └─ RouteHost (Control)
├─ ToastLayer (CanvasLayer)
├─ ModalLayer (CanvasLayer)
└─ GlobalLoadingOverlay (CanvasLayer)
```

可按实际布局略微调整，但必须保留：

```text
Sidebar
NavigationList
ContentHost
RouteHost
六个导航按钮
```

## Sidebar

- 最小宽度约 220px。
- 不被内容区挤压到不可用。
- 可显示 `GameLexicon`。
- 不实现正式主题系统。

## 导航按钮

文本：

```text
首页
截图收件箱
词条库
今日复习
统计
设置
```

要求：

- 六个按钮可点击。
- 宽度填满 Sidebar。
- 使用 `ToggleMode`、`ButtonGroup` 或等价方式表达互斥选中状态。
- 当前页面按钮保持选中。
- 默认首页选中。
- 当前不添加图标或第三方字体。

## RouteHost

- 位于 `ContentHost` 内。
- Layout 为 Full Rect。
- 只承载当前页面或缓存页面。
- 不得重新加载整个 `App.tscn`。
- 不得通过更改主场景实现导航。

---

# 9. 更新 `AppRoot.cs`

M0-T03 中 `AppRoot.cs` 仅负责导航装配：

1. 获取 `RouteHost`。
2. 获取六个导航按钮。
3. 加载六个 `PackedScene`。
4. 创建 `NavigationService`。
5. 注册六条路由。
6. 连接按钮事件。
7. 默认导航到 Dashboard。
8. 更新互斥选中状态。
9. 保留 M0-T02 初始化输出。

保留：

```text
GameLexicon AppRoot initialized.
```

可新增：

```text
GameLexicon navigation initialized.
Navigated to: Dashboard
```

不得添加：

- 数据库
- 配置和日志
- OCR
- CaptureBridge
- TTS
- 词条或复习业务
- 静态服务定位器
- Autoload
- 页面历史栈

运行时场景路径使用 `res://`，不得硬编码 Windows 绝对路径。

---

# 10. 自动验证

## 10.1 Godot 项目构建

```powershell
dotnet build `
  "D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj"
```

要求：

- 0 错误。
- 记录警告数。

## 10.2 根解决方案

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet restore GameLexicon.sln
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目全部构建成功。
- 所有现有测试通过。
- 不删除测试换取通过。

## 10.3 Headless

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --editor `
  --build-solutions `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --quit
```

再执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --quit-after 3
```

如参数不同，读取 `--help` 后使用等价命令并记录。

不得出现：

```text
SCRIPT ERROR
Parser Error
Failed to load
Invalid get node
NullReferenceException
Unhandled exception
```

应能看到：

```text
GameLexicon AppRoot initialized.
GameLexicon navigation initialized.
```

默认路由应为 Dashboard。

## 10.4 结构检查

确认：

- 六个页面场景全部存在。
- `App.tscn` 包含 `NavigationList` 和 `RouteHost`。
- 六个按钮节点全部存在。
- 没有使用 `ChangeSceneToFile("res://scenes/App.tscn")` 作为导航实现。
- 运行时代码没有 Windows 绝对资源路径。
- `NavigationService` 没有数据库、OCR 或 CaptureBridge 逻辑。

---

# 11. GUI 人工验收

运行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

## 初始状态

确认：

- 应用正常启动。
- 默认显示首页。
- 首页按钮默认选中。
- 其他按钮未选中。
- Sidebar 约 220px。
- AppRoot 没有重复出现。

## 六个导航项

依次点击：

```text
首页
截图收件箱
词条库
今日复习
统计
设置
```

每次确认：

- 页面标题与按钮一致。
- 占位文本正确。
- 当前按钮选中。
- 上一按钮取消选中。
- ContentHost 没有错误叠加页面。
- Sidebar 不消失。
- 窗口不重启。
- 无异常或明显布局跳变。

## 重复点击

连续点击当前按钮至少两次，确认：

- 页面不叠加副本。
- AppRoot 不重建。
- 无异常。
- 当前按钮保持选中。

## 往返切换

执行：

```text
首页 → 设置 → 首页 → 词条库 → 首页
```

确认内容与选中状态均正确。

Codex 无法观察 GUI 时：

- 状态设置为 `Awaiting Manual Verification`。
- 不得在用户确认前将 M0-T03 标为 Done。

---

# 12. 本任务明确不做

不得实现：

- 页面真实业务数据
- Dashboard 指标
- 截图收件箱列表
- OCR 工作台
- 词条库搜索
- 复习题目
- 学习统计
- 配置持久化
- 日志系统
- JSON 设置
- SQLite
- CaptureBridge
- TTS
- 图标系统
- 正式主题
- 动画框架
- 页面历史栈
- 返回/前进导航
- 路由参数
- 深链接
- 多窗口
- M0-T04

---

# 13. 强制停止条件

出现以下任意情况时停止：

- Git 工作区不干净且变更未确认。
- 无法确认 M0-T02 已提交。
- M0-T02 未标记 Done。
- Godot 编辑器正在打开同一项目。
- 基线构建或测试失败。
- 根解决方案不是 8 个项目。
- Godot 项目不再是 `Godot.NET.Sdk/4.7.1` 或 `net8.0`。
- 现有 `App.tscn` 与 M0-T02 报告严重不一致。
- 必须重建整个 Godot 工程。
- 必须修改目标框架或引用结构。
- 必须修改 Godot 安装目录或 Steam 设置。
- 导航实现需要提前实现 M0-T04 或后续业务。
- 发现来源不明的用户文件可能被覆盖。

停止后不得：

- 自动恢复用户文件。
- `git reset --hard`。
- `git clean -fd`。
- 自动提交。
- 自动执行 M0-T04。

---

# 14. Git 与文档收尾

完成代码和自动验证后执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
```

确认：

- 只有 M0-T03 范围内变更。
- `.godot/`、`bin/`、`obj/` 未进入 Git。
- 未修改目标框架、Godot 安装目录或 Steam 设置。
- 未实现后续业务。
- 未创建 Git 提交。

GUI 人工验收前：

```text
M0-T03 = Awaiting Manual Verification
```

GUI 全部通过后更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

记录：

- Task ID、名称、状态
- 开始和完成时间
- 创建和修改文件
- 六个路由
- 默认路由
- 页面实例策略
- 按钮选中状态实现
- 构建、测试、Headless 和 GUI 结果
- Git diff 概况
- 已知限制
- 六页仍为占位页面

下一任务：

```text
M0-T04：配置与日志
```

状态：

```text
Not Started
```

不得自动执行 M0-T04。

更新：

```text
docs/AGENT_HANDOFF.md
```

只有环境事实变化时才更新 `docs/ENVIRONMENT.md`。

---

# 15. Skill Impact Review

任务结束后执行 `skill-maintenance`，报告：

```text
Primary domain
Primary agent
Supporting agents
Skills used
Skill update required
Skills updated
Documentation updated
Restart required
```

正常情况下：

```text
Skill update required: No
```

仅在可复用工作流、构建命令、Agent 路由或验收模板发生变化时更新 Skill。普通导航代码和场景变化不构成 Skill 更新理由。

---

# 16. 自动验收清单

- [ ] M0-T02 已提交
- [ ] 工作区初始干净
- [ ] M0-T02 状态为 Done
- [ ] 基线构建成功
- [ ] 基线测试通过
- [ ] 创建 AppRoute
- [ ] 创建 NavigationService
- [ ] 创建六个独立占位页面
- [ ] App.tscn 包含 Sidebar
- [ ] App.tscn 包含 NavigationList
- [ ] App.tscn 包含 RouteHost
- [ ] 六个按钮存在
- [ ] 默认路由为 Dashboard
- [ ] 当前路由有状态
- [ ] 重复导航不会重复创建页面
- [ ] 导航不重新创建 AppRoot
- [ ] Godot 项目构建成功
- [ ] 根解决方案构建成功
- [ ] 全部测试通过
- [ ] Headless 构建通过
- [ ] Headless 场景加载通过
- [ ] 无脚本和资源错误
- [ ] 未实现后续业务
- [ ] git diff --check 通过
- [ ] Skill Impact Review 完成

---

# 17. 人工验收清单

- [ ] 应用正常启动
- [ ] 默认显示首页
- [ ] 首页按钮默认选中
- [ ] Sidebar 约 220px
- [ ] 六个按钮全部可点击
- [ ] 六个页面标题正确
- [ ] 六个页面占位文本正确
- [ ] 当前按钮有选中状态
- [ ] 上一按钮取消选中
- [ ] 页面只在 RouteHost 内切换
- [ ] Sidebar 不因导航消失
- [ ] AppRoot 不重新创建
- [ ] 重复点击不叠加页面
- [ ] 往返切换无异常
- [ ] 无 C# 异常
- [ ] 无资源加载错误
- [ ] 无明显布局崩坏
- [ ] 未提前实现业务功能

---

# 18. 最终报告格式

```markdown
## 任务结果

- Task ID: M0-T03
- 名称: 实现基础导航
- 状态:
- 是否执行 M0-T04: No
- Git commit created: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M0-T02 commit:
- Initial Git status:
- Solution projects:
- Baseline build:
- Baseline tests:

## 导航实现

- Routes:
- Default route:
- NavigationService:
- Page instance strategy:
- Current selection strategy:
- AppRoot recreation prevention:

## 创建的文件

- ...

## 修改的文件

- ...

## 执行命令

```text
...
```

## 构建结果

- Godot project:
- Root solution:
- Warnings:
- Errors:

## 测试结果

- Total:
- Passed:
- Failed:
- Skipped:

## Godot 验证

- Headless build:
- Headless scene load:
- Initialization output:
- Navigation output:
- GUI manual verification:

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

## 已知限制

- Pages are placeholders.
- No business functionality implemented.
- ...

## 下一任务

- M0-T04：配置与日志
- Status: Not Started
- Not automatically executed
```

---

# 19. 可直接执行的总指令

请执行：

```text
M0-T03：实现基础导航
```

严格按照：

```text
docs/MT_INSTRUCTION/M0-T03_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先确认用户通过 UGit 提交的 M0-T02 已存在。
2. 开始时必须是干净工作区。
3. 只实现 Sidebar、RouteHost、六个占位页面和 NavigationService。
4. 导航不得重新创建 AppRoot。
5. 当前页面必须有选中状态。
6. 不实现页面业务。
7. 不执行 M0-T04。
8. 不创建 Git 提交。
9. 自动验证后等待 GUI 人工验收。
10. GUI 验收通过后再把 M0-T03 标记为 Done。
11. 完成后执行 Git diff 和 Skill Impact Review。
