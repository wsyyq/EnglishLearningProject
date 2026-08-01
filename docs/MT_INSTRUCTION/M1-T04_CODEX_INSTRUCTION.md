# M1-T04 Codex 执行指令

## 任务名称

```text
M1-T04：持久化接口与查询契约
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T04_CODEX_INSTRUCTION.md
```

本任务只实现 Application 层的：

```text
IVocabularyRepository
ISentenceExampleRepository
ITagRepository
PagedResult<T>
VocabularySearchQuery
词条摘要、详情、例句详情和标签摘要查询模型
查询枚举与参数校验
Application 单元测试
```

本任务不实现：

- SQLite Repository。
- SQL。
- Migration002。
- 数据库表修改。
- Application UseCase。
- Godot UI。
- 词条创建流程。
- 重复决策流程。
- 搜索页面。
- M1-T05 或任何后续任务。

---

# 1. 已确认的前置基线

用户已确认最新提交：

```text
decfb68cdf7990c84047d350a25f98606ec2a054
```

当前已知状态：

- 当前分支：`main`
- Git 工作区干净。
- M1-T03 提交内容完整。
- M1-T03 为 `Done`。
- M1-T04 为 `Not Started`。
- 当前无 Godot 编辑器或残留 Godot 进程。
- 根解决方案包含 8 个项目。
- `EnglishLearningProject`、Domain、Application、Infrastructure 为 `net8.0`。
- 三个测试项目和 CaptureBridge 为 `net10.0`。
- 构建成功，0 警告、0 错误。
- 测试成功，145/145 通过。
- Migration001 未被 M1-T03 修改。
- 数据库、日志、`.godot/`、`bin/`、`obj/` 未进入 Git。

Codex 开始时仍须重新核验，不得只依赖本文件。

---

# 2. 任务目标

本任务建立 Application 与 Infrastructure 之间的持久化边界。

依赖方向必须保持：

```text
Domain
↑
Application
↑
Infrastructure
↑
Godot Composition / Presentation
```

更准确地说：

```text
Application
└─ 定义 Repository 接口和查询契约
   └─ 只引用 Domain 和 BCL

Infrastructure
└─ 后续实现 Application 中的 Repository 接口

Godot View
└─ 后续调用 Application UseCase / ViewModel
   └─ 不直接调用 SQL 或 SqliteConnection
```

本任务的产物需要支持后续：

```text
M1-T05 Migration002
M1-T06 SQLite 例句 Repository
M1-T07 SQLite 标签 Repository
M1-T08 SQLite 词条 Repository 写操作
M1-T09 SQLite 查询与生命周期
M1-T10～M1-T13 Application UseCase
M1-T14～M1-T16 Godot 页面
```

但不得提前实现这些任务。

---

# 3. 产品规格依据

产品规格明确给出：

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
        VocabularyEntry aggregate,
        CancellationToken cancellationToken);
}
```

本任务必须保留上述四个核心能力和基本语义。

产品功能要求支持：

- 精确规范化词头查重。
- 词条详情。
- 词条库分页。
- 关键词搜索。
- 游戏筛选。
- 标签筛选。
- EntryType 筛选。
- 活动、归档状态筛选。
- 全部例句。
- 标签和来源信息。
- 后续编辑、归档、恢复和永久删除。

M1 当前不实现复习系统，因此：

- 不加入真实复习状态筛选。
- 不加入到期数量。
- 不加入 ReviewCard 或 ReviewLog 查询。
- M6 再扩展复习查询。

---

# 4. 必须阅读

开始前完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/AGENT_HANDOFF.md
docs/MT_INSTRUCTION/M1-T04_CODEX_INSTRUCTION.md
```

重点读取：

```text
PRODUCT_SPEC.md
- F07：词条编辑
- F08：重复词条处理
- F09：词条库
- 第 7 节分层职责
- 第 10 节领域模型
- 第 11 节规范化
- 第 12 节数据库
- 第 13.5 节 Repository
- 第 18 节词条和例句策略

DECISIONS.md
- ADR-007：SentenceExample.CaptureId 可空
```

读取现有代码：

```text
src/GameLexicon.Domain/Entries/**
src/GameLexicon.Domain/Text/**
src/GameLexicon.Application/**
tests/GameLexicon.Application.Tests/**
src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs
```

Migration001 只读，不得修改。

如存在以下 Skills，也必须读取：

```text
.agents/skills/project-routing/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Application / Persistence Contracts

Primary writer:
primary coordinator

Supporting agents:
- milestone architect：只读审查接口最小性、查询模型和后续任务兼容性
- skill curator：仅在收尾 Skill Impact Review 需要时调用
```

本任务通常不需要 godot specialist。

---

# 5. 阶段 0：重新核验基线

## 5.1 Git

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline decfb68cdf7990c84047d350a25f98606ec2a054
git diff --check
```

必须确认：

- 当前分支为 `main`。
- 工作区干净。
- 提交存在。
- 提交完整包含 M1-T03。
- 没有未确认的用户修改。

工作区不干净时立即停止：

- 不恢复。
- 不覆盖。
- 不暂存。
- 不提交。
- 不执行 `git reset --hard`。
- 不执行 `git clean -fd`。

## 5.2 状态

确认：

```text
M1-T03 = Done
M1-T04 = Not Started
```

状态不一致时停止。

## 5.3 解决方案与框架

执行：

```powershell
dotnet sln GameLexicon.sln list
```

确认仍为 8 个项目。

确认：

```text
EnglishLearningProject                  net8.0
GameLexicon.Domain                      net8.0
GameLexicon.Application                 net8.0
GameLexicon.Infrastructure              net8.0
GameLexicon.Domain.Tests                net10.0
GameLexicon.Application.Tests           net10.0
GameLexicon.Infrastructure.Tests        net10.0
GameLexicon.CaptureBridge               net10.0
```

不得修改目标框架或项目引用。

## 5.4 基线构建与测试

优先执行：

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

预期：

```text
Build: 0 warnings, 0 errors
Tests: 145/145 passed
```

本任务不新增 NuGet 包，通常不需要 Restore。

只有资产文件缺失时才执行：

```powershell
dotnet restore GameLexicon.sln
```

不得禁用 NuGet Audit。

---

# 6. 建议目录

建议创建：

```text
src/GameLexicon.Application/
├─ Abstractions/
│  └─ Persistence/
│     ├─ IVocabularyRepository.cs
│     ├─ ISentenceExampleRepository.cs
│     └─ ITagRepository.cs
└─ Entries/
   └─ Queries/
      ├─ PagedResult.cs
      ├─ VocabularySearchQuery.cs
      ├─ VocabularyArchiveFilter.cs
      ├─ VocabularySortOrder.cs
      ├─ VocabularyEntrySummary.cs
      ├─ VocabularyEntryDetails.cs
      ├─ SentenceExampleDetails.cs
      └─ TagSummary.cs
