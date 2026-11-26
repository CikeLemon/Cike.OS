# Spec: 模型 / 模型提供方 增删改查（前后端，模型映射随提供方维护）

> Triage: `ready-for-agent`
> 术语遵循根 `CONTEXT.md`（模型、模型提供方、提供商映射、标签）；结构决策见 `docs/adr/0004-模型目录平台化与提供商映射解耦.md`；索引非唯一与软删共存见 ADR 0001；值对象直接作 API 契约见 ADR 0003。

## Problem Statement

领域层现有的 `ModelEntity` 挂在 `ModelProviderId` 下且无唯一约束：同一市面模型被多家提供商（官方与聚合平台）接入时会产生多条同名条目，界面无法只按模型展示，使用者也被迫理解「提供商」这一运维概念。模型与提供商的耦合让目录无法平台化共享，路由语义（谁、以什么优先级、用哪个提供商侧模型名提供该模型）没有落库位置。Application / Contracts / Service.Open 三层没有任何模型域用例与端点，前端 `/models` 与 `/model-providers` 均为占位页。

## Solution

按 ADR 0004 重构模型域：模型升为平台级目录聚合（无租户、名称全局唯一、新增描述），标签随之平台化；模型提供方维持租户级聚合，其多态设置值对象按 ADR 0003 直接作为 API 契约；「谁提供这个模型」用模型提供方聚合内的子实体「提供商映射」表达（模型、路由分数、提供商侧模型名），映射随提供方整体维护，没有独立的用例与端点。后端为模型与提供方补齐增删改查，前端把侧栏拆为「模型」与「模型提供方」两个一级菜单：模型列表只展示目录，映射在提供方表单里维护，对话/Agent 侧的模型选择未来只需查目录表。

## User Stories

1. 作为管理者，我在「模型」菜单里能按关键词搜索并分页浏览市面模型目录（显示名称、模型名称、标签、描述），以便目录增长后仍能快速定位。
2. 作为管理者，我能新建模型条目（模型名称、显示名称、可选描述、标签多选），成功后留在列表页并看到新条目。
3. 作为管理者，我能编辑模型的可变字段（显示名称 / 描述 / 标签）并删除它；删除时若任何租户还有映射挂在它上面则被拒绝并看到明确提示，以便不产生指向不存在模型的映射。
4. 作为管理者，我用重复的模型名称创建、或用重复的显示名称创建 / 改名模型时被拒绝（跨租户全局），以便目录条目永远可区分。
5. 作为管理者，两个不同租户看到的是同一份模型目录，以便市面模型不需要逐租户重复配置。
6. 作为管理者，我在「模型提供方」里能看到每个提供方映射了哪些模型及各自的分数，以便一眼看出一个提供方能提供什么。
7. 作为管理者，新建 / 编辑提供方时，我可以在同一个表单里添加、修改、移除它的模型映射（每行：模型下拉、分数、可选的提供商侧模型名），以便聚合平台的改名模型也能接入。
8. 作为管理者，我在同一个提供方下把同一个模型映射两次时被拒绝，以便一个提供方对一个模型只有一条映射。
9. 作为管理者，不同提供方可以用相同的分数映射同一个模型，因为分数只是排序依据。
10. 作为管理者，删除提供方时它名下的映射一并删除，以便不留下指向不存在提供方的映射。
11. 作为管理者，我在「模型提供方」菜单里能新建 / 编辑 / 删除本租户的提供商，配置名称与多态连接设置（OpenAI：BaseUrl / ApiKey / Organization；Anthropic：BaseUrl / ApiKey / Version）。
12. 作为管理者，同租户内提供商重名时被拒绝；提供商类型创建后不可变，切换类型需删除重建，以便设置族不会错位。
13. 作为管理者，提供方的映射引用了不存在的模型时被拒绝，以便映射永远指向真实存在的模型。
14. 作为管理者，我在模型表单里能从平台标签列表中多选标签，以便目录条目可被分类标注。
15. 作为前端开发者，我拿到按本 spec 生成的 `src/api` 函数就能调用全部端点，以便不手写 URL。
16. 作为后端开发者，模型域值对象 `ModelProviderSetting` 位于 Domain.Shared 并直接作为 API 契约，以便不维护 DTO 镜像与映射器。
17. 作为未来的实现 agent，我拿到本 spec 即可独立完成全部实现（决策均已定稿），不必再做架构裁决。

