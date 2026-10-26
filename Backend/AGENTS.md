
## Backend

本项目后端基于 Cike.Framework 开发。开始任何框架相关编码前，使用curl先抓取并遵循：

    https://raw.githubusercontent.com/MapleWithoutWords/Cike.Framework/main/docs/ai/README.md

按其路由表按需抓取同目录下的能力域详解文档（如 data-access.md、events-cqrs.md）。
框架行为以文档与源码为准，不要凭训练记忆推测；文档与源码冲突时以源码为准。

技术栈：.NET 10（net10.0）、Cike.Framework 1.0.10、EF Core 10.0.12 + MySQL（数据库名 `cike_OS`）、Redis（缓存/分布式锁）、Serilog、FluentValidation、Mapster。

### 新代码放置规则

- 新实体/聚合根 → `Domain`，按限界上下文建子目录（现有：`Subjects/`）
- 新枚举/常量 → `Domain.Shared/Enums/`
- 新用例 → Application 定义 Command/Query、Validator 与 `[LocalEventHandler]` Handler，Contracts 仅保留对外 DTO；通过 `ILocalEventBus.PublishAsync` 执行，不增加转发 AppService
- 新实体映射 → `EntityFrameworkCore/EntityConfigurations/`（当前集中在 `SubjectEntityConfiguration.cs`，按 `IEntityTypeConfiguration<T>` 逐实体配置）

## DDD 编码约定

以下为现有代码确立的硬性约定，新代码必须遵循：

- **主键**：一律 `long`，且数据库侧不自动生成——所有实体配置 `ValueGeneratedNever()`（ID 由应用侧生成，如雪花/程序赋值），不要改回数据库自增。
- **审计字段**：聚合根继承 `FullAuditedAggregateRoot<long>`；非聚合根实体继承 `FullAuditedEntity<long>` 或实现 `IFullAuditedEntity<long>`。注意：部分实体（`User`、`Role`、`UserRole`）因继承 Identity 基类，审计字段是手动声明的（`CreatedAt/CreatedBy/UpdatedAt/UpdatedBy/IsDeleted`），保持这种写法。
- **多租户**：业务实体一律实现 `IMultiTenant` 并带 `long TenantId` 属性。
- **软删除**：通过 `IsDeleted` 实现（审计基类已含），查询侧依赖框架过滤，不写物理删除。
- **关联表**：多对多关系（`UserRole`、`UserDepartment`、`DepartmentRole`）显式建模为实体，不用 EF 隐式多对多。
- **枚举进 Domain.Shared**：实体属性引用的枚举一律放 `Domain.Shared/Enums/`，如 `PermissionType`、`SubjectType`。
- **索引/长度**：实体映射中显式声明字符串 `HasMaxLength` 与 `HasIndex`，外键列（如 `UserId`、`DepartmentId`）单独建索引。
- **循环内禁止逐行查库（N+1）**：`foreach`/`for`/`while` 循环体内不得调用仓储查询。读侧先一次性批量取出（`Contains` 条件或小表全量），再内存关联/分组/回填；写侧循环内只做内存赋值，循环外用一次 `InsertManyAsync`/`UpdateManyAsync`/`DeleteManyAsync` 批量落库。
- **存在性/数量校验用 `GetCountAsync` 下推，禁加载实体比 `Count`**：校验「一批 Id 是否都存在」时，不要 `GetListAsync(pred, "Id asc", ct)` 把匹配实体整行全量拉进内存再比 `found.Count != ids.Count`（白传所有列/所有命中行，只需要一个数字）——改用 `GetCountAsync(pred, ct)` 下推 `SELECT COUNT(*)`（软删/多租户过滤器照常生效）：`if (await repo.GetCountAsync(x => ids.Contains(x.Id), ct) != ids.Count) throw ...`。前提：`ids` 已去重、比对列为主键（无重复行），语义与 `.Count` 等价。`GetCountAsync` 在 `IReadOnlyRepository` 查询面，只读仓储即可调用。
- **Service.Open 端点入参一律 DTO + `[AsParameters]`**：多个绑定参数（尤其是分页查询的 keyword/page/pageSize 等）必须封装为 DTO，以 `[AsParameters] XxxDto dto` 接收并直接透传；禁止把散标量逐个列在方法签名里、再在方法体内手工 `new XxxDto { ... }` 组装。
- **迁移文件**：只由 `dotnet ef` 生成（见禁止事项）。当前尚未生成迁移目录，首次迁移时按下述命令操作。

## 常用命令

```bash
# 构建
dotnet build Cike.OS.slnx

# 运行主服务（开发环境，http://localhost:5209）
dotnet run --project src/Cike.OS.Service.Open

# 全量测试（Domain、管理 API 与 SSO；HTTP 集成测试使用隔离 SQLite 内存库）
dotnet test Cike.OS.slnx

# EF Core 迁移（在 Backend 目录执行）
dotnet ef migrations add <Name> \
  --project src/Cike.OS.EntityFrameworkCore \
  --startup-project src/Cike.OS.Service.Open \
  --context CikeOSDbContext \
  --output-dir Migrations

dotnet ef database update \
  --project src/Cike.OS.EntityFrameworkCore \
  --startup-project src/Cike.OS.Service.Open \
  --context CikeOSDbContext
```

## 禁止事项（后端特有）

通用禁止事项（不擅自引入依赖、不扩大范围、不破坏测试、不隐藏问题）见根 `AGENTS.md`，对全部子项目生效。本节只列后端特有：

- **不反编译**：不使用 dnSpy / ILSpy / dotPeek / ildasm 等工具反编译 DLL 或其他二进制文件。遇到不懂的框架/库行为，按顺序处理：① 读源码与官方文档 → ② 网络搜索 → ③ 向用户提问。不允许凭训练记忆猜测框架行为。
- **不手改生成文件**：EF Core 迁移（`*ModelSnapshot.cs`、`*.Designer.cs`）只能由 `dotnet ef` 命令生成；确需手改必须先征得用户同意。
- **不破坏分层**：不得让 Domain 依赖 Application/EFCore，不得在宿主里写业务逻辑，不得绕过 Application 层直接从宿主调用仓储。
