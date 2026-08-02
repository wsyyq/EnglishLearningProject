# M1-T10 Codex 执行指令

## 任务

```text
M1-T10：手工创建词条 UseCase
```

保存位置：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T10_CODEX_INSTRUCTION.md
```

本任务仅实现 Application 层的手工创建流程：

```text
CreateVocabularyEntryCommand
CreateVocabularyEntryUseCase
CreateVocabularyEntryResult
创建验证结果
活动精确重复检测
IClock
IGuidGenerator
Application 自动化测试
```

本任务不实现：

```text
M1-T11 精确重复决策与例句合并
独立重复词条创建
标签或例句创建/关联
Primary、GameTitle、Capture、OCR、Screenshot
归档状态创建
Infrastructure 适配器
Godot 接线或 UI
```

---

# 1. 已确认基线

最新提交：

```text
eb3208ad20aa0fdd404f7b6c047fedf847b71fb0
```

已确认：

- 分支：`main`
- 工作区干净
- M1-T09 = `Done`
- M1-T10 = `Not Started`
- 无 Godot 进程
- 解决方案 8 个项目
- 构建 0 警告、0 错误
- 测试 299/299：
  - Domain 111
  - Application 61
  - Infrastructure 127
- Migration001：
  `1fd5546081fe87c479ebd21d52e26f7d1dfaa636`
- Migration002：
  `d8ce250e24442ece38c231e3ae8286a4d0def4c5`
- `SqliteVocabularyRepository` 已完整实现 `IVocabularyRepository`
- `SaveAsync` 仍为 M1-T08 写侧实现
- 三个 Repository 已提交且无修改
- Application 当前没有 UseCase、Command、通用 Result、IClock 或 IGuidGenerator
- Repository 不生成 ID、时间或 NormalizedHeadword
- 业务时间统一使用 `DateTimeOffset`
- Application API 未泄漏 SQLite、Godot、Infrastructure 或 `IQueryable`

开始时仍须重新核验。

---

# 2. 产品要求与本任务固定决策

PRODUCT_SPEC 明确支持：

- 用户手工输入目标表达和释义。
- Headword 与 EntryType 是核心字段。
- EntryType：
  - Word
  - Phrase
  - Expression
  - SentencePattern
- PartOfSpeech、Phonetic、DefinitionEnglish、TranslationChinese 可空。
- Notes 属于词条字段。
- NormalizedHeadword 必须使用既有 `ITextNormalizer`。
- 精确重复定义为 NormalizedHeadword 完全相同。
- MVP 自动阻止精确重复。
- 完整重复选择及例句合并属于后续任务。

产品规格没有定义 Command、Result、时间/ID 抽象或重复结果形式，因此 M1-T10 固定：

```text
公开方法：ExecuteAsync
验证失败：返回 ValidationFailed
活动精确重复：返回 ExactDuplicate
创建成功：返回 Created
基础设施、取消和依赖失败：抛异常
创建状态：始终 Active
```

不得建立通用 `Result<T>` 或通用 Handler 框架。

---

# 3. M1-T10 与 M1-T11 边界

M1-T10 流程：

```text
验证输入
→ Normalize Headword
→ 查找活动精确重复
→ 无重复时生成 ID 和 UTC 时间
→ 构造 Active VocabularyEntry
→ SaveAsync
→ 返回 Created
```

发现活动精确重复：

```text
返回 ExactDuplicate
不生成 ID
不读取时间
不 Save
不修改已有词条
```

M1-T11 才负责：

- 合并例句。
- 创建独立重复词条。
- 合并/独立创建/取消决策。
- Primary 与多来源例句处理。
- 近似重复提示或策略。

M1-T10 不得绕过活动词头唯一索引。

---

# 4. 必须阅读

完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/AGENT_HANDOFF.md
docs/MT_INSTRUCTION/M1-T10_CODEX_INSTRUCTION.md
```

代码：