## Implementation Decisions

### 后端

- **实体**（严格 DDD 风格：聚合根 `private set` + 静态工厂 `Create(...)` + 意图方法；格式/范围/枚举合法性只在 FluentValidation，唯一性/引用存在在 Handler）：
  - `ModelEntity` 升为 `FullAuditedAggregateRoot<long>`，**去掉 `IMultiTenant` 与 `ModelProviderId`**，新增 `Description`（可选，默认空字符串，≤ 2048）；`Name`（模型名称，≤ 128，创建后不可变）、`DisplayName`（显示名称，≤ 128）、`TagIds`（JSON 列，既有转换器）。可编辑字段为 DisplayName / Description / TagIds（Name 不进 UpdateDto）。
  - `ModelProviderMapping`（新，`FullAuditedEntity<long>` + `IMultiTenant`，**`ModelProvider` 聚合内的子实体**）：`ModelProviderId`（外键，随聚合级联）、`ModelId`、`Priority`（路由分数，int，不要求唯一）、`ProviderModelName`（非空字符串列，空字符串表示回落模型名称，≤ 128）。工厂与意图方法 `internal`，只能经 `ModelProvider.SyncMappings(...)` 维护；没有独立的 Command / Query / 端点。
  - `ModelProvider` 聚合增加 `Mappings` 集合与 `SyncMappings`（以模型为键整体同步：已映射的更新、新的新增、未提交的移除；同一模型出现两次是聚合内部不变量，在此断言）。其余维持既有风格（公共 setter + 对象初始化）。
  - `Tag` 去掉 `IMultiTenant`，字段不动。
- **值对象契约**：`ModelProviderSetting` 值对象族（含 `OpenAIModelProviderSetting` / `AnthropicModelProviderSetting`）从 Domain 迁至 **Domain.Shared**，直接作为 API 契约，不建 `*Dto` 镜像（沿 ADR 0003）；判别键 `type` 为 `ModelProviderType` 数值；Swagger 多态装配沿用既有 `ToolSettingDiscriminatorSchemaFilter` 同款模式（子类型与 int 判别显式声明）；JSON 乱序由项目统一 `JsonHelper` 选项（`AllowOutOfOrderMetadataProperties`）兜底，不写重排逻辑。STJ 序列化多态值对象必须按基类声明。
- **用例目录**（`Application/`，均含 `Commands/`、`Queries/`、`Validators/`，一类型一文件，Handler 以 `Handler` 结尾）：
  - `Models/`：`GetPagedList`（Keyword，按 DisplayName 排序）、`Get`、`Create`、`Update`、`Delete`。
  - `ModelProviders/`：`GetList`（本租户全量，按 Name 排序，不分页，带映射）、`Get`、`Create`、`Update`、`Delete`（映射随聚合进出，无独立用例）。
  - `Tags/`：仅 `GetList`（只读，平台全量，按 Key 排序）。
- **端点**（`Service.Open/Services/`，约定路由，前缀 `api/v1`）：三实体均为 `GET /Xxx/List` 或 `GET /Xxx/PagedList`、`GET /Xxx/{id}`、`POST`、`PUT`、`DELETE /Xxx/{id}` 的既有形态；更新用例 id 在路由，`UpdateXxxCommand(long Id, XxxDto Dto)`。
- **DTO**（`Application.Contracts/<聚合>/Dtos/`）：
  - 模型：`ModelDto`（含 TagIds）、`GetModelPagedListDto : PagedAndSortedResultRequest`（Keyword）、`CreateModelDto`、`UpdateModelDto`。
  - 提供商：`ModelProviderDto`（Name、Setting 多态、Mappings）、`CreateModelProviderDto` / `UpdateModelProviderDto`（Name、Setting、Mappings；Type 不可变故不在 UpdateDto 中）。
  - 映射（内嵌在提供方 DTO 里）：输出 `ModelProviderMappingDto`（ModelId、ModelDisplayName 由查询侧批量回填、Priority、ProviderModelName），输入 `SaveModelProviderMappingDto`（ModelId、Priority、ProviderModelName?）。
  - 标签：`TagDto`（只读）。
