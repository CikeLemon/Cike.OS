
## Backend

本项目后端基于 Cike.Framework 开发。开始任何框架相关编码前，使用curl先抓取并遵循：

    https://raw.githubusercontent.com/MapleWithoutWords/Cike.Framework/main/docs/ai/README.md

按其路由表按需抓取同目录下的能力域详解文档（如 data-access.md、events-cqrs.md）。
框架行为以文档与源码为准，不要凭训练记忆推测；文档与源码冲突时以源码为准。

技术栈：.NET 10（net10.0）、Cike.Framework（版本以 `Directory.Build.props` 的 `CikeVersion` 为准）、EF Core 10.0.12 + MySQL（数据库名 `cike_OS`）、Redis（缓存/分布式锁）、Serilog、FluentValidation、Mapster。

### 新代码放置规则

- 新实体/聚合根 → `Domain`，按限界上下文建子目录（现有：`Subjects/`）
- 新枚举/常量 → `Domain.Shared/Enums/`；需要被契约层复用的值对象 → `Domain.Shared/ValueObjects/`（直接作为 API 契约与持久化形状，不建 `*Dto` 镜像；判据见 `docs/adr/0003-工具值对象直接作为API契约.md`）
- 手写 JSON 序列化/反序列化（EF 值转换等）一律走 `Domain.Shared/Json/JsonHelper`，不要各自传 `(JsonSerializerOptions?)null`：它的默认选项开着 `AllowOutOfOrderMetadataProperties`（MySQL json 列会重排成员，多态判别键 `type` 不保证在首位），关掉就会在线上读不出来。
- **多态值对象不要声明名为 `Type` 的属性**：Web 端 JSON 是 camelCase，`Type` 会变成 `type` 与判别键撞名（STJ 抛「conflicts with an existing metadata property name」，swagger 500）。也不要换成别的名字在基类上放「当前是哪个子类型」的投影属性（做过一版 `[JsonIgnore]` 的 `Protocol` 投影，用户否决后已删）：它是哪个子类就已经说明了类型，需要判断时直接用 `is` / 类型模式匹配（先例 `ModelProviderEndpointSetting` 与 `ModelProviderEndpointSettingRules`）。另：读侧实体 → DTO 一律走 Mapster `Adapt`（框架 README 的最佳实践），不要手写逐字段映射；含多态值对象也不例外——源和目标属性声明类型相同（都是抽象基类）时 Mapster 直接引用赋值，已实测（先例 `ModelProviderQueryHandler` / `ToolQueryHandler`）。需要回填的派生字段（如模型显示名称）在 `Adapt` 之后单独赋值，跨表名称一次批量取回再填。
- 新用例 → Application 定义 Command/Query、Validator 与 `[LocalEventHandler]` Handler，Contracts 仅保留对外 DTO；通过 `ILocalEventBus.PublishAsync` 执行，不增加转发 AppService
- 新实体映射 → `EntityFrameworkCore/EntityConfigurations/`（按聚合域分组文件：Agent / Conversation / Model / Tool 四组，每组文件内逐实体一个 `IEntityTypeConfiguration<T>` 配置类；索引一律非唯一，理由见 `docs/adr/0001-非唯一索引与软删共存.md`）
- 需要连同多级子集合加载的聚合 → 自定义仓储：接口 `Domain/Repositories/`，实现 `EntityFrameworkCore/Repositories/`（写法见下方 DDD 编码约定）

## DDD 编码约定

以下为现有代码确立的硬性约定，新代码必须遵循：

