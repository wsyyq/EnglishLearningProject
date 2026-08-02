# M1-T09 Codex 执行指令

## 任务名称

```text
M1-T09：SQLite 查询与生命周期
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T09_CODEX_INSTRUCTION.md
```

本任务只实现：

```text
SqliteVocabularyRepository 查询侧
FindByNormalizedHeadwordAsync
GetDetailsAsync
SearchAsync
SqliteVocabularyRepository 正式实现 IVocabularyRepository
活动/归档查询生命周期
详情聚合
关键词、游戏、标签、类型和归档筛选
稳定排序与分页
Infrastructure 自动化测试
```

本任务不实现：

- 永久删除接口或 SQL
- Application UseCase
- 重复词条业务决策
- Godot 组合根接线
- Godot UI
- Migration003
- FTS5
- M1-T10 或任何后续任务

---

# 1. 已确认的前置基线

用户已确认最新提交：

```text
11dc170281cd3c2c4961d164bb76a20c4a3d9564
```

当前已知状态：

- 当前分支：`main`
- Git 工作区干净
- M1-T08 提交内容完整
- M1-T08 = `Done`
- M1-T09 = `Not Started`
- 当前无 Godot 编辑器或残留进程
- 根解决方案仍包含 8 个项目
- 目标框架：
  - Godot 桌面：`net8.0`
  - Godot Android 条件目标：`net9.0`
  - Domain、Application、Infrastructure：`net8.0`
  - 三个测试项目、CaptureBridge：`net10.0`
- 构建成功，0 警告、0 错误
- 测试 266/266 通过：
  - Domain：111
  - Application：61
  - Infrastructure：94
- Migration001 哈希：
  `1fd5546081fe87c479ebd21d52e26f7d1dfaa636`
- Migration002 哈希：
  `d8ce250e24442ece38c231e3ae8286a4d0def4c5`
- `SqliteVocabularyRepository` 当前为：
  `public sealed partial class`
- 当前只包含写侧 `SaveAsync`
- 尚未声明实现 `IVocabularyRepository`
- 没有查询方法占位
- 没有 `NotImplementedException`
- 没有 `NotSupportedException`
- `SqliteSentenceExampleRepository`、`SqliteTagRepository` 已提交且无修改
- Application 公共 API 未泄漏 SQLite、Godot、Infrastructure 或 `IQueryable`
- 数据库、WAL/SHM、日志、备份和构建产物未进入 Git

Codex 开始时仍须重新核验，不得只依赖本文件。

---

# 2. 当前固定接口

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

本任务完成后：

```text
SqliteVocabularyRepository
→ 正式实现 IVocabularyRepository
→ 四个方法全部可用
→ 不存在占位实现
```

不得新增：

```text
DeletePermanentlyAsync
ArchiveAsync
RestoreAsync
UpdateAsync
InsertAsync
```

归档和恢复已经通过：

```text
VocabularyEntry.SetArchived(...)
→ SaveAsync(...)
```

完成持久化。

本任务标题中的“生命周期”仅包括：

- 精确查重只查活动词条。
- 详情可读取活动或归档词条。
- SearchAsync 支持 ActiveOnly、ArchivedOnly、All。
- 已有 SaveAsync 负责创建、编辑、归档和恢复。

永久删除不在当前四方法契约内，留给后续任务显式处理，不得偷偷加入。

---

# 3. 当前查询契约

## 3.1 `VocabularySearchQuery`

```text
SearchText   string?                    默认 null
GameTitle    string?                    默认 null
TagIds       IReadOnlyList<Guid>        默认空集合
EntryType    EntryType?                 默认 null
ArchiveFilter VocabularyArchiveFilter   默认 ActiveOnly
SortOrder    VocabularySortOrder        默认 UpdatedAtDescending
PageNumber   int                        默认 1
PageSize     int                        默认 50
```

现有 Query 已验证：

- `PageNumber >= 1`
- `PageSize` 为 `1–200`
- Tag ID 不得为 `Guid.Empty`
- Tag ID 不得重复
- 枚举必须有效
- 字符串不能是空或纯空白
- 字符串原值保留
- 不 Trim
- 不规范化
- TagIds 防御性复制

Repository 不得重复改写这些输入。

## 3.2 `VocabularyArchiveFilter`

```csharp
ActiveOnly = 0
ArchivedOnly = 1
All = 2
```

## 3.3 `VocabularySortOrder`

```csharp
UpdatedAtDescending = 0
HeadwordAscending = 1
CreatedAtDescending = 2
```

所有排序必须附加稳定次级键：

```text
Id ASC
```

## 3.4 `PagedResult<T>`

必须正确提供：

```text
Items
PageNumber
PageSize
TotalCount
TotalPages
HasPreviousPage
HasNextPage
```

Repository 传入：

- 当前页 Items。
- 全部匹配词条的 TotalCount。
- 原 Query 的 PageNumber、PageSize。

超出最后一页时：

```text
Items = empty
TotalCount = 实际匹配总数
```

---

# 4. 当前读模型

必须使用现有类型，不得创建重复 DTO：

```text
VocabularyEntrySummary
VocabularyEntryDetails
SentenceExampleDetails
TagSummary
```

## 4.1 `VocabularyEntrySummary`

包含：

```text
Id
Headword
EntryType
TranslationChinese
PrimaryExampleText
PrimaryGameTitle
Tags
IsArchived
CreatedAt
UpdatedAt
```

要求：

- Tags 完整、稳定排序。
- 0 个 Primary 时两个 Primary 字段为 null。
- 恰好 1 个 Primary 时复制该例句正文和 GameTitle。
- 多个 Primary 视为数据库损坏，不能任意选一个。

## 4.2 `VocabularyEntryDetails`

包含完整词条字段，以及：

```text
Examples : IReadOnlyList<SentenceExampleDetails>
Tags     : IReadOnlyList<TagSummary>
```

构造规则：

- Examples 按 `SortOrder ASC`、`Id ASC`。
- Tags 按 `NormalizedName ASC`、`Id ASC`。
- 多个 Primary 被拒绝。
- 0 个 Primary 允许。
- 重复 ExampleId 被拒绝。
- 重复 TagId 被拒绝。
- 所有例句必须属于当前词条。

## 4.3 `SentenceExampleDetails`

通过：

```csharp
SentenceExample
EntryExampleLink
```