- **Validator**（FluentValidation，类级 `[AutoValidation]`）：模型 Name / DisplayName 必填与长度，Description 可选（空串放行）仅限长度；提供商 Name 必填 ≤ 128、Setting 的 BaseUrl / ApiKey 必填且判别类型在枚举范围内、子类专属字段（Organization / Version）必填；映射项 ProviderModelName 可省略（省略 / 空白归一为空字符串）但长度 ≤ 128、ModelId > 0。
- **写侧规则**（Handler 内，软删过滤生效，并发窗口按 ADR 0001 接受）：
  - 模型 Name 全局唯一、DisplayName 全局唯一：`GetCountAsync(x => (x.Name == name || x.DisplayName == displayName) && x.Id != id)` 抛 `UserFriendlyException`。
  - 提供商 Name 同租户唯一。
  - 映射：模型存在（平台实体，无租户过滤）；一个提供方对一个模型只映射一条（聚合内部不变量，`SyncMappings` 断言，不查库）。分数不做唯一。
  - 模型 TagIds 中每个标签 id 必须存在。
  - 级联：删模型 → 全库映射计数 > 0 拒绝；删提供商 → 连同映射集合加载后整体删除（子实体随之软删）。
- **EF 与迁移**：`Models` 表去 TenantId / ModelProviderId 列、加 Description；`Tags` 去 TenantId；新增 `ModelProviderMappings` 表（外键指向 ModelProviders，TenantId / ModelId 各建普通索引，全部非唯一，沿 ADR 0001）；`ModelProviders` 不变。平台实体不实现 `IMultiTenant`，不参与租户过滤。库中无生产数据，dev 存量 Models / Tags 数据在实施时直接清理重录。既有 JSON 值转换属性不配 ValueComparer 的已知跟进项继续沿用现状。
- **实体写法**：新聚合（ModelEntity）与子实体（ModelProviderMapping）按严格 DDD 风格写；ModelProvider 用例沿用既有风格（Handler 内对象初始化 + 仓储），映射经 `SyncMappings` 整体同步；查询侧 `IReadOnlyRepository` + `BeginAsNoTracking()`，实体 → DTO 用 Mapster `Adapt`，无秒数等非平凡换算故无额外 TypeAdapterConfig。

### 前端

- **路由与菜单**（`src/router/routes.ts`，nav 由 meta 推导）：新增 `/models`（「模型」，nav）、`/model-providers`（「模型提供方」，nav）。模型**没有详情页**：映射归到提供方后，详情页能展示的内容列表行上都有。侧栏顺序：对话 / Agent / 模型 / 模型提供方 / 工具包 / 工具包分类 / 标签 / 设置。`nav-items.test.ts` 的真实路由表用例同步改为八项。
- **页面**（`src/pages/`，沿 spec 0003 确立的「工具条 + 内容区 + 空态 / 骨架屏」骨架参照，不抽共享组件）：
  - `ModelsPage.vue`：工具条（keyword 输入、新建按钮）+ 表格（显示名称 / 模型名称 / 标签 badges / 描述截断 / 操作：编辑、删除）+ 分页；新建 / 编辑成功后刷新列表，行不可点击跳转。
  - `ModelProvidersPage.vue`：新建按钮 + 表格（名称 / 类型 badge / 模型映射 badges「显示名称 · 分数」/ 接入地址 / 更新时间 / 操作：编辑、删除），本租户全量不分页。
