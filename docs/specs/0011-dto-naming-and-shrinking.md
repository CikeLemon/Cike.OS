# Spec: DTO 命名规范落地、输出 DTO 按消费方收窄、Agent 工具选项改两层

> Triage: `ready-for-agent`
> 术语遵循根 `CONTEXT.md`（Agent、Agent 工具、用户工具配置、工具参数、工具包、工具包分类、工具、标签、模型提供方、模型）；值对象直接作 API 契约见 ADR 0003；DTO 继承、分页透传、目录约定见 `Backend/AGENTS.md`；本 spec 修订 spec 0010 中「Agent 第二步按分类 → 工具包分组」与「现有 Get 不动」两条，并修订 `Backend/AGENTS.md`「嵌套在父 Dto 里的子实体 Dto 继承审计基类」一条。
> 2026-11-28 经设计访谈定稿。仓库现状：Contracts 下 30 个 DTO 文件，裸 `XxxDto` 既当详情又当列表行，`XxxListDto` 与 `List<XxxDto>` 两种列表写法并存，分页列表直接返回详情形状（Readme / Setting / 参数定义全量），三个详情接口无人调用，用户工具配置的详情接口缺参数定义导致编辑弹窗只能拿列表行回填。本 spec 是实现的唯一标准。

## Problem Statement

管理者感知不到 DTO，但开发者每次打开一个 DTO 都要翻代码才知道它是干什么的：`ToolDto` 既是详情、又是分页列表的一行、又是包内工具表的一行；`AgentListDto` 和 `List<TagDto>` 是同一类东西两种叫法；`SaveAgentToolDto` 到底是新增还是保存说不清。更要紧的是列表接口把整个实体的形状都吐出去（工具包列表带 Readme、工具列表带 Setting 和全部参数定义），而真正该给编辑用的详情接口反而残废：用户工具配置的 `GET /{id}` 没有参数定义，编辑弹窗只能拿列表行回填，别人刚改过的参数定义会被旧列表覆盖。Agent 的工具选择树多套了一层分类，而分类只是工具包列表的检索维度，在勾选工具时没有任何用处。

## Solution

定一套一眼能看懂的 DTO 命名：详情 `XxxDetailDto`、列表行 `XxxItemDto`、其他功能形状 `Xxx<功能>ItemDto`、动作结果 `XxxResultDto`、新增 `CreateXxxDto`、编辑 `UpdateXxxDto`、保存 `SaveXxxDto`、查询请求 `GetXxxListRequestDto` / `GetXxxPagedListRequestDto`，裸 `XxxDto` 禁止。全仓库 30 个 DTO 一次改齐。改名的同时逐个核对消费方：每个输出 DTO 只留前端实际读到的字段，没读到的干掉；用户工具配置的详情补全成编辑弹窗需要的全部东西，所有编辑入口改为「点编辑先调详情」；Agent 的工具选项砍掉分类层，只剩「工具包 → 工具」两层。

## User Stories

