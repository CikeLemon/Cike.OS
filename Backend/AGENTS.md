
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
- **一个聚合根一套目录，不与别的聚合混放**：Application 与 Contracts 都按聚合根分目录（如 `ModelProviders/` 与 `ModelProviderEndpoints/` 各自的 `Commands/` `Queries/` `Validators/` 与 `Dtos/`），命名空间随目录走；端点类也一个聚合一个类。子实体的 DTO / Validator 跟着它的聚合根走。某个东西一旦升为聚合根，目录、命名空间、Handler、端点类要一起搬出去，不能留在原聚合的目录里（2026-11-27 用户点名：「独立聚合在 application 层应该独立一个目录，service 也应该单独一个类，混到一起像什么样子」）。
- **DTO 继承框架基类，不手写 Id / 审计字段**（`Cike.Contracts.EntityDtos`，详见框架 `framework-core.md`「Dto 编写约定」；2026-11-27 用户点名）：凡是**实体**的输出 Dto（`XxxDto`、列表专用的 `XxxListDto`、嵌套在父 Dto 里的子实体 Dto 如接入点 / 映射）一律继承 `AuditedEntityDto<long>`（Id + 创建 / 更新时间与人），需要外发软删标记才用 `FullAuditedEntityDto<long>`，只要 Id 的轻量选项用 `EntityDto<long>`；Mapster `Adapt` 会按名字把继承来的字段自动带上。输入 Dto（`CreateXxxDto` / `UpdateXxxDto` / `SaveXxxDto`）与非实体的结果 Dto 平铺、不继承。分页：参数用 `PagedAndSortedResultRequest` 对象承载（端点 `[AsParameters]` 绑定，Query 形如 `GetTagPagedListQuery(string? Keyword, PagedAndSortedResultRequest Paging)`），少量筛选字段平铺、字段多时才建继承它的 `GetXxxPagedListDto`；返回 `PagedResultDto<T>`，不自己造分页形状。
- 新实体映射 → `EntityFrameworkCore/EntityConfigurations/`（按聚合域分组文件：Agent / Conversation / Model / Tool 四组，每组文件内逐实体一个 `IEntityTypeConfiguration<T>` 配置类；索引一律非唯一，理由见 `docs/adr/0001-非唯一索引与软删共存.md`）
- 聚合内的一级子集合用通用仓储的预加载即可（见下方 DDD 编码约定）；只有先例确认通用仓储不够用时才写自定义仓储：接口 `Domain/Repositories/`，实现 `EntityFrameworkCore/Repositories/`

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
- **聚合边界按不变量划分，不按归属感划分**：两个实体之间没有必须事务一致的不变量，却一方有全局 Id、独立用例 / DTO / 接口、被直接指认，它就该是独立的聚合根（按 Id 引用被引用方，对方身上不保留它的集合导航），接口也随之独立（自己的端点类、顶层资源、单条详情）。判断依据见 `docs/adr/0006-接入点升为聚合根.md`；反面教材是把接入点做成提供方的子实体。真正的子实体写法：继承 `FullAuditedEntity<long>`（非聚合根），工厂与意图方法 `internal`，只允许聚合根创建 / 修改；EF 用 `HasMany(集合).WithOne().HasForeignKey(外键).OnDelete(Cascade)`；用例侧把聚合根连同子集合加载（跟踪）后在聚合根上以意图方法同步子项；子项没有自己的用例 / 路由，随聚合根的请求整体提交。删除聚合根或移除子项前先把子集合加载跟踪，子实体才会跟着软删。子项之间的互不重复是聚合内部不变量，在聚合里断言、不查库——**前提是集合已加载**（只 `FindAsync` 聚合根时集合为空，不变量会漏判）。先例 `ModelProviderEndpoint → ModelProviderMapping`（映射随接入点的新建 / 更新请求整体同步）。
  **一级子集合的加载用通用仓储的预加载**：`WithDetails(x => x.Nav)` 返回带 `Include` 的 `IQueryable`，用 `ToListAsync(query, ct)` 物化（读侧先 `BeginAsNoTracking`；写侧要删子项时保持默认跟踪）。它只能表达一级导航——如果发现需要两级预加载，先回头重新评估聚合边界（多半是把独立聚合误做成了子实体），不要写自定义仓储去两级 `Include`，也不要靠「同一 DbContext 再查一次孙实体、靠 EF 关系修正挂回集合」这种隐式做法。先例 `ModelProviderEndpointCommandHandler.FindWithMappingsAsync`。