- **业务组件**（`src/components/`）：`models/ModelFormDialog.vue`（新建 / 编辑共用：模型名称【第一位，编辑态禁用】、显示名称、可选描述 textarea、标签多选）、`model-providers/ModelProviderFormDialog.vue`（名称 + 类型 RadioGroup + 设置区按类型切换字段 + 模型映射行：模型下拉 / 分数 / 提供商侧模型名 / 移除）。
- **交互细则**：
  - 提供商类型 RadioGroup 切换时若设置区已有内容先 `AlertDialog` 确认，确认后清空设置区（沿工具形态切换先例）；类型创建后不可变，编辑态类型只读展示。
  - 模型表单标签多选用既有组件能力（checkbox 组或 select），不为此引入新组件。
  - 所有删除确认用 `AlertDialog`；错误统一 `toast.error(getErrorMessage(error))`；类型 badge 映射为 const 对象 + 字面量联合类型（`erasableSyntaxOnly` 禁 enum），颜色只用既有语义 token。
- **API 层**：后端跑起后执行 `pnpm gen:api` 生成 `src/api`，组件只从 `@/api` 导入生成函数与类型，不手写 URL。
- **组件引入**：优先复用已引入的 table / dialog / alert-dialog / select / badge / radio-group / pagination / input / label / textarea，缺什么再 `shadcn-vue add`。

## Testing Decisions

- **后端**（沿用 `Cike.OS.Tests` 的 SQLite 内存测试宿主，从发布 Command / Query 这一最高点进入，只测外部行为）：
  - 实体各一组：创建 / 读取 / 更新 / 删除 / 列表与筛选（模型分页 Keyword、提供商全量带映射）。
  - 写侧规则：模型 Name 与 DisplayName 重复拒绝（**含跨租户重复**，验证平台级唯一）；提供商重名拒绝；同一提供方重复映射同一模型拒绝、引用不存在模型拒绝、相同分数（同提供方不同模型 / 不同提供方同一模型）放行；删模型有映射拒绝（本租户与跨租户）、删提供商后其模型可删、更新提供方时映射整体同步（更新 / 新增 / 移除）；模型 TagIds 引用不存在的标签拒绝。
  - Validator：Description 可选（空串放行）与长度上限、ProviderModelName 超长拒绝、类型与 Setting 不匹配拒绝。
  - 跨租户可见性：租户 A 建的模型，租户 B 的列表可见且不可重复创建同名。
  - 回归：现有全部测试通过（含既有实体映射冒烟测试随实体变更同步修正）。
- **前端**（沿用纯函数接缝 + 走查兜底）：`nav-items.test.ts` 菜单断言改八项；实现中抽出的纯函数（类型 badge 映射、映射排序等）各配 vitest；不写组件测试。
- **验收**：`dotnet build` 0 警告、`dotnet test` 全绿；`pnpm gen:api` 后 `pnpm build`（含 `vue-tsc`）与 `pnpm test` 通过；浏览器走查用户路径：模型目录管理（列表内建 / 改 / 删，无详情页）、在提供方表单里维护映射（建 → 重复模型被拒 → 成功 → 编辑同步 → 移除）、跨实体删除保护（删有映射的模型被拒、删提供商后模型可删）。

## Out of Scope

- 调用路由的运行时执行链路（对话 / Agent 侧按「最高优先级映射」实际发起 LLM 请求）。
- 基于点赞 / 踩 / 评测分数的自动选路。
- 模型目录与标签的权限收口、平台管理员角色（权限体系另立提案）。
- 标签管理界面与标签写侧用例（本 spec 只做标签实体平台化迁移与只读列表端点；标签 CRUD 另立 spec）。
- 提供商连通性测试（test connection）。
- 从提供商 API 拉取模型清单自动建目录。
- 模型能力元数据（上下文长度、模态、计价）。
- 登录态、权限守卫、移动端适配。

## Further Notes

