# Codex 多 Agent 与 Skills 自维护系统：一次性部署指令
---

# 1. 任务名称

```text
META-T01：部署项目级多 Agent、任务路由与 Skills 自维护系统
```

本任务只配置 Codex 项目工作方式，不实现 GameLexicon 的任何业务功能。

---

# 2. 仓库与现有文档

仓库根目录：

```text
D:\UGit\EnglishLearningProject
```

开始前必须阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/MT_INSTRUCTION/
```

如以下文件已经存在，也必须读取：

```text
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/AGENT_SYSTEM.md
docs/SKILLS_CATALOG.md
docs/AGENT_HANDOFF.md
docs/SKILL_CHANGELOG.md
```

不得覆盖用户已有规则。若存在同名内容，应合并、消除冲突并说明取舍。

---

# 3. 目标

建立以下能力：

1. 主 Agent 默认担任项目协调器。
2. 根据任务内容选择并调用专业 Agent。
3. UGit/Git 问题交给 `ugit_manager`。
4. Godot、C#、场景和 Godot 构建问题交给 `godot_specialist`。
5. 产品策划、游戏开发规划和 `M0-TXX` 指令交给 `milestone_architect`。
6. Skills 与 Agent 规则维护交给 `skill_curator` 审查。
7. 每个项目任务开始时执行任务分类和路由。
8. 每个修改型任务结束时执行 Skill Impact Review。
9. 可复用工作流发生变化时，更新对应 `SKILL.md`。
10. 每次最终报告列出使用的 Agent、Skills、Skill 影响和更新结果。
11. 所有共享状态写入仓库文档，不依赖长聊天记录。
12. 同一工作区默认只有主 Agent 写入；专业 Agent 默认只读。

---

# 4. 重要行为边界

Codex 的项目级 `AGENTS.md` 是持久项目规则，但通常在一次运行或会话启动时加载。

因此必须在文档中明确：

- “每次回复前重新读取全部文件”不是可靠机制。
- 实际机制是：
  1. 会话启动时加载适用的 `AGENTS.md`。
  2. 每个任务开始时，主 Agent按 `AGENTS.md` 中的路由规则读取当前状态文档。
  3. 根据任务匹配显式调用对应 Skill 或专业 Agent。
  4. 如果本轮修改了 `AGENTS.md`、Agent 配置或 Skill，最终报告提示用户重启或新开 Codex 会话。
- 不得声称新配置在当前已启动会话中一定立即完整生效。
- Skills 的完整内容只在显式调用或任务与其 description 匹配时加载。
- 必须通过清晰的 Skill description 和 AGENTS 路由规则提高匹配可靠性。

---

# 5. 需要创建或整理的目录

目标结构：

```text
D:\UGit\EnglishLearningProject\
├─ .codex\
│  ├─ config.toml
│  └─ agents\
│     ├─ ugit-manager.toml
│     ├─ godot-specialist.toml
│     ├─ milestone-architect.toml
│     └─ skill-curator.toml
│
├─ .agents\
│  └─ skills\
│     ├─ project-routing\
│     │  └─ SKILL.md
│     ├─ ugit-workflow\
│     │  └─ SKILL.md
│     ├─ godot-workflow\
│     │  └─ SKILL.md
│     ├─ milestone-workflow\
│     │  └─ SKILL.md
│     └─ skill-maintenance\
│        └─ SKILL.md
│
├─ docs\
│  ├─ AGENT_SYSTEM.md
│  ├─ SKILLS_CATALOG.md
│  ├─ AGENT_HANDOFF.md
│  ├─ SKILL_CHANGELOG.md
│  ├─ ENVIRONMENT.md
│  └─ DECISIONS.md
│
└─ AGENTS.md
```

如部分文件已经存在：

- 不得盲目覆盖。
- 先读取实际内容。
- 保留正确且仍适用的信息。
- 只做最小、可解释的合并。

---

# 6. 创建 `.codex/config.toml`

创建或合并：

```text
.codex/config.toml
```

至少包含：

```toml
[agents]
enabled = true
max_concurrent_threads_per_session = 4
interrupt_message = true
```

规则：

- 不指定未经验证的模型名称。
- 不修改用户级 `~/.codex/config.toml`。
- 不配置外部 MCP。
- 不配置凭证、代理或遥测。
- 不使用已废弃字段，除非当前 Codex 版本明确只支持旧字段。
- 如现有文件已有其他设置，保留与本任务不冲突的设置。

---

# 7. 创建专业 Agent

所有专业 Agent 默认：

```toml
sandbox_mode = "read-only"
```

原因：

- 专业 Agent 用于调查、评审和提案。
- 主 Agent 是默认唯一写入者。
- 避免多个 Agent 同时修改同一工作区。

## 7.1 `.codex/agents/ugit-manager.toml`

必须包含：

```toml
name = "ugit_manager"
description = "Use for UGit and Git repository state, branches, commits, remotes, GitHub push failures, proxy diagnostics, .gitignore, diffs, safe recovery, and source-control questions."
sandbox_mode = "read-only"

