# Spec: 提供商接入点与协议（连接下沉到接入点，映射归接入点，随提供方整体保存）

> Triage: `ready-for-agent`
> 术语遵循根 `CONTEXT.md`（模型提供方、接入点、协议、提供商映射、模型）；结构决策见 `docs/adr/0005-提供商接入点与协议.md`（取代 ADR 0004 中映射归属与 ModelProviders 表结构部分）；索引非唯一与软删共存见 ADR 0001；值对象直接作 API 契约见 ADR 0003。

## Problem Statement

同一家模型供应商会同时提供多种协议的端点（例如智谱既有 OpenAI 兼容端点，也有 Claude / Anthropic 兼容端点）。但系统里一个模型提供方只能保存一份连接设置，协议创建后还不能改：想给智谱接上第二种协议，只能再建一个同名提供方，「供应商」这个分组就散了。提供商映射又直接挂在提供方上，没有地方表达「这条映射走的是哪个协议、哪个地址、哪把密钥」。

## Solution

按 ADR 0005 重构：模型提供方收敛为一个名称；连接（协议 + 地址 + 密钥 + 协议专属字段）下沉为提供方下的「接入点」，提供商映射归接入点。管理者先建提供方、进入它的详情页，再在详情页里逐个维护接入点——一个表单同时装连接信息和该接入点的全部模型映射。对外只有模型提供方一个入口：接入点与映射内嵌在提供方里整体保存。原来的「提供商类型」正名为「协议」。

## User Stories

1. 作为管理者，我只填名称就能新建模型提供方并直接进入它的详情页，以便先立起供应商、之后再配接入点。
2. 作为管理者，我在提供方详情页里给同一个提供方添加多个接入点（例如一个 OpenAI 兼容、一个 Anthropic 兼容），以便同一供应商的各种协议端点归在一处。
3. 作为管理者，新建接入点时选择协议，并填写名称、接入地址、密钥和该协议的专属字段，以便接入不同协议的模型服务。
4. 作为管理者，我在接入点表单里同时维护它的模型映射（模型、分数、可选的提供商侧模型名），以便连接和它能提供的模型一次配好。
5. 作为管理者，编辑接入点时协议只读，要换协议只能删掉重建，以便协议专属设置不会错位。
6. 作为管理者，我在同一个接入点里把同一个模型映射两次时被拒绝，以便一个接入点对一个模型只有一条映射。
7. 作为管理者，我可以把同一个模型分别映射到同一提供方的两个接入点下，以便同一个模型既能走 OpenAI 兼容、又能走 Anthropic 兼容。
8. 作为管理者，不同接入点、不同提供方可以用相同的分数映射同一个模型，因为分数只是排序依据。
9. 作为管理者，接入点的映射引用了不存在的模型时被拒绝且什么都不保存，以便映射永远指向真实存在的模型。
10. 作为管理者，同一提供方下两个接入点重名时被拒绝，不同提供方下的接入点可以重名。
11. 作为管理者，我提交的协议与设置类型对不上时被拒绝，以便保存下来的设置与协议一致。
12. 作为管理者，我不填 OpenAI 的组织标识、不填 Anthropic 的 API 版本也能保存接入点。
13. 作为管理者，我删除接入点时它的映射一并删除，之后这些模型就可以被删除。
14. 作为管理者，我可以把接入点删到零个，提供方依然存在。
15. 作为管理者，我删除提供方时它名下所有接入点和映射一并删除。
16. 作为管理者，我删除一个仍被任何接入点映射引用的模型时被拒绝，以便不留下指向不存在模型的映射。
17. 作为管理者，我在提供方详情页修改名称，同租户内重名时被拒绝；改名之后名下的接入点和映射原样保留。
18. 作为管理者，新增、编辑或删除某一个接入点后，其余接入点（包括它们的模型映射、密钥、协议专属设置）原样保留。
19. 作为管理者，列表页里每个提供方一行，能看到它用了哪些协议、支持哪些模型、有几个接入点，以便不进详情就能了解概况。
20. 作为管理者，我看不到别的租户的提供方、接入点和映射。
21. 作为前端开发者，我执行 `pnpm gen:api` 后拿到的函数覆盖全部端点，以便不手写 URL。
22. 作为后端开发者，协议专属设置值对象位于 Domain.Shared 并直接作为 API 契约，以便不维护 DTO 镜像与映射器。
23. 作为后端开发者，列表接口只返回列表页要用的字段、查询只取需要的列，以便数据量上来后数据库不被整棵聚合拖垮。
24. 作为未来的实现 agent，我拿到本 spec 即可独立完成全部实现（决策均已定稿），不必再做架构裁决。