```

测试：

```text
tests/GameLexicon.Application.Tests/
├─ Abstractions/
│  └─ Persistence/
│     └─ PersistenceContractTests.cs
└─ Entries/
   └─ Queries/
      ├─ PagedResultTests.cs
      ├─ VocabularySearchQueryTests.cs
      └─ EntryReadModelTests.cs
```

可按现有命名风格小幅调整，但必须：

- Repository 接口位于 Application。
- 查询模型位于 Application。
- 不放进 Domain。
- 不放进 Infrastructure。
- 不放进 Godot。

---

# 7. 总体契约原则

## 7.1 不泄漏实现细节

Application 的公共 API 不得引用：

```text
Microsoft.Data.Sqlite
SqliteConnection
SqliteTransaction
SqliteDataReader
System.Data.Common
Godot
Godot.Collections
Infrastructure 类型
SQL 字符串
表名或列名
```

Repository 只暴露：

- Domain 类型。
- Application 查询模型。
- BCL 类型。
- `Task`。
- `CancellationToken`。
- `IReadOnlyList<T>`。

## 7.2 接口保持最小

不得创建通用框架式接口：

```text
IRepository<T>
IReadRepository<T>
IWriteRepository<T>
IUnitOfWork
IGenericSpecification
IQueryable<T>
Expression<Func<T, bool>>
```

原因：

- 当前产品需要明确业务语义。
- 避免 Infrastructure 查询实现泄漏到 Application。
- 避免 Godot View 拼接任意数据库查询。
- 避免范围膨胀。

## 7.3 不返回 IQueryable

Repository 方法不得返回：

```csharp
IQueryable<T>
IAsyncEnumerable<T>
DbDataReader
DataTable
```

MVP 使用明确的异步方法和不可变结果。

## 7.4 CancellationToken

所有可能访问持久化层的异步方法必须显式接收：

```csharp
CancellationToken cancellationToken
```

规则：

- 不使用可选默认值，除非现有 Application 接口风格已经统一采用默认值。
- 参数放在最后。
- 不在接口层创建或取消令牌。
- 后续实现必须传播令牌。

## 7.5 不记录学习文本

这些契约可能携带：

- 词头。
- 例句。
- 定义。
- 翻译。
- 笔记。
- 标签。

因此：

- 构造验证异常不回显输入文本。
- 不添加 `ToString()` 输出全部内容。
- 不添加日志。
- 不将输入文本写入静态缓存。

---

# 8. `IVocabularyRepository`

创建：

```text
src/GameLexicon.Application/Abstractions/Persistence/IVocabularyRepository.cs
```

核心接口必须包含：

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

## 8.1 可加入永久删除能力

后续 M1-T09 和 M1-T13 要求显式永久删除。

允许本任务加入：

```csharp
Task DeletePermanentlyAsync(
    Guid entryId,
    CancellationToken cancellationToken);
```

规则：

- 只加入永久删除。
- 归档与恢复由 `VocabularyEntry.SetArchived(...)` 后调用 `SaveAsync` 完成。
- 不建立 `ArchiveAsync`、`RestoreAsync`、`UpdateAsync`、`InsertAsync` 等重复方法。
- `SaveAsync` 表示按实体 ID 保存新建或已有实体。
- 本任务不定义 SQLite UPSERT 细节。
- 本任务不定义永久删除确认 UI；确认属于 UseCase。

如果 milestone architect 基于现有项目证据认为永久删除应延后：

- 可不加入该方法。
- 必须在最终报告说明。
- 不得加入一组不确定的生命周期方法。

## 8.2 `FindByNormalizedHeadwordAsync`

契约语义：

- 接收调用方已经通过 `ITextNormalizer` 生成的规范化词头。
- Repository 不自行执行文本规范化。
- 空或纯空白参数应由实现拒绝。
- 返回活动词条优先遵循当前活动唯一索引语义。
- 当前不支持同规范化词头的多个活动词条。
- 是否包含归档词条必须明确。

本任务统一规定：

```text
FindByNormalizedHeadwordAsync
→ 只查活动词条
→ IsArchived = false
```

理由：

- 当前唯一索引只约束活动词条。
- F08 保存前精确查重针对活动词条。
- 归档恢复冲突由后续 UseCase 处理。

## 8.3 `SaveAsync`

契约语义：

- 新 ID：创建。
- 已存在 ID：更新。
- 不自动规范化词头。
- 不自动保存例句和标签链接，除非后续实现明确使用事务组合。
- 不使用 `INSERT OR REPLACE`。
- 规范化词头唯一冲突不能静默覆盖其他词条。
- 失败不应留下部分保存。

本任务只定义契约，不定义冲突结果或异常类型。

原因：

- 产品规格给出的接口返回 `Task`。
- 稳定冲突映射留给 M1-T08 在实现时根据实际 SQLite 错误设计最小 Application 异常或结果。
- 不在本任务凭空创建复杂错误体系。

---

# 9. `ISentenceExampleRepository`

创建：

```text
src/GameLexicon.Application/Abstractions/Persistence/ISentenceExampleRepository.cs
```

建议最小接口：

```csharp
public interface ISentenceExampleRepository
{
    Task<SentenceExample?> GetByIdAsync(
        Guid exampleId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SentenceExampleDetails>> GetForEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        SentenceExample example,
        CancellationToken cancellationToken);