```text
src/GameLexicon.Domain/Entries/VocabularyEntry.cs
src/GameLexicon.Domain/Entries/EntryType.cs
src/GameLexicon.Domain/Entries/EntryGuard.cs

src/GameLexicon.Application/Abstractions/ITextNormalizer.cs
src/GameLexicon.Application/Abstractions/Persistence/IVocabularyRepository.cs
src/GameLexicon.Application/Entries/Queries/**

src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteVocabularyRepository.cs
SqliteVocabularyRepository.Queries.cs

tests/GameLexicon.Application.Tests/**
```

Skills：

```text
.agents/skills/project-routing/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Application / Entries / Creation

Primary writer:
primary coordinator

Supporting:
milestone architect 只读复核首个 UseCase 契约
skill curator 仅在 Skill Impact Review 需要时调用
```

---

# 5. 前置核验

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline eb3208ad20aa0fdd404f7b6c047fedf847b71fb0
git diff --check

dotnet sln GameLexicon.sln list

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration002_ManualExamplesAndSearchSupport.cs"

dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

必须确认：

```text
Branch = main
Worktree = clean
M1-T09 = Done
M1-T10 = Not Started
Projects = 8
Build = success
Tests = 299/299
Migration hashes = expected
```

任一不符立即停止。不得恢复、清理、覆盖、暂存或提交用户内容。

---

# 6. 固定依赖契约

不得修改：

```csharp
public interface ITextNormalizer
{
    string Normalize(string value);
}
```

不得修改：

```csharp
public interface IVocabularyRepository
{
    Task<VocabularyEntry?> FindByNormalizedHeadwordAsync(
        string normalizedHeadword,
        CancellationToken cancellationToken);

    Task<VocabularyEntryDetails?> GetDetailsAsync(
        Guid entryId,
        CancellationToken cancellationToken);

    Task<PagedResult<VocabularyEntrySummary>> SearchAsync(
        VocabularySearchQuery query,
        CancellationToken cancellationToken);

    Task SaveAsync(
        VocabularyEntry entry,
        CancellationToken cancellationToken);
}
```

M1-T10 只调用：

```text
FindByNormalizedHeadwordAsync
SaveAsync
```

禁止调用：

```text
GetDetailsAsync
SearchAsync
ISentenceExampleRepository
ITagRepository
```

---

# 7. 建议文件

生产代码：

```text
src/GameLexicon.Application/Abstractions/Time/IClock.cs
src/GameLexicon.Application/Abstractions/Identity/IGuidGenerator.cs

src/GameLexicon.Application/Entries/Creation/
CreateVocabularyEntryCommand.cs
CreateVocabularyEntryOutcome.cs
CreateVocabularyEntryValidationCode.cs
CreateVocabularyEntryValidationError.cs
CreateVocabularyEntryResult.cs
CreateVocabularyEntryUseCase.cs
```

测试：

```text
tests/GameLexicon.Application.Tests/Entries/Creation/
CreateVocabularyEntryUseCaseTests.cs
CreateVocabularyEntryResultTests.cs
```

测试 Fake 可放在：

```text
tests/GameLexicon.Application.Tests/TestDoubles/**
```

不得新增 NuGet 包。

---

# 8. 时间和 ID 抽象

## `IClock`

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

要求：

- 位于 Application。
- UseCase 验证 Offset 为 `TimeSpan.Zero`。
- 本任务不提供系统实现。
- 不直接使用 `DateTimeOffset.UtcNow`、`DateTime.UtcNow` 或 `TimeProvider.System`。

## `IGuidGenerator`

```csharp
public interface IGuidGenerator
{
    Guid NewGuid();
}
```

要求：

- 位于 Application。
- 返回 `Guid.Empty` 时 UseCase 抛 `InvalidOperationException`。
- 本任务不提供系统实现。
- UseCase 不直接调用 `Guid.NewGuid()`。

---

# 9. Command

创建：

```csharp
public sealed record CreateVocabularyEntryCommand(
    string? Headword,
    EntryType EntryType,
    string? PartOfSpeech = null,
    string? Phonetic = null,
    string? DefinitionEnglish = null,
    string? TranslationChinese = null,
    string? Notes = null);
```

Command 不得包含：

```text
Id
NormalizedHeadword
CreatedAt
UpdatedAt
IsArchived
TagIds
Examples
GameTitle
CaptureId
OcrRegionId
Screenshot
```

要求：