## Implementation Decisions

### 后端

- **枚举**：`ModelProviderType` 改名 `ModelProviderProtocol`，取值 `OpenAI = 1`、`Anthropic = 2`，数值不变（它同时是 JSON 判别值）。
- **值对象**（Domain.Shared，直接作为 API 契约，沿 ADR 0003）：
  - `ModelProviderSetting` 族改名 `ModelProviderEndpointSetting` / `OpenAIModelProviderEndpointSetting` / `AnthropicModelProviderEndpointSetting`。
  - 基类去掉 `BaseUrl`、`ApiKey`，只剩协议专属字段：`Organization`（OpenAI）、`Version`（Anthropic），均为可空字符串（`string?`），没填即 null。
  - 基类上不放「当前是哪个协议」的投影属性：它是哪个子类就已经说明了协议，需要判断时用类型模式匹配（实施时曾有一版 `[JsonIgnore]` 的 `Protocol` 投影，用户 review 后删除）。
  - 判别键仍为 `type`、取值仍为枚举数值；Swagger 多态装配沿用项目既有的通用反射装配与判别键 SchemaFilter，不新增装配代码。
- **实体**（Domain；聚合为三层：`ModelProvider → ModelProviderEndpoint → ModelProviderMapping`）：
  - `ModelProvider`（租户级聚合根）：`Name`、`Endpoints`（私有 set）。严格 DDD：私有无参构造、静态工厂 `Create(name)`、意图方法 `Rename(name)` 与 `SyncEndpoints(...)`（按提交的接入点整体同步，以 Id 为键：名下已有的更新、Id 为 0 的新增、未提交的移除；映射随各自的接入点同步）。聚合不变量在 `SyncEndpoints` 里断言：同一接入点只提交一次、接入点名称互不重复、带 Id 的必须是名下已有的接入点、协议创建后不可变。
  - `ModelProviderEndpoint`（`FullAuditedEntity<long>` + `IMultiTenant`，聚合内子实体）：`ModelProviderId`（私有 set，外键，级联）、`Name`（≤ 128）、`Protocol`（私有 set，创建后不可变）、`BaseUrl`、`ApiKey`、`Setting`（JSON 列，多态值对象）、`Mappings`（私有 set）。工厂与意图方法 `internal`：`Create(...)`、`Update(name, baseUrl, apiKey, setting)`、`SyncMappings(...)`（一个接入点对一个模型只映射一条在此断言）。
  - `ModelProviderMapping`：外键由 `ModelProviderId` 改为 `ModelProviderEndpointId`，其余字段（`ModelId`、`Priority`、`ProviderModelName`）不变。
  - **实体 / 工厂 / 意图方法内不写任何单字段基础校验**（非空、长度、枚举合法性只在 FluentValidation）；唯一性中需要查库的（提供方名称同租户唯一）与引用存在性（映射的模型存在）在 Handler。
