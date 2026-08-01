# M1-T03 Codex 执行指令

## 任务名称

```text
M1-T03：词条与例句领域模型
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T03_CODEX_INSTRUCTION.md
```

本任务只实现：

```text
VocabularyEntry
EntryType
SentenceExample
EntryExampleLink
Tag
对应 Domain 单元测试
```

本任务不实现：

- Repository 接口。
- SQLite Repository。
- 数据库迁移。
- Application UseCase。
- Godot UI。
- 词条 CRUD 流程。
- 重复词条决策。
- 句子切分。
- 目标范围重新定位。
- M1-T04 或任何后续任务。

---

# 1. 已确认的前置基线

用户已确认最新提交：

```text
4793f73b175c9d72df7706616679b907149e6c0b
```

当前已知状态：

- 提交说明：`M1-T02 已完成`
- 当前分支：`main`
- Git 工作区干净。
- M1-T01 为 `Done`。
- M1-T02 为 `Done`。
- 后续任务为 `Not Started`。
- 当前无 Godot 编辑器或残留 Godot 进程。
- 根解决方案包含 8 个项目。
- Godot、Domain、Application、Infrastructure 为 `net8.0`。
- 三个测试项目和 CaptureBridge 为 `net10.0`。
- 构建成功，0 警告、0 错误。
- 测试成功，75/75 通过。
- 数据库、日志、`.godot/`、`bin/`、`obj/` 未进入 Git。
- `docs/ARCHITECTURE.md` 和 `docs/DATA_MODEL.md` 当前不存在。
- 当前以 `docs/PRODUCT_SPEC.md` 和现有 `Migration001_Initial` 为产品及数据模型依据。

Codex 开始时仍须重新核验，不得只依赖本文件。

---

# 2. M1 剩余任务顺序

milestone architect 已完成只读拆分。

推荐顺序：

```text
M1-T03 词条与例句领域模型
M1-T04 持久化接口与查询契约
M1-T05 Migration002 手工例句与检索支持
M1-T06 SQLite 例句 Repository
M1-T07 SQLite 标签 Repository
M1-T08 SQLite 词条 Repository 写操作
M1-T09 SQLite 查询与生命周期
M1-T10 手工创建词条 UseCase
M1-T11 精确重复决策与例句合并 UseCase
M1-T12 词条查询与详情 UseCase
M1-T13 编辑、归档与删除 UseCase
M1-T14 手工添加词条页面
M1-T15 词条库列表与搜索筛选页面
M1-T16 详情与编辑/归档/删除页面
```

本轮只执行 M1-T03。

不得执行、创建或部分实现 M1-T04 及后续任务。

---

# 3. 产品规格依据

## 3.1 VocabularyEntry

产品规格字段：