- Headword 可空，以便 UseCase 返回验证结果。
- Command 不执行 Normalize。
- 所有可选文本原样保存。
- 不 Trim。
- 不增加不存在的长度限制。

---

# 10. Result 契约

## Outcome

```csharp
public enum CreateVocabularyEntryOutcome
{
    Created = 0,
    ExactDuplicate = 1,
    ValidationFailed = 2
}
```

## ValidationCode

```csharp
public enum CreateVocabularyEntryValidationCode
{
    HeadwordRequired = 0,
    NormalizedHeadwordEmpty = 1,
    InvalidEntryType = 2
}
```

## ValidationError

```csharp
public sealed record CreateVocabularyEntryValidationError(
    string Field,
    CreateVocabularyEntryValidationCode Code);
```

Field 使用：

```text
nameof(CreateVocabularyEntryCommand.Headword)
nameof(CreateVocabularyEntryCommand.EntryType)
```

不得包含用户输入文本或 UI 文案。

## Result

创建不可变任务专用类型，公开：

```text
Outcome
CreatedEntryId
ExistingEntryId
ExistingHeadword
Errors
```

推荐工厂：

```csharp
public static CreateVocabularyEntryResult Created(Guid entryId);

public static CreateVocabularyEntryResult ExactDuplicate(
    Guid existingEntryId,
    string existingHeadword);

public static CreateVocabularyEntryResult ValidationFailed(
    IEnumerable<CreateVocabularyEntryValidationError> errors);
```

状态不变量：

### Created

```text
Outcome = Created
CreatedEntryId = non-empty
ExistingEntryId = null
ExistingHeadword = null
Errors = empty
```

### ExactDuplicate

```text
Outcome = ExactDuplicate
CreatedEntryId = null
ExistingEntryId = non-empty
ExistingHeadword = non-blank, preserved exactly
Errors = empty
```

### ValidationFailed

```text
Outcome = ValidationFailed
all IDs/headword = null
Errors = non-empty, defensive copy, read-only
```

要求：

- 不允许公开构造无效组合。
- Errors 不得包含 null。
- 相同 Field + Code 不得重复。
- 无效工厂参数抛参数异常。
- 不新增通用 Result。

---

# 11. UseCase

创建：

```csharp
public sealed class CreateVocabularyEntryUseCase
```

构造函数：

```csharp
public CreateVocabularyEntryUseCase(
    IVocabularyRepository vocabularyRepository,
    ITextNormalizer textNormalizer,
    IGuidGenerator guidGenerator,
    IClock clock)
```

公开方法：

```csharp
public Task<CreateVocabularyEntryResult> ExecuteAsync(
    CreateVocabularyEntryCommand command,
    CancellationToken cancellationToken);
```

要求：

- 依赖为 null 时 `ArgumentNullException`。
- command 为 null 时 `ArgumentNullException`。
- 不新增同步 Execute 或 Handle。
- 不依赖 Logger、Infrastructure、Godot、标签或例句 Repository。

---

# 12. 固定执行顺序

## 12.1 Null 与取消

顺序：

```text
1. command null 检查
2. cancellationToken.ThrowIfCancellationRequested()
```

预取消时：

- 抛 `OperationCanceledException`。
- 不调用 Normalizer、Repository、Guid 或 Clock。

## 12.2 第一阶段验证

验证：

```text
Headword null / empty / whitespace
EntryType 是否为已定义值
```

可以一次返回多个错误，顺序固定：

```text
Headword
EntryType
```

有错误时：

- 返回 ValidationFailed。
- 不调用 Normalizer 或任何依赖。

## 12.3 Normalize

只调用一次：

```csharp
var normalizedHeadword =
    _textNormalizer.Normalize(command.Headword);
```

禁止：

- 先 Trim。
- 复制规范化规则。
- 调用两次。
- 修改 raw Headword。

Normalize 结果为空或纯空白：

```text
ValidationFailed
Field = Headword
Code = NormalizedHeadwordEmpty
```

此时不调用 Repository、Guid 或 Clock。

Normalizer 异常原样传播。

## 12.4 活动精确重复

调用：

```csharp
FindByNormalizedHeadwordAsync(
    normalizedHeadword,
    cancellationToken)
```