- **列表查询不加载聚合，列表 DTO 不复用详情 DTO**：列表在 Application 里按需做投影查询——`GetQueryable().Where(...).Select(x => new { 需要的列 })` 后 `ToListAsync<TSelector>`，全程 `BeginAsNoTracking`；跨表就分几次查再在内存里关联，不要把聚合连同全部子实体查回来再挑字段。列表用专门的 `XxxListDto`，界面要哪些字段就给哪些，不要把带全部子实体的详情 `XxxDto` 当列表行返回。一级预加载只给单个聚合的读取与修改用。先例 `ModelProviderQueryHandler.GetListAsync` / `ModelProviderListDto`。
- **索引/长度**：实体映射中显式声明字符串 `HasMaxLength` 与 `HasIndex`，外键列（如 `UserId`、`DepartmentId`）单独建索引。
- **循环内禁止逐行查库（N+1）**：`foreach`/`for`/`while` 循环体内不得调用仓储查询。读侧先一次性批量取出（`Contains` 条件或小表全量），再内存关联/分组/回填；写侧循环内只做内存赋值，循环外用一次 `InsertManyAsync`/`UpdateManyAsync`/`DeleteManyAsync` 批量落库。
- **存在性/数量校验用 `GetCountAsync` 下推，禁加载实体比 `Count`**：校验「一批 Id 是否都存在」时，不要 `GetListAsync(pred, "Id asc", ct)` 把匹配实体整行全量拉进内存再比 `found.Count != ids.Count`（白传所有列/所有命中行，只需要一个数字）——改用 `GetCountAsync(pred, ct)` 下推 `SELECT COUNT(*)`（软删/多租户过滤器照常生效）：`if (await repo.GetCountAsync(x => ids.Contains(x.Id), ct) != ids.Count) throw ...`。前提：`ids` 已去重、比对列为主键（无重复行），语义与 `.Count` 等价。`GetCountAsync` 在 `IReadOnlyRepository` 查询面，只读仓储即可调用。
- **Service.Open 端点入参不在方法体内手工组装**：分页参数用 `[AsParameters] PagedAndSortedResultRequest paging` 从查询串直接绑定；少量筛选参数平铺为简单参数（`string? keyword`），筛选字段多时才封装为 DTO 并以 `[AsParameters] XxxDto dto` 接收；两者都原样透传给 Query，禁止把散标量逐个列在方法签名里、再在方法体内手工 `new PagedAndSortedResultRequest { ... }` 组装。
- **分页查询直接透传分页对象**：分页参数用 `PagedAndSortedResultRequest` 对象承载，Handler 直接 `GetPagedListAsync(paging, predicate, ct)`。筛选字段少时 Query 平铺筛选参数、分页对象单独一个参数（`GetTagPagedListQuery(string? Keyword, PagedAndSortedResultRequest Paging)`，先例 `TagQueryHandler`）；筛选字段多时才建继承它的 `GetXxxPagedListDto`（先例 `ModelQueryHandler` / `ToolPackageQueryHandler`）。`Sorting` 是 System.Linq.Dynamic.Core 字符串，由前端传入，支持多字段（`"DisplayName asc, Name desc"`）；不要在 Handler 里另建一个固定排序的请求对象——字段名不合法只会在应用层解析时报错，不会进 SQL。默认顺序由前端传，不烤进后端。先例 `ModelQueryHandler` / `ToolPackageQueryHandler`。
- **更新用例 RESTful**：`id` 在路由（端点方法 `long id` 参数 + body DTO，约定路由自动生成 `PUT /Xxx/{id}`），Command 形如 `UpdateXxxCommand(long Id, XxxDto Dto)`，`UpdateXxxDto` 只含可变字段、不含 Id；不可变字段（如 `Tool.Type`）不进 UpdateDto。路由形状（动词、路径、查询串与 id 段的参数绑定、两种 400、404）由 HTTP 级测试宿主 `ServiceOpenTestHost`（起真实主服务入口 + SQLite 内存库）覆盖，先例 `ModelProviderEndpointHttpTests`。约定路由覆盖不了的路径才用 `[MinimalApiRoute]` 显式指定（先例 `SkillPackageService.UploadAsync`）——一个资源大量依赖显式路由，通常说明它不该是子资源（见上方「聚合边界按不变量划分」）。
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