- **仓储**：
  - 自定义仓储 `IModelProviderRepository : IRepository<ModelProvider, long>`（接口在 `Domain/Repositories/`），声明 `FindWithEndpointsAsync(id, asNoTracking, ct)`；实现 `ModelProviderRepository`（在 `EntityFrameworkCore/Repositories/`）继承 `EfCoreRepository`，用两级 `Include` 把「提供方 → 接入点 → 映射」一次查全，标注 `IScopedDependency` 由约定注册接管默认仓储。原因：通用仓储的 `WithDetails` 只能预加载一级导航；不用「同一 DbContext 再查一次映射、靠 EF 关系修正挂回集合」的隐式做法。
  - 这个「连同子集合加载」只用于单个聚合的读取与修改（详情、更新、删除）。**列表不加载聚合**：在 Application 里分四次投影查询，每次只取需要的列（提供方 Id / Name / UpdatedAt → 接入点 Id / 提供方 Id / 协议 → 映射 接入点 Id / 模型 Id / 分数 → 模型 Id / 显示名称），全程 `BeginAsNoTracking`，再在内存里按提供方拼成每行。
- **用例**（Application；一类型一文件，Handler 以 `Handler` 结尾）：
  - `ModelProviders/`：`GetList`（列表页用，返回列表 DTO）、`Get`（详情页用，带接入点与映射）、`Create`（仅名称）、`Update`（名称 + 全部接入点）、`Delete`。接入点与映射都没有独立用例。
  - `Update` / `Delete` 经 `FindWithEndpointsAsync` 加载聚合（不存在抛「模型提供方不存在」，不区分「不属于本租户」）；`Update` 先 `Rename` 再 `SyncEndpoints`。
  - 跨聚合校验收敛为一个私有 `ValidateAsync`，创建 / 更新共用：名称同租户唯一；各接入点映射引用的模型必须全部存在于平台目录（跨全部接入点一次 COUNT 核对去重后的引用数）。
- **端点**（Service.Open，只有提供方一个端点类，约定路由，前缀 `api/v1`）：`GET /ModelProviders/List`、`GET /ModelProviders/{id}`、`POST /ModelProviders`、`PUT /ModelProviders/{id}`、`DELETE /ModelProviders/{id}`。没有接入点的子资源路由。
- **DTO**（Application.Contracts，全部在 `ModelProviders/Dtos/`）：
  - 列表专用 `ModelProviderListDto`：`Id`、`Name`、`Protocols`（名下接入点协议去重、按枚举顺序）、`ModelDisplayNames`（跨接入点按模型去重，分数高的在前）、`EndpointCount`、`UpdatedAt`。界面要什么给什么，不复用详情 DTO。
  - 详情 `ModelProviderDto`：`Id` + 审计字段 + `Name` + `Endpoints: List<ModelProviderEndpointDto>`；`ModelProviderEndpointDto`：`Id`、`Name`、`Protocol`、`BaseUrl`、`ApiKey`、`Setting`、`Mappings`。
  - 输入：`CreateModelProviderDto`（`Name`）；`UpdateModelProviderDto`（`Name` + `Endpoints: List<SaveModelProviderEndpointDto>`，提交的是期望的完整状态，没提交的接入点会被删除）；`SaveModelProviderEndpointDto`（`Id` 为 0 表示新建，`Name`、`Protocol`、`BaseUrl`、`ApiKey`、`Setting`、`Mappings`——已有的接入点提交时必须带现有协议）。
  - 映射 DTO：输出 `ModelProviderMappingDto`（`ModelId`、`ModelDisplayName`、`Priority`、`ProviderModelName`），输入 `SaveModelProviderMappingDto`。
  - 查询侧实体 → DTO 手写映射（含多态值对象，Mapster `Adapt` 无法实例化抽象基类，同工具域先例）；详情里接入点按名称排序、接入点下的映射按分数降序；模型显示名称批量回填，不逐行查库。
  - 缓存层的 `ModelProviderCacheDto` 无人引用，直接删除。