找到活动词条时返回：

```text
Outcome = ExactDuplicate
ExistingEntryId = existing.Id
ExistingHeadword = existing.Headword
```

并确认：

- 不生成 ID。
- 不读取 Clock。
- 不构造新 Entry。
- 不 Save。
- 不查询 Details/Search。
- 不修改已有 Entry。

Find 异常原样传播。

## 12.5 生成 ID

无重复时只调用一次：

```csharp
_guidGenerator.NewGuid()
```

`Guid.Empty`：

```text
InvalidOperationException
```

不得重试或改用系统 Guid。

## 12.6 获取时间

无重复时只读取一次：

```csharp
_clock.UtcNow
```

必须：

```text
Offset == TimeSpan.Zero
```

否则：

```text
InvalidOperationException
```

不得自动转换或读取系统时间。

## 12.7 构造 Domain

等价于：

```csharp
new VocabularyEntry(
    id: generatedId,
    headword: command.Headword,
    normalizedHeadword: normalizedHeadword,
    entryType: command.EntryType,
    partOfSpeech: command.PartOfSpeech,
    phonetic: command.Phonetic,
    definitionEnglish: command.DefinitionEnglish,
    translationChinese: command.TranslationChinese,
    notes: command.Notes,
    isArchived: false,
    createdAt: now,
    updatedAt: now);
```

要求：

- Raw Headword 原样保存。
- 可选文本原样保存。
- null 不变空字符串。
- 空字符串不变 null。
- 创建始终 Active。
- CreatedAt 与 UpdatedAt 完全相同。

Domain 异常原样传播。

## 12.8 Save

调用一次：

```csharp
SaveAsync(entry, cancellationToken)
```

成功后：

```text
Outcome = Created
CreatedEntryId = generatedId
```

Save 异常原样传播：

- 不返回 Created。
- 不返回 ExactDuplicate。
- 不重试。
- 不再次 Find。

---

# 13. 并发竞态边界

并发可能出现：

```text
两个 Find 均为 null
一个 Save 成功
另一个 Save 被数据库唯一约束拒绝
```

M1-T10 不得：

- 引用 `Microsoft.Data.Sqlite`。
- 捕获所有异常后重新查询。
- 把任何 Save 异常转换为 ExactDuplicate。
- 自动创建 Archived 重复词条。
- 绕过唯一索引。

记录为已知限制：

```text
正常重复由预检查返回 ExactDuplicate；
并发竞态由数据库唯一约束兜底；
当前不在 Application 翻译 SQLite 冲突。
```

---

# 14. 返回与异常分类

返回 `ValidationFailed`：

```text
HeadwordRequired
NormalizedHeadwordEmpty
InvalidEntryType
```

返回 `ExactDuplicate`：

```text
Find 返回活动词条
```

抛异常：

```text
null command
cancellation
Normalizer failure
Repository failure
Guid.Empty
non-UTC clock
Domain construction failure
Save failure
```

不得新增业务异常基类、DuplicateException 或 SQLite 异常映射。

---

# 15. 文本规则

Headword：

- Raw 值原样保存。
- NormalizedHeadword 只来自 `ITextNormalizer`。
- UseCase 不 Trim、不 Lower、不 Form KC、不折叠空白。

可选字段：

```text
PartOfSpeech
Phonetic
DefinitionEnglish
TranslationChinese
Notes
```

全部原样传递，允许：

```text
null
empty
whitespace
Unicode
newline
```

不得发明长度限制。

---

# 16. Application 测试

必须使用：

```text
xUnit
内存 Fake IVocabularyRepository
Fake ITextNormalizer
Fake IClock
Fake IGuidGenerator
```

不得使用：

```text
SQLite
Infrastructure
Godot
新 mocking 包
```

Fake Repository 记录：

```text
Find 调用次数和参数
Find token
Save 调用次数
Saved VocabularyEntry
Save token
配置 Existing Entry
配置 Find/Save exception
GetDetails/Search 是否被意外调用
```

未使用的 Repository 方法被调用时应让测试失败，不得返回伪造结果掩盖越界调用。

---

# 17. 必须覆盖的测试