- **主键**：一律 `long`，且数据库侧不自动生成——所有实体配置 `ValueGeneratedNever()`（ID 由应用侧生成，如雪花/程序赋值），不要改回数据库自增。
- **审计字段**：聚合根继承 `FullAuditedAggregateRoot<long>`；非聚合根实体继承 `FullAuditedEntity<long>` 或实现 `IFullAuditedEntity<long>`。注意：部分实体（`User`、`Role`、`UserRole`）因继承 Identity 基类，审计字段是手动声明的（`CreatedAt/CreatedBy/UpdatedAt/UpdatedBy/IsDeleted`），保持这种写法。
- **多租户**：业务实体一律实现 `IMultiTenant` 并带 `long TenantId` 属性。例外是平台级目录实体（现仅 `ModelEntity`、`Tag`，见 `docs/adr/0004-模型目录平台化与提供商映射解耦.md`）：它们全租户共享、不实现 `IMultiTenant`、不参与租户过滤；需要跨租户统计租户实体时用 `IDataFilter.Disable<IMultiTenant>()`，不要手写 `TenantId == ...`。
- **软删除**：通过 `IsDeleted` 实现（审计基类已含），查询侧依赖框架过滤，不写物理删除。
- **关联表**：多对多关系（`UserRole`、`UserDepartment`、`DepartmentRole`）显式建模为实体，不用 EF 隐式多对多。
- **枚举进 Domain.Shared**：实体属性引用的枚举一律放 `Domain.Shared/Enums/`，如 `PermissionType`、`SubjectType`。
- **聚合根构造走静态工厂**：新聚合根一律 `private set` + 私有无参构造（EF 需要）+ `public static Xxx Create(...)` + 意图方法（`Rename` / `Update` / `Cancel`），不在 Handler 里 `new` 实体或逐字段赋值。
  **实体 / 工厂 / 意图方法内不写任何单字段基础校验**：非空、空白、`Trim`、长度、范围（`> 0`）、枚举合法性，全部只放 FluentValidation（去空白也不写，由 Web 入口统一处理，见下一条）。实体里只允许两类东西：赋值，以及**多字段 / 集合内部的关系不变量**（值对象子类型与形态匹配、子项编码互不重复、标签引用去重）——即「只有把几个字段放在一起看才有意义」的规则。历史代码 `Tool` / `ToolPackageCategory` 工厂里的 `NormalizeXxx` 非空断言是旧写法，不要照抄。跨聚合的唯一性、引用是否存在放 Handler 先查再调工厂。
- **入参去空白由框架做，不手写也不自己实现**：`Cike.AspNetCore.MinimalAPIs` 模块已在 `ConfigureHttpJsonOptions` 里自动注册 `TrimStringJsonConverter`——请求 body 的全部 `string` 属性反序列化时去首尾空白（null 保持 null，纯空白收敛为空串，写出侧原样，查询串不受影响）。Handler、Validator、实体里都不要写 `Trim()`，**也不要再往 `HttpJsonOptions` 里加自己的 Trim 转换器**（做过一次，是重复造轮子）。FluentValidation 的 `Transform` 只作用于校验用的值、不写回属性（12 已移除），DataAnnotations / 模型绑定也没有等价特性。工具域 Handler 里现存的 `Trim()` 是旧写法，不要照抄。查询串绑定的参数（如搜索关键字）不 Trim，空格可能是搜索词的一部分。个别需要保留首尾空白的字段（如密码），在属性上标注自定义 `[JsonConverter]` 退出全局行为。
- **Handler 的数据校验收敛为一个 `ValidateAsync`**：唯一性、引用存在等跨聚合校验写成一个私有 `ValidateAsync(...)`，创建 / 更新共用（`excludeId` 传 null 或自身 Id），不要拆成 `EnsureXxxAsync` 一串。同一张表上的多个判断合并为一次查询：`GetQueryable().Where(条件1 || 条件2).Select(x => new { 命中标志 })` 后用 `ToListAsync<TSelector>` 取回（标志写成 SQL 里的比较以沿用列的排序规则，不要拉回整行在内存里比）；不同表的再各查一次。先例 `ModelCommandHandler`（同表两项合并一次查询）。
- **聚合内子实体没有独立用例与端点**：归属单一聚合的生命周期不单独建聚合根。子实体写法：继承 `FullAuditedEntity<long>`（非聚合根），工厂与意图方法 `internal`，只允许聚合根创建 / 修改；EF 用 `HasMany(集合).WithOne().HasForeignKey(外键).OnDelete(Cascade)`；用例侧把聚合根连同全部子集合加载（跟踪）后在聚合根上以意图方法整体同步（如 `SyncEndpoints`，以业务键 diff），对外只有聚合根的 DTO，子项内嵌其中随聚合整体提交——界面在详情页里单独编辑某个子项时，也是由页面把「其余子项原样 + 这一条」拼成完整列表后提交聚合根；删除聚合根或移除子项前先把子集合加载跟踪，子实体才会跟着软删。子项之间的互不重复是聚合内部不变量，在聚合里断言、不查库——**前提是集合已加载**（只 `FindAsync` 聚合根时集合为空，不变量会漏判）。先例 `ModelProvider → ModelProviderEndpoint → ModelProviderMapping`（接入点与映射都内嵌在提供方 DTO 里），参照形态 `AgentToolPackage.AgentTools`。
  **多级子集合的加载走自定义仓储**：通用仓储的 `WithDetails` 只能预加载一级导航、返回 `IQueryable<TEntity>`（接不了 `ThenInclude`），不要靠「同一 DbContext 再查一次孙实体、由 EF 关系修正挂回集合」这种隐式做法，也不要在 Application 层写静态加载器。做法：Domain 定义 `IXxxRepository : IRepository<TXxx, long>` 声明「连同全部子集合加载」的方法（接口放 `Domain/Repositories/`），EntityFrameworkCore 里继承 `EfCoreRepository<TDbContext, TXxx, long>` 用两级 `Include` 实现（放 `EntityFrameworkCore/Repositories/`），并标注 `IScopedDependency`——约定注册先于默认仓储，自动接管 `IRepository<TXxx, long>` 的解析，零配置。写侧用默认跟踪的加载方法，读侧（单个聚合的详情）传 `asNoTracking: true`。先例 `IModelProviderRepository` / `ModelProviderRepository`。