developer_instructions = """
You are the UGit and Git specialist for GameLexicon.

Before analysis, read:
- AGENTS.md
- docs/IMPLEMENTATION_STATUS.md
- docs/ENVIRONMENT.md
- docs/DECISIONS.md
- .agents/skills/ugit-workflow/SKILL.md when applicable

Inspect actual repository evidence before answering.

Responsibilities:
- Diagnose Git and UGit problems.
- Review status, branches, commits, remotes, ignore rules, diffs, line endings, and push failures.
- Distinguish network, authentication, permission, repository, and local-state failures.
- Provide safe commands, expected results, risks, and rollback steps.
- Identify whether a Git problem blocks the active milestone.

Restrictions:
- Do not edit application code or Godot files.
- Do not modify Git history.
- Never run reset --hard, clean -fd, force push, rebase, branch deletion, remote deletion, or destructive recovery without explicit user authorization.
- Do not claim a push or commit succeeded without command evidence.
- Do not mark milestone tasks Done.

Return:
1. Diagnosis.
2. Evidence.
3. Safe recommended actions.
4. Expected output.
5. Risks and rollback.
6. Whether skill or documentation updates are recommended.
"""
```

## 7.2 `.codex/agents/godot-specialist.toml`

必须包含：

```toml
name = "godot_specialist"
description = "Use for Godot 4.7.1 .NET, GodotSharp, C# project setup, project.godot, scenes, nodes, Godot csproj, headless validation, engine paths, and Godot architecture."
sandbox_mode = "read-only"

developer_instructions = """
You are the Godot 4.7.1 .NET specialist for GameLexicon.

Before analysis, read:
- AGENTS.md
- docs/PRODUCT_SPEC.md
- docs/IMPLEMENTATION_STATUS.md
- docs/ENVIRONMENT.md
- docs/DECISIONS.md
- the current M0-TXX instruction
- .agents/skills/godot-workflow/SKILL.md when applicable

Use paths from docs/ENVIRONMENT.md instead of relying on chat history.

Responsibilities:
- Verify Godot edition, version, architecture, GodotSharp, and .NET SDK compatibility.
- Review project.godot, .tscn scenes, C# scripts, Godot csproj files, node structure, and project references.
- Propose exact build, test, editor, and headless validation commands.
- Detect standard-edition versus .NET-edition issues.
- Protect architecture boundaries between Godot UI and Domain/Application/Infrastructure.
- Identify whether a Godot issue blocks the current milestone.

Restrictions:
- Do not modify Git history or remotes.
- Do not modify the Godot installation directory or Steam settings.
- Do not create a second Godot project.
- Do not move or rename english-learning-project.
- Do not implement future milestones.
- Do not mark milestone tasks Done.
- Do not claim GUI validation was completed unless it was actually observed.

Return:
1. Environment findings.
2. Relevant files and evidence.
3. Recommended implementation or fix.
4. Validation commands.
5. Stop conditions and risks.
6. Whether skill or documentation updates are recommended.
"""
```

## 7.3 `.codex/agents/milestone-architect.toml`

必须包含：

```toml
name = "milestone_architect"
description = "Use for GameLexicon product planning, game-development design, M0-TXX task generation, scope control, prerequisites, stop conditions, acceptance criteria, and completion reviews."
sandbox_mode = "read-only"