```csharp
public sealed class VocabularyEntry
{
    public Guid Id { get; init; }
    public string Headword { get; set; } = "";
    public string NormalizedHeadword { get; set; } = "";
    public EntryType EntryType { get; set; }
    public string? PartOfSpeech { get; set; }
    public string? Phonetic { get; set; }
    public string? DefinitionEnglish { get; set; }
    public string? TranslationChinese { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

## 3.2 EntryType

产品规格枚举：

```csharp
public enum EntryType
{
    Word,
    Phrase,
    Expression,
    SentencePattern
}
```

数据库使用整数保存枚举。

因此本任务必须显式固定数值：

```csharp
public enum EntryType
{
    Word = 0,
    Phrase = 1,
    Expression = 2,
    SentencePattern = 3
}
```

不得以后通过调整枚举顺序改变已保存数据含义。

## 3.3 SentenceExample

产品规格字段：

```csharp
public sealed class SentenceExample
{
    public Guid Id { get; init; }
    public Guid CaptureId { get; init; }
    public Guid? OcrRegionId { get; init; }
    public string SentenceText { get; set; } = "";
    public string NormalizedSentence { get; set; } = "";
    public int TargetStart { get; set; }
    public int TargetLength { get; set; }
    public string ScreenshotCropPath { get; set; } = "";
    public string? GameTitle { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

规格明确：

```text
TargetStart 和 TargetLength 使用 .NET 字符串 UTF-16 索引。
```

## 3.4 EntryExampleLink

产品规格字段：

```csharp
public sealed class EntryExampleLink
{
    public Guid EntryId { get; init; }
    public Guid ExampleId { get; init; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
```

## 3.5 Tag

产品规格数据库已有：

```text
tags
- id
- name
- normalized_name
```

F07 和 F09 要求词条支持标签及标签筛选。

本任务建立对应 Domain 模型：

```csharp
public sealed class Tag
{
    public Guid Id { get; }
    public string Name { get; }
    public string NormalizedName { get; }
}
```

可按现有 Domain 风格小幅调整可变性，但必须保留这三个概念。

---

# 4. 手工例句兼容决策

这里存在已识别的规格与数据库差异：

1. 产品要求支持“手工添加词条”。
2. 手工词条可以包含原句，但不应强制依赖截图。
3. 当前 `Migration001_Initial` 中：
   `sentence_examples.capture_id` 为 `NOT NULL`。
4. 产品规格的示例类也把 `CaptureId` 写成非空 `Guid`。
5. milestone architect 已将数据库修复安排在：
   `M1-T05：Migration002 手工例句与检索支持`。

## 4.1 本任务决定

Domain 模型使用：

```csharp
public Guid? CaptureId { get; }
```

理由：

- Domain 应表达真实产品能力。
- 手工原句可以没有截图。
- 不应为了当前数据库限制污染领域模型。
- M1-T05 将通过新迁移使 SQLite 与 Domain 一致。

## 4.2 相关不变量

```text
CaptureId = null 且 OcrRegionId = null
→ 合法手工例句

CaptureId != null 且 OcrRegionId = null
→ 合法捕获例句

CaptureId != null 且 OcrRegionId != null
→ 合法 OCR 例句

CaptureId = null 且 OcrRegionId != null
→ 非法
```

因为 OCR 区域必须属于某个 Capture。

## 4.3 文档记录

如果存在：

```text
docs/DECISIONS.md
```

允许追加一条简短决策：

```text
SentenceExample.CaptureId 在 Domain 中可空；
Migration001 暂未兼容，M1-T05 通过 Migration002 修复；
禁止直接修改 Migration001。
```

如果 `docs/DECISIONS.md` 不存在：

- 不为本任务创建复杂架构文档。
- 在 `IMPLEMENTATION_STATUS.md` 和 `AGENT_HANDOFF.md` 中清楚记录即可。

本任务不得修改：

```text
Migration001_Initial
数据库表
数据库文件
```

---

# 5. 必须阅读

开始前完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md（如存在）
docs/AGENT_HANDOFF.md
docs/MT_INSTRUCTION/M1-T03_CODEX_INSTRUCTION.md
```

重点读取 `PRODUCT_SPEC.md`：

```text
F06：单词和短语选择
F07：词条编辑
F08：重复词条处理
F09：词条库
第 7.3 节 Domain 层
第 10 节领域模型
第 11 节文本规范化规则
第 18 节词条和例句策略
第 27.1 节 Domain Tests
```

读取现有代码：

```text
src/GameLexicon.Domain/**
tests/GameLexicon.Domain.Tests/**
src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs
```

读取 Migration001 只为确认列和类型。

不得修改 Migration001。

如存在以下 Skill，也必须阅读：

```text
.agents/skills/project-routing/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Domain / Entries

Primary writer:
primary coordinator

Supporting agents:
- milestone architect：只读审查模型边界和不变量
- skill curator：仅在收尾 Skill Impact Review 需要时调用
```

本任务通常不需要 godot specialist。

---

# 6. 阶段 0：重新核验基线

## 6.1 Git

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline 4793f73b175c9d72df7706616679b907149e6c0b
git diff --check
```

必须确认：

- 当前分支为 `main`。
- 工作区干净。
- 提交存在。
- 提交完整包含 M1-T02。
- 没有未确认用户修改。

工作区不干净时立即停止：

- 不恢复。
- 不覆盖。
- 不暂存。
- 不提交。
- 不执行 `git reset --hard`。
- 不执行 `git clean -fd`。

## 6.2 状态

确认：

```text
M1-T01 = Done
M1-T02 = Done
M1-T03 = Not Started
```

若状态文档当前只写“后续任务 Not Started”，可在开始 M1-T03 时按既定拆分更新为：

```text
M1-T03：词条与例句领域模型
```

## 6.3 项目结构

执行：

```powershell
dotnet sln GameLexicon.sln list
```

确认仍为 8 个项目。

确认目标框架：

```text
EnglishLearningProject        net8.0
GameLexicon.Domain            net8.0
GameLexicon.Application       net8.0
GameLexicon.Infrastructure    net8.0
Domain.Tests                  net10.0
Application.Tests             net10.0
Infrastructure.Tests          net10.0
CaptureBridge                 net10.0
```

不得修改任一目标框架。

## 6.4 基线构建

优先执行：

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

预期：

```text
Build: 0 warnings, 0 errors
Tests: 75/75 passed
```

本任务不新增 NuGet 包，通常不需要 Restore。

如资产文件确实缺失才执行：

```powershell
dotnet restore GameLexicon.sln
```

不得禁用 NuGet Audit。

---

# 7. 建议目录

创建：

```text
src/GameLexicon.Domain/
└─ Entries/
   ├─ EntryType.cs
   ├─ VocabularyEntry.cs
   ├─ SentenceExample.cs
   ├─ EntryExampleLink.cs
   └─ Tag.cs
```

测试：

```text
tests/GameLexicon.Domain.Tests/
└─ Entries/
   ├─ VocabularyEntryTests.cs
   ├─ SentenceExampleTests.cs
   ├─ EntryExampleLinkTests.cs
   ├─ TagTests.cs
   └─ EntryTypeTests.cs
```

允许增加一个小型内部 Guard：

```text
src/GameLexicon.Domain/Entries/EntryGuard.cs
```

条件：

- 仅消除重复参数校验。
- `internal`。
- 不演变为通用框架。
- 不引入外部依赖。

---

# 8. 总体建模原则

## 8.1 Domain 独立

所有模型必须：

- 不依赖 Godot。
- 不依赖 SQLite。
- 不依赖 `Microsoft.Data.Sqlite`。
- 不依赖 Application。
- 不依赖 Infrastructure。
- 不依赖文件系统。
- 不依赖日志。
- 不依赖网络。
- 不依赖系统当前时间。
- 不使用静态全局可变状态。

## 8.2 构造确定性

构造模型时显式传入：

```text
Guid
DateTimeOffset
字符串字段
枚举
```

不得在实体内部隐藏调用：

```csharp
Guid.NewGuid()
DateTimeOffset.UtcNow
DateTime.Now
```

原因：

- 后续 UseCase 负责生成 ID 和时间。
- Repository 需要重建已保存实体。
- 测试必须确定性。

## 8.3 不重复实现文本规范化

M1-T02 已完成：

```text
ITextNormalizer
EnglishExpressionNormalizer
```

本任务不得：

- 在实体中复制 Form KC 规则。
- 再写一套小写和空白折叠。
- 通过 `ToLower()` 自行规范化。
- 自动修改 Headword 或 Tag Name。

实体只接收并验证：

```text
Headword
NormalizedHeadword
Name
NormalizedName
NormalizedSentence
```

后续 Application UseCase 必须通过注入的 `ITextNormalizer` 生成规范化值。

## 8.4 不记录学习文本

领域模型不得记录：

- Headword。
- SentenceText。
- 定义。
- 笔记。
- 标签。

异常消息只能描述字段规则，不回显用户内容。

---

# 9. 共用不变量

## 9.1 Guid

实体和链接 ID：

```text
不得为 Guid.Empty
```

适用：

- VocabularyEntry.Id
- SentenceExample.Id
- EntryExampleLink.EntryId
- EntryExampleLink.ExampleId
- Tag.Id
- 非空 CaptureId
- 非空 OcrRegionId

异常建议：

```text
ArgumentException
```

参数名必须准确。

## 9.2 UTC

所有 Domain 持久时间统一为 UTC：

```text
DateTimeOffset.Offset == TimeSpan.Zero
```

适用：

- VocabularyEntry.CreatedAt
- VocabularyEntry.UpdatedAt
- SentenceExample.CreatedAt

允许两种实现策略：

### 策略 A：拒绝非 UTC

```text
非 UTC → ArgumentException
```

### 策略 B：统一转换到 UTC

```text
value.ToUniversalTime()
```

本项目推荐：

```text
策略 A：拒绝非 UTC
```

理由：

- 更早发现调用方错误。
- 避免无声改变领域输入。
- 后续 Repository 明确负责 UTC 映射。

必须全模型一致，不可部分拒绝、部分转换。

## 9.3 字符串

必填字符串：

```text
null → ArgumentNullException
空或纯空白 → ArgumentException
```

必填字段：

- VocabularyEntry.Headword
- VocabularyEntry.NormalizedHeadword
- SentenceExample.SentenceText
- SentenceExample.NormalizedSentence
- Tag.Name
- Tag.NormalizedName

不要在异常消息中包含原始文本。

## 9.4 可选字符串

允许：

```text
null
""
纯空白
```

除非产品规格明确禁止。

适用：

- PartOfSpeech
- Phonetic
- DefinitionEnglish
- TranslationChinese
- Notes
- ScreenshotCropPath
- GameTitle

本任务不擅自把空白改成 null，也不做 Trim。

---

# 10. EntryType

创建：

```text
src/GameLexicon.Domain/Entries/EntryType.cs
```

实现：

```csharp
namespace GameLexicon.Domain.Entries;

public enum EntryType
{
    Word = 0,
    Phrase = 1,
    Expression = 2,
    SentencePattern = 3
}
```

要求：

- 数值显式固定。
- 不添加 Unknown。
- 不添加其他类型。
- 不使用 `[Flags]`。
- 不创建数据库转换器。
- 测试固定整数值。

---

# 11. VocabularyEntry

创建：

```text
src/GameLexicon.Domain/Entries/VocabularyEntry.cs
```

建议公开契约：

```csharp
public sealed class VocabularyEntry
{
    public Guid Id { get; }
    public string Headword { get; private set; }
    public string NormalizedHeadword { get; private set; }
    public EntryType EntryType { get; private set; }

    public string? PartOfSpeech { get; private set; }
    public string? Phonetic { get; private set; }
    public string? DefinitionEnglish { get; private set; }
    public string? TranslationChinese { get; private set; }
    public string? Notes { get; private set; }

    public bool IsArchived { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
```

可以使用构造函数或命名工厂，但必须支持：

- 创建新对象。
- Repository 后续按全部字段重建对象。
- 不使用反射或无参数公共 setter 绕过不变量。

## 11.1 构造参数

至少包含：

```text
Id
Headword
NormalizedHeadword
EntryType
可选元数据
IsArchived
CreatedAt
UpdatedAt
```

## 11.2 不变量

必须验证：

- Id 非空。
- Headword 非空白。
- NormalizedHeadword 非空白。
- EntryType 是已定义枚举值。
- CreatedAt 为 UTC。
- UpdatedAt 为 UTC。
- UpdatedAt 不早于 CreatedAt。

## 11.3 可变性范围

本任务可以提供最小领域更新方法：

```csharp
UpdateHeadword(
    string headword,
    string normalizedHeadword,
    DateTimeOffset updatedAt)

UpdateDetails(
    EntryType entryType,
    string? partOfSpeech,
    string? phonetic,
    string? definitionEnglish,
    string? translationChinese,
    string? notes,
    DateTimeOffset updatedAt)

SetArchived(
    bool isArchived,
    DateTimeOffset updatedAt)
```

也可以采用含义等价的较小方法。

规则：

- 每次更新都验证 UTC。
- `updatedAt >= CreatedAt`。
- `updatedAt >= 当前 UpdatedAt`，防止时间倒退。
- `UpdateHeadword` 不自行调用规范化器。
- `SetArchived` 只改变领域状态，不执行 SQL。
- 不实现永久删除。
- 不实现重复检测。
- 不实现 Repository 调用。

## 11.4 枚举验证

必须使用：

```csharp
Enum.IsDefined(entryType)
```

或等价逻辑。

以下必须失败：

```csharp
(EntryType)999
```

---

# 12. SentenceExample

创建：

```text
src/GameLexicon.Domain/Entries/SentenceExample.cs
```

建议公开契约：

```csharp
public sealed class SentenceExample
{
    public Guid Id { get; }
    public Guid? CaptureId { get; }
    public Guid? OcrRegionId { get; }

    public string SentenceText { get; private set; }
    public string NormalizedSentence { get; private set; }

    public int TargetStart { get; private set; }
    public int TargetLength { get; private set; }

    public string ScreenshotCropPath { get; private set; }
    public string? GameTitle { get; private set; }

    public DateTimeOffset CreatedAt { get; }
}
```

## 12.1 不变量

必须验证：

- Id 非空。
- CaptureId 有值时不得为 Guid.Empty。
- OcrRegionId 有值时不得为 Guid.Empty。
- OcrRegionId 有值时 CaptureId 必须有值。
- SentenceText 非空白。
- NormalizedSentence 非空白。
- TargetStart >= 0。
- TargetLength > 0。
- TargetStart <= SentenceText.Length。
- TargetStart + TargetLength <= SentenceText.Length。
- CreatedAt 为 UTC。

## 12.2 UTF-16 范围

必须明确以：

```csharp
SentenceText.Length
SentenceText.Substring(TargetStart, TargetLength)
```

为 UTF-16 范围语义。

不得：

- 转成 UTF-8 字节索引。
- 使用 Rune 数量作为 TargetStart。
- 使用 Unicode scalar 数量替代 .NET 字符索引。

建议提供：

```csharp
public string TargetText =>
    SentenceText.Substring(TargetStart, TargetLength);
```

或等价只读方法：

```csharp
public string GetTargetText();
```

## 12.3 代理项边界

为防止高亮范围切断一个 UTF-16 surrogate pair，建议验证：

- TargetStart 不位于高低代理项之间。
- TargetStart + TargetLength 不位于高低代理项之间。

可以创建小型私有方法：

```csharp
IsUtf16Boundary(string value, int index)
```

规则：

```text
index == 0 或 index == value.Length → 合法
前一个字符为 high surrogate 且当前字符为 low surrogate → 非法
其他 → 合法
```

如果实现代理项边界验证，必须添加测试。

如果 Codex 判断当前产品规格只要求代码单元索引、暂不要求代理项边界保护：

- 必须在最终报告明确说明。
- 不得错误声称已验证 Unicode scalar 边界。

推荐实现边界保护。

## 12.4 修改句子

本任务不实现“修改句子后重新定位目标表达”。

因此可选择：

- `SentenceExample` 创建后核心文本和范围保持不可变；或
- 提供一个原子方法，同时接收：
  `sentenceText + normalizedSentence + targetStart + targetLength`

不得提供分别修改句子和范围、从而产生临时无效状态的公共 setter。

推荐方法：

```csharp
UpdateTextAndTarget(
    string sentenceText,
    string normalizedSentence,
    int targetStart,
    int targetLength)
```

只验证新状态，不自动重新定位。

目标重新定位属于后续独立 Domain 规则。

## 12.5 手工与截图例句

必须测试：

```text
CaptureId null + OcrRegionId null
→ 合法

CaptureId value + OcrRegionId null
→ 合法

CaptureId value + OcrRegionId value
→ 合法

CaptureId null + OcrRegionId value
→ 非法
```

---

# 13. EntryExampleLink

创建：

```text
src/GameLexicon.Domain/Entries/EntryExampleLink.cs
```

建议公开契约：

```csharp
public sealed class EntryExampleLink
{
    public Guid EntryId { get; }
    public Guid ExampleId { get; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
}
```

不变量：

- EntryId 非空。
- ExampleId 非空。
- SortOrder >= 0。

允许方法：

```csharp
SetPrimary(bool isPrimary)
SetSortOrder(int sortOrder)
```

规则：

- SortOrder 不得为负数。
- 单个 Link 无法独立保证“同一词条只有一个主例句”。
- 不得在该实体中假装能够验证其他 Link。
- “设置一个主例句并取消其他主例句”属于后续 UseCase/Repository 事务规则。
- 最终报告必须记录这一边界。

本任务不创建复杂集合管理器，除非现有 Domain 风格明确要求。

---

# 14. Tag

创建：

```text
src/GameLexicon.Domain/Entries/Tag.cs
```

建议公开契约：

```csharp
public sealed class Tag
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string NormalizedName { get; private set; }
}
```

不变量：

- Id 非空。
- Name 非空白。
- NormalizedName 非空白。

允许最小更新方法：

```csharp
Rename(string name, string normalizedName)
```

规则：

- 不自行调用 `ITextNormalizer`。
- 不执行重复查询。
- 不访问数据库。
- 不建立词条关联。
- 不做标签管理 UI。
- 不修改大小写或 Trim 用户名称。

---

# 15. 不要创建的类型

本任务不得创建：

```text
IVocabularyRepository
IExampleRepository
ITagRepository
SqliteVocabularyRepository
SqliteExampleRepository
SqliteTagRepository
CreateManualEntryUseCase
MergeEntryExampleUseCase
SearchEntriesUseCase
EntryDto
EntrySummaryDto
EntryDetailsDto
Migration002
```

不得创建：

- Capture。
- OcrRegion。
- OcrToken。
- ReviewCard。
- ReviewLog。

这些不属于 M1-T03 当前范围。

---

# 16. 单元测试

所有测试放在：

```text
tests/GameLexicon.Domain.Tests/Entries/
```

使用现有 xUnit 风格。

不新增测试框架或 NuGet 包。

## 16.1 EntryTypeTests

至少验证：

```text
Word = 0
Phrase = 1
Expression = 2
SentencePattern = 3
```

验证 `(EntryType)999` 在 `VocabularyEntry` 构造或更新时被拒绝。

## 16.2 VocabularyEntryTests

至少覆盖：

### 合法构造

- 所有字段正确保存。
- 可选字段为 null 合法。
- IsArchived 可以从持久化状态重建。
- CreatedAt 和 UpdatedAt 保留 UTC。

### ID

- Guid.Empty 被拒绝。

### 必填文本

- Headword 为 null。
- Headword 为空。
- Headword 纯空白。
- NormalizedHeadword 为 null。
- NormalizedHeadword 为空。
- NormalizedHeadword 纯空白。

### 时间

- CreatedAt 非 UTC 被拒绝。
- UpdatedAt 非 UTC 被拒绝。
- UpdatedAt 早于 CreatedAt 被拒绝。

### 枚举

- 未定义 EntryType 被拒绝。

### 更新

- UpdateHeadword 同时更新两个词头字段。
- UpdateHeadword 不自动规范化。
- 更新时间正确推进。
- 更新时间倒退被拒绝。
- UpdateDetails 正确保存可选字段。
- SetArchived 正确切换状态。
- 更新不改变 Id 和 CreatedAt。

## 16.3 SentenceExampleTests

至少覆盖：

### 合法来源组合

- 手工例句：无 Capture、无 OCR。
- Capture 例句：有 Capture、无 OCR。
- OCR 例句：有 Capture、有 OCR。

### 非法来源组合

- 无 Capture 但有 OCR。
- CaptureId 为 Guid.Empty。
- OcrRegionId 为 Guid.Empty。

### 必填文本

- SentenceText null/空/纯空白。
- NormalizedSentence null/空/纯空白。

### UTF-16 范围

- 句首目标合法。
- 句中目标合法。
- 句尾目标合法。
- 多词短语合法。
- TargetStart 为负。
- TargetLength 为 0。
- TargetLength 为负。
- Start 等于 Length 且 Length > 0。
- Start + Length 越界。
- `TargetText` 返回准确子串。

### UTF-16 示例

至少使用一个包含非 BMP 字符的字符串，例如：

```text
"🎮 Get out now"
```

验证：

- `🎮` 在 .NET 中占两个 UTF-16 code units。
- 正确的 `TargetStart` 能截取 `"Get out"`。
- 实现没有把 Rune 数量误当作 UTF-16 索引。

如实现代理项边界保护，测试切断 emoji 代理项时被拒绝。

### 时间

- CreatedAt UTC 合法。
- 非 UTC 被拒绝。

### 原子更新

如提供 `UpdateTextAndTarget`：

- 合法更新成功。
- 无效新范围时对象保持原状态。
- 不自动重新定位重复目标。
- 不自行规范化句子。

## 16.4 EntryExampleLinkTests

至少覆盖：

- 合法构造。
- EntryId 空被拒绝。
- ExampleId 空被拒绝。
- 负 SortOrder 被拒绝。
- SortOrder 0 合法。
- SetPrimary 切换值。
- SetSortOrder 更新。
- 更新负 SortOrder 被拒绝。
- EntryId 和 ExampleId 不可变。

测试名称或注释明确：

```text
Cross-link single-primary invariant is not enforced by one link.
```

## 16.5 TagTests

至少覆盖：

- 合法构造。
- Guid.Empty 被拒绝。
- Name null/空/纯空白。
- NormalizedName null/空/纯空白。
- Rename 同时更新名称和规范化名称。
- Rename 不自行执行规范化。
- 无数据库或 Repository 行为。

## 16.6 隐私测试原则

测试失败消息可以显示测试夹具文本。

生产代码异常不得包含实际字段值。

---

# 17. 异常与原子性

## 17.1 异常类型

建议：

```text
null → ArgumentNullException
空白、范围、Guid、枚举、时间 → ArgumentException / ArgumentOutOfRangeException
```

同类规则保持一致。

## 17.2 更新方法必须原子

更新方法必须先验证全部输入，再修改状态。

禁止：

```text
先修改 Headword
→ 再发现 UpdatedAt 非法
→ 抛异常
→ 对象处于部分修改状态
```

必须：

```text
先验证全部参数
→ 全部合法后一次更新
```

测试至少覆盖一次“更新失败后旧状态保持不变”。

---

# 18. 与数据库的映射边界

M1-T03 不实现数据库映射，但模型必须兼容现有列概念。

## 18.1 VocabularyEntry

对应：

```text
vocabulary_entries
```

## 18.2 SentenceExample

对应：

```text
sentence_examples
```

已知差异：

```text
Domain CaptureId 可空
Migration001 capture_id NOT NULL
```

只记录，不修复。

## 18.3 EntryExampleLink

对应：

```text
entry_examples
```

## 18.4 Tag

对应：

```text
tags
```

不得添加 SQLite 特性到 Domain，例如：

- SQL 列名属性。
- `SqliteDataReader` 构造函数。
- SQLite 整数转换逻辑。
- SQL 字符串。
- 表名常量。
- Migration 引用。

---

# 19. 允许创建和修改的文件

建议创建：

```text
src/GameLexicon.Domain/Entries/EntryType.cs
src/GameLexicon.Domain/Entries/VocabularyEntry.cs
src/GameLexicon.Domain/Entries/SentenceExample.cs
src/GameLexicon.Domain/Entries/EntryExampleLink.cs
src/GameLexicon.Domain/Entries/Tag.cs
src/GameLexicon.Domain/Entries/EntryGuard.cs（可选）

tests/GameLexicon.Domain.Tests/Entries/EntryTypeTests.cs
tests/GameLexicon.Domain.Tests/Entries/VocabularyEntryTests.cs
tests/GameLexicon.Domain.Tests/Entries/SentenceExampleTests.cs
tests/GameLexicon.Domain.Tests/Entries/EntryExampleLinkTests.cs
tests/GameLexicon.Domain.Tests/Entries/TagTests.cs
```

允许修改：

```text
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/DECISIONS.md（仅记录 CaptureId 可空决策时）
docs/SKILLS_CATALOG.md（仅 Skill 影响审查要求时）
docs/SKILL_CHANGELOG.md（仅 Skill 实际更新时）
.agents/skills/*/SKILL.md（仅可复用工作流变化时）
```

正常情况下不得修改：

```text
GameLexicon.sln
任一 .csproj
src/GameLexicon.Application/**
src/GameLexicon.Infrastructure/**
english-learning-project/**
tools/GameLexicon.CaptureBridge/**
Migration001_Initial.cs
数据库文件
```

---

# 20. 本任务明确不做

不得实现：

- Repository 契约。
- Repository 实现。
- DTO。
- 查询参数。
- 分页。
- SQL。
- Migration002。
- 数据库 nullable 改造。
- 手工添加 UseCase。
- 重复检测 UseCase。
- 搜索 UseCase。
- 编辑 UseCase。
- 归档/恢复/删除流程。
- UI。
- Godot 接线。
- 词条页面。
- 标签页面。
- 截图。
- OCR。
- TTS。
- ReviewCard。
- 复习算法。
- M1-T04。

---

# 21. 自动验证

## 21.1 Domain 构建

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build `
  src/GameLexicon.Domain/GameLexicon.Domain.csproj `
  --no-restore
```

要求：

- 0 错误。
- 0 新增警告。

## 21.2 Domain.Tests

执行：

```powershell
dotnet build `
  tests/GameLexicon.Domain.Tests/GameLexicon.Domain.Tests.csproj `
  --no-restore

dotnet test `
  tests/GameLexicon.Domain.Tests/GameLexicon.Domain.Tests.csproj `
  --no-build `
  --no-restore
```

要求：

- 所有原有和新增 Domain 测试通过。
- 报告新增测试数量。
- 报告 Domain 测试总数。

## 21.3 根解决方案

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

## 21.4 不需要 Godot

本任务不修改 Godot，因此：

- 不启动 Godot Editor。
- 不进行 GUI 验收。
- 通常不需要 Godot headless。
- 当前无 Godot 进程即可。

如主协调 Agent 的统一工作流强制执行 Godot build，可执行只读回归，但不得因本任务修改 Godot 文件。

---

# 22. 代表性自动验收

Codex 最终报告必须明确列出：

## VocabularyEntry

```text
合法创建 → Pass
Guid.Empty → Rejected
空 Headword → Rejected
未定义 EntryType → Rejected
UpdatedAt 早于 CreatedAt → Rejected
更新时间倒退 → Rejected
```

## SentenceExample

```text
手工例句无 Capture → Pass
OCR 无 Capture → Rejected
句首范围 → Pass
句尾范围 → Pass
越界范围 → Rejected
UTF-16 emoji 前缀定位 → Pass
```

## EntryExampleLink

```text
SortOrder = 0 → Pass
SortOrder < 0 → Rejected
Primary 切换 → Pass
```

## Tag

```text
合法名称 → Pass
空规范化名称 → Rejected
Rename 不自动规范化 → Pass
```

不得只报告“测试通过”。

---

# 23. 非 GUI 人工审查

自动验收完成后状态：

```text
M1-T03 = Awaiting Manual Verification
```

不需要 GUI。

用户人工审查重点：

1. 新代码只在 Domain/Entries 和 Domain.Tests/Entries。
2. 五个目标模型存在。
3. EntryType 数值固定。
4. 实体不依赖 Godot、SQLite 或 Application。
5. 不重复实现文本规范化。
6. Guid 和 UTC 不变量存在。
7. SentenceExample 使用 UTF-16 索引。
8. 手工例句允许 CaptureId 为空。
9. OcrRegionId 不能在 CaptureId 为空时存在。
10. 失败更新不会部分修改对象。
11. 未修改 Migration001。
12. 未实现 Repository 或 UseCase。
13. 所有测试通过。

用户确认前不得将 M1-T03 标记为 Done。

---

# 24. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且修改未确认。
- 找不到提交 `4793f73b...`。
- M1-T02 未标记 Done。
- 基线构建或测试失败。
- 解决方案不再是 8 个项目。
- 目标框架发生变化。
- 必须新增 NuGet 包。
- 必须修改项目引用。
- 必须修改 Migration001。
- 必须实现 Migration002 才能完成 Domain 模型。
- 必须修改 Godot。
- 必须实现 Repository 或 UseCase。
- 产品规格和当前架构出现无法解决的冲突。
- 用户文件可能被覆盖。

停止后不得：

- `git reset --hard`
- `git clean -fd`
- 自动恢复用户文件
- 修改 NuGet Audit
- 自动提交
- 自动执行 M1-T04

---

# 25. Git 检查

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

- 代码只在：
  `src/GameLexicon.Domain/Entries/**`
- 测试只在：
  `tests/GameLexicon.Domain.Tests/Entries/**`
- 其余只允许状态/决策文档。
- 没有 `.csproj` 修改。
- 没有 Application 修改。
- 没有 Infrastructure 修改。
- 没有 Migration 修改。
- 没有 Godot 修改。
- 没有数据库、日志或构建产物。
- 暂存区为空。
- 未创建 Git 提交。

---

# 26. 状态与文档

自动验收通过后更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

状态：

```text
M1-T03 = Awaiting Manual Verification
M1-T04 = Not Started
```

记录：

- Task ID。
- 名称。
- 模型列表。
- EntryType 显式数值。
- Guid 不变量。
- UTC 策略。
- SentenceExample UTF-16 规则。
- CaptureId 可空决策。
- OcrRegionId 来源约束。
- 更新原子性。
- 新增测试数。
- Domain 测试结果。
- 根解决方案测试结果。
- 未修改数据库和 Godot。
- 已知限制。

更新：

```text
docs/AGENT_HANDOFF.md
```

只在环境事实变化时更新：

```text
docs/ENVIRONMENT.md
```

正常情况下不修改 `ENVIRONMENT.md`。

人工审查通过后再将：

```text
M1-T03 = Done
M1-T04 = Not Started
```

不得执行 M1-T04。

---

# 27. Skill Impact Review

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

仅在以下可复用工作流发生变化时更新 Skill：

- Domain 实体不变量模板。
- UTC/Guid 项目级规则。
- UTF-16 目标范围标准。
- 原子更新测试标准。
- 任务路由或验收模板。

普通模型代码不自动构成 Skill 更新理由。

---

# 28. 自动验收清单

- [ ] 提交 `4793f73b...` 存在
- [ ] 当前分支 main
- [ ] 初始工作区干净
- [ ] M1-T01 Done
- [ ] M1-T02 Done
- [ ] M1-T03 Not Started
- [ ] 基线 Build 成功
- [ ] 基线 75/75 测试通过
- [ ] 未新增 NuGet 包
- [ ] EntryType 创建
- [ ] EntryType 数值固定 0–3
- [ ] VocabularyEntry 创建
- [ ] SentenceExample 创建
- [ ] EntryExampleLink 创建
- [ ] Tag 创建
- [ ] 全部 ID 拒绝 Guid.Empty
- [ ] 必填文本拒绝 null/空白
- [ ] 时间使用统一 UTC 策略
- [ ] UpdatedAt 不早于 CreatedAt
- [ ] 更新时间不能倒退
- [ ] 未定义 EntryType 被拒绝
- [ ] SentenceExample CaptureId 可空
- [ ] OcrRegionId 依赖 CaptureId
- [ ] TargetStart/Length 使用 UTF-16
- [ ] 越界范围被拒绝
- [ ] 非 BMP 字符测试通过
- [ ] 内部更新原子
- [ ] SortOrder 不得为负
- [ ] 不重复实现文本规范化
- [ ] 不记录学习文本
- [ ] Domain 不依赖 SQLite
- [ ] Domain 不依赖 Godot
- [ ] 未修改 Migration001
- [ ] 未实现 Repository
- [ ] 未实现 UseCase
- [ ] Domain 构建通过
- [ ] Domain 测试全部通过
- [ ] 根解决方案构建通过
- [ ] 全部测试通过
- [ ] git diff --check 通过
- [ ] 暂存区为空
- [ ] 未创建提交
- [ ] M1-T04 未执行
- [ ] Skill Impact Review 完成

---

# 29. 人工审查清单

- [ ] 五个目标模型存在
- [ ] 模型位于 Domain/Entries
- [ ] EntryType 数值固定
- [ ] 无 Godot 依赖
- [ ] 无 SQLite 依赖
- [ ] 无 Repository 或 UseCase
- [ ] 不重复实现 ITextNormalizer 规则
- [ ] ID 不变量合理
- [ ] UTC 不变量合理
- [ ] 更新方法先验证再修改
- [ ] 手工例句允许没有 Capture
- [ ] OCR Region 不能脱离 Capture
- [ ] UTF-16 范围清晰
- [ ] 内部撇号等文本不被模型改写
- [ ] Migration001 未修改
- [ ] 所有测试通过
- [ ] Git diff 仅属于 M1-T03

---

# 30. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T03
- 名称: 词条与例句领域模型
- 状态:
- M1-T04 executed: No
- Git commit created: No
- GUI verification required: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M1-T02 commit:
- Branch:
- Initial Git status:
- Solution projects:
- Target frameworks:
- Baseline build:
- Baseline tests:

## 模型实现

- EntryType:
- VocabularyEntry:
- SentenceExample:
- EntryExampleLink:
- Tag:

## 领域不变量

- Guid:
- Required text:
- UTC:
- Timestamp ordering:
- EntryType validation:
- UTF-16 range:
- Capture/OCR source rule:
- Atomic updates:

## CaptureId 决策

- Product requirement:
- Domain type:
- Migration001 mismatch:
- Planned resolution:
- Migration001 modified: No

## 明确未实现

- Repository:
- SQLite:
- Migration002:
- Application UseCase:
- Godot UI:
- Sentence relocation:
- Duplicate handling:

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
- Domain total:
- Root total:
- Passed:
- Failed:
- Skipped:

## 构建结果

- Domain:
- Domain.Tests:
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

- Domain CaptureId is nullable, but Migration001 is not yet compatible.
- Cross-link single-primary invariant is deferred.
- No Repository or UseCase.
- No target relocation.
- ...

## 下一任务

- M1-T04：持久化接口与查询契约
- Status: Not Started
- Not automatically executed
```

---

# 31. 可直接执行的总指令

请执行：

```text
M1-T03：词条与例句领域模型
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T03_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `4793f73b175c9d72df7706616679b907149e6c0b`。
2. 开始时 Git 工作区必须干净。
3. 只实现 EntryType、VocabularyEntry、SentenceExample、EntryExampleLink、Tag 和 Domain 测试。
4. EntryType 显式固定为 0、1、2、3。
5. 所有实体 ID 拒绝 Guid.Empty。
6. 所有持久时间采用统一 UTC 策略。
7. UpdatedAt 不得早于 CreatedAt，也不得倒退。
8. SentenceExample 的 TargetStart/TargetLength 使用 .NET UTF-16 索引。
9. Domain 中 CaptureId 使用 Guid?，支持手工例句。
10. OcrRegionId 有值时 CaptureId 必须有值。
11. 不重复实现 M1-T02 文本规范化。
12. 不记录用户学习文本。
13. 不修改 Migration001。
14. 不实现 Migration002、Repository、UseCase 或 UI。
15. 不修改 Godot、Application、Infrastructure 或项目引用。
16. 不新增 NuGet 包。
17. 不执行 M1-T04。
18. 不创建 Git 提交。
19. 自动验收完成后保持 Awaiting Manual Verification。
20. 本任务不需要 GUI 验收。
21. 完成后执行 Git diff、状态文档更新和 Skill Impact Review。