1. 作为后端开发者，我看到 `XxxDetailDto` 就知道它是单个实体的完整输出，看到 `XxxItemDto` 就知道它是列表的一行，不必翻代码确认。
2. 作为后端开发者，我看到 `CreateXxxDto` / `UpdateXxxDto` / `SaveXxxDto` 就知道分别是新增、编辑、新增修改一体（或子项随父提交）的输入，三者不混用。
3. 作为后端开发者，我看到 `GetXxxListRequestDto` / `GetXxxPagedListRequestDto` 就知道它是查询请求，不会和输出 DTO 混在一起。
4. 作为后端开发者，仓库里不再有任何裸 `XxxDto`，新代码也没有理由再造一个。
5. 作为后端开发者，其他功能形状（下拉候选、选项树等）按「实体名 + 功能词 + Item」命名，功能词按实际用途起（Option / Basic / Summary…），不强行归入 Detail 或 Item。
6. 作为前端开发者，工具包分页列表只返回列表页显示的列，不再带着整篇 Readme，以便列表请求更轻。
7. 作为前端开发者，包内工具列表只返回编码、名称、形态、描述和参数个数，不再带每个工具的完整设置与参数定义。
8. 作为前端开发者，用户工具配置的卡片列表不再带没人显示的工具描述。
9. 作为前端开发者，批量新增的候选列表不再带没人显示的「配置时间」。
10. 作为前端开发者，Agent 详情里的工具条目、模型提供方详情里的模型条目只带表单回填要用的字段，不带没人读的审计字段。
11. 作为管理者，我点工具包列表的「编辑」时，表单里是这个工具包此刻最新的内容（含 Readme），以便别人刚改过的不会被我的旧列表覆盖。
12. 作为管理者，我点包内某个工具的「编辑」时，编辑面板里是这个工具此刻最新的参数定义与设置。
13. 作为管理者，我点标签 / 工具包分类的「编辑」时，表单是从详情接口取的，与其它模块行为一致。
14. 作为管理者，我点工具参数卡片的「编辑」时，弹窗按工具此刻的参数定义生成字段并回填我已存的值，以便工具后来加的参数也能在编辑时看到。
15. 作为管理者，我在 Agent 表单第二步看到的是「工具包 → 工具」两层：每个工具包一行「整包」勾选，下面是它的工具；没有分类层。
16. 作为管理者，我在第二步搜索时只过滤工具行，工具包名命中显示它全部工具，空包跟随隐藏。
17. 作为管理者，勾「整包」、整包下取消单个工具转单选等勾选行为与之前完全一致。
18. 作为管理者，新建 Agent 后「跳去填参数」的判定在两层树上照常工作。
19. 作为前端开发者，所有编辑入口统一为「点编辑 → 按 Id 调详情 → 成功才开表单，失败 toast 不打开」，任何表单都不再拿列表行回填。
20. 作为前端开发者，我执行 `pnpm gen:api` 后拿到的类型名与后端一一对应，手写代码只改类型引用，不手写请求函数。
21. 作为后端开发者，查询请求 DTO 改名后端点绑定方式、路由、查询串参数名都不变，前端请求代码零改动。
22. 作为后端开发者，Query 类名与 DTO 的功能词一致（`GetXxxDetailQuery`），Validator 随输入 DTO 名（输入 DTO 不改名，Validator 不动）。
23. 作为测试者，我能验证用户工具配置详情返回参数定义、工具名与已存值，以及未配置 / 工具已删时 404。
24. 作为测试者，我能验证 Agent 工具选项只有两层、只含有工具的包、按名称排序。
25. 作为测试者，我能验证工具列表与工具包列表不再带被砍掉的字段。
26. 作为未来的实现 agent，我拿到本 spec 即可独立完成全部实现（决策均已定稿），不必再做架构裁决。

## Implementation Decisions

### 命名规范（写入 `Backend/AGENTS.md`，替换现有「DTO 继承框架基类」一条）

| 用途 | 命名 | 基类 |
|---|---|---|
| 单个实体的完整输出 | `XxxDetailDto` | `AuditedEntityDto<long>` |
| 列表的一行（分页 / 非分页） | `XxxItemDto` | `AuditedEntityDto<long>` |
| 详情内嵌的子实体条目 | `XxxItemDto` | 平铺，不继承（前端不读审计字段） |
| 其他功能形状（下拉候选、选项树节点等） | `Xxx<功能>ItemDto`，功能词按实际用途起 | 平铺，不继承；若前端要显示时间则按需继承 |
| 动作结果（非实体） | `XxxResultDto` | 平铺 |
| 新增 | `CreateXxxDto` | 平铺 |
| 编辑 | `UpdateXxxDto`（不含 Id、不含不可变字段） | 平铺 |
| 保存：新增修改一体 / 子项随父提交 | `SaveXxxDto` | 平铺 |
| 查询请求（筛选字段多时才建） | `GetXxxListRequestDto` / `GetXxxPagedListRequestDto` | `PagedAndSortedResultRequest` |

- 裸 `XxxDto` 禁止。输出 DTO 只放消费方实际读到的字段，列表不复用详情，字段随消费方变化而增减。
- 筛选字段少时仍平铺到 Query 参数、不建 Request DTO（框架口径不变）。
- Query 类名跟功能词：`GetXxxDetailQuery`、`GetXxxListQuery`、`GetXxxPagedListQuery`。
- 本条覆盖 2026-11-27「嵌套在父 Dto 里的子实体 Dto 也继承审计基类」的规则。