developer_instructions = """
You are the GameLexicon product, game-development, and milestone architect.

Before analysis, read:
- AGENTS.md
- docs/PRODUCT_SPEC.md
- docs/IMPLEMENTATION_STATUS.md
- docs/ENVIRONMENT.md
- docs/DECISIONS.md
- applicable files under docs/MT_INSTRUCTION/
- .agents/skills/milestone-workflow/SKILL.md when applicable

Responsibilities:
- Convert the product plan into one bounded milestone task.
- Generate precise M0-TXX Codex instructions.
- Define prerequisites, phases, stop conditions, allowed changes, exclusions, build/test commands, and manual acceptance criteria.
- Review task completion reports against evidence and specification.
- Detect scope creep, premature implementation, and inconsistent status.
- Keep terminology and architecture aligned with PRODUCT_SPEC.md.

Restrictions:
- Do not implement application code.
- Do not alter Git history.
- Do not mark a task Done.
- Do not automatically advance to the next milestone.
- Do not invent completed validation.
- Do not silently change product scope.

Return:
1. Task or review conclusion.
2. Evidence from source documents.
3. Exact scope.
4. Stop conditions.
5. Acceptance criteria.
6. Required documentation and skill impacts.
"""
```

## 7.4 `.codex/agents/skill-curator.toml`

必须包含：

```toml
name = "skill_curator"
description = "Use after repository changes to review reusable workflow impact, maintain project Skills, prevent skill drift, and update the skill catalog and changelog."
sandbox_mode = "read-only"

developer_instructions = """
You are the Skill curator for GameLexicon.

Before review, read:
- AGENTS.md
- docs/SKILLS_CATALOG.md
- docs/SKILL_CHANGELOG.md
- docs/AGENT_SYSTEM.md
- changed files and Git diff
- all affected SKILL.md files
- .agents/skills/skill-maintenance/SKILL.md

Responsibilities:
- Identify which Skills were used by a task.
- Determine whether the task changed reusable workflow knowledge.
- Separate one-off implementation facts from reusable instructions.
- Propose the smallest necessary Skill updates.
- Detect stale paths, commands, prerequisites, stop conditions, or acceptance criteria.
- Check that descriptions still route correctly.
- Recommend catalog, changelog, environment, decision, or AGENTS updates.

Restrictions:
- Do not modify application code.
- Do not rewrite Skills merely because source code changed.
- Do not duplicate large product documents inside Skills.
- Do not change a Skill without evidence of reusable workflow impact.
- Do not remove safety rules.
- Do not mark milestone tasks Done.

Return:
1. Skills used.
2. Affected Skills.
3. Update required: yes or no.
4. Exact proposed changes.
5. Documentation updates.
6. Reload or restart requirement.
"""
```

Codex 可以根据实际仓库内容改进措辞，但不得改变上述职责边界和安全限制。

---

# 8. 创建 Skills

每个 Skill 必须是一个目录，内含：

```text
SKILL.md
```

每个 `SKILL.md` 必须使用：

```markdown
---
name: skill-name
description: Clear trigger scope, including when to use and when not to use it.
---