## 构造与 API

- 四个构造依赖分别为 null。
- command 为 null。
- ExecuteAsync 返回正确 Task 类型。
- Command 不含 ID、时间、NormalizedHeadword、IsArchived、Tags、Examples。
- 新增生产代码无 Infrastructure、Godot、SQLite 或 `IQueryable` 泄漏。

## 输入验证

分别测试：

```text
null headword
empty headword
ASCII whitespace
Unicode whitespace
invalid EntryType
headword + invalid EntryType 多错误
Normalize 后 empty
Normalize 后 whitespace
```

确认：

- Outcome 和 Code 正确。
- 错误顺序稳定。
- 验证失败不调用后续依赖。
- Normalized empty 路径只调用 Normalizer 一次。

## Result 不变量

- Errors 防御性复制、不可修改。
- 空错误集合不能构造 ValidationFailed。
- 重复 Field + Code 被拒绝。
- Guid.Empty 不能构造 Created/ExactDuplicate。
- 空白 ExistingHeadword 被拒绝。
- 三种 Outcome 的其他字段互斥。

## 正常创建

覆盖：

- 全字段。
- 全部可选字段 null。
- 空/空白可选字段原样保留。
- 四种 EntryType。
- Raw Headword 与 NormalizedHeadword 分离。
- 创建始终 Active。
- CreatedAt == UpdatedAt。
- Result ID 与生成 ID 相同。
- Save 接收刚创建的对象。

调用次数：

```text
Normalizer 1
Find 1
Guid 1
Clock 1
Save 1
```

执行顺序至少验证：

```text
Normalize
Find
Guid
Clock
Save
```

## 精确重复

Fake Find 返回活动 Existing Entry：

- Outcome = ExactDuplicate。
- ExistingEntryId/Headword 正确。
- ExistingHeadword 原样保留。
- Guid 0 次。
- Clock 0 次。
- Save 0 次。
- GetDetails/Search 0 次。
- Existing Entry 未修改。

Fake Find 返回 null 代表没有活动重复，即使测试数据概念上存在 archived duplicate，也正常创建。

## Cancellation

- 预取消：所有依赖 0 次。
- Find 收到原 token。
- Save 收到原 token。
- Find cancellation 原样传播且不生成 ID/时间。
- Save cancellation 原样传播且不重试。

## 依赖失败

- Normalizer 异常传播。
- Find 异常传播。
- Guid.Empty → InvalidOperationException。
- 非 UTC Clock → InvalidOperationException。
- Domain 构造异常传播。
- Save 异常传播。
- 模拟唯一竞态异常不转换为 ExactDuplicate。
- 失败路径不进行后续调用。

## 静态边界

新增生产文件不得出现：

```text
Guid.NewGuid(
DateTimeOffset.UtcNow
DateTime.UtcNow
TimeProvider.System
Microsoft.Data.Sqlite
Godot
```

---

# 18. 隐私

不注入 Logger。

不得记录：

- Headword。
- NormalizedHeadword。
- 释义、翻译、音标、词性、笔记。
- ExistingHeadword。
- Repository 参数。

Result 返回 ExistingHeadword 属于业务输出，不属于日志。

---

# 19. 允许修改范围

允许创建：

```text
src/GameLexicon.Application/Abstractions/Time/IClock.cs
src/GameLexicon.Application/Abstractions/Identity/IGuidGenerator.cs
src/GameLexicon.Application/Entries/Creation/**
tests/GameLexicon.Application.Tests/Entries/Creation/**
tests/GameLexicon.Application.Tests/TestDoubles/**
```

允许修改：

```text
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/DECISIONS.md
```

仅 Skill 实际变化时允许：

```text
docs/SKILLS_CATALOG.md
docs/SKILL_CHANGELOG.md
.agents/skills/*/SKILL.md
```

正常情况下不得修改：

```text
GameLexicon.sln
任一 .csproj
src/GameLexicon.Domain/**
src/GameLexicon.Infrastructure/**
src/GameLexicon.Application/Abstractions/Persistence/**
src/GameLexicon.Application/Entries/Queries/**
english-learning-project/**
tests/GameLexicon.Domain.Tests/**
tests/GameLexicon.Infrastructure.Tests/**
```