### 全量改名映射（后端 Contracts / Application / Service.Open / 测试，前端生成层重生成 + 手写引用）

| 现名 | 新名 | 返回它的 Query |
|---|---|---|
| `AgentDto` | `AgentDetailDto` | `GetAgentDetailQuery` |
| `AgentToolDto`（详情内嵌） | `AgentToolItemDto` | — |
| `AgentListDto` | `AgentItemDto` | `GetAgentPagedListQuery` |
| `AgentToolOptionDto`（分类层） | **删除** | — |
| `AgentToolPackageOptionDto` | `AgentToolPackageOptionItemDto` | `GetAgentToolOptionsQuery` → `List<AgentToolPackageOptionItemDto>` |
| `AgentToolOptionItemDto`（工具层） | 不变 | — |
| `ModelProviderDto` | `ModelProviderDetailDto` | `GetModelProviderDetailQuery` |
| `ModelDto`（详情内嵌） | `ModelItemDto` | — |
| `ModelProviderListDto` | `ModelProviderItemDto` | `GetModelProviderListQuery` |
| `TagDto` | 拆为 `TagDetailDto` + `TagItemDto` | `GetTagDetailQuery` / `GetTagListQuery` / `GetTagPagedListQuery`（后两者 → `TagItemDto`） |
| `ToolPackageCategoryDto` | 拆为 `ToolPackageCategoryDetailDto` + `ToolPackageCategoryItemDto` | `GetToolPackageCategoryDetailQuery` / `GetToolPackageCategoryListQuery` |
| `ToolPackageDto` | 拆为 `ToolPackageDetailDto` + `ToolPackageItemDto` | `GetToolPackageDetailQuery` / `GetToolPackagePagedListQuery`（→ `PagedResultDto<ToolPackageItemDto>`） |
| `ToolDto` | 拆为 `ToolDetailDto` + `ToolItemDto` | `GetToolDetailQuery` / `GetToolListQuery`（→ `List<ToolItemDto>`） |
| `UserToolConfigurationDto` | `UserToolConfigurationDetailDto` | `GetUserToolConfigurationDetailQuery(long ToolId)` |
| `UserToolConfigurationListDto` | `UserToolConfigurationItemDto` | `GetUserToolConfigurationPagedListQuery` |
| `UserToolConfigurationToolOptionDto` | `UserToolConfigurationToolOptionItemDto` | `GetUserToolConfigurationToolOptionsQuery` |
| `GetToolPackagePagedListDto` | `GetToolPackagePagedListRequestDto` | `GetToolPackagePagedListQuery(GetToolPackagePagedListRequestDto)` |

- 输入 DTO（`CreateXxxDto` / `UpdateXxxDto` / `SaveXxxDto`）与 `UploadSkillPackageResultDto` 名字不变，Validator 类不动。
- 端点方法、路由、HTTP 动词、查询串参数名全部不变；Swagger schema 名随类名变，前端 `pnpm gen:api` 后类型名一一对应。
- 一个 DTO 一个文件，文件名随类名；详情与列表行拆开后各自一个文件，同一聚合的 DTO 仍放在该聚合的 `Dtos/` 目录。

### 各输出 DTO 的终态字段（按前端手写代码逐文件核对的结果）