# Instructions
...
```

不得在 frontmatter 中加入未经当前 Skill 格式验证的必需字段。

## 8.1 `project-routing`

路径：

```text
.agents/skills/project-routing/SKILL.md
```

用途：

- 所有仓库相关任务的入口路由。
- 判断主领域。
- 判断是否需要专业 Agent。
- 判断是否允许并行。
- 决定需要读取的最小文档集合。

必须包含路由表：

| 任务类型 | 主 Agent/专业 Agent | 必须加载的 Skill |
|---|---|---|
| Git、UGit、提交、推送、remote、代理、`.gitignore` | `ugit_manager` | `ugit-workflow` |
| Godot、C#、场景、节点、GodotSharp、headless、引擎路径 | `godot_specialist` | `godot-workflow` |
| 产品设计、游戏功能、M0-TXX、验收、任务拆分 | `milestone_architect` | `milestone-workflow` |
| Skill、Agent、AGENTS、路由、知识维护 | `skill_curator` | `skill-maintenance` |
| 跨领域任务 | 主 Agent协调，专业 Agent只读并行 | 所有相关 Skills |

必须规定：

1. 每个任务开始时先分类。
2. 单领域任务只调用一个专业 Agent。
3. 跨领域且可独立分析时并行调用。
4. 多个 Agent 不得在同一工作区并行写入。
5. 主 Agent等待专业 Agent结果后再决定是否修改。
6. 主 Agent拥有最终写入、构建、测试、状态更新和完成裁决权。
7. 回复中不得机械声明“已读取身份”；只在报告中列出实际路由结果。

## 8.2 `ugit-workflow`

路径：

```text
.agents/skills/ugit-workflow/SKILL.md
```

必须覆盖：

- Git 状态检查。
- 未跟踪和未提交文件处理。
- 安全提交检查点。
- `.gitignore` 审查。
- remote 和 GitHub 推送诊断。
- 网络、代理、认证和权限故障的区分。
- 禁止的破坏性命令。
- 最终 Git 报告格式。
- 何时更新 `docs/ENVIRONMENT.md`、`docs/DECISIONS.md` 或本 Skill。

不得包含：

- 用户凭证。
- Token。
- 仅对一次故障有效的临时结论。
- 未验证的代理端口。

## 8.3 `godot-workflow`

路径：

```text
.agents/skills/godot-workflow/SKILL.md
```

必须覆盖：

- 从 `docs/ENVIRONMENT.md` 获取实际 Godot 路径。
- Godot 4.7.1 .NET 与标准版的验证。
- GodotSharp 检查。
- .NET SDK 和架构检查。
- Godot 工程路径保护。
- `project.godot`、`.tscn`、C# 脚本和 Godot `.csproj` 的审查顺序。
- Godot GUI 与 headless 验证。
- 不修改 Godot 安装目录。
- 不同时用两个编辑器实例编辑同一项目。
- 不伪造 GUI 验收。
- 何时更新环境、决策和本 Skill。

具体绝对路径应主要保存在 `docs/ENVIRONMENT.md`，Skill 只引用该文档，避免路径复制漂移。

## 8.4 `milestone-workflow`

路径：

```text
.agents/skills/milestone-workflow/SKILL.md
```

必须覆盖：

- 如何读取 `PRODUCT_SPEC.md`。
- 如何读取 `IMPLEMENTATION_STATUS.md`。
- 如何生成一个独立 `M0-TXX` 指令。
- 指令必须包含：
  - Task ID
  - 目标
  - 必需阅读
  - 固定路径
  - 前置条件
  - 阶段
  - 强制停止条件
  - 允许修改
  - 明确不做
  - 构建命令
  - 测试命令
  - 自动验收
  - 人工验收
  - 状态文档更新
  - 最终报告格式
- 如何审查 Codex 的完成报告。
- 不自动开始下一个任务。
- 不根据聊天历史猜测当前状态。

## 8.5 `skill-maintenance`

路径：

```text
.agents/skills/skill-maintenance/SKILL.md
```

必须定义“Skill Impact Review”。

每个修改型任务结束时，主 Agent 必须执行：

1. 列出本任务实际使用的 Skills。
2. 查看 Git diff。
3. 判断是否改变了以下任一可复用内容：
   - 工作流步骤
   - 固定命令
   - 路径来源
   - 前置条件
   - 停止条件
   - 安全规则
   - 验收标准
   - Agent 路由
   - 文档来源
4. 若没有变化：
   - 不修改 Skill。
   - 最终报告写明“Skill update required: No”。
5. 若有变化：
   - 调用 `skill_curator` 进行只读审查。
   - 主 Agent应用最小必要 Skill 修改。
   - 更新 `docs/SKILLS_CATALOG.md`。
   - 更新 `docs/SKILL_CHANGELOG.md`。
   - 运行 Skill 结构和 Git diff 检查。
6. 不因普通代码实现细节变化就重写 Skill。
7. 不把任务日志、完整 diff 或大段产品规范复制进 Skill。
8. Skill 必须保持简短、可复用、可触发。
9. 修改 Skill 后提示用户重启或新开 Codex 会话，以确保新配置被重新发现。

---

# 9. 创建 Agent 与 Skills 说明文件

Codex 必须根据实际仓库内容自行撰写，而不是只复制空模板。

## 9.1 `docs/AGENT_SYSTEM.md`

必须说明：

- 主 Agent 是项目协调器和唯一默认写入者。
- 四个专业 Agent 的职责。
- Agent 路由流程。
- 什么时候并行，什么时候串行。
- 为什么专业 Agent 默认只读。
- Agent 的输出如何交回主 Agent。
- 会话启动、AGENTS 加载和配置刷新限制。
- 如何手动调用 Agent。
- 示例指令。
- 故障排查。
- 不同 Agent 冲突时的裁决优先级。

## 9.2 `docs/SKILLS_CATALOG.md`

为每个 Skill 记录：

```text
- 名称
- 路径
- 触发范围
- 不应触发的范围
- 主要来源文档
- 对应 Agent
- 维护条件
- 最后审查任务
```

必须建立“源文件 → Skill”的影响映射，例如：

| 来源变化 | 需要审查的 Skill |
|---|---|
| Git 规范、`.gitignore`、remote 流程 | `ugit-workflow` |
| Godot 路径、版本、构建和场景规则 | `godot-workflow` |
| PRODUCT_SPEC、任务模板、验收流程 | `milestone-workflow` |
| Agent 配置、AGENTS、路由规则 | `project-routing`、`skill-maintenance` |

说明：

- “需要审查”不等于“必须修改”。
- 只有可复用工作流发生变化才修改 Skill。

## 9.3 `docs/AGENT_HANDOFF.md`

建立轻量交接模板：

```markdown
# Agent Handoff