- **Validator**（FluentValidation，类级 `[AutoValidation]`）：
  - 提供方：`Name` 必填且 ≤ 128；更新时 `Endpoints` 非空集合。
  - 接入点（`SaveModelProviderEndpointDtoValidator`）：`Name` 必填且 ≤ 128；`BaseUrl`、`ApiKey` 必填；`Protocol` 在枚举范围内；`Setting` 非空且其子类型必须与 `Protocol` 一致（子类型由判别键决定）；`Organization` / `Version` 可选、不限长度；`Mappings` 非空集合。
  - 映射项：`ModelId > 0`，`ProviderModelName` 可省略（省略 / 空白由聚合归一为空字符串）且 ≤ 128。
  - body 字符串去空白由框架的 `TrimStringJsonConverter` 负责，Validator / Handler / 实体里都不写 `Trim()`。
- **EF 与迁移**：
  - `ModelProviders` 表去掉设置列；新增 `ModelProviderEndpoints` 表（`Protocol` 枚举列、`Name` ≤ 128、`Setting` json 列、`TenantId` 与 `ModelProviderId` 各建普通索引）；`ModelProviderMappings` 的外键列由指向提供方改为指向接入点，索引随之改名。全部索引非唯一（沿 ADR 0001）。关系：提供方 `HasMany(Endpoints)` 级联；接入点 `HasMany(Mappings)` 级联。
  - 在现有迁移之后**新增一个 EF 迁移**（`ProviderEndpointAndProtocol`），不回退重建、不写数据迁移。**应用该迁移前必须先清空 `ModelProviderMappings` 与 `ModelProviders`（含已软删的行）**：迁移是把映射外键列改名而非重建，存量映射行指向不存在的接入点，加外键时会失败。
  - 三层聚合在仓库里是首例：落地第一步先在既有实体映射冒烟测试里验证孙实体的行为——随聚合保存时自动生成雪花 Id、写入 TenantId、外键回填；从集合移除后在保存前转软删；删提供方时接入点与映射一并软删；自定义仓储的两级 Include 能把两层子集合一次查全。
- **文档同步**：后端 AGENTS.md 写入「聚合内子实体没有独立用例与端点」「多级子集合的加载走自定义仓储」「列表查询不加载聚合、列表 DTO 不复用详情 DTO」三条口径。

### 前端

- **路由**（nav 由 meta 推导）：`/model-providers`（「模型提供方」，nav，列表页）与 `/model-providers/:id`（详情页，不进 nav）。
- **列表页**：
  - 「新建模型提供方」对话框只有一个名称字段，保存成功后跳转到新提供方的详情页。
  - 表格每行一个提供方（数据来自列表 DTO）：名称（可点击进入详情）/ 协议（`protocols` 徽标）/ 支持的模型（`modelDisplayNames` 徽标）/ 接入点（`endpointCount`）/ 更新时间 / 操作（仅删除）。删除确认提示「名下全部接入点与映射会一并删除」。
- **详情页**：
  - 头部为提供方名称 + 「编辑名称」（对话框仅名称，同租户重名由后端拒绝并 toast）。
  - 接入点表格：名称 / 协议 / 接入地址 / 支持的模型 / 操作（编辑、删除）；右上角「新建接入点」；无接入点时显示空态。删除接入点用 `AlertDialog`；允许删到零个。
- **接入点表单**（右侧抽屉，新建 / 编辑共用）：
  - 字段顺序：名称、协议（新建为下拉，编辑为只读文本）、接入地址、密钥、当前协议的专属字段（可空）；下方「模型映射」行：模型下拉 / 分数 / 提供商侧模型名 / 移除，可继续添加。映射行用稳定的 `rowKey` 作 `v-for` key 与校验错误键。
  - 新建态切换协议时，若专属字段已有输入，先 `AlertDialog` 确认再清空专属字段（沿工具形态切换的既有先例）；名称、地址、密钥、映射保留。
