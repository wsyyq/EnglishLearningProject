# M1-T06 Codex 执行指令

## 任务名称

```text
M1-T06：SQLite 例句 Repository
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T06_CODEX_INSTRUCTION.md
```

本任务只实现：

```text
SqliteSentenceExampleRepository
ISentenceExampleRepository 六个方法
例句保存与读取
词条—例句链接保存与读取
主要例句原子切换
链接移除
Infrastructure 自动化测试
```

本任务不实现：

- `SqliteVocabularyRepository`
- `SqliteTagRepository`
- Application UseCase
- Godot UI
- 查询页面
- Migration003
- M1-T07 或任何后续任务

---

# 1. 已确认的前置基线

用户已确认最新提交：

```text
52160ac85ba27362f7bea76feec49e6d2036cc93
```

当前已知状态：

- 当前分支：`main`
- Git 工作区干净
- M1-T05 提交内容完整
- M1-T05 = `Done`
- M1-T06 = `Not Started`
- 当前无 Godot 编辑器或残留进程
- 根解决方案包含 8 个项目
- 目标框架：
  - Godot 桌面：`net8.0`
  - Godot Android 条件目标：`net9.0`
  - Domain、Application、Infrastructure：`net8.0`
  - 三个测试项目、CaptureBridge：`net10.0`
- 构建成功，0 警告、0 错误
- 测试 212/212 通过：
  - Domain：111
  - Application：61
  - Infrastructure：40
- Migration001 哈希：
  `1fd5546081fe87c479ebd21d52e26f7d1dfaa636`
- Migration002 已提交，Version = 2
- 运行时同时注册 Migration001 和 Migration002
- `ISentenceExampleRepository` 六个方法完整
- Application 公共 API 未泄漏 SQLite、Godot、Infrastructure 或 IQueryable
- 数据库、sidecar、日志、备份及构建产物未进入 Git

Codex 开始时仍须重新核验，不得只依赖本文件。

---

# 2. 任务目标

实现：

```text
GameLexicon.Infrastructure
└─ SqliteSentenceExampleRepository
   └─ implements ISentenceExampleRepository
```

负责：

1. 根据 ID 读取例句。
2. 按词条读取全部例句及链接信息。
3. 保存或更新例句本体。
4. 保存或更新词条—例句链接。
5. 在事务中原子设置主要例句。
6. 幂等移除词条—例句链接。

依赖方向必须保持：

```text
Application
└─ 定义 ISentenceExampleRepository

Infrastructure
└─ 实现 ISentenceExampleRepository
   ├─ 使用 SqliteConnectionFactory
   ├─ 使用 Microsoft.Data.Sqlite
   └─ 映射 Domain / Application 读模型

Godot View
└─ 不直接调用 Repository 或 SQL
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
docs/MT_INSTRUCTION/M1-T06_CODEX_INSTRUCTION.md
```

重点阅读：

```text
PRODUCT_SPEC.md
- F07：词条编辑
- F08：重复词条处理
- F09：词条库
- 第 7 节分层职责
- 第 10 节领域模型
- 第 12 节 SQLite 数据模型
- 第 13 节数据库和 Repository
- 第 18 节词条与例句策略

DECISIONS.md
- ADR-007：SentenceExample.CaptureId 可空
```

完整阅读现有代码：

```text
src/GameLexicon.Domain/Entries/SentenceExample.cs
src/GameLexicon.Domain/Entries/EntryExampleLink.cs

src/GameLexicon.Application/Abstractions/Persistence/
ISentenceExampleRepository.cs

src/GameLexicon.Application/Entries/Queries/
SentenceExampleDetails.cs

src/GameLexicon.Infrastructure/Persistence/
DatabaseOptions.cs
SqliteConnectionFactory.cs

src/GameLexicon.Infrastructure/Persistence/Migrations/
IDatabaseMigration.cs
MigrationRunner.cs
Migration001_Initial.cs
Migration002_ManualExamplesAndSearchSupport.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/**
```

必须以当前真实接口签名和命名空间为准，不得凭本文件猜测。

如存在以下 Skills，也必须阅读：

```text
.agents/skills/project-routing/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Infrastructure / Persistence / Sentence Examples

Primary writer:
primary coordinator

Supporting agents:
- milestone architect：只读审查 Repository 语义和范围
- skill curator：仅在 Skill Impact Review 需要时调用
```