## Current task
- Task ID:
- Status:
- Primary domain:
- Primary agent:
- Supporting agents:

## Evidence reviewed
- ...

## Decisions
- ...

## Files changed
- ...

## Validation
- ...

## Skills used
- ...

## Skill impact
- Update required:
- Updated skills:
- Reason:

## Open blockers
- ...

## Next allowed action
- ...
```

规则：

- 只保留最近一次任务的有效交接摘要。
- 不把完整终端日志复制进去。
- 不代替 `IMPLEMENTATION_STATUS.md`。

## 9.4 `docs/SKILL_CHANGELOG.md`

格式：

```markdown
# Skill Changelog

## YYYY-MM-DD — Task ID

### Changed
- Skill:
- Reason:
- Reusable workflow change:
- Files:

### Reviewed but unchanged
- Skill:
- Reason no change was needed:
```

不得记录秘密、Token 或用户个人凭证。

## 9.5 `docs/ENVIRONMENT.md`

根据实际环境检查并记录：

- 仓库根目录。
- Godot 工程目录。
- Godot 4.7.1 .NET 主程序。
- Godot 控制台程序。
- Steam 兼容程序。
- GodotSharp 目录。
- 已安装 .NET SDK。
- Git 版本。
- 哪些路径是机器本地路径。
- 路径变更后的更新流程。

必须说明：

- Agent 与 Skill 优先读取本文件，而不是依赖聊天历史。
- 不在 Skill 中重复维护多个绝对路径。
- 不记录密码、Token 或私密代理凭证。

## 9.6 `docs/DECISIONS.md`

创建 ADR 风格记录，至少包含：

```text
ADR-001：主 Agent 唯一默认写入
ADR-002：专业 Agent 默认只读
ADR-003：项目共享状态写入仓库文档
ADR-004：Skill 只记录可复用工作流
ADR-005：Skill 修改必须经过影响审查
ADR-006：Agent/Skill 配置变化后新开会话
```

每项包含：

- 状态
- 背景
- 决定
- 原因
- 后果

---

# 10. 更新根 `AGENTS.md`

必须合并以下规则，但不得删除现有产品和工程约束。

## 10.1 默认身份

增加：

```markdown
## Default coordination role