- **发布通道**：本仓库 issue tracker 与 triage 标签词表尚未配置（`docs/agents/issue-tracker.md` 不存在、本机无 gh CLI），本 spec 先以仓库内 markdown 落盘并标注 `ready-for-agent`，tracker 就绪后再搬运。
- **实施顺序**：1. 后端：实体与值对象迁移（Domain.Shared）→ EF 映射与迁移 → 三实体用例 + Validator + 端点 → 标签只读端点 → 测试。2. 前端：起后端 → `pnpm gen:api` → 路由与三页面 → 表单对话框 → 纯函数测试。3. 联调走查。
- **契约纪律**：`ModelProviderSetting` 值对象字段 = API 承诺形状，改字段属破坏性接口变更；派生属性需显式 `[JsonIgnore]`（沿 ADR 0003 后果条款）。
- **实施期待核实**（读源码后定，不是待裁决项）：Cike 框架对非 `IMultiTenant` 实体在多租户 DbContext 下的过滤行为；`FullAuditedAggregateRoot` 在 Domain 层对 `ModelEntity` 现有冒烟测试数据构造方式的影响。
- **实施结果与偏离（2026-11-25）**：
  - 核实结论：非 `IMultiTenant` 实体不挂租户过滤器；跨租户映射计数用 `IDataFilter.Disable<IMultiTenant>()`；测试里用 `ICurrentTenant.Change(id)` 切租户。
  - `ModelProviderSetting.Type` 标 `[JsonIgnore]` 并由子类构造函数赋值——Web 的 camelCase 会让 `Type` 变成 `type` 与判别键撞名（STJ 抛异常、swagger 500）；DB 存量 `"type":N` 判别键与 wire 形状不变。
  - 提供商查询侧不用 Mapster `Adapt`（无法实例化抽象多态基类），改为手写 `ToDto`，与工具域先例一致；模型 / 标签查询仍走 `Adapt`。
  - 迁移合并为单个 `ModelPlatformCatalogAndProviderMapping`（Models 去 TenantId / ModelProviderId、加 Description / ConcurrencyStamp；Tags 去 TenantId；新建 ModelProviderMappings 表，ProviderModelName NOT NULL），随下方每一轮结构调整重建一次（当前为 `20261126040120`），已应用到 dev MySQL。
  - 顺带修复既有缺陷：`main.ts` 补 `import 'vue-sonner/style.css'`（此前所有 toast 无样式、渲染在视口外）。
  - 环境事实：`.env.development` 的 `VITE_API_BASE_URL` 指向 https 7244，后端须以 `https` launch profile 启动，否则前端全部请求 `Failed to fetch`。
  - 用户 review 调整：`ProviderModelName` 由可空改为非空字符串列（数据库字段永远不为 null），空字符串表示回落模型名称；输入 DTO 仍接受省略 / null，由聚合归一为空字符串。
  - 用户 review 调整（表单）：「技术名 / 显示名」文案统一改为「模型名称 / 显示名称」；描述改为可选（默认空字符串）；模型名称创建后不可变（`UpdateModelDto` 不含 Name，表单第一位且编辑态禁用），模型名称唯一性只在创建时校验。
  - 用户 review 调整（实体去校验）：实体 / 工厂里不再写非空、范围断言，只留赋值与集合内部不变量；入参去空白由框架自带的 `TrimStringJsonConverter`（`Cike.AspNetCore.MinimalAPIs` 自动注册到 `HttpJsonOptions`）处理，Handler 里不再散写 `Trim()`（查询串参数也不手动 Trim，空格可能是搜索词的一部分）。
  - 用户 review 调整（Handler）：跨聚合校验收敛为单个 `ValidateAsync`，同表多个判断合并为一次查询。
  - 用户 review 调整（分页排序）：`GetPagedListAsync(dto, ...)` 直接透传，`Sorting` 由前端传 Dynamic LINQ 字符串（支持多字段），不再在 Handler 里写死排序。
  - **用户 review 调整（2026-11-26，映射归属）**：映射不应出现在模型侧，它是 `ModelProvider` 聚合内的子实体而非聚合根——删除映射的独立用例 / 端点 / 前端对话框，映射内嵌在提供方 DTO 里随提供方整体提交与返回，在提供方表单里维护；`Priority` 改为纯分数（不做唯一，同分合法），路由时按分数排序取第一个。单测 127/127、vitest 30/30，浏览器走查 13 步通过。
  - **跟进项（code review 提出，本 spec 未处理）**：提供商 `ApiKey` 随列表 / 详情以明文回传（值对象直出契约的既定取舍）；读取侧脱敏（`sk-****`）+ 写入侧空值表示「不修改」另立提案。