| DTO | 字段（基类字段略） | 砍掉的 |
|---|---|---|
| `AgentItemDto` | Name、Description | — |
| `AgentDetailDto` | Name、Description、Instructions、Options、`Tools: List<AgentToolItemDto>` | — |
| `AgentToolItemDto`（平铺） | Type、TargetId | Id、审计 |
| `AgentToolPackageOptionItemDto`（平铺） | ToolPackageId、Name、`Tools: List<AgentToolOptionItemDto>` | 分类层整层 |
| `AgentToolOptionItemDto`（平铺） | ToolId、Name、Type、ParameterCount | — |
| `ModelProviderItemDto` | Name、Protocol、ModelNames | — |
| `ModelProviderDetailDto` | Name、Protocol、BaseUrl、ApiKey、Setting、`Models: List<ModelItemDto>` | — |
| `ModelItemDto`（平铺） | Name、Description、TagIds | Id、审计 |
| `TagItemDto` / `TagDetailDto` | Key、Name | — |
| `ToolPackageCategoryItemDto` / `ToolPackageCategoryDetailDto` | Name | — |
| `ToolPackageItemDto` | Code、Name、Description、Icon、ToolPackageCategoryName、Status | Readme、ToolPackageCategoryId |
| `ToolPackageDetailDto` | Code、Name、Description、Icon、Readme、ToolPackageCategoryId、ToolPackageCategoryName、Status | — |
| `ToolItemDto` | Code、Name、Description、Type、ParameterCount | Setting、ToolPackageId、Parameters（整个列表换成个数） |
| `ToolDetailDto` | Code、Name、Description、Type、ToolPackageId、Parameters、Setting | — |
| `UserToolConfigurationItemDto` | ToolId、ToolName、ToolType、ToolPackageName、ToolPackageCategoryName、Parameters、Values | ToolDescription |
| `UserToolConfigurationDetailDto` | ToolId、ToolName、Parameters（工具此刻的参数定义）、Values（已存的值） | —（原只有 ToolId / Values，本次补全） |
| `UserToolConfigurationToolOptionItemDto`（平铺） | ToolId、ToolName、ToolType、ToolPackageName、Parameters、Configured | ConfiguredAt |

- `ParameterCount`：参数定义是 json 列不能下推计数，取回后在内存里数；投影 → DTO 仍走 Mapster `Adapt`，派生字段在 Adapt 之后赋值。
- 列表 Handler 的取数方式不变（仓储分页只接受实体查询，整行取回后 `Adapt`，不在 DTO 上的字段自然丢掉）；跨表回填字段仍在 Adapt 之后单独赋值。

### Agent 工具选项改两层

- `GET /Agents/ToolOptions` 路由不变，返回 `List<AgentToolPackageOptionItemDto>`：工具包 Id / 名称 → `Tools`（工具 Id / 名称 / 形态 / 参数个数）。
- Handler 去掉分类查询：工具、工具包各一次投影（只取 Id、名称、归属、形态、参数定义），只保留有工具的包，包与工具各按名称排序；不限工具包状态。
- 分类仍是工具包列表页的筛选维度，与 Agent 勾选无关。

### 用户工具配置详情补全

- `GET /UserToolConfigurations/{id}` 的 `{id}` 仍是**工具 Id**（按「当前用户 + 工具」取），与 Save / Delete 三件套同一把钥匙，不改。
- 返回 `UserToolConfigurationDetailDto`：当前用户在该工具上的配置（Values + 审计）联合工具的名称与参数定义；当前用户没有这条配置、或工具已不存在时返回 null（端点 404），与卡片列表的内连接口径一致。
- 卡片列表 `UserToolConfigurationItemDto` 仍带 Parameters + Values：卡片本身是「参数名：值」的显示态，这是它自己的职责，不再给编辑弹窗用。

### 前端

- **编辑入口统一**（写入 `Frontend/cike.os/AGENTS.md`）：所有表单「点编辑 → 按 Id 调详情 → 成功才开表单；失败 toast 不打开」，禁止拿列表行回填。本次改五处：标签、工具包分类、工具包列表、包内工具表、工具参数卡片（Agent、模型提供方已是此写法，只改类型名）。工具包详情页的「编辑工具包」已有详情对象，直接复用，不再请求。
- **工具参数编辑弹窗**：页面 `openEdit(item)` 用 `item.toolId` 调详情，成功后把 `UserToolConfigurationDetailDto` 交给弹窗；弹窗按 `parameters` 生成字段、`values` 回填，保存仍走单条 Save（工具 Id 来自详情）。删除不变（传工具 Id）。
- **Agent 第二步**：`AgentToolPicker` 的入参改为 `AgentToolPackageOptionItemDto[]`，渲染为工具包平铺：每个工具包一个边框块，首行「整包」勾选 + 包名，下面是工具勾选（名称 + 形态徽标）；去掉分类层与分类 Collapsible。搜索过滤、整包 / 单选互斥逻辑不变。「新建后跳去填参数」的纯函数按两层树展开。
- 包内工具表的「参数数」列改读 `parameterCount`。
- `types.gen.ts` 等生成物由 `pnpm gen:api` 重生成；手写代码只改类型引用，不手写请求函数。