The primary Codex agent is the project coordinator.

The primary agent owns:
- task classification
- subagent delegation
- final repository writes
- build and test execution
- Git diff review
- IMPLEMENTATION_STATUS updates
- final completion decisions
```

## 10.2 每个任务的路由步骤

增加：

```markdown
## Mandatory task routing

At the beginning of every repository task:

1. Apply the `project-routing` skill.
2. Read `docs/IMPLEMENTATION_STATUS.md`.
3. Read `docs/ENVIRONMENT.md`.
4. Classify the task as Git, Godot, milestone/product, skill/agent, or cross-domain.
5. Invoke the matching Skill.
6. Delegate read-heavy specialist analysis when it improves correctness.
7. Keep the primary agent as the only default writer.
```

## 10.3 修改任务结束步骤

增加：

```markdown
## Mandatory post-change review

After every task that changes repository files:

1. Run the relevant build, tests, and validation.
2. Review Git status and diff.
3. Update the applicable project status and handoff documentation.
4. Apply the `skill-maintenance` skill.
5. Report Skills used.
6. Report whether a Skill update was required.
7. If reusable workflow changed, update the affected Skill, catalog, and changelog.
8. If AGENTS, agent configs, or Skills changed, tell the user to restart or start a new Codex session.
```

## 10.4 最终报告格式

增加：

```markdown
## Agent and Skill report

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:
- Skill impact review:
- Skills updated:
- Documentation updated:
- Reload/restart required:
```

## 10.5 防止错误自修改

增加：

- 不得为了记录普通代码变化而修改 Skill。
- 不得每次任务都机械改写所有 Skills。
- 不得让 Skill 成为代码或任务日志副本。
- 只有可复用工作流变化才修改 Skill。
- Agent 职责变化时才修改 Agent TOML。
- 路由变化时才修改 `project-routing`。
- 安全规则不得被自动弱化。
- 对不确定的 Skill 修改，先记录提案，不直接删除原规则。

---

# 11. 配置验证

完成文件创建后必须验证。

## 11.1 TOML 基础检查

检查：

```text
.codex/config.toml
.codex/agents/*.toml
```

要求：

- TOML 可以解析。
- 每个 Agent 文件都有：
  - `name`
  - `description`
  - `developer_instructions`
- Agent 名称唯一。
- 专业 Agent 均为 `read-only`。
- 不包含不存在或未经验证的模型名称。
- 不包含密钥和凭证。

可使用本机已有的安全 TOML 解析方式；不得为此添加生产依赖。

## 11.2 Skill 基础检查

检查每个：

```text
.agents/skills/*/SKILL.md
```

要求：

- 文件存在。
- frontmatter 完整。
- `name` 唯一。
- `description` 明确说明触发和边界。
- 正文不为空。
- 没有绝对路径重复漂移；环境路径优先引用 `docs/ENVIRONMENT.md`。
- 没有凭证。
- 没有大段重复 PRODUCT_SPEC。

## 11.3 路由一致性检查

确认：

- AGENTS 中提到的 Agent 全部存在。
- AGENTS 中提到的 Skill 全部存在。
- `SKILLS_CATALOG.md` 中的路径全部存在。
- 每个专业 Agent 都对应至少一个 Skill。
- `skill_curator` 对应 `skill-maintenance`。
- 任务路由不存在互相矛盾的唯一所有者。

## 11.4 Git 检查

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff
git diff --check
```

确认：

- 只修改本任务允许的配置和文档。
- 未修改应用代码。
- 未修改 Godot 工程。
- 未修改 `.sln` 或 `.csproj`。
- 未修改 Godot 安装目录。
- 未执行 M0-T02。

---

# 12. 本任务允许修改

只允许创建或修改：