- **列表查询不加载聚合，列表 DTO 不复用详情 DTO**：列表在 Application 里按需做投影查询——`GetQueryable().Where(...).Select(x => new { 需要的列 })` 后 `ToListAsync<TSelector>`，全程 `BeginAsNoTracking`；跨表就分几次查再在内存里关联，不要把聚合连同全部子实体查回来再挑字段。列表用专门的 `XxxListDto`，界面要哪些字段就给哪些，不要把带全部子实体的详情 `XxxDto` 当列表行返回。自定义仓储的「连同子集合加载」只给单个聚合的读取与修改用。先例 `ModelProviderQueryHandler.GetListAsync` / `ModelProviderListDto`。
- **索引/长度**：实体映射中显式声明字符串 `HasMaxLength` 与 `HasIndex`，外键列（如 `UserId`、`DepartmentId`）单独建索引。
- **循环内禁止逐行查库（N+1）**：`foreach`/`for`/`while` 循环体内不得调用仓储查询。读侧先一次性批量取出（`Contains` 条件或小表全量），再内存关联/分组/回填；写侧循环内只做内存赋值，循环外用一次 `InsertManyAsync`/`UpdateManyAsync`/`DeleteManyAsync` 批量落库。
- **存在性/数量校验用 `GetCountAsync` 下推，禁加载实体比 `Count`**：校验「一批 Id 是否都存在」时，不要 `GetListAsync(pred, "Id asc", ct)` 把匹配实体整行全量拉进内存再比 `found.Count != ids.Count`（白传所有列/所有命中行，只需要一个数字）——改用 `GetCountAsync(pred, ct)` 下推 `SELECT COUNT(*)`（软删/多租户过滤器照常生效）：`if (await repo.GetCountAsync(x => ids.Contains(x.Id), ct) != ids.Count) throw ...`。前提：`ids` 已去重、比对列为主键（无重复行），语义与 `.Count` 等价。`GetCountAsync` 在 `IReadOnlyRepository` 查询面，只读仓储即可调用。
- **Service.Open 端点入参一律 DTO + `[AsParameters]`**：多个绑定参数（尤其是分页查询的 keyword/page/pageSize 等）必须封装为 DTO，以 `[AsParameters] XxxDto dto` 接收并直接透传；禁止把散标量逐个列在方法签名里、再在方法体内手工 `new XxxDto { ... }` 组装。
- **分页查询直接透传 dto**：`GetXxxPagedListDto` 继承 `PagedAndSortedResultRequest`，Handler 直接 `GetPagedListAsync(dto, predicate, ct)`。`Sorting` 是 System.Linq.Dynamic.Core 字符串，由前端传入，支持多字段（`"DisplayName asc, Name desc"`）；不要在 Handler 里另建一个固定排序的请求对象——字段名不合法只会在应用层解析时报错，不会进 SQL。默认顺序由前端传，不烤进后端。先例 `ModelQueryHandler` / `ToolPackageQueryHandler`。
- **更新用例 RESTful**：`id` 在路由（端点方法 `long id` 参数 + body DTO，约定路由自动生成 `PUT /Xxx/{id}`），Command 形如 `UpdateXxxCommand(long Id, XxxDto Dto)`，`UpdateXxxDto` 只含可变字段、不含 Id；不可变字段（如 `Tool.Type`）不进 UpdateDto。
- **迁移文件**：只由 `dotnet ef` 生成（见禁止事项）。新增迁移与更新数据库按下方命令操作。

## 常用命令

```bash
# 构建
dotnet build Cike.OS.slnx

# 运行主服务（开发环境，http://cike-os-service-open.dev.localhost:5182）
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