### 文档同步

- `Backend/AGENTS.md`：用上文命名表替换「DTO 继承框架基类」一条；「列表查询不加载聚合，列表 DTO 不复用详情 DTO」一条里的 `XxxListDto` 改为 `XxxItemDto`；「分页查询直接透传」里的 `GetXxxPagedListDto` 改为 `GetXxxPagedListRequestDto`。
- `Frontend/cike.os/AGENTS.md`：新增「编辑一律调详情接口，禁止拿列表行回填」。
- `docs/adr/0008` 与 `docs/specs/0010`：原地加修订注记——Agent 第二步改为「工具包 → 工具」两层；用户工具配置详情接口补全并成为编辑入口，「现有 Get 不动」作废。不新开 ADR（命名与形状可逆）。

## Testing Decisions

- **测试接缝**（全部沿用现有，不新增）：
  - **后端业务规则 / 查询结果**：从发布 Query（经 `ILocalEventBus`）这一最高点进入，既有 SQLite 内存测试宿主。先例 `AgentTests`、`TagTests`、`ToolTests`、`UserToolConfigurationTests`。
  - **HTTP 形状**：真实宿主 `ServiceOpenTestHost`。先例 `AgentHttpTests`、`UserToolConfigurationHttpTests`、`ModelProviderHttpTests`。
  - **前端**：纯函数接缝（vitest）+ 浏览器走查兜底，不写组件测试。先例 `agent-form.test.ts`、`provider-form.test.ts`、`tag-form.test.ts`。
- **好的测试**：只断言可观察行为——查询返回了哪些字段、哪些字段不再出现、404 与 200 的边界；不断言 Mapster 内部、不依赖 SQL。
- **改名的连带**：现有测试只改类型与 Query 名，断言不动，改完必须仍是全绿——这是「纯改名没改行为」的证据。
- **新增 / 改写的用例**：
  - Agent 工具选项（改写 `AgentTests` 现有用例）：返回的是包层列表；只含有工具的包；没有工具的包不出现；包与工具各按名称排序；工具条目带 `ParameterCount`。
  - 用户工具配置详情（新增到 `UserToolConfigurationTests` + `UserToolConfigurationHttpTests`）：已配置的工具返回工具名、参数定义（与工具此刻的定义一致）、已存值；当前用户未配置返回 null / 404；别的用户配了不算；工具已删返回 null / 404。
  - 工具列表（扩展 `ToolTests` / HTTP 测试）：`ToolItemDto` 有 `ParameterCount` 且等于参数定义条数；响应 JSON 里不再出现 `setting`、`toolPackageId`、`parameters`。
  - 工具包分页列表（扩展 `ToolPackageTests`）：响应 JSON 不再出现 `readme`、`toolPackageCategoryId`，其余列不变。
  - 用户工具配置卡片列表与候选：响应 JSON 不再出现 `toolDescription` / `configuredAt`。
- **验收**：`dotnet build` 0 新增警告、`dotnet test` 全绿；抓 swagger 核对全部 schema 名与字段清单与本 spec 的两张表一致（以此作为「没有漏改」的机械核对）；`pnpm gen:api` 后看生成层 diff、`pnpm build`（含 vue-tsc）与 `pnpm test` 通过；dev 库真实 HTTP 冒烟；浏览器走查：五个编辑入口各自「点编辑 → 网络面板看到详情请求 → 表单内容正确」；Agent 第二步两层勾选（整包 / 取消单个转单选 / 搜索）→ 新建后跳转判定；工具参数卡片编辑弹窗按详情渲染。

## Out of Scope