本任务通常不需要 godot specialist。

---

# 4. 阶段 0：重新核验基线

## 4.1 Git

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline 52160ac85ba27362f7bea76feec49e6d2036cc93
git diff --check
```

必须确认：

- 当前分支为 `main`
- 工作区干净
- 提交存在
- 提交完整包含 M1-T05
- 没有未确认用户修改

工作区不干净时立即停止：

- 不恢复
- 不覆盖
- 不暂存
- 不提交
- 不执行 `git reset --hard`
- 不执行 `git clean -fd`

## 4.2 状态

确认：

```text
M1-T05 = Done
M1-T06 = Not Started
```

状态不一致时停止。

## 4.3 项目与框架

执行：

```powershell
dotnet sln GameLexicon.sln list
```

确认仍有 8 个项目。

不得修改：

- 目标框架
- 项目引用
- 解决方案结构
- NuGet 包版本

## 4.4 Migration 完整性

记录：

```powershell
git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"
```

必须为：

```text
1fd5546081fe87c479ebd21d52e26f7d1dfaa636
```

确认 Migration002：

```text
Version = 2
已提交
已注册
```

本任务不得修改 Migration001 或 Migration002。

## 4.5 Repository 契约

确认 `ISentenceExampleRepository` 当前六个方法及精确签名：

```text
GetByIdAsync
GetForEntryAsync
SaveAsync
SaveLinkAsync
SetPrimaryAsync
RemoveLinkAsync
```

不得修改接口。

## 4.6 基线构建与测试

优先执行：

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

预期：

```text
Build: 0 warnings, 0 errors
Tests: 212/212 passed
```

本任务不新增 NuGet 包，通常不需要 Restore。

---

# 5. 建议目录

建议创建：

```text
src/GameLexicon.Infrastructure/
└─ Persistence/
   └─ Repositories/
      └─ SqliteSentenceExampleRepository.cs
```

测试：

```text
tests/GameLexicon.Infrastructure.Tests/
└─ Persistence/
   └─ Repositories/
      └─ SqliteSentenceExampleRepositoryTests.cs
```

允许创建一个最小内部映射 helper：

```text
SentenceExampleSqlMapper.cs
```

条件：

- 仅服务于当前 Repository
- `internal`
- 不形成通用 ORM
- 不泄漏到 Application 公共 API
- 不引入第三方包

---

# 6. `SqliteSentenceExampleRepository`

必须：

```csharp
public sealed class SqliteSentenceExampleRepository
    : ISentenceExampleRepository
```

构造函数建议：

```csharp
public SqliteSentenceExampleRepository(
    SqliteConnectionFactory connectionFactory)