- **整体提交**：接入点随提供方保存，没有接入点自己的接口。抽屉保存 = `PUT /ModelProviders/{id}`，请求体是「提供方名称 + 全部接入点」：没改的接入点原样转回输入，只替换或追加当前这一条；删除接入点 = 带上其余接入点、不带它；改名 = 新名称 + 全部接入点原样。由纯函数 `providerUpdateBody` / `endpointToSaveDto` / `upsertEndpoint` / `removeEndpoint` 负责拼装。
- **纯函数层**：`provider-form.ts`（名称表单 + 整体提交的拼装）、`endpoint-form.ts`（接入点表单状态 ↔ 输入项、按协议组装 `setting`、前端自身校验）、`provider-summary.ts`（详情页接入点的模型显示名称）；列表页的协议 / 模型 / 数量由后端汇总，前端不计算。并排多栏表单行必须加 `items-start`。
- **API 层**：后端跑起后执行 `pnpm gen:api` 生成 `src/api`，组件只从 `@/api` 导入生成函数与类型，不手写 URL。
- **组件引入**：优先复用已引入的 table / dialog / alert-dialog / sheet / select / badge / input / label，缺什么再 `shadcn-vue add`。

## Testing Decisions

- **测试接缝**（沿用既有接缝，不新增）：
  - **后端主接缝**：从发布 Command / Query（经 `ILocalEventBus`）这一最高点进入，使用既有的 SQLite 内存测试宿主，只测外部行为（返回的 DTO、抛出的业务异常、随后查询到的状态），不测实体内部方法。
  - **EF 行为接缝**：既有的实体映射冒烟测试，用于三层聚合的孙实体 Id / 租户 / 软删行为，以及自定义仓储的两级加载。
  - **前端**：纯函数接缝（vitest）+ 浏览器走查兜底，不写组件测试。
- **好的测试**：只断言可观察行为——保存了什么、读出来是什么、什么被拒绝；不依赖实体私有方法名、不依赖 SQL。
- **后端用例清单**（`ModelProviderTests`，接入点的增删改都经提供方的更新用例提交完整状态）：
  - 提供方：只填名称创建 → 详情返回空接入点列表；改名后接入点保留；同租户重名拒绝、跨租户同名放行；更新 / 删除不存在的提供方被拒；删除后详情为空。
  - 列表：按名称排序；每个提供方的协议（去重、按枚举顺序）、支持的模型（跨接入点去重、分数高的在前）、接入点数正确。
  - 接入点：同一提供方下建两个不同协议的接入点并读回（地址、密钥在接入点上，专属设置子类正确、未填时为 null）；编辑不改协议、协议变更被拒且不改任何东西；带未知 Id 被拒；接入点重名拒绝且不保存（含同一次提交内重名，整次提交被拒）；跨提供方重名放行；删到零个后提供方仍在；提交时未带的接入点被删除且其映射一并删除。
  - 映射：同一接入点重复映射同一模型拒绝且不保存；同一模型挂在同一提供方的两个接入点下放行；相同分数放行；引用不存在的模型拒绝且不保存；更新接入点时映射整体同步（更新 / 新增 / 移除）。
  - 级联：删接入点后其映射不再阻止模型删除；删提供方后模型可删；删被引用的模型被拒（本租户与跨租户）。
  - 隔离：租户 A 的提供方与接入点 id 在租户 B 下更新 / 删除一律按「提供方不存在」被拒。
- **前端**：路由表用例（nav 仍为八项、详情路由不进 nav）；接入点表单纯函数（按协议组装 `setting`、未填专属字段为 null、映射行转提交项、前端校验）；整体提交拼装（没改的接入点原样转回、新增追加、编辑替换、删除不带、更新请求体 = 名称 + 全部接入点）。
- **验收**：`dotnet build` 0 警告、`dotnet test` 全绿；`pnpm gen:api` 后 `pnpm build`（含 `vue-tsc`）与 `pnpm test` 通过；真实 HTTP 冒烟与浏览器走查：新建提供方 → 跳详情 → 新建两个不同协议的接入点 → 编辑其中一个（协议只读、其余接入点与映射原样）→ 删到零 → 列表三列正确 → 改名后接入点仍在 → 删除提供方。

## Out of Scope