    Task SaveLinkAsync(
        EntryExampleLink link,
        CancellationToken cancellationToken);

    Task SetPrimaryAsync(
        Guid entryId,
        Guid exampleId,
        CancellationToken cancellationToken);

    Task RemoveLinkAsync(
        Guid entryId,
        Guid exampleId,
        CancellationToken cancellationToken);
}
```

## 9.1 语义

### `GetByIdAsync`

- 返回 Domain `SentenceExample`。
- 找不到返回 null。
- 不返回 SQLite DTO。

### `GetForEntryAsync`

- 按 `SortOrder` 升序。
- 相同 `SortOrder` 时使用稳定的 `ExampleId` 顺序。
- 返回例句及链接元数据：
  - `IsPrimary`
  - `SortOrder`
- 返回空列表而不是 null。

### `SaveAsync`

- 只保存例句自身。
- 不自动创建词条。
- 不自动创建 Capture 或 OCR Region。
- M1-T05 之后必须支持 `CaptureId = null`。

### `SaveLinkAsync`

- 保存或更新 `(EntryId, ExampleId)` 链接。
- 同一链接重复保存应幂等。
- 不允许链接不存在的词条或例句。
- 不自动把其他链接取消 Primary。

### `SetPrimaryAsync`

必须定义为一个持久化原子操作：

```text
验证目标链接存在
→ 将同一 Entry 的其他链接 IsPrimary 置为 false
→ 将目标链接 IsPrimary 置为 true
→ 同一事务提交
```

原因：

- 单个 Domain `EntryExampleLink` 无法保证跨链接只有一个主要例句。
- 该不变量需要 Repository 事务支持。

本任务只定义接口语义，不实现事务。

### `RemoveLinkAsync`

- 只删除链接。
- 不自动删除例句实体。
- 不自动删除截图。
- 不进行孤儿例句清理。
- 找不到链接时后续实现应幂等完成或返回稳定结果；本任务不定义复杂结果类型。

## 9.2 不加入的能力

当前不加入：

```text
DeleteExampleAsync
DeleteOrphansAsync
ReplaceAllLinksAsync
MoveLinkAsync
BulkSaveAsync
```

除非现有产品规格有直接证据。

---

# 10. `ITagRepository`

创建：

```text
src/GameLexicon.Application/Abstractions/Persistence/ITagRepository.cs
```

建议最小接口：

```csharp
public interface ITagRepository
{
    Task<Tag?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken);

    Task<Tag> GetOrCreateAsync(
        Tag candidate,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Tag>> GetForEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken);

    Task SetForEntryAsync(
        Guid entryId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken);
}
```

## 10.1 语义

### `FindByNormalizedNameAsync`

- 接收调用方已通过 `ITextNormalizer` 生成的规范化标签。
- Repository 不自行规范化。
- 找不到返回 null。

### `GetOrCreateAsync`

- `candidate` 包含调用方生成的 ID、Name 和 NormalizedName。
- 已有相同 `NormalizedName`：
  - 返回已有 Tag。
  - 不创建重复记录。
- 不存在：
  - 保存 candidate。
  - 返回 candidate 或等价重建对象。
- 并发冲突后仍应返回唯一 Tag。
- 本任务不实现 SQLite 并发处理。

### `GetForEntryAsync`

- 返回与词条关联的标签。
- 建议按 `NormalizedName`、再按 `Id` 稳定排序。
- 返回空列表而不是 null。

### `SetForEntryAsync`

语义：

- 将词条标签集合替换为传入集合。
- 整个关联替换在一个事务中完成。
- `tagIds` 不得为 null。
- 不得包含 Guid.Empty。
- 重复 ID 应被调用契约拒绝，而不是静默重复。
- 空列表表示清空词条标签。
- 不创建不存在的 Tag。
- 不修改 Tag 的 Name。
- 不删除未被使用的 Tag。

本任务只定义接口，不实现事务。

## 10.2 不加入的能力

当前不加入：

```text
DeleteTagAsync
RenameTagAsync
SearchTagsAsync
MergeTagsAsync
Tag management UI
```

Tag 重命名和管理不属于 M1 当前最小链路。

---

# 11. `PagedResult<T>`

创建：

```text
src/GameLexicon.Application/Entries/Queries/PagedResult.cs
```

建议实现为不可变 sealed 类型：

```csharp
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public long TotalCount { get; }

    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }
}
```

## 11.1 构造不变量

必须验证：

```text
items != null
PageNumber >= 1
PageSize >= 1
TotalCount >= 0
Items.Count <= PageSize
```

允许：

```text
TotalCount = 0
Items.Count = 0
PageNumber = 1
```

当请求页超过最后一页时，允许：

```text
Items.Count = 0
TotalCount > 0
PageNumber > TotalPages
```

Repository 后续可以返回空页，Application UseCase 决定是否调整页码。

## 11.2 防御性复制

必须防止调用方后续修改输入集合影响结果。

不得直接保存可变 `List<T>` 引用。

可使用：

```csharp
Array.AsReadOnly(items.ToArray())
```

不需要新增 `System.Collections.Immutable` 包。

## 11.3 TotalPages

使用安全的整数计算：

```text
TotalCount == 0 → 0
否则 → ceil(TotalCount / PageSize)
```

避免：

- 浮点精度问题。
- int 溢出。
- 除零。

`TotalCount` 使用 `long`，因为数据库 COUNT 返回可能超过 int。

## 11.4 HasPreviousPage / HasNextPage

建议：

```text
HasPreviousPage = PageNumber > 1
HasNextPage = TotalPages > 0 && PageNumber < TotalPages
```

---

# 12. 查询枚举

## 12.1 `VocabularyArchiveFilter`

创建：

```text
src/GameLexicon.Application/Entries/Queries/VocabularyArchiveFilter.cs
```

建议：

```csharp
public enum VocabularyArchiveFilter
{
    ActiveOnly = 0,
    ArchivedOnly = 1,
    All = 2
}
```

不得加入其他状态。

## 12.2 `VocabularySortOrder`

创建：

```text
src/GameLexicon.Application/Entries/Queries/VocabularySortOrder.cs
```

MVP 建议：

```csharp
public enum VocabularySortOrder
{
    UpdatedAtDescending = 0,
    HeadwordAscending = 1,
    CreatedAtDescending = 2
}
```

规则：

- 数值显式固定。
- 不接受未定义枚举值。
- 后续 SQLite 查询必须使用稳定次级排序：
  `Id ASC`。
- 本任务不写 SQL。

---

# 13. `VocabularySearchQuery`

创建：

```text
src/GameLexicon.Application/Entries/Queries/VocabularySearchQuery.cs
```

建议不可变契约：

```csharp
public sealed class VocabularySearchQuery
{
    public string? SearchText { get; }
    public string? GameTitle { get; }
    public IReadOnlyList<Guid> TagIds { get; }
    public EntryType? EntryType { get; }
    public VocabularyArchiveFilter ArchiveFilter { get; }
    public VocabularySortOrder SortOrder { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
}
```

## 13.1 默认值

建议：

```text
SearchText = null
GameTitle = null
TagIds = empty
EntryType = null
ArchiveFilter = ActiveOnly
SortOrder = UpdatedAtDescending
PageNumber = 1
PageSize = 50
```

## 13.2 校验

必须验证：

```text
PageNumber >= 1
PageSize >= 1
PageSize <= 200
ArchiveFilter 是已定义值
SortOrder 是已定义值
EntryType null 或已定义值
TagIds != null
TagIds 不包含 Guid.Empty
TagIds 不包含重复 ID
```

对于字符串筛选：

```text
null → 不筛选
空字符串或纯空白 → 拒绝
```

理由：

- 避免 null 和空白表示同一语义却产生不同查询。
- UseCase/UI 应将未填写筛选转换为 null。
- Repository 不应偷偷 Trim 或规范化。

## 13.3 文本规范化边界

`SearchText` 可能是：

- 原始用户查询。
- 词头查询。
- 翻译查询。

本任务不强制使用 `ITextNormalizer` 改写 SearchText。

规则：

- 精确重复检测必须使用 `FindByNormalizedHeadwordAsync`。
- 一般搜索的大小写、部分匹配和转义语义由 M1-T09 定义。
- Query 不自行小写、Trim 或 Form KC。
- Query 只拒绝纯空白。

`GameTitle` 同样不自动规范化。

## 13.4 TagIds 防御性复制

必须复制输入集合，防止调用方修改。

不要静默去重。

重复 ID 应抛出参数异常。

---

# 14. `TagSummary`

创建：

```text
src/GameLexicon.Application/Entries/Queries/TagSummary.cs
```

建议不可变 record/class：

```csharp
public sealed record TagSummary(
    Guid Id,
    string Name,
    string NormalizedName);
```

如果使用 primary constructor record，仍需提供校验。

要求：

- Id 非 Guid.Empty。
- Name 非空白。
- NormalizedName 非空白。
- 不自行规范化。
- 不引用 Domain `Tag` 可变对象。
- 不输出日志。

可使用显式构造的 sealed class 以便验证。

---

# 15. `SentenceExampleDetails`

创建：

```text
src/GameLexicon.Application/Entries/Queries/SentenceExampleDetails.cs
```

建议只读字段：

```text
Id
CaptureId?
OcrRegionId?
SentenceText
NormalizedSentence
TargetStart
TargetLength
ScreenshotCropPath
GameTitle?
CreatedAt
IsPrimary
SortOrder
TargetText（可选计算属性）
```

## 15.1 校验

应复用与 Domain 一致的基础语义：

- Id 非空。
- Capture/OCR 组合合法。
- SentenceText 和 NormalizedSentence 非空白。
- TargetStart/TargetLength 在 UTF-16 范围内。
- 不切断 surrogate pair。
- CreatedAt 为 UTC。
- SortOrder >= 0。

但不得复制大量 Domain 实现。

推荐方式：

- 构造时接收 `SentenceExample example` 和 `EntryExampleLink link`；
- 验证两者 ID 对应；
- 将标量值复制为不可变读模型。

示例：

```csharp
public SentenceExampleDetails(
    SentenceExample example,
    EntryExampleLink link)
```

要求：

- `example.Id == link.ExampleId`
- 只复制标量，不长期保存可变 Domain 引用。
- 不要求 link.EntryId，因为详情模型不一定单独公开 EntryId；可以公开 `EntryId` 以增强一致性。

也允许显式标量构造，但必须避免与 Domain 不变量漂移。

---

# 16. `VocabularyEntrySummary`

创建：

```text
src/GameLexicon.Application/Entries/Queries/VocabularyEntrySummary.cs
```

M1 词条库列表最小字段建议：

```text
Id
Headword
EntryType
TranslationChinese?
PrimaryExampleText?
PrimaryGameTitle?
Tags
IsArchived
CreatedAt
UpdatedAt
```

建议公开：

```csharp
public sealed class VocabularyEntrySummary
{
    public Guid Id { get; }
    public string Headword { get; }
    public EntryType EntryType { get; }
    public string? TranslationChinese { get; }
    public string? PrimaryExampleText { get; }
    public string? PrimaryGameTitle { get; }
    public IReadOnlyList<TagSummary> Tags { get; }
    public bool IsArchived { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
}
```

## 16.1 不加入的字段

本任务不加入：

- Review due count。
- Review status。
- Ease factor。
- Screenshot pixels。
- OCR tokens。
- TTS state。
- Database row version。

## 16.2 校验

- Id 非空。
- Headword 非空白。
- EntryType 有效。
- 时间为 UTC。
- UpdatedAt 不早于 CreatedAt。
- Tags 非 null，防御性复制。
- Tag ID 不重复。
- 不自动修改文本。

可选字符串允许 null 或空字符串，遵循 M1-T03 Domain 约定。

---

# 17. `VocabularyEntryDetails`

创建：

```text
src/GameLexicon.Application/Entries/Queries/VocabularyEntryDetails.cs
```

必须能支持 M1 详情页的完整读取。

建议字段：

```text
Id
Headword
NormalizedHeadword
EntryType
PartOfSpeech?
Phonetic?
DefinitionEnglish?
TranslationChinese?
Notes?
IsArchived
CreatedAt
UpdatedAt
Examples
Tags
```

建议：

```csharp
public sealed class VocabularyEntryDetails
{
    // Scalar entry fields
    public IReadOnlyList<SentenceExampleDetails> Examples { get; }
    public IReadOnlyList<TagSummary> Tags { get; }
}
```

## 17.1 设计规则

- 不长期保存可变 `VocabularyEntry` 引用。
- 构造时可接收 Domain 实体并复制标量。
- Examples 和 Tags 必须防御性复制。
- Examples 按 `SortOrder`，再按 `Id` 稳定排序。
- 只能有 0 或 1 个 `IsPrimary = true`。
- 如果输入存在多个 Primary，应拒绝，而不是静默选择。
- Tag ID 不得重复。
- Example ID 不得重复。
- Example 的 EntryId（如公开）必须与详情 Id 一致。
- 不包含 SQLite 类型。
- 不包含 UI 状态。

## 17.2 无主要例句

允许：

```text
Examples 非空
但 0 个 IsPrimary
```

原因：

- 数据修复或创建中间状态可能暂时没有主要例句。
- 后续 UseCase 可以要求新创建词条至少一个 Primary。
- 查询契约不应伪造主要例句。

---

# 18. 参数和查询模型异常

建议：

```text
null → ArgumentNullException
Guid.Empty → ArgumentException
空白字符串 → ArgumentException
页码、页大小、SortOrder → ArgumentOutOfRangeException / ArgumentException
重复 ID → ArgumentException
不一致的 Entity/Link → ArgumentException
```

要求：

- 同类规则保持一致。
- 异常消息不回显学习文本。
- 参数名准确。
- 不创建自定义异常框架。

---

# 19. Repository 接口文档

每个接口和方法需要简洁 XML 文档，至少说明：

- null/找不到语义。
- 活动词条查重语义。
- 排序语义。
- 是否为原子操作。
- 是否幂等。
- 是否自动规范化。
- CancellationToken 必须传播。

不要写大段实现教程。

不得在 XML 文档中：

- 写具体 SQL。
- 承诺 SQLite 专属行为。
- 泄漏用户文件路径。
- 包含用户学习文本示例。

---

# 20. Application 单元测试

所有测试使用现有 xUnit。

不新增 NuGet 包。

## 20.1 `VocabularySearchQueryTests`

至少覆盖：

### 默认值

```text
ArchiveFilter = ActiveOnly
SortOrder = UpdatedAtDescending
PageNumber = 1
PageSize = 50
TagIds empty
```

### 页码

- PageNumber 0 被拒绝。
- PageNumber 负数被拒绝。
- PageSize 0 被拒绝。
- PageSize 201 被拒绝。
- PageSize 1 合法。
- PageSize 200 合法。

### 枚举

- 未定义 EntryType 被拒绝。
- 未定义 ArchiveFilter 被拒绝。
- 未定义 SortOrder 被拒绝。

### 字符串

- null SearchText 合法。
- 非空 SearchText 保留原值。
- 空 SearchText 被拒绝。
- 纯空白 SearchText 被拒绝。
- null GameTitle 合法。
- 纯空白 GameTitle 被拒绝。

### TagIds

- null 被拒绝。
- 空集合合法。
- Guid.Empty 被拒绝。
- 重复 ID 被拒绝。
- 构造后修改原列表不影响 Query。

### 不自动规范化

验证：

```text
SearchText 原值保留
GameTitle 原值保留
```

不调用 `ITextNormalizer`。

## 20.2 `PagedResultTests`

至少覆盖：

- 合法第一页。
- TotalCount 0。
- TotalPages 0。
- TotalCount 恰好整除 PageSize。
- TotalCount 非整除。
- HasPreviousPage。
- HasNextPage。
- 超出最后一页为空结果。
- PageNumber 0 被拒绝。
- PageSize 0 被拒绝。
- TotalCount 负数被拒绝。
- Items.Count 大于 PageSize 被拒绝。
- items null 被拒绝。
- 输入 List 后续修改不影响 Items。
- 大 TotalCount 不发生 int 溢出。

## 20.3 `EntryReadModelTests`

至少覆盖：

### TagSummary

- 合法。
- 空 ID。
- 空名称。
- 空规范化名称。

### SentenceExampleDetails

- 从匹配的 Domain example/link 创建。
- ExampleId 不匹配被拒绝。
- 手工例句可空 Capture。
- UTF-16 目标文本正确。
- SortOrder 保留。
- IsPrimary 保留。

### VocabularyEntrySummary

- 合法字段。
- Tags 防御性复制。
- 重复 Tag ID 被拒绝。
- 未定义 EntryType 被拒绝。
- 时间顺序错误被拒绝。
- 不自动修改文本。

### VocabularyEntryDetails

- 完整字段复制。
- Examples 稳定排序。
- Tags 稳定或输入顺序策略明确。
- 集合防御性复制。
- 重复 Example ID 被拒绝。
- 重复 Tag ID 被拒绝。
- 多个 Primary 被拒绝。
- 无 Primary 合法。
- Domain 实体后续更新不影响已创建的 Details 标量。

## 20.4 `PersistenceContractTests`

使用反射或编译期测试至少确认：

- 三个 Repository 接口位于 Application。
- 所有异步方法返回 `Task` 或 `Task<T>`。
- 所有异步方法最后一个参数为 `CancellationToken`。
- 公共接口不引用：
  - `Microsoft.Data.Sqlite`
  - `Godot`
  - Infrastructure namespace
  - `IQueryable`
- `IVocabularyRepository` 包含产品规格四个核心方法。
- Application 项目不引用 Infrastructure 项目。
- Domain 项目不引用 Application 项目。

不得通过字符串脆弱地验证所有源码格式；优先反射验证公开 API。

---

# 21. 不要添加的类型与能力

本任务不得创建：

```text
SqliteVocabularyRepository
SqliteSentenceExampleRepository
SqliteTagRepository
Migration002
CreateManualEntryUseCase
DuplicateEntryDecision
SearchEntriesUseCase
UpdateEntryUseCase
ArchiveEntryUseCase
Godot ViewModel
Godot Scene
```

不得添加：

```text
IUnitOfWork
Generic Repository
Specification Pattern
CQRS/MediatR
AutoMapper
FluentValidation
Entity Framework
Dapper
System.Collections.Immutable package
```

不得创建 Review Repository 或 Review 查询模型。

---

# 22. 允许创建和修改的文件

建议创建：

```text
src/GameLexicon.Application/Abstractions/Persistence/IVocabularyRepository.cs
src/GameLexicon.Application/Abstractions/Persistence/ISentenceExampleRepository.cs
src/GameLexicon.Application/Abstractions/Persistence/ITagRepository.cs

src/GameLexicon.Application/Entries/Queries/PagedResult.cs
src/GameLexicon.Application/Entries/Queries/VocabularySearchQuery.cs
src/GameLexicon.Application/Entries/Queries/VocabularyArchiveFilter.cs
src/GameLexicon.Application/Entries/Queries/VocabularySortOrder.cs
src/GameLexicon.Application/Entries/Queries/VocabularyEntrySummary.cs
src/GameLexicon.Application/Entries/Queries/VocabularyEntryDetails.cs
src/GameLexicon.Application/Entries/Queries/SentenceExampleDetails.cs
src/GameLexicon.Application/Entries/Queries/TagSummary.cs

tests/GameLexicon.Application.Tests/Abstractions/Persistence/PersistenceContractTests.cs
tests/GameLexicon.Application.Tests/Entries/Queries/PagedResultTests.cs
tests/GameLexicon.Application.Tests/Entries/Queries/VocabularySearchQueryTests.cs
tests/GameLexicon.Application.Tests/Entries/Queries/EntryReadModelTests.cs
```

允许修改：

```text
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/DECISIONS.md（仅发现新的架构决策时）
docs/SKILLS_CATALOG.md（仅 Skill Impact Review 需要时）
docs/SKILL_CHANGELOG.md（仅 Skill 实际更新时）
.agents/skills/*/SKILL.md（仅可复用工作流变化时）
```

正常情况下不得修改：

```text
GameLexicon.sln
任一 .csproj
src/GameLexicon.Domain/**
src/GameLexicon.Infrastructure/**
english-learning-project/**
Migration001_Initial.cs
数据库文件
```

如果 Application 当前没有引用 Domain，说明基线异常，应停止而不是自行改变项目引用。

---

# 23. 本任务明确不做

不得实现：

- SQL。
- SQLite 连接。
- Repository 具体类。
- Migration002。
- CaptureId 数据库可空改造。
- 数据库索引。
- UseCase。
- DTO 映射服务。
- 工厂。
- 依赖注入注册。
- AppServices 接线。
- 手工词条页面。
- Library 页面。
- 查询执行。
- 重复词条弹窗。
- 编辑、归档、恢复、永久删除流程。
- Review 查询。
- 截图、OCR、TTS。
- M1-T05。

---

# 24. 自动验证

## 24.1 Application 构建

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build `
  src/GameLexicon.Application/GameLexicon.Application.csproj `
  --no-restore
```

要求：

- 0 错误。
- 0 新增警告。

## 24.2 Application.Tests

执行：

```powershell
dotnet build `
  tests/GameLexicon.Application.Tests/GameLexicon.Application.Tests.csproj `
  --no-restore

dotnet test `
  tests/GameLexicon.Application.Tests/GameLexicon.Application.Tests.csproj `
  --no-build `
  --no-restore
```

要求：

- 所有原有与新增 Application 测试通过。
- 报告新增测试数量。
- 报告 Application 测试总数。

## 24.3 根解决方案

执行：

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功。
- 所有测试通过。
- 0 错误。
- 0 新增警告。

## 24.4 不需要 Godot

本任务不修改 Godot：

- 不启动 Godot Editor。
- 不需要 GUI 验收。
- 通常不需要 Godot headless。
- 确认当前没有 Godot 进程即可。

---

# 25. 代表性自动验收

最终报告必须明确列出：

## Repository 边界

```text
IVocabularyRepository 四个规格核心方法 → Present
ISentenceExampleRepository → Present
ITagRepository → Present
CancellationToken → All async methods
SQLite types in public API → None
Godot types in public API → None
IQueryable → None
```

## Search Query

```text
默认 ActiveOnly / UpdatedAtDescending / Page 1 / Size 50 → Pass
PageSize 200 → Pass
PageSize 201 → Rejected
Guid.Empty Tag → Rejected
重复 Tag ID → Rejected
调用方修改原 Tag 列表 → Query unchanged
```

## Paging

```text
TotalCount 0 → TotalPages 0
TotalCount 101 / PageSize 50 → TotalPages 3
Page 2 / TotalPages 3 → HasPrevious true, HasNext true
Mutable input list modified → Result unchanged
```

## Details

```text
Example/link ID mismatch → Rejected
Two primary examples → Rejected
No primary example → Pass
Duplicate Tag ID → Rejected
Domain entity changed after projection → Details unchanged
```

不得只说“测试全部通过”。

---

# 26. 非 GUI 人工审查

自动验收通过后状态：

```text
M1-T04 = Awaiting Manual Verification
M1-T05 = Not Started
```

本任务不需要 GUI。

人工审查重点：

1. Repository 接口只在 Application。
2. 公共 API 不包含 SQLite、Godot、Infrastructure 或 IQueryable。
3. `IVocabularyRepository` 保留规格四个核心方法。
4. 例句 Repository 明确跨链接主要例句事务语义。
5. 标签 Repository 明确幂等 GetOrCreate 和原子关联替换。
6. 分页、筛选、排序契约校验完整。
7. Query 和读模型不可变并进行防御性复制。
8. 查询模型不自动规范化用户文本。
9. 没有 SQL、Migration、UseCase 或 UI。
10. 所有测试通过。

用户确认前不得将 M1-T04 标记为 Done。

---

# 27. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且修改未确认。
- 找不到提交 `decfb68c...`。
- M1-T03 未标记 Done。
- M1-T04 状态不是 Not Started。
- 基线构建或测试失败。
- 解决方案不再是 8 个项目。
- 目标框架发生变化。
- Application 不再引用 Domain。
- 必须新增 NuGet 包。
- 必须修改项目引用。
- 必须修改 Domain 实体。
- 必须修改 Migration001。
- 必须实现 Migration002。
- 必须实现 SQLite Repository。
- 必须修改 Godot。
- 查询契约需要提前加入 M6 复习功能。
- 用户文件可能被覆盖。

停止后不得：

- `git reset --hard`
- `git clean -fd`
- 自动恢复用户文件
- 禁用 NuGet Audit
- 自动提交
- 自动执行 M1-T05

---

# 28. Git 检查

完成自动验证后执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
git diff --name-only
```

确认：

- 生产代码只在：
  `src/GameLexicon.Application/**`
- 测试只在：
  `tests/GameLexicon.Application.Tests/**`
- 其余只允许状态/决策文档。
- 没有 `.csproj` 修改。
- 没有 Domain 修改。
- 没有 Infrastructure 修改。
- 没有 Migration 修改。
- 没有 Godot 修改。
- 没有数据库、日志或构建产物。
- 暂存区为空。
- 未创建 Git 提交。

额外检查：

```powershell
git diff --name-only |
  Select-String -Pattern `
    "Infrastructure|Migration|english-learning-project|\.csproj$"
```

正常应无匹配。

---

# 29. 状态与文档

自动验收通过后更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

状态：

```text
M1-T04 = Awaiting Manual Verification
M1-T05 = Not Started
```

记录：

- Task ID。
- 名称。
- 三个 Repository 接口。
- IVocabularyRepository 核心方法。
- 例句主要链接事务契约。
- 标签幂等和关联契约。
- 查询筛选字段。
- 页码和页大小规则。
- 排序规则。
- ArchiveFilter。
- 读模型列表。
- 防御性复制。
- CancellationToken 覆盖。
- 公共 API 无 SQLite/Godot 类型。
- 新增测试数量。
- Application 测试结果。
- 根解决方案测试结果。
- 未修改数据库、Godot 和 Domain。
- 已知限制。

更新：

```text
docs/AGENT_HANDOFF.md
```

仅在新增长期架构决定时更新：

```text
docs/DECISIONS.md
```

建议只有以下情况才新增 ADR：

- 永久删除是否加入 IVocabularyRepository。
- Repository 的单一主要例句原子语义。
- 标签集合采用替换语义。

如这些只是直接来自任务和规格，可记录在状态文档，不必滥增 ADR。

只有环境事实变化时才更新：

```text
docs/ENVIRONMENT.md
```

正常情况下不修改 `ENVIRONMENT.md`。

人工审查通过后：

```text
M1-T04 = Done
M1-T05 = Not Started
```

不得执行 M1-T05。

---

# 30. Skill Impact Review

任务结束后应用：

```text
skill-maintenance
```

报告：

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

只有以下可复用规则发生变化时更新 Skill：

- Application Repository 契约标准。
- 公共 API 泄漏检查。
- 查询模型不可变性和防御性复制标准。
- CancellationToken 接口规则。
- 任务路由或验收模板。

普通接口和查询模型代码不自动构成 Skill 更新理由。

---

# 31. 自动验收清单

- [ ] 提交 `decfb68c...` 存在
- [ ] 当前分支 main
- [ ] 初始工作区干净
- [ ] M1-T03 Done
- [ ] M1-T04 Not Started
- [ ] 基线 Build 成功
- [ ] 基线 145/145 测试通过
- [ ] 未新增 NuGet 包
- [ ] IVocabularyRepository 位于 Application
- [ ] IVocabularyRepository 含规格四个核心方法
- [ ] ISentenceExampleRepository 位于 Application
- [ ] ITagRepository 位于 Application
- [ ] 所有异步持久化方法有 CancellationToken
- [ ] 公共 API 无 SQLite 类型
- [ ] 公共 API 无 Godot 类型
- [ ] 公共 API 无 Infrastructure 类型
- [ ] 不返回 IQueryable
- [ ] PagedResult<T> 创建
- [ ] PagedResult 防御性复制
- [ ] TotalPages 正确
- [ ] VocabularySearchQuery 创建
- [ ] PageNumber >= 1
- [ ] PageSize 1～200
- [ ] TagIds 无空 ID 和重复 ID
- [ ] ArchiveFilter 创建
- [ ] SortOrder 创建
- [ ] 未定义枚举被拒绝
- [ ] Query 不自动规范化文本
- [ ] VocabularyEntrySummary 创建
- [ ] VocabularyEntryDetails 创建
- [ ] SentenceExampleDetails 创建
- [ ] TagSummary 创建
- [ ] 读模型防御性复制
- [ ] 重复 Example/Tag ID 被拒绝
- [ ] 多 Primary 被拒绝
- [ ] 无 Primary 合法
- [ ] Domain 变更不影响已创建读模型
- [ ] 未修改 Domain
- [ ] 未修改 Migration001
- [ ] 未实现 Migration002
- [ ] 未实现 SQLite Repository
- [ ] 未实现 UseCase
- [ ] 未修改 Godot
- [ ] Application 构建通过
- [ ] Application 测试通过
- [ ] 根解决方案构建通过
- [ ] 全部测试通过
- [ ] git diff --check 通过
- [ ] 暂存区为空
- [ ] 未创建提交
- [ ] M1-T05 未执行
- [ ] Skill Impact Review 完成

---

# 32. 人工审查清单

- [ ] 三个 Repository 接口存在
- [ ] 接口都位于 Application
- [ ] 接口不泄漏 SQLite/Godot
- [ ] IVocabularyRepository 符合产品规格
- [ ] FindByNormalizedHeadword 只查活动词条语义明确
- [ ] SaveAsync 不自动规范化
- [ ] SetPrimaryAsync 事务语义明确
- [ ] SetForEntryAsync 替换语义明确
- [ ] 查询条件覆盖关键词、游戏、标签、类型、归档
- [ ] 分页参数范围合理
- [ ] 排序具有稳定性约定
- [ ] 查询模型不可变
- [ ] 集合防御性复制
- [ ] 无 Review/M6 范围
- [ ] 无 SQL、Migration、UseCase、UI
- [ ] 所有测试通过
- [ ] Git diff 仅属于 M1-T04

---

# 33. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T04
- 名称: 持久化接口与查询契约
- 状态:
- M1-T05 executed: No
- Git commit created: No
- GUI verification required: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M1-T03 commit:
- Branch:
- Initial Git status:
- Solution projects:
- Target frameworks:
- Baseline build:
- Baseline tests:

## Repository 接口

- IVocabularyRepository:
- ISentenceExampleRepository:
- ITagRepository:
- CancellationToken coverage:
- SQLite types exposed:
- Godot types exposed:
- IQueryable exposed:

## 查询契约

- PagedResult:
- VocabularySearchQuery:
- Archive filter:
- Sort order:
- Page limits:
- Tag filters:
- Text normalization policy:

## 读模型

- VocabularyEntrySummary:
- VocabularyEntryDetails:
- SentenceExampleDetails:
- TagSummary:
- Defensive copies:
- Duplicate protections:
- Primary-example rule:

## 语义决定

- FindByNormalizedHeadword scope:
- Save semantics:
- Permanent delete included:
- Primary example transaction:
- Tag set replacement:
- Review filtering deferred:

## 明确未实现

- SQLite:
- Migration002:
- Repository implementations:
- UseCases:
- Godot UI:
- Review:

## 代表案例

| Case | Actual | Expected | Result |
|---|---|---|---|
| ... | ... | ... | Pass |

## 创建的文件

- ...

## 修改的文件

- ...

## 自动化测试

- Baseline total:
- Added:
- Application total:
- Root total:
- Passed:
- Failed:
- Skipped:

## 构建结果

- Application:
- Application.Tests:
- Root solution:
- Warnings:
- Errors:

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

## 已知限制

- No SQLite implementation.
- No stable duplicate-conflict result type yet.
- No Review/M6 filters.
- No UseCases or UI.
- ...

## 下一任务

- M1-T05：Migration002 手工例句与检索支持
- Status: Not Started
- Not automatically executed
```

---

# 34. 可直接执行的总指令

请执行：

```text
M1-T04：持久化接口与查询契约
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T04_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `decfb68cdf7990c84047d350a25f98606ec2a054`。
2. 开始时 Git 工作区必须干净。
3. 只在 Application 和 Application.Tests 中实现持久化接口、查询契约和测试。
4. `IVocabularyRepository` 保留产品规格四个核心方法。
5. 定义最小 `ISentenceExampleRepository` 和 `ITagRepository`。
6. 所有异步持久化方法显式接收 CancellationToken。
7. 公共 API 不得泄漏 SQLite、Godot、Infrastructure、SQL 或 IQueryable。
8. 实现不可变 `PagedResult<T>`、`VocabularySearchQuery` 和读模型。
9. 分页 PageNumber >= 1，PageSize 1～200。
10. Query 支持关键词、游戏、标签、EntryType 和归档筛选。
11. Query 不自行规范化或修改用户文本。
12. 集合必须防御性复制。
13. 多个主要例句的详情输入必须被拒绝；没有主要例句允许。
14. 例句 SetPrimary 定义为后续 Repository 的原子事务。
15. 标签 SetForEntry 定义为后续 Repository 的原子替换。
16. 不修改 Domain、Infrastructure、Migration001、Godot 或项目引用。
17. 不实现 Migration002、SQLite Repository、UseCase 或 UI。
18. 不新增 NuGet 包。
19. 不执行 M1-T05。
20. 不创建 Git 提交。
21. 自动验收后保持 Awaiting Manual Verification。
22. 本任务不需要 GUI 验收。
23. 完成后执行 Git diff、状态文档更新和 Skill Impact Review。