```

要求：

- `connectionFactory` 为 null 时抛 `ArgumentNullException`
- Repository 不持有长期打开的连接
- 每次方法按需打开连接
- 所有连接和事务正确 Dispose/await Dispose
- 不依赖 Godot
- 不依赖 View
- 不记录学习文本
- 不缓存例句内容

---

# 7. 数据映射规则

## 7.1 GUID

数据库保存：

```text
小写 GUID 字符串
```

写入使用：

```csharp
guid.ToString("D").ToLowerInvariant()
```

或等价稳定格式。

读取必须：

- 使用 `Guid.TryParse`
- 无效 GUID 视为数据库损坏
- 抛出清晰异常
- 不静默生成新 GUID
- 不把原始数据库文本写进异常

## 7.2 UTC

数据库时间：

```text
UTC ISO 8601
```

写入使用项目既有格式。

读取必须：

- 使用 InvariantCulture
- 要求 offset 为 zero
- 非法或非 UTC 数据视为数据库损坏
- 不静默使用本地时间
- 不调用 `DateTimeOffset.Now`

## 7.3 Nullable

正确映射：

```text
capture_id NULL → CaptureId = null
ocr_region_id NULL → OcrRegionId = null
game_title NULL → GameTitle = null
```

`screenshot_crop_path` 数据库为非空，空字符串合法。

## 7.4 整数和布尔

```text
is_primary = 0 → false
is_primary = 1 → true
```

读取其他值时必须拒绝，不能把任意非零值默认为 true。

```text
sort_order >= 0
target_start >= 0
target_length > 0
```

Domain 构造会再次验证。

## 7.5 不复制规范化逻辑

Repository 不得：

- 调用 `ToLower`
- Trim `SentenceText`
- 重新计算 `NormalizedSentence`
- 修改 `GameTitle`
- 调整目标范围

只持久化 Domain 已验证的值。

---

# 8. `GetByIdAsync`

语义：

- `exampleId == Guid.Empty` → 参数异常
- 找不到 → `null`
- 找到 → 返回完整 Domain `SentenceExample`
- 传播 `CancellationToken`
- 不返回数据库 DTO
- 不读取链接元数据

SQL 只查询：

```text
sentence_examples
```

必须显式列出字段，禁止 `SELECT *`。

---

# 9. `GetForEntryAsync`

语义：

- `entryId == Guid.Empty` → 参数异常
- 不存在或没有链接 → 空只读列表
- 返回 `SentenceExampleDetails`
- 排序：
  1. `sort_order ASC`
  2. `example_id ASC`

查询 join：

```text
entry_examples
sentence_examples
```

构造：

```text
SentenceExample
EntryExampleLink
SentenceExampleDetails
```

要求：

- 使用现有 `SentenceExampleDetails`
- 返回集合不可被调用方修改
- 不依赖数据库默认行顺序
- 不自动创建 Primary
- 不自动调整 SortOrder
- 不吞掉数据库损坏

---

# 10. `SaveAsync`

保存例句本体：

```text
不存在相同 ID → INSERT
存在相同 ID → UPDATE
```

必须使用：

```sql
INSERT ... ON CONFLICT(id) DO UPDATE
```

或等价安全 UPSERT。

禁止：

```sql
INSERT OR REPLACE
REPLACE INTO
```

原因：REPLACE 可能触发级联删除并破坏已有 `entry_examples` 链接。

写入全部字段：

```text
id
capture_id
ocr_region_id
sentence_text
normalized_sentence
target_start
target_length
screenshot_crop_path
game_title
created_at_utc
```

必须显式事务：

```text
打开连接
→ BeginTransaction
→ UPSERT
→ Commit
```

失败回滚。

以下必须失败且不写入：

- 不存在的 CaptureId
- 不存在的 OcrRegionId
- OcrRegionId 有值但 CaptureId 为空

不得自动修复或创建来源数据。

---

# 11. `SaveLinkAsync`

保存或更新：

```text
(entry_id, example_id)
```

字段：

```text
is_primary
sort_order
```

使用安全 UPSERT，禁止 REPLACE。

语义：

- 新链接 → 创建
- 已有链接 → 更新 `IsPrimary` 和 `SortOrder`
- 重复保存相同值 → 幂等
- 不自动保存词条
- 不自动保存例句
- 不自动清除其他 Primary

以下必须失败：

- EntryId 不存在
- ExampleId 不存在

必须显式事务。

---

# 12. `SetPrimaryAsync`

该方法实现跨链接不变量：

```text
同一词条最多一个主要例句
```

语义：

```text
1. 验证 entryId 非空
2. 验证 exampleId 非空
3. BeginTransaction
4. 确认目标链接存在
5. 同一 entryId 全部链接 is_primary = 0
6. 目标链接 is_primary = 1
7. 确认目标更新行数 = 1
8. Commit
```

目标链接不存在时，采用：

```text
KeyNotFoundException
```

或项目已有等价稳定异常。

要求：

- 事务回滚
- 原 Primary 保持
- 不修改 SortOrder

---

# 13. `RemoveLinkAsync`

语义：

- 验证两个 Guid 非空
- 删除指定链接
- 不删除例句
- 不删除词条
- 不删除截图
- 不重新选择 Primary
- 链接不存在时幂等成功
- 删除当前 Primary 后允许暂时 0 个 Primary

必须显式事务。

---

# 14. CancellationToken

所有方法必须：

- 打开连接时传递 token
- 异步事务和命令执行时传递 token
- 读取循环适当检查 token
- 预取消 token 抛 `OperationCanceledException`
- 取消后不提交部分事务

不得捕获取消后返回成功或 null。

---

# 15. SQL 参数化

所有值必须使用参数。

禁止：

```text
字符串拼接 SQL
插值用户值
```

参数明确处理：

- GUID
- nullable GUID
- nullable string
- bool 0/1
- UTC 时间

不记录参数值。

---

# 16. 资源管理

每个方法必须正确释放：

```text
connection
transaction
command
reader
```

不得长期缓存连接、命令或事务。

失败时：

- 尝试回滚
- 保留原始异常
- 不让回滚异常覆盖原异常
- 释放资源

---

# 17. 数据库损坏处理

以下必须失败：

- 无效 GUID
- 非 UTC 时间
- is_primary 非 0/1
- 无效目标范围
- 非法 OCR/Capture 组合
- 负 SortOrder
- 必填列为 NULL

不得：

- 静默跳过坏行
- 返回部分对象
- 生成默认值
- 自动修复数据库

建议使用 `InvalidDataException` 或项目已有等价异常。

异常不得包含用户学习文本。

---

# 18. 日志与隐私

Repository 默认不需要 Logger。

不得记录：

- SentenceText
- NormalizedSentence
- GameTitle
- ScreenshotCropPath
- SQL 参数
- 数据库行内容

本任务优先保持 Repository 无日志依赖。

---

# 19. 测试准备

测试使用真实临时 SQLite 文件：

```text
Migration001
→ Migration002
→ Repository 测试
```

词条、Capture、OCR Repository 尚未实现，测试可使用参数化原始 SQL helper 仅做种子数据。

helper 必须：

- 只在测试项目
- 使用参数
- 不进入生产代码
- 不复制 Repository 业务逻辑

---

# 20. 必须测试：读取与保存

至少覆盖：

- 手工例句 round-trip
- Capture 例句 round-trip
- OCR 例句 round-trip
- 找不到返回 null
- Guid.Empty 被拒绝
- UTF-16 范围往返
- 非法 GUID 被拒绝
- 非 UTC 时间被拒绝
- 预取消 token
- 同 ID 更新成功
- 更新不删除已有链接
- 非法 Capture FK 回滚
- 非法 OCR FK 回滚
- Capture null + OCR value 被拒绝
- 更新失败后原记录保持
- null example 被拒绝

---

# 21. 必须测试：链接

至少覆盖：

- 新链接保存
- 重复保存幂等
- 更新 IsPrimary
- 更新 SortOrder
- 更新不删除例句
- Entry 不存在失败
- Example 不存在失败
- 失败后原链接保持
- SaveLink 不自动清除其他 Primary

明确验证：

```text
A Primary = true
B SaveLink(IsPrimary = true)
→ A 与 B 可暂时都为 true
```

随后通过 `SetPrimaryAsync` 恢复唯一 Primary。

---

# 22. 必须测试：按词条读取

至少覆盖：

- 无链接返回空列表
- 按 SortOrder、ExampleId 稳定排序
- IsPrimary 正确
- SortOrder 正确
- 混合返回手工、Capture、OCR 例句
- Entry 不存在返回空列表
- Guid.Empty 被拒绝
- 非法 is_primary 数据被拒绝
- 非法例句数据被拒绝

---

# 23. 必须测试：主要例句

至少覆盖：

- 单链接设置 Primary
- 多链接切换 Primary
- 其他链接全部 false
- 重复设置同一目标幂等
- SortOrder 不变化
- 目标不存在时抛稳定异常
- 目标不存在时原 Primary 保持
- Entry 不存在无数据变化
- Guid.Empty 被拒绝
- 预取消无部分修改

---

# 24. 必须测试：移除链接

至少覆盖：

- 删除存在链接
- 删除不存在链接幂等
- 不删除例句
- 不删除词条
- 删除当前 Primary 后允许 0 个 Primary
- Guid.Empty 被拒绝
- 预取消无数据变化

---

# 25. 文件锁与边界

测试结束后必须成功删除：

```text
.db
-wal
-shm
临时目录
```

还要确认：

- Repository 多次调用不持有长期连接
- Application 接口未修改
- Domain 未引用 Infrastructure
- Application 未引用 Infrastructure
- Repository 公共 API 不暴露 SqliteConnection
- 无 Godot 类型
- 无 IQueryable

---

# 26. 允许创建和修改

建议创建：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteSentenceExampleRepository.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/
SqliteSentenceExampleRepositoryTests.cs
```