- 列表查询改为真正的投影分页（仓储分页只接受实体查询，本次不动取数方式）。
- 输入 DTO（Create / Update / Save）的字段与名字；Validator 规则。
- 任何路由、HTTP 动词、查询串参数名、响应状态码。
- 用户工具配置 Delete / Save 的 `{id}` 语义（保持工具 Id）。
- 值对象（ADR 0003）的形状；`UserToolConfiguration` 实体的 DDD 收口。
- 标签 / 工具包分类详情页（没有列表之外的内容，不建页面，详情接口只服务编辑回填）。
- 权限、租户策略、删除守护策略。

## Further Notes

- **发布通道**：仓库的 issue tracker 与 triage 标签词表仍未配置（`docs/agents/` 不存在、本机无 gh CLI），本 spec 沿前例以仓库内 markdown 落盘并标注 `ready-for-agent`，tracker 就绪后再搬运。
- **设计来源**：命名规范与「字段没用到就干掉」由 2026-11-28 设计访谈逐条定稿；三个「无人调用」的详情接口曾一度拟删，用户纠正为「编辑必须调详情接口」，故全部保留并接上，用户工具配置的详情补全。
- **环境事实**：运行中的主服务会锁输出 DLL，编译前先停、验证完再拉起；后端须以 `https` launch profile 启动供 `gen:api` 抓 swagger。用户常自己开着 Vite，不要动。
- **风险**：前端手写代码里对生成类型的引用约 10 处、后端约 21 处，vue-tsc 与 `dotnet build` 能抓全；真正要人工核对的是「字段被砍掉后前端有没有地方还在读」——以 swagger 字段清单为准逐页走查。

- **实施结果（2026-11-28，未提交待 review）**：
  - 后端：Contracts 下 30 个 DTO 按映射表改名 / 拆分（一类型一文件，文件随类名移动），Query 类名随功能词改；新增 `GetTagDetailQuery` + `TagService.GetAsync`（标签原本没有 `GET /{id}`）；`AgentQueryHandler.GetToolOptionsAsync` 去掉分类投影，两次投影组两层树；`UserToolConfigurationQueryHandler.GetDetailAsync` 补全（配置 ⋈ 工具，缺配置或工具已删返回 null）；`ToolQueryHandler.GetListAsync` 与 `ToolPackageQueryHandler.GetPagedListAsync` 的派生 / 回填字段在 `Adapt` 之后按取回实体顺序 `Zip` 一一对应赋值（列表行不带实体键）。
  - 约定路由事实：按 Id 取详情的端点方法必须叫 `GetAsync(long id)`，写成 `GetByIdAsync` 会生成错误路由（HTTP 测试 405）。
  - 测试：改名的连带只改类型 / Query 名；`AgentTests` / `ModelProviderTests` 原本用子实体 `Id` 断言「仍是原来那一条 / 被软删」，子实体条目不带 Id 后改为对着 `DbContext` 的行验（更新后要新开 scope，否则拿到更新前已跟踪的实例）；选项树用例改为两层；`UserToolConfigurationTests` 新增详情两例；新增 `DtoShapeHttpTests`（工具列表只带 `parameterCount`、工具包列表不带 Readme / 分类 Id 而详情带、用户工具配置详情 200 / 404 与卡片 / 候选不含被砍字段、标签与分类详情接口）。`dotnet test` 235/235。
  - 前端：`pnpm gen:api` 重生成后手写代码只改类型引用；五处编辑入口（标签 / 工具包分类 / 工具包列表 / 包内工具表 / 工具参数卡片）改为先调详情，请求期间编辑按钮禁用；`AgentToolPicker` 改为工具包平铺（每个包一个边框块：整包勾选 + 工具），去掉分类 Collapsible；`pendingParameterToolIds` 按两层树展开；工具表「参数数」列改读 `parameterCount`。vue-tsc 0 错、vitest 55/55、`pnpm build` 通过；dev 库 HTTP 冒烟（列表行字段 / 详情字段 / 两层树形状）通过。
  - 文档：`Backend/AGENTS.md`「DTO 继承框架基类」一条替换为命名表 + 「输出 DTO 只放消费方实际读到的字段」，新增「详情接口是编辑回填的唯一来源」；`Frontend/cike.os/AGENTS.md` 新增「编辑一律调详情接口」；ADR 0008 与 spec 0010 加修订注记。