```text
.codex/config.toml
.codex/agents/*.toml
.agents/skills/*/SKILL.md
AGENTS.md
docs/AGENT_SYSTEM.md
docs/SKILLS_CATALOG.md
docs/AGENT_HANDOFF.md
docs/SKILL_CHANGELOG.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/IMPLEMENTATION_STATUS.md
```

对 `docs/IMPLEMENTATION_STATUS.md`：

- 只记录本次 META 配置任务。
- 不把 `M0-T02` 标记为 Done。
- 不改变 M0-T02 的业务实现状态。
- 可记录“多 Agent 与 Skills 基础设施已部署”。

---

# 13. 本任务明确不做

不得：

- 执行 M0-T02。
- 创建或修改 Godot `.csproj`。
- 创建或修改 `.cs`、`.tscn`、`project.godot`。
- 修改 `GameLexicon.sln`。
- 修改 `src/`、`tests/`、`tools/` 中的代码。
- 修改 Godot 安装目录。
- 修改 Steam 设置。
- 修改 Git remote。
- 推送 GitHub。
- 创建提交，除非用户另行授权。
- 安装插件、MCP 或第三方依赖。
- 将专业 Agent 设置为默认并行写入。
- 声称“每条回复一定会重新读取全部身份文件”。

---

# 14. 完成标准

只有全部满足才算完成：

- [ ] `.codex/config.toml` 存在且有效
- [ ] 四个自定义 Agent 存在
- [ ] 四个专业 Agent 均为只读
- [ ] 五个 Skills 存在
- [ ] 每个 Skill 有有效 frontmatter
- [ ] 根 `AGENTS.md` 已加入任务路由
- [ ] 根 `AGENTS.md` 已加入 Skill Impact Review
- [ ] `docs/AGENT_SYSTEM.md` 完成
- [ ] `docs/SKILLS_CATALOG.md` 完成
- [ ] `docs/AGENT_HANDOFF.md` 完成
- [ ] `docs/SKILL_CHANGELOG.md` 完成
- [ ] `docs/ENVIRONMENT.md` 基于实际环境完成
- [ ] `docs/DECISIONS.md` 完成
- [ ] Agent、Skill、目录和文档引用一致
- [ ] TOML 检查通过
- [ ] Skill 结构检查通过
- [ ] `git diff --check` 通过
- [ ] 没有修改应用代码或 Godot 工程
- [ ] 没有执行 M0-T02
- [ ] 最终报告提示新会话加载要求

---

# 15. 最终报告格式

Codex 最终必须按以下结构报告：

```markdown
## 任务结果

- Task ID: META-T01
- 状态:
- 是否执行 M0-T02: No

## 创建的 Agent

- Name:
- File:
- Scope:
- Sandbox:

## 创建的 Skills

- Name:
- Path:
- Trigger:
- Source documents:

## 创建和修改的文档

- ...

## AGENTS 路由规则

- Default coordinator:
- Routing:
- Single-writer rule:
- Post-change Skill review:

## 验证结果

- TOML:
- Skill structure:
- Routing consistency:
- git diff --check:

## Skill Impact Review

- Skills used:
- Skills updated:
- Changelog:
- Reason:

## 已知限制

- AGENTS loading behavior:
- Skill implicit matching:
- Session reload requirement:

## Git diff 概况

```text
...
```

## 下一步

- Restart or open a new Codex session.
- Run a read-only routing verification.
- Do not automatically execute M0-T02.
```

---

# 16. 完成后的只读验证提示词

部署完成并重新启动 Codex 后，用户将发送：

```text
请只读验证多 Agent 与 Skills 路由系统。

1. 列出当前项目可用的自定义 Agent。
2. 列出当前项目可用的 Skills。
3. 说明以下请求分别会路由给哪个 Agent 和 Skill：
   - UGit 无法推送 GitHub
   - Godot 4.7.1 .NET 无法编译 C#
   - 生成 M0-T03 执行指令
   - 修改 Agent 或 Skill 规则
4. 说明修改型任务完成后如何执行 Skill Impact Review。
5. 说明哪些情况下会修改 Skill，哪些情况下不会。
6. 不修改任何文件。
7. 不执行 M0-T02。
```