构造。

不得手写另一套不一致 DTO。

## 4.4 `TagSummary`

直接复制：

```text
Id
Name
NormalizedName
```

不得重新规范化。

---

# 5. 当前数据库与索引

必须使用现有表：

```text
vocabulary_entries
sentence_examples
entry_examples
tags
entry_tags
```

必须保留并利用现有索引：

```text
ux_vocabulary_entries_normalized_active
ix_vocabulary_entries_archive_updated
ix_vocabulary_entries_archive_type_updated
ix_entry_examples_entry_sort
ix_entry_examples_example_entry
ix_entry_tags_tag_entry
ix_sentence_examples_game_created
```

不得：

- 修改 Migration001。
- 修改 Migration002。
- 新增 Migration003。
- 新增索引。
- 新增 FTS。
- 修改表结构。

---

# 6. 必须阅读

开始前完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/AGENT_HANDOFF.md
docs/MT_INSTRUCTION/M1-T09_CODEX_INSTRUCTION.md
```

重点阅读：

```text
PRODUCT_SPEC.md
- F07：词条编辑
- F08：重复词条处理
- F09：词条库
- 第 7 节分层职责
- 第 10 节领域模型
- 第 11 节规范化
- 第 12 节 SQLite 数据结构
- 第 13 节 Repository
- 第 18 节词条和例句策略
```

完整阅读：

```text
src/GameLexicon.Domain/Entries/**
src/GameLexicon.Application/Abstractions/Persistence/IVocabularyRepository.cs
src/GameLexicon.Application/Entries/Queries/**

src/GameLexicon.Infrastructure/Persistence/SqliteConnectionFactory.cs
src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteVocabularyRepository.cs
SqliteSentenceExampleRepository.cs
SqliteTagRepository.cs

src/GameLexicon.Infrastructure/Persistence/Migrations/
Migration001_Initial.cs
Migration002_ManualExamplesAndSearchSupport.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/**
tests/GameLexicon.Application.Tests/Entries/Queries/**
```

必须以实际构造函数、命名空间和属性为准，不得凭本文件猜测。

阅读已有 Repository 的目的：

- 复用 GUID、UTC、布尔、异常和资源管理风格。
- 不重构已完成的 Repository。
- 不创建新的数据库基础层。

如存在以下 Skills，也必须阅读：

```text
.agents/skills/project-routing/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Infrastructure / Persistence / Vocabulary Queries

Primary writer:
primary coordinator

Supporting agents:
- milestone architect：只读审查搜索语义、聚合一致性和任务边界
- skill curator：仅在 Skill Impact Review 需要时调用
```

本任务通常不需要 Godot specialist。

---

# 7. 阶段 0：重新核验基线

## 7.1 Git

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline 11dc170281cd3c2c4961d164bb76a20c4a3d9564
git diff --check
```

必须确认：

- 当前分支 `main`。
- 工作区干净。
- 提交存在。
- M1-T08 提交内容完整。
- 无未确认用户修改。

工作区不干净时立即停止：

- 不恢复。
- 不覆盖。
- 不暂存。
- 不提交。
- 不执行 `git reset --hard`。
- 不执行 `git clean -fd`。

## 7.2 状态

确认：

```text
M1-T08 = Done
M1-T09 = Not Started
```

状态不一致时停止。

## 7.3 解决方案和框架

```powershell
dotnet sln GameLexicon.sln list
```

必须仍为 8 个项目。

不得修改：

- 目标框架。
- 项目引用。
- 解决方案结构。
- NuGet 包。

## 7.4 迁移哈希

```powershell
git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration002_ManualExamplesAndSearchSupport.cs"
```

必须分别为：

```text
1fd5546081fe87c479ebd21d52e26f7d1dfaa636
d8ce250e24442ece38c231e3ae8286a4d0def4c5
```

## 7.5 Repository 状态

确认：

```text
SqliteVocabularyRepository
- public sealed partial
- 只有 SaveAsync
- 未声明 IVocabularyRepository
- 无查询占位
```

确认例句和标签 Repository 无修改。

## 7.6 基线构建和测试

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

预期：

```text
Build: 0 warnings, 0 errors
Tests: 266/266 passed
```

本任务不新增 NuGet 包。

---

# 8. 建议文件

创建：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteVocabularyRepository.Queries.cs
```

测试：

```text
tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/
SqliteVocabularyRepositoryQueryTests.cs
```

允许创建最小内部 helper：

```text
VocabularyQuerySqlBuilder.cs
SqliteReadValue.cs
```

条件：

- `internal`。
- 只服务当前查询实现。
- 不形成通用 ORM 或 Specification 框架。
- 不修改 Application。
- 不引入第三方包。
- 不重构 M1-T06、M1-T07、M1-T08。

正常情况下不要修改 M1-T08 的写侧文件。

---

# 9. 完成 partial 类型

在查询 partial 中声明：

```csharp
public sealed partial class SqliteVocabularyRepository
    : IVocabularyRepository
```

C# partial 类型合并后必须：

- 使用 M1-T08 已有构造函数。
- 使用 M1-T08 已有 `_connectionFactory`。
- 保留已有 `SaveAsync`。
- 新增三个真实查询方法。
- 正式满足完整接口。
- 不重复定义构造函数。
- 不重复定义 SaveAsync。
- 不出现接口方法占位。

反射测试必须确认：

```text
typeof(IVocabularyRepository)
    .IsAssignableFrom(typeof(SqliteVocabularyRepository))
```

---

# 10. 通用读取映射

## 10.1 GUID

读取数据库 GUID：

- 使用 `Guid.TryParse`。
- 无效值视为数据库损坏。
- 不生成新 Guid。
- 不将坏值原文写入异常。

## 10.2 UTC

读取时间：

- 使用项目既有严格 UTC 解析。
- Offset 必须为 0。
- `UpdatedAt >= CreatedAt`。
- 非法数据视为数据库损坏。
- 不自动使用 `UtcNow`。
- 不自动修复。

## 10.3 EntryType

只接受：

```text
0, 1, 2, 3
```

未知值失败。

## 10.4 布尔

只接受：

```text
0, 1
```

其他值失败。

## 10.5 文本

必填列：

```text
headword
normalized_headword
sentence_text
normalized_sentence
tag name
tag normalized_name
```

不得为 NULL 或纯空白。

可空列保持 null。

Repository 不得：

- Trim。
- ToLower。
- Form KC。
- 重新规范化。
- 自动修复。

---

# 11. `FindByNormalizedHeadwordAsync`

精确实现当前接口签名。

## 11.1 参数

```text
null
→ ArgumentNullException

空字符串或纯空白
→ ArgumentException
```

不得 Trim。

## 11.2 查询语义

只查活动词条：

```sql
WHERE
    normalized_headword = @normalized_headword
    AND is_archived = 0
```

要求：

- 精确相等。
- 不使用 `LOWER`。
- 不使用 `TRIM`。
- 不使用 `LIKE`。
- 不使用 `COLLATE NOCASE`。
- 不重新调用 `ITextNormalizer`。
- 找不到返回 null。
- 归档词条不返回。

示例：

```text
数据库 normalized_headword = "get out"

输入 "get out"
→ 找到活动词条

输入 "Get Out"
→ Repository 不自动改写，找不到

输入 " get out "
→ Repository 不 Trim，找不到
```

## 11.3 返回

返回完整 Domain `VocabularyEntry`。

不得返回 Summary 或数据库 DTO。

若数据库损坏导致多个活动匹配：

- 不任意取第一条。
- 抛 `InvalidDataException` 或当前稳定等价异常。

可读取最多两行检测异常。

## 11.4 取消和资源

- 单查询不需要写事务。
- 传播 CancellationToken。
- 正确释放 command 和 reader。

---

# 12. `GetDetailsAsync`

精确实现当前接口签名。

## 12.1 参数和返回

```text
entryId == Guid.Empty
→ ArgumentException

词条不存在
→ null

活动或归档词条存在
→ 返回完整 VocabularyEntryDetails
```

不得因为词条归档而返回 null。

## 12.2 一致快照

详情由多个表聚合。

必须在同一连接和同一只读事务中读取：

```text
1. vocabulary_entries
2. entry_examples + sentence_examples
3. entry_tags + tags
```

目的：

- 避免读到不同时间点的半套聚合。
- 不调用两个 Repository 打开独立连接拼接。
- 不缓存结果。

## 12.3 词条读取

显式读取全部词条列并构造 Domain `VocabularyEntry`。

禁止 `SELECT *`。

## 12.4 例句读取

查询：

```text
entry_examples
INNER JOIN sentence_examples
```

排序：

```text
entry_examples.sort_order ASC
entry_examples.example_id ASC
```

每行构造：

```text
SentenceExample
EntryExampleLink
SentenceExampleDetails
```

要求：

- 读取全部例句字段。
- 读取 IsPrimary 和 SortOrder。
- 支持无 Capture 的手工例句。
- 不伪造 Primary。
- 不自动修复多个 Primary。
- 不漏掉非 Primary 例句。

## 12.5 标签读取

查询：

```text
entry_tags
INNER JOIN tags
```

排序：

```text
tags.normalized_name ASC
tags.id ASC
```

每行构造 `TagSummary`。

## 12.6 构造 Details

使用现有 `VocabularyEntryDetails` 构造规则。

允许：

```text
Examples empty
Tags empty
0 Primary
```

必须拒绝或传播读模型拒绝：

```text
多个 Primary
重复 ExampleId
重复 TagId
例句链接属于其他词条
损坏数据
```

事务完成后返回不可变详情对象。

---

# 13. `SearchAsync` 固定 MVP 语义

M1-T04 将一般搜索的具体语义留给本任务。

本任务统一采用以下可测试语义。

## 13.1 筛选组合

所有非 null 筛选使用：

```text
AND
```

即同时满足所有条件。

## 13.2 SearchText

`SearchText` 是：

```text
ASCII 大小写不敏感的字面子串搜索
```

搜索以下词条自身字段：

```text
headword
normalized_headword
part_of_speech
phonetic
definition_english
translation_chinese
notes
```

不搜索：

```text
sentence_examples.sentence_text
game_title
tag name
```

理由：

- GameTitle 和 TagIds 已有独立筛选。
- 当前没有 FTS。
- 避免 SearchText 引入多表重复和不明确语义。
- 原句全文搜索可在后续明确扩展。

实现使用参数化 `LIKE`，并把输入中的：

```text
\
%
_
```

转义为字面字符。

必须使用：

```sql
ESCAPE '\'
```

要求：

- Repository 不 Trim。
- Repository不执行 Form KC。
- Repository 不使用 `ITextNormalizer`。
- 输入大小写原样保留，但比较采用 SQLite MVP 的 ASCII case-insensitive LIKE 语义。
- 中文等非 ASCII 文本按 SQLite 当前字面比较能力工作。
- `%` 和 `_` 不能成为用户可注入的通配符。
- `\` 必须正确转义。

测试必须明确覆盖：

```text
SearchText = "%"
→ 只匹配实际包含 % 的字段

SearchText = "_"
→ 只匹配实际包含 _ 的字段
```

不得把 SearchText 拼接进 SQL。

## 13.3 GameTitle

GameTitle 表示按某个来源游戏精确筛选。

词条匹配条件：

```text
存在至少一个已链接例句
其 game_title 与输入精确相等
使用 SQLite COLLATE NOCASE
```

SQL 采用 `EXISTS`，不得通过主查询 JOIN 造成重复词条。

语义：

- ASCII 大小写不敏感。
- 不 Trim。
- 不是子串。
- 不匹配 NULL。
- 手工例句无 GameTitle 不匹配。
- 同一词条有多个匹配例句仍只出现一次。

示例：

```text
输入 "Halo"
→ 匹配 "halo"

输入 "Halo"
→ 不匹配 "Halo Infinite"
```

现有索引：

```text
ix_sentence_examples_game_created
```

不得修改。

## 13.4 TagIds

多个 TagIds 采用：

```text
ALL / AND 语义
```

即词条必须拥有传入的每一个 Tag。

示例：

```text
TagIds = [A, B]
词条只有 A
→ 不匹配

词条拥有 A、B、C
→ 匹配
```

可使用：

```text
IN + GROUP BY + HAVING COUNT(DISTINCT tag_id) = @tag_count
```

或多个参数化 `EXISTS`。

要求：

- 不拼接 GUID 值。
- 不静默忽略不存在 TagId。
- 不存在的 TagId 使结果为空，不抛 KeyNotFoundException。
- 不创建 Tag。
- 不使用 OR / ANY 语义。
- 不产生重复词条。

## 13.5 EntryType

非 null 时精确匹配数据库整数。

## 13.6 ArchiveFilter

```text
ActiveOnly
→ is_archived = 0

ArchivedOnly
→ is_archived = 1

All
→ 不添加归档条件
```

## 13.7 筛选组合示例

```text
SearchText + GameTitle + TagIds + EntryType + ArchivedOnly
```

必须同时满足全部条件。

---

# 14. `SearchAsync` 排序

## 14.1 UpdatedAtDescending

```sql
ORDER BY
    updated_at_utc DESC,
    id ASC
```

## 14.2 HeadwordAscending

```sql
ORDER BY
    headword COLLATE NOCASE ASC,
    id ASC
```

要求：

- ASCII 大小写不敏感排序。
- 相同 Headword 通过 Id 稳定排序。

## 14.3 CreatedAtDescending

```sql
ORDER BY
    created_at_utc DESC,
    id ASC
```

不得：

- 使用不稳定默认顺序。
- 在内存中对完整结果排序后分页。
- 忽略 Id 次级键。

---

# 15. `SearchAsync` 分页

## 15.1 TotalCount

必须统计：

```text
应用全部筛选后
分页前
去重后的词条数量
```

不得统计 JOIN 行数。

优先使用：

```text
以 vocabulary_entries 为主表
EXISTS 子查询筛选
```

这样无需 `COUNT(DISTINCT ...)`。

## 15.2 Offset

使用 64 位安全计算：

```csharp
long offset =
    checked(((long)query.PageNumber - 1L) * query.PageSize);
```

不得使用 int 乘法导致溢出。

参数化传入：

```text
LIMIT @page_size
OFFSET @offset
```

## 15.3 越界页

页码超过最后一页：

- Items 为空。
- TotalCount 保持实际值。
- 不抛异常。

## 15.4 一致快照

`SearchAsync` 包含：

1. Count。
2. 当前页词条。
3. 当前页 Primary 例句。
4. 当前页 Tags。

全部必须在同一连接和同一只读事务中完成。

避免：

- Count 与 Items 来自不同快照。
- 页面读到一半发生修改导致关联不一致。

---

# 16. `SearchAsync` 聚合策略

不得使用一个巨型多表 JOIN 直接分页，因为会：

- 把一个词条乘成多行。
- 破坏 LIMIT/OFFSET。
- 错误计算 TotalCount。
- 重复 Tag。
- 随机选择 Primary。

推荐固定数量查询：

```text
1. COUNT 匹配词条
2. SELECT 当前页词条基础字段
3. SELECT 当前页所有 Primary 例句
4. SELECT 当前页所有 Tags
```

PageSize 最大 200，可为当前页 ID 创建参数化 IN 列表：

```text
@entry_id_0
@entry_id_1
...
```

参数名只能由整数索引生成。

GUID 值必须仍通过参数传入。

## 16.1 当前页为空

如果当前页无词条：

- 不执行无意义的 `IN ()`。
- 直接构造空 PagedResult。
- 仍返回正确 TotalCount。

## 16.2 Primary 例句

查询当前页：

```text
entry_examples.is_primary = 1
```

读取：

```text
entry_id
example_id
sentence_text
game_title
```

每个词条：

```text
0 行
→ PrimaryExampleText = null
→ PrimaryGameTitle = null

1 行
→ 使用该行

>1 行
→ InvalidDataException
```

不得使用：

```text
MIN
MAX
LIMIT 1
任意第一行
```

掩盖多个 Primary。

## 16.3 Tags

查询当前页全部 Tags，排序：

```text
entry_id
tags.normalized_name
tags.id
```

构造每个词条的 `TagSummary` 列表。

重复 TagId 视为数据损坏，不静默去重。

## 16.4 Summary

对每个基础词条构造：

```text
VocabularyEntrySummary
```

Items 顺序必须保持第 2 步页面查询的排序，不能因 Dictionary 枚举而改变。

---

# 17. SQL 构建安全

允许根据 Query 动态添加固定 SQL 片段。

禁止：

- 拼接 SearchText。
- 拼接 GameTitle。
- 拼接 GUID 值。
- 拼接 enum 数值。
- 拼接 PageNumber 或 PageSize。
- 接受任意 ORDER BY 字符串。
- 使用用户输入作为列名或排序表达式。

SortOrder 必须通过封闭 switch 选择固定 SQL。

Tag 参数名和页面 ID 参数名可以由安全整数索引生成。

所有数据值使用参数。

禁止：

```sql
SELECT *
```

---

# 18. 数据损坏策略

查询遇到以下情况必须失败：

- 无效 GUID。
- 未定义 EntryType。
- is_archived 非 0/1。
- is_primary 非 0/1。
- 非法或非 UTC 时间。
- UpdatedAt 早于 CreatedAt。
- 例句目标范围非法。
- OCR/Capture 组合非法。
- 负 SortOrder。
- 必填列 NULL。
- 必填文本空白。
- 多个 Primary。
- 重复 ExampleId。
- 重复 TagId。

不得：

- 跳过损坏行。
- 返回部分详情。
- 自动修复。
- 使用默认值。
- 自动归档。
- 任意选择一个 Primary。

建议抛：

```text
InvalidDataException
```

或项目当前已统一的等价异常。

异常不得回显学习文本。

---

# 19. CancellationToken

三个查询方法必须：

- 打开连接时传递 token。
- BeginTransactionAsync 时传递 token（API 支持时）。
- ExecuteReaderAsync / ExecuteScalarAsync 传递 token。
- Reader 循环适当检查 token。
- CommitAsync 传递 token。

预取消：

```text
OperationCanceledException
```

不得：

- 返回 null 伪装取消。
- 返回空页伪装取消。
- 返回半套 Details。
- 捕获取消后继续下一条查询。

---

# 20. 事务和资源释放

读取事务：

- 不修改业务表。
- 不创建临时持久对象。
- 正常完成后提交或按当前项目只读事务安全方式结束。
- 失败时安全回滚。
- 回滚异常不能覆盖原始异常。

正确释放：

```text
connection
transaction
command
reader
```

不得：

- 长期持有连接。
- 缓存 Reader。
- 在返回对象中保存数据库资源。
- 留下 WAL/SHM 锁。

---

# 21. 日志和隐私

Repository 默认不注入 Logger。

不得记录：

- SearchText。
- GameTitle。
- Headword。
- NormalizedHeadword。
- Definition。
- Translation。
- Notes。
- SentenceText。
- Tag Name。
- SQL 参数。
- 数据库行内容。
- 完整连接字符串。

测试报告只记录案例名称、数量和通过状态。

---

# 22. 自动化测试数据库

测试必须使用真实临时 SQLite 文件：

```text
Migration001
→ Migration002
→ 已有三个 Repository / 参数化测试种子
```

不得：

- 手写简化表替代迁移。
- 使用真实 user:// 数据库。
- 写入仓库目录。
- 依赖测试顺序。
- 留下 `.db`、`-wal`、`-shm`。

测试数据可通过：

- 已有 SaveAsync。
- 已有例句 Repository。
- 已有标签 Repository。
- 测试项目内参数化 SQL helper。

不得为测试修改生产 API。

---

# 23. `FindByNormalizedHeadwordAsync` 测试

至少覆盖：

1. 活动词条精确找到。
2. 找不到返回 null。
3. 只有归档同名时返回 null。
4. 活动与归档同名时返回活动词条。
5. 不改变大小写。
6. 不 Trim。
7. null 被拒绝。
8. 空字符串被拒绝。
9. 纯空白被拒绝。
10. 返回完整 Domain 字段。
11. 四种 EntryType 正确映射。
12. nullable 字段正确映射。
13. 无效 GUID 被拒绝。
14. 无效 EntryType 被拒绝。
15. 无效布尔被拒绝。
16. 非 UTC 或损坏时间被拒绝。
17. 预取消 token。
18. 查询后数据库文件可释放。

---

# 24. `GetDetailsAsync` 测试

至少覆盖：

## 24.1 基础

- Guid.Empty 被拒绝。
- 不存在返回 null。
- 活动词条返回。
- 归档词条也返回。
- 无例句、无标签时返回空集合。

## 24.2 全字段

验证完整词条字段逐项一致。

## 24.3 例句

建立：

- 手工例句。
- Capture 例句。
- OCR 例句。
- 不同 SortOrder。
- 相同 SortOrder 不同 ExampleId。
- 一个 Primary。
- 多个非 Primary。

验证：

- 全部返回。
- 排序正确。
- Primary 正确。
- TargetText 正确。
- Capture/OCR nullable 映射正确。

## 24.4 标签

验证：

- 全部返回。
- 按 NormalizedName、Id 排序。
- Name 原样保留。
- 不重新规范化。

## 24.5 0 Primary

有例句但无 Primary：

- Details 成功。
- 不伪造 Primary。

## 24.6 多 Primary

通过测试 SQL 构造两个 Primary：

- GetDetailsAsync 失败。
- 不任意选一个。
- 不修改数据库。

## 24.7 损坏数据

至少覆盖：

- is_primary 非 0/1。
- 负 SortOrder。
- 非法例句 GUID 或时间。
- 非法 Tag 数据。
- 非法词条 EntryType 或时间。

## 24.8 取消和资源

- 预取消无结果。
- DB/WAL/SHM 可删除。

---

# 25. `SearchAsync` 基础和分页测试

至少覆盖：

1. null query 被拒绝。
2. 默认 Query 只返回 Active。
3. 默认按 UpdatedAt DESC、Id ASC。
4. TotalCount 为全部匹配数。
5. PageSize 生效。
6. PageNumber 生效。
7. 多页无重复、无遗漏。
8. 超出末页返回空 Items。
9. TotalPages 正确。
10. HasPreviousPage、HasNextPage 正确。
11. PageSize 1。
12. PageSize 200。
13. Offset 使用 long，不因极大 PageNumber 溢出。
14. 空数据库返回 TotalCount 0、TotalPages 0。
15. 返回集合顺序稳定。

极大 PageNumber 测试可以验证：

- 不发生 int overflow。
- 返回空页。
- 不要求创建海量数据。

---

# 26. SearchText 测试

至少覆盖各字段：

```text
headword
normalized_headword
part_of_speech
phonetic
definition_english
translation_chinese
notes
```

验证：

- 字面子串。
- ASCII 大小写不敏感。
- 不 Trim。
- 不执行 Form KC。
- 不搜索 GameTitle。
- 不搜索 Tag Name。
- 不搜索 SentenceText。
- `%` 按字面匹配。
- `_` 按字面匹配。
- `\` 正确转义。
- SQL 注入样式文本只作为值处理。
- 多个字段 OR 匹配。
- 与其他筛选组合时使用 AND。

示例：

```text
SearchText = "QUEST"
Headword = "Quest Marker"
→ 匹配

SearchText = " quest "
Headword = "Quest"
→ 不匹配，因未 Trim

SearchText = "%"
只有字段真实包含 %
→ 才匹配
```

---

# 27. GameTitle 测试

至少覆盖：

1. 任一链接例句精确匹配时返回词条。
2. ASCII 大小写不敏感。
3. 不执行 Trim。
4. 不做子串。
5. GameTitle 为 null 的例句不匹配。
6. 一个词条多个匹配例句只返回一次。
7. 同一游戏多个词条均返回。
8. 无链接例句不匹配。
9. 与 Archive、Tag、Type、SearchText 组合使用 AND。
10. GameTitle 中 `%`、`_` 不作为通配符，因为使用等号。

---

# 28. TagIds 测试

至少覆盖：

1. 单个 Tag。
2. 多个 Tag 使用 ALL 语义。
3. 拥有额外 Tag 仍匹配。
4. 只拥有部分 Tag 不匹配。
5. 不存在 TagId 返回空结果。
6. TagIds 不造成重复词条。
7. 与 GameTitle、Type、Archive、SearchText 组合使用 AND。
8. 不修改 Tag 或关联。
9. 合理数量的多个 Tag 参数化执行。

Query 已拒绝重复和 Guid.Empty，不修改 Query 契约。

---

# 29. Archive 和 EntryType 测试

## Archive

验证：

```text
ActiveOnly
ArchivedOnly
All
```

并确认 Summary 的 `IsArchived` 正确。

## EntryType

分别筛选：

```text
Word
Phrase
Expression
SentencePattern
```

与 Archive 组合正确。

---

# 30. 排序测试

## UpdatedAtDescending

- 时间降序。
- 相同时间按 Id 升序。

## HeadwordAscending

- `COLLATE NOCASE` 排序。
- 相同排序键按 Id 升序。
- 稳定跨页。

## CreatedAtDescending

- 时间降序。
- 相同时间按 Id 升序。

不得只测试第一项排序键。

---

# 31. Summary 聚合测试

至少覆盖：

1. 无 Primary：
   - `PrimaryExampleText = null`
   - `PrimaryGameTitle = null`

2. 一个 Primary：
   - 正确正文。
   - 正确 GameTitle。

3. Primary 的 GameTitle 为 null：
   - Text 有值。
   - GameTitle null。

4. 多个 Primary：
   - SearchAsync 失败。
   - 不任意选一个。

5. Tags：
   - 全部返回。
   - 稳定排序。
   - 无标签为空列表。
   - 不包含其他词条标签。

6. Page 聚合：
   - Items 顺序与页面基础查询完全一致。
   - 关联查询不改变排序。
   - 一个词条多 Tags/Examples 不重复 Summary。

---

# 32. 一致快照和查询数量

代码审查必须确认：

```text
GetDetailsAsync
→ 单连接 + 单只读事务

SearchAsync
→ 单连接 + 单只读事务
→ Count、Page、Primary、Tags 同一快照
```

不允许：

- Search 每个词条分别打开新连接。
- 调用 `SqliteTagRepository.GetForEntryAsync` 形成 N+1。
- 调用 `SqliteSentenceExampleRepository.GetForEntryAsync` 形成 N+1。
- Count 和 Items 使用独立连接。

自动测试不要求依赖脆弱的精确 SQL 命令计数，但最终报告必须说明实际查询结构。

---

# 33. 数据库索引检查

测试再次确认现有索引存在：

```text
ux_vocabulary_entries_normalized_active
ix_vocabulary_entries_archive_updated
ix_vocabulary_entries_archive_type_updated
ix_entry_examples_entry_sort
ix_entry_examples_example_entry
ix_entry_tags_tag_entry
ix_sentence_examples_game_created
```

不得新增索引。

不要求对 SQLite 查询计划做脆弱的完全字符串断言。

如使用 `EXPLAIN QUERY PLAN`：

- 只做辅助报告。
- 不把特定 SQLite 版本的完整文本作为硬性测试。

---

# 34. 接口和边界测试

确认：

- `SqliteVocabularyRepository` 正式实现 `IVocabularyRepository`。
- 四个方法均真实可调用。
- 无查询占位。
- 无 `NotImplementedException`。
- 无 `NotSupportedException`。
- Application 接口未修改。
- 查询模型未修改。
- Domain 未修改。
- 迁移未修改。
- M1-T06、M1-T07、M1-T08 写侧逻辑未重构。
- 公共方法不暴露 `SqliteConnection`。
- 无 Godot 类型。
- 无 `IQueryable`。
- 无通用 Specification 或 UnitOfWork。

---

# 35. 文件锁和清理

所有测试完成后：

1. Dispose Reader。
2. Dispose Command。
3. Dispose Transaction。
4. Dispose Connection。
5. 删除 `.db`。
6. 删除 `-wal`。
7. 删除 `-shm`。
8. 删除临时目录。

必须验证：

```text
DB/WAL/SHM 可删除
```

取消、损坏数据和异常测试后也必须释放连接。

---

# 36. 允许创建和修改的文件

建议创建：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteVocabularyRepository.Queries.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/
SqliteVocabularyRepositoryQueryTests.cs
```

可选内部 helper：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
VocabularyQuerySqlBuilder.cs
SqliteReadValue.cs
```

允许修改：

```text
tests/GameLexicon.Infrastructure.Tests/Persistence/**
（仅最小测试 helper）

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
SqliteVocabularyRepository.cs
SqliteSentenceExampleRepository.cs
SqliteTagRepository.cs
Migration001_Initial.cs
Migration002_ManualExamplesAndSearchSupport.cs
MigrationRunner.cs
SqliteConnectionFactory.cs
english-learning-project/**
tools/GameLexicon.CaptureBridge/**
```

只有在 C# partial 接口声明无法在新文件完成且有编译证据时，才允许对现有 `SqliteVocabularyRepository.cs` 做最小声明调整；不得改变 M1-T08 的 SaveAsync 逻辑。

本任务不注册到 `AppServices`。

---

# 37. 本任务明确不做

不得实现：

- 永久删除。
- `DeletePermanentlyAsync`。
- 新生命周期接口。
- Application UseCase。
- 创建词条工作流。
- 重复词条合并工作流。
- 编辑/归档/删除 UseCase。
- Godot ViewModel。
- Godot Scene。
- AppServices 接线。
- 复习状态筛选。
- ReviewCard / ReviewLog。
- FTS5。
- 原句全文搜索。
- 标签名称搜索。
- 游戏名称模糊搜索。
- Migration003。
- 新索引。
- M1-T10。

---

# 38. 自动验证命令

## Infrastructure

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-restore

dotnet test `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-build `
  --no-restore
```

## 根解决方案

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功。
- 所有测试通过。
- 0 错误。
- 0 新增警告。

## Godot

本任务不修改 Godot：

```text
GUI verification required: No
Godot headless required: No
```

不启动 Godot Editor。

---

# 39. 代表性自动验收

最终报告必须逐项报告：

```text
Find active exact normalized headword → Pass
Find archived-only duplicate → null
Find does not trim or normalize → Confirmed

GetDetails active → Pass
GetDetails archived → Pass
GetDetails all examples and tags → Pass
GetDetails zero primary → Pass
GetDetails multiple primary → Rejected

Default search active only → Pass
SearchText literal contains → Pass
SearchText wildcard escaping → Pass
GameTitle exact NOCASE filter → Pass
TagIds ALL semantics → Pass
EntryType filter → Pass
Archive filters → Pass
Combined filters use AND → Pass

UpdatedAt sort + Id tie-break → Pass
Headword sort + Id tie-break → Pass
CreatedAt sort + Id tie-break → Pass
Pagination no duplicates or omissions → Pass
Page beyond end → Empty with correct count

Summary primary fields → Pass
Summary tags → Pass
Multiple primary in summary → Rejected

SqliteVocabularyRepository implements full interface → Pass
No query stubs → Pass
DB/WAL/SHM deletable → Pass
```

不得只报告“测试全部通过”。

---

# 40. 非 GUI 人工审查

自动验收后：

```text
M1-T09 = Awaiting Manual Verification
M1-T10 = Not Started
```

本任务不需要 GUI。

人工审查重点：

1. Partial 类型正式实现完整接口。
2. 三个查询方法均为真实实现。
3. 没有修改 Application 契约。
4. Find 只查活动词条并精确匹配。
5. Details 使用一致只读快照。
6. Search 的具体 MVP 语义与本指令一致。
7. TagIds 为 ALL 语义。
8. GameTitle 为精确 NOCASE。
9. SearchText 转义 `%`、`_`、`\`。
10. Count 和分页不受多表重复影响。
11. 三种排序都有 Id 次级键。
12. Summary 不掩盖多个 Primary。
13. Details 返回全部例句和标签。
14. 数据损坏不会被静默忽略。
15. CancellationToken 完整传播。
16. 无连接或文件锁泄漏。
17. 未实现删除、UseCase、UI 或新迁移。
18. 所有测试通过。

用户确认前不得将 M1-T09 标记为 Done。

---

# 41. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且修改未确认。
- 找不到提交 `11dc1702...`。
- M1-T08 未标记 Done。
- M1-T09 不是 Not Started。
- 基线构建或测试失败。
- 解决方案不再是 8 个项目。
- 目标框架或项目引用变化。
- Migration001 或 Migration002 哈希变化。
- 必须修改 Application 查询契约。
- 必须修改 Domain。
- 必须新增 Migration 或索引。
- 必须重构 M1-T06、M1-T07 或 M1-T08。
- 必须新增 NuGet 包。
- 必须修改 Godot。
- 无法避免分页 JOIN 重复。
- 无法保证 Details/Search 一致快照。
- 无法检测多个 Primary。
- 测试数据库无法删除。
- 用户文件可能被覆盖。

停止后不得：

- 删除用户数据库。
- 修改迁移历史。
- `git reset --hard`。
- `git clean -fd`。
- 禁用 NuGet Audit。
- 自动提交。
- 自动执行 M1-T10。

---

# 42. Git 检查

完成自动验证后：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
git diff --name-only
```

再次确认迁移哈希：

```powershell
git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration002_ManualExamplesAndSearchSupport.cs"
```

必须仍为：

```text
1fd5546081fe87c479ebd21d52e26f7d1dfaa636
d8ce250e24442ece38c231e3ae8286a4d0def4c5
```

确认：

- 生产代码只新增查询侧 partial 和必要 internal helper。
- 测试只属于查询侧。
- 其余只允许状态文档。
- Domain 未修改。
- Application 未修改。
- Migration 未修改。
- M1-T06/M1-T07 Repository 未修改。
- M1-T08 SaveAsync 逻辑未修改。
- Godot 未修改。
- `.csproj` 未修改。
- 数据库、WAL、SHM、日志、备份未进入 Git。
- 暂存区为空。
- 未创建提交。

---

# 43. 状态与文档

自动验收通过后更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

状态：

```text
M1-T09 = Awaiting Manual Verification
M1-T10 = Not Started
```

记录：

- Task ID 和名称。
- `SqliteVocabularyRepository` 已正式实现完整接口。
- 三个查询方法。
- Find 活动精确匹配语义。
- Details 一致快照和聚合规则。
- SearchText 字段范围、字面 contains 和转义规则。
- GameTitle 精确 NOCASE。
- TagIds ALL 语义。
- Archive 和 EntryType 筛选。
- 三种排序和 Id 次级键。
- Count、分页和聚合查询结构。
- Summary Primary 处理。
- 多 Primary 损坏策略。
- CancellationToken 覆盖。
- 新增测试数量。
- Infrastructure 测试结果。
- 根解决方案测试结果。
- DB/WAL/SHM 删除结果。
- 未修改迁移、Domain、Application、已有 Repository 写逻辑和 Godot。
- 已知限制。

更新：

```text
docs/AGENT_HANDOFF.md
```

只有长期架构决策变化时才更新：

```text
docs/DECISIONS.md
```

本任务固定的一般搜索 MVP 语义会影响未来 UI 和 UseCase。

milestone architect 必须判断是否需要新增简短 ADR，至少记录：

```text
SearchText 字段范围
GameTitle 精确 NOCASE
TagIds ALL 语义
```

如果这些仅在状态文档中已经足够且现有决策文档规范不要求 ADR，可不修改；最终报告必须说明判断。

只有环境事实变化时修改：

```text
docs/ENVIRONMENT.md
```

人工审查通过后：

```text
M1-T09 = Done
M1-T10 = Not Started
```

不得执行 M1-T10。

---

# 44. Skill Impact Review

任务结束后报告：

- Primary domain。
- Primary agent。
- Supporting agents。
- Skills used。
- Skill update required。
- Skills updated。
- Documentation updated。
- Restart required。

正常预期：

```text
Skills used:
- project-routing
- milestone-workflow
- skill-maintenance

Skill update required:
No
```

只有以下可复用流程确实变化时更新 Skill：

- 多表聚合分页避免行倍增标准。
- 一致只读快照标准。
- LIKE 字面转义标准。
- 多值标签 ALL 筛选标准。
- Primary 数据损坏检测标准。

普通查询实现不自动构成 Skill 更新理由。

---

# 45. 自动验收清单

- [ ] 提交 `11dc1702...` 存在
- [ ] 当前分支 main
- [ ] 初始工作区干净
- [ ] M1-T08 Done
- [ ] M1-T09 Not Started
- [ ] 基线 Build 成功
- [ ] 基线 266/266 测试通过
- [ ] Migration001 哈希不变
- [ ] Migration002 哈希不变
- [ ] IVocabularyRepository 未修改
- [ ] Query/读模型未修改
- [ ] 查询 partial 创建
- [ ] 类型正式实现 IVocabularyRepository
- [ ] SaveAsync 未重复
- [ ] 无查询占位
- [ ] Find 精确活动匹配
- [ ] Find 不 Trim、不规范化
- [ ] Details 活动/归档都可读
- [ ] Details 使用一致只读事务
- [ ] Details 返回全部例句和标签
- [ ] 0 Primary 允许
- [ ] 多 Primary 拒绝
- [ ] Search null Query 拒绝
- [ ] 默认 ActiveOnly
- [ ] SearchText 字段范围正确
- [ ] SearchText LIKE 字面转义正确
- [ ] SearchText 不搜索 Game/Tag/例句
- [ ] GameTitle 精确 NOCASE
- [ ] TagIds ALL 语义
- [ ] EntryType 筛选正确
- [ ] 三种 ArchiveFilter 正确
- [ ] 筛选组合 AND
- [ ] UpdatedAt 排序稳定
- [ ] Headword 排序稳定
- [ ] CreatedAt 排序稳定
- [ ] TotalCount 去重且分页前计算
- [ ] long Offset
- [ ] 越界页返回空 Items
- [ ] Summary Primary 正确
- [ ] Summary Tags 正确
- [ ] Items 顺序未被关联聚合改变
- [ ] SQL 全参数化
- [ ] 未使用 SELECT *
- [ ] 未新增 Migration/索引/FTS
- [ ] 数据损坏明确失败
- [ ] CancellationToken 传播
- [ ] 连接和事务释放
- [ ] DB/WAL/SHM 可删除
- [ ] 未记录学习文本
- [ ] Domain 未修改
- [ ] Application 未修改
- [ ] 已有 Repository 未重构
- [ ] Godot 未修改
- [ ] 未实现删除
- [ ] 未实现 UseCase/UI
- [ ] Infrastructure 测试通过
- [ ] 根解决方案构建通过
- [ ] 全部测试通过
- [ ] git diff --check 通过
- [ ] 暂存区为空
- [ ] 未创建提交
- [ ] M1-T10 未执行
- [ ] Skill Impact Review 完成

---

# 46. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T09
- 名称: SQLite 查询与生命周期
- 状态:
- M1-T10 executed: No
- Git commit created: No
- GUI verification required: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M1-T08 commit:
- Branch:
- Initial Git status:
- Solution projects:
- Target frameworks:
- Baseline build:
- Baseline tests:
- Migration001 hash:
- Migration002 hash:

## Repository 完成状态

- Type:
- Partial files:
- Implements IVocabularyRepository:
- Four methods available:
- Query stubs:
- Existing SaveAsync modified:

## FindByNormalizedHeadwordAsync

- Input validation:
- Exact comparison:
- Active-only:
- Normalization in Repository:
- Archived behavior:
- Corrupt duplicate behavior:

## GetDetailsAsync

- Read transaction:
- Entry mapping:
- Examples:
- Tags:
- Zero Primary:
- Multiple Primary:
- Archived entry:
- Not found:

## SearchAsync 语义

- SearchText fields:
- SearchText comparison:
- Wildcard escaping:
- GameTitle:
- TagIds:
- EntryType:
- ArchiveFilter:
- Combined filter logic:

## 排序和分页

- UpdatedAtDescending:
- HeadwordAscending:
- CreatedAtDescending:
- Stable tie-break:
- Count strategy:
- Offset type:
- Page beyond end:

## 聚合策略

- Query count/shape:
- Primary examples:
- Tags:
- Duplicate prevention:
- Snapshot consistency:

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
- Infrastructure total:
- Root total:
- Passed:
- Failed:
- Skipped:
- DB/WAL/SHM deletion:

## 边界检查

- IVocabularyRepository modified:
- Query models modified:
- Domain modified:
- Migrations modified:
- Existing repositories modified:
- Godot modified:
- Permanent delete:
- UseCases/UI:

## Git diff

```text
...
```

## Decision review

- Search semantics ADR required:
- DECISIONS.md updated:
- Reason:

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

- No permanent delete.
- No review-state filtering.
- No FTS.
- SearchText does not search examples, game titles, or tag names.
- SQLite NOCASE/LIKE provides MVP ASCII-oriented case handling.
- No UseCase or UI.

## 下一任务

- M1-T10：手工创建词条 UseCase
- Status: Not Started
- Not automatically executed
```

---

# 47. 可直接执行的总指令

请执行：

```text
M1-T09：SQLite 查询与生命周期
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T09_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `11dc170281cd3c2c4961d164bb76a20c4a3d9564`。
2. 开始时 Git 工作区必须干净。
3. 新增查询侧 partial 文件。
4. 让 `SqliteVocabularyRepository` 正式实现完整 `IVocabularyRepository`。
5. 保留 M1-T08 的 SaveAsync，不重复、不重构。
6. 实现三个真实查询方法，不允许占位。
7. 不修改 Application 接口或查询模型。
8. Find 只查活动词条，精确匹配已规范化输入。
9. Find 不 Trim、不改大小写、不调用规范化器。
10. GetDetails 对活动和归档词条均有效。
11. GetDetails 在单连接、单只读事务中读取词条、全部例句和标签。
12. SearchText 使用字面子串搜索指定七个词条字段。
13. SearchText 必须转义 `\`、`%`、`_`。
14. SearchText 不搜索原句、GameTitle 或 Tag Name。
15. GameTitle 使用任一链接例句的精确 `COLLATE NOCASE` 匹配。
16. TagIds 使用 ALL/AND 语义。
17. 所有筛选条件组合为 AND。
18. 三种排序必须有 `Id ASC` 稳定次级键。
19. Count 必须在分页前统计去重词条。
20. Offset 使用 checked long。
21. Search 使用单连接、单只读事务完成 Count、Page、Primary、Tags。
22. 不用巨型 JOIN 直接分页。
23. Summary 0 Primary 返回 null 字段。
24. Summary 多 Primary 必须失败，不能任意选一个。
25. Details 多 Primary、重复 ID 或损坏数据必须失败。
26. 所有 SQL 参数化，禁止 `SELECT *`。
27. CancellationToken 完整传播。
28. 测试 DB/WAL/SHM 必须可删除。
29. 不修改 Migration001、Migration002、Domain、Application、已有 Repository 写逻辑、Godot、项目引用或目标框架。
30. 不新增 Migration、索引、FTS 或 NuGet 包。
31. 不实现永久删除、UseCase 或 UI。
32. 不执行 M1-T10。
33. 不创建 Git 提交。
34. 自动验收后保持 Awaiting Manual Verification。
35. 本任务不需要 GUI 验收。
36. 完成后执行 Git diff、状态文档、Decision Review 和 Skill Impact Review。