可选：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
SentenceExampleSqlMapper.cs
```

允许修改：

```text
tests/GameLexicon.Infrastructure.Tests/Persistence/**
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/DECISIONS.md（仅长期架构决策变化时）
docs/SKILLS_CATALOG.md（仅 Skill Impact Review 需要时）
docs/SKILL_CHANGELOG.md（仅 Skill 实际更新时）
.agents/skills/*/SKILL.md（仅可复用流程变化时）
```

正常情况下不得修改：

```text
GameLexicon.sln
任一 .csproj
src/GameLexicon.Domain/**
src/GameLexicon.Application/**
Migration001_Initial.cs
Migration002_ManualExamplesAndSearchSupport.cs
MigrationRunner.cs
SqliteConnectionFactory.cs
english-learning-project/**
tools/GameLexicon.CaptureBridge/**
```

本任务不需要注册到 AppServices。

---

# 27. 明确不做

不得实现：

- `SqliteVocabularyRepository`
- `SqliteTagRepository`
- 聚合保存事务
- 标签关联
- 词头查重
- 搜索/分页 SQL
- Archive/Restore/Delete
- UseCase
- ViewModel
- Godot UI
- Migration003
- FTS
- Capture/OCR Repository
- M1-T07

---

# 28. 自动验证命令

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-restore

dotnet test `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-build `
  --no-restore

dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功
- 所有测试通过
- 0 错误
- 0 新增警告

本任务不需要 Godot GUI 或 headless。

---

# 29. 代表性自动验收

最终报告必须明确列出：

```text
Manual example round-trip → Pass
Capture example round-trip → Pass
OCR example round-trip → Pass
Update keeps entry links → Pass
Invalid Capture FK → Rejected
Invalid OCR FK → Rejected

Save link → Pass
Repeat save → Idempotent
Update SortOrder → Pass
Second Primary is not auto-cleared by SaveLink → Confirmed

SetPrimary clears other links → Pass
Missing target rolls back → Pass
SortOrder unchanged → Pass

Remove existing link → Pass
Remove missing link → Idempotent
Example row remains → Pass

DB/WAL/SHM deletable → Pass
Cancellation leaves no partial write → Pass
```

不得只报告“测试全部通过”。

---

# 30. 非 GUI 人工审查

自动验收后：

```text
M1-T06 = Awaiting Manual Verification
M1-T07 = Not Started
```

人工审查重点：

1. Repository 位于 Infrastructure。
2. 精确实现六个接口方法。
3. Application 接口未修改。
4. SQL 参数化。
5. 不使用 REPLACE。
6. Save 更新不删除链接。
7. SetPrimary 是原子事务。
8. RemoveLink 不删除例句。
9. nullable CaptureId 映射正确。
10. CancellationToken 完整传播。
11. 连接无泄漏。
12. 不记录学习文本。
13. 未实现其他 Repository、UseCase 或 UI。
14. 所有测试通过。

用户确认前不得标记 Done。

---

# 31. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且修改未确认
- 找不到提交 `52160ac8...`
- M1-T05 未标记 Done
- M1-T06 不是 Not Started
- 基线构建或测试失败
- 解决方案不再是 8 个项目
- 目标框架或项目引用变化
- Migration001 哈希变化
- 必须修改 Migration001 或 Migration002
- 必须修改 Application 接口
- 必须新增 NuGet 包
- 必须实现词条或标签 Repository
- 必须修改 Godot
- 无法保证 SetPrimary 原子性
- 无法避免 REPLACE
- 测试数据库无法删除
- 用户文件可能被覆盖

停止后不得：

- 删除用户数据库
- 修改迁移历史
- `git reset --hard`
- `git clean -fd`
- 禁用 NuGet Audit
- 自动提交
- 自动执行 M1-T07

---

# 32. Git 检查

完成后执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
git diff --name-only

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"
```

Migration001 必须仍为：

```text
1fd5546081fe87c479ebd21d52e26f7d1dfaa636
```

确认：

- 生产代码只在 Infrastructure Repository
- 测试只在 Infrastructure.Tests
- 其余只允许状态文档
- Application、Domain、Migration、Godot 未修改
- `.csproj` 未修改
- 数据库、WAL、SHM、日志、备份未进入 Git
- 暂存区为空
- 未创建提交

---

# 33. 状态与文档

自动验收通过后：

```text
M1-T06 = Awaiting Manual Verification
M1-T07 = Not Started
```

更新：

```text
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
```

记录：

- Repository 类型
- 六个方法
- GUID/UTC 映射
- nullable Capture/OCR
- UPSERT 策略
- 未使用 REPLACE
- SetPrimary 事务
- RemoveLink 幂等
- CancellationToken 覆盖
- 新增测试数量
- Infrastructure 与根测试结果
- DB/WAL/SHM 删除结果
- 未修改迁移、Application、Godot
- 已知限制

只有长期架构决策变化时修改 `DECISIONS.md`。

只有环境事实变化时修改 `ENVIRONMENT.md`。

人工审查通过后：

```text
M1-T06 = Done
M1-T07 = Not Started
```

不得执行 M1-T07。

---

# 34. Skill Impact Review

任务结束后报告：

- Primary domain
- Primary agent
- Supporting agents
- Skills used
- Skill update required
- Skills updated
- Documentation updated
- Restart required

正常预期：

```text
Skills used:
- project-routing
- milestone-workflow
- skill-maintenance

Skill update required:
No
```

---

# 35. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T06
- 名称: SQLite 例句 Repository
- 状态:
- M1-T07 executed: No
- Git commit created: No
- GUI verification required: No

## 前置基线

- M1-T05 commit:
- Branch:
- Initial Git status:
- Baseline build:
- Baseline tests:
- Migration001 hash:

## Repository 实现

- Type:
- Interface:
- Constructor:
- Connection policy:
- Transaction policy:
- Cancellation coverage:

## 方法语义

- GetByIdAsync:
- GetForEntryAsync:
- SaveAsync:
- SaveLinkAsync:
- SetPrimaryAsync:
- RemoveLinkAsync:

## SQL 与映射

- Parameterized:
- SELECT * used:
- UPSERT:
- REPLACE used:
- GUID format:
- UTC format:
- Nullable mapping:
- Corrupt data behavior:

## 原子性

- Save rollback:
- SaveLink rollback:
- SetPrimary transaction:
- Missing target rollback:
- Remove idempotency:

## 代表案例

| Case | Actual | Expected | Result |
|---|---|---|---|
| ... | ... | ... | Pass |

## 创建和修改的文件

- ...

## 自动化测试

- Baseline total:
- Added:
- Infrastructure total:
- Root total:
- Passed:
- Failed:
- Skipped:
- DB/WAL/SHM deletion:

## 边界检查

- Application modified:
- Domain modified:
- Migrations modified:
- Godot modified:
- Vocabulary repository:
- Tag repository:
- UseCases/UI:

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

## 人工审查

- Awaiting user review.
- No GUI run is required.

## 下一任务

- M1-T07：SQLite 标签 Repository
- Status: Not Started
- Not automatically executed
```

---

# 36. 可直接执行的总指令

请执行：

```text
M1-T06：SQLite 例句 Repository
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T06_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `52160ac85ba27362f7bea76feec49e6d2036cc93`。
2. 开始时 Git 工作区必须干净。
3. 只实现 `SqliteSentenceExampleRepository` 和 Infrastructure 测试。
4. 精确实现现有六个接口方法。
5. 不修改 Application 接口。
6. 使用 `SqliteConnectionFactory`，不长期持有连接。
7. 所有 SQL 参数化，禁止 `SELECT *`。
8. Save 和 SaveLink 使用安全 UPSERT。
9. 禁止 `INSERT OR REPLACE` 和 `REPLACE INTO`。
10. Save 更新不得删除已有链接。
11. SetPrimaryAsync 必须为单一事务。
12. 目标链接不存在时完整回滚，原 Primary 保持。
13. RemoveLinkAsync 幂等且不删除例句。
14. 正确映射 nullable CaptureId/OcrRegionId。
15. GUID 和 UTC 使用项目固定格式。
16. 数据损坏不得静默忽略。
17. CancellationToken 必须传播。
18. 测试数据库、WAL 和 SHM 必须可删除。
19. 不修改 Migration001、Migration002、Domain、Application、Godot、项目引用或目标框架。
20. 不实现词条 Repository、标签 Repository、UseCase 或 UI。
21. 不新增 NuGet 包。
22. 不执行 M1-T07。
23. 不创建 Git 提交。
24. 自动验收后保持 Awaiting Manual Verification。
25. 本任务不需要 GUI 验收。
26. 完成后执行 Git diff、状态文档更新和 Skill Impact Review。