不要删除现有 Application `Class1`，避免无关 diff。

---

# 20. 明确禁止

不得实现：

```text
M1-T11
DuplicateDecision
MergeExample
CreateIndependentDuplicate
Similar/fuzzy duplicate detection
Tag/Example 创建或关联
Primary 切换
Game/Capture/OCR/Screenshot
创建 Archived Entry
编辑/归档/删除 UseCase
Godot UI 或 AppServices
Infrastructure Clock/Guid adapter
通用 Result<T>
通用 Handler/Pipeline
MediatR
FluentValidation
新 NuGet 包
Migration003
```

---

# 21. 自动验证

Application：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build `
  tests/GameLexicon.Application.Tests/GameLexicon.Application.Tests.csproj `
  --no-restore

dotnet test `
  tests/GameLexicon.Application.Tests/GameLexicon.Application.Tests.csproj `
  --no-build `
  --no-restore
```

根解决方案：

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功。
- 所有测试通过。
- 0 错误。
- 0 新增警告。

Godot：

```text
GUI verification required: No
Godot headless required: No
```

不得启动 Godot Editor。

---

# 22. 代表性验收

最终逐项报告：

```text
Required headword validation → Pass
Invalid EntryType validation → Pass
Normalized-empty validation → Pass
Validation prevents dependency calls → Pass

Normalizer called once → Pass
Find receives normalized value → Pass
Active exact duplicate → ExactDuplicate
Duplicate prevents ID/time/save → Pass

Create all fields → Pass
Raw Headword preserved → Pass
Optional text preserved → Pass
Creates Active only → Pass
CreatedAt equals UpdatedAt → Pass
Guid generated once → Pass
Clock read once → Pass
Save called once → Pass

Pre-cancelled execution → No dependency calls
Find/Save cancellation propagation → Pass
Guid.Empty rejected → Pass
Non-UTC clock rejected → Pass
Repository exceptions propagated → Pass
Unique-race exception not translated → Pass

No tag/example dependency → Confirmed
No Infrastructure/Godot reference → Confirmed
```

---

# 23. 文档与状态

自动验收通过后：

```text
M1-T10 = Awaiting Manual Verification
M1-T11 = Not Started
```

更新：

```text
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
```

记录：

- UseCase、Command、Result。
- 三种 Outcome。
- 三种 ValidationCode。
- IClock 与 IGuidGenerator。
- 固定执行顺序。
- 活动精确重复结果。
- 创建始终 Active。
- ID/时间生成规则。
- CancellationToken。
- 并发唯一竞态限制。
- 新增测试和总测试结果。
- 未修改 Domain、Infrastructure、Repository、Godot。
- M1-T11 未执行。

## ADR-009

本任务建立首个 Application UseCase 模式，必须在：

```text
docs/DECISIONS.md
```

新增 ADR-009，至少记录：

```text
- UseCase 采用 ExecuteAsync。
- 使用任务专用 Command 和 Result。
- 暂不建立通用 Result<T> 或 Handler 框架。
- 预期验证错误返回 ValidationFailed。
- 活动精确重复返回 ExactDuplicate，而非异常。
- ID 和时间由 IGuidGenerator / IClock 注入。
- Repository 不生成 ID、时间或规范化值。
- 手工创建始终 Active。
- 标签、例句与完整重复决策留给后续任务。
- 并发预检查竞态由数据库唯一约束兜底；
  Application 当前不翻译 SQLite 冲突。
```

不得修改 ADR-008 的语义。

只有环境事实变化时修改 `ENVIRONMENT.md`。

---

# 24. Skill Impact Review

报告：

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

正常预期：

```text
project-routing
milestone-workflow
skill-maintenance

Skill update required: No
```

只有首个 UseCase 模式被确认需要固化为可复用 Skill 时才更新 Skill。无论是否更新 Skill，都必须保留 ADR-009。

---