- 调用路由的运行时执行链路（对话 / Agent 侧按「分数最高的映射」实际发起模型请求）；同分如何取舍。
- 新增协议（Gemini、Azure OpenAI 等）及其专属字段。
- 接入点连通性测试（test connection）、从供应商 API 拉取模型清单。
- `ApiKey` 读取侧脱敏与「留空表示不修改」（值对象直出契约的既定取舍，另立提案）。
- 缓存层的提供方缓存结构（随本 spec 删除，运行时路由再做时重建）。
- 模型目录、标签、权限体系的任何改动。

## Further Notes

- **发布通道**：本仓库的 issue tracker 与 triage 标签词表仍未配置（`docs/agents/` 不存在、本机无 gh CLI），本 spec 沿前例以仓库内 markdown 落盘并标注 `ready-for-agent`，tracker 就绪后再搬运。
- **契约纪律**：`ModelProviderEndpointSetting` 值对象字段 = API 承诺形状，改字段属破坏性接口变更；派生属性需显式 `[JsonIgnore]`（沿 ADR 0003 后果条款）。
- **环境事实**：`.env.development` 的 `VITE_API_BASE_URL` 指向 https 端口，后端须以 `https` launch profile 启动；用户常自己开着 Vite 与后端，在跑就直接用，不另起、不杀。
- **验证链**：build + `dotnet test` + 抓 swagger + `pnpm gen:api` 后看 `src/api` diff + `pnpm typecheck` 一趟过；SQLite 测试不能替代 MySQL 行为验证。
- **实施结果（2026-11-26，未提交待 review）**：
  - 后端 `dotnet build` 0 警告、`dotnet test` 146/146；前端 `vue-tsc`、vitest 40/40、`pnpm build` 通过；真实 HTTP 冒烟（dev MySQL）与浏览器走查（两轮）通过。
  - 迁移 `20261126071016_ProviderEndpointAndProtocol` 已应用到 dev MySQL（应用前硬删了 3 条旧映射、6 个旧提供方）。`dotnet ef database update --no-build` 会用旧的启动程序集误报「已是最新」，应用新迁移不要带 `--no-build`。
  - **用户 review 调整（接口形态）**：第一版把接入点做成挂在提供方下的子资源（读走提供方 DTO、写走 `POST/PUT/DELETE .../Endpoints`），用户否决「不伦不类」——合并进提供方，接入点随提供方整体提交，删除了接入点的端点类、三个 Command、独立 Handler 与两个输入 DTO。
  - **用户 review 调整（聚合加载）**：第一版在 Application 层写静态加载器（带接入点查聚合根后，在同一 DbContext 再查映射、靠 EF 关系修正挂回集合），用户否决「太抽象」——改为自定义仓储 `IModelProviderRepository` / `ModelProviderRepository`，两级 `Include` 显式加载。
  - **用户 review 调整（列表）**：第一版列表也用仓储把全部提供方连同子实体查回来、再复用详情 `ModelProviderDto`，用户否决「数据库扛不住 / 系统容易被搞死」——列表改为 Application 里四次投影查询只取需要的列，新增列表专用 `ModelProviderListDto`。
  - **用户 review 调整（值对象）**：删掉基类上 `[JsonIgnore]` 的 `Protocol` 投影属性，设置与协议是否匹配改用类型模式匹配。
  - **聚合不变量依赖集合已加载**：只 `FindAsync` 聚合根不加载接入点时「接入点重名」会漏判（测试实际抓到），所以更新 / 删除一律走 `FindWithEndpointsAsync`。
  - 浏览器走查时标签页处于隐藏状态，原生点击与截图不可用，走查由页面内脚本派发事件完成；两栏表单对齐用 `getBoundingClientRect` 测量确认，**没有截图留档**。
  - code review 后的处理：删除两份误写到无 csproj 目录的重复 Validator；过期的注释 / 先例指向 / spec 0004 修订注记已补；临时下载的框架文档已清理，不入库。