# 25. Git 最终检查

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
git diff --name-only
```

再次确认迁移哈希不变。

确认：

- 生产变更仅为 Application 创建流程和两个抽象。
- 测试仅为 Application 创建测试/Fake。
- 其他仅状态和 ADR 文档。
- Domain、Infrastructure、Repository、Godot、项目文件无修改。
- 数据库、WAL、SHM、日志、备份、bin、obj、.godot 未进入 Git。
- 暂存区为空。
- 未创建提交。
- 未执行 M1-T11。

---

# 26. 强制停止条件

任一情况出现时停止：

- 工作区不干净且修改未确认。
- 提交不存在。
- M1-T09 不是 Done。
- M1-T10 不是 Not Started。
- 基线构建或测试失败。
- 解决方案项目/框架变化。
- 迁移哈希变化。
- 必须修改 Domain、Repository 接口或 Infrastructure。
- 必须新增包。
- 必须实现 M1-T11。
- 必须创建标签或例句。
- 必须引入通用框架。
- 用户文件可能被覆盖。

不得执行：

```text
git reset --hard
git clean -fd
自动提交
自动执行 M1-T11
```

---

# 27. 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T10
- 名称: 手工创建词条 UseCase
- 状态:
- M1-T11 executed: No
- Git commit created: No
- GUI verification required: No

## 前置基线

- M1-T09 commit:
- Branch:
- Initial Git status:
- Projects:
- Baseline build/tests:
- Migration hashes:

## Application 契约

- UseCase:
- Method:
- Command:
- Outcomes:
- Validation codes:
- IClock:
- IGuidGenerator:
- Generic Result/Handler introduced:

## 执行流程

- Validation:
- Normalization:
- Duplicate lookup:
- ID/time:
- Domain construction:
- Save:
- Cancellation:

## 创建与重复语义

- Raw Headword:
- NormalizedHeadword:
- Optional fields:
- IsArchived:
- CreatedAt/UpdatedAt:
- Active duplicate result:
- Duplicate path dependency calls:
- Concurrent unique-race behavior:

## 测试

- Added Application tests:
- Application total:
- Root total:
- Passed/failed/skipped:
- Representative cases:

## 边界

- Domain modified:
- Infrastructure modified:
- Repository modified:
- Godot modified:
- Tags/examples:
- M1-T11:
- New packages:

## Decision Review

- ADR-009:
- Key decisions:

## Skill Impact Review

- ...

## 人工审查

- Awaiting user review.
- No GUI run is required.

## 下一任务

- M1-T11：精确重复决策与例句合并 UseCase
- Status: Not Started
```

---

# 28. 可直接执行的总指令

请执行：

```text
M1-T10：手工创建词条 UseCase
```

严格按照本文件执行。

特别要求：

1. 核验提交 `eb3208ad20aa0fdd404f7b6c047fedf847b71fb0`。
2. 初始工作区必须干净。
3. 只在 Application 新增创建流程和测试。
4. 新增 `IClock.UtcNow` 与 `IGuidGenerator.NewGuid()`。
5. 不提供系统实现，不直接调用系统 Guid/时间。
6. Command 不包含 ID、规范化值、时间、归档状态、Tag 或 Example。
7. UseCase 采用 `ExecuteAsync`。
8. 使用任务专用 Result，不新增通用 Result。
9. 三类输入问题返回 ValidationFailed。
10. 活动精确重复返回 ExactDuplicate。
11. 重复路径不生成 ID、不读时间、不 Save。
12. 正常路径 Normalize 恰好一次。
13. Raw Headword 和可选文本原样保留。
14. 生成一个非空 Guid，读取一次 UTC Clock。
15. CreatedAt 与 UpdatedAt 使用同一值。
16. 创建始终 Active。
17. 只调用 Repository 的 Find 和 Save。
18. Save 异常原样传播，不翻译 SQLite 冲突。
19. CancellationToken 完整传播。
20. 使用 Application Fake 测试。
21. 不修改 Domain、Infrastructure、Repository、迁移、Godot、项目或框架。
22. 不创建标签、例句或来源数据。
23. 不实现 M1-T11。
24. 不新增 NuGet 包。
25. 不创建 Git 提交。
26. 自动验收后保持 Awaiting Manual Verification。
27. M1-T11 保持 Not Started。
28. 本任务不需要 GUI。
29. 新增 ADR-009。
30. 完成 Git、Decision 和 Skill Review。
