# Spec: Agent 增删改查（含工具绑定）与「工具参数」模块

> Triage: `ready-for-agent`
> **2026-11-28 修订（spec 0011）**：① Agent 第二步的选择树改为「工具包 → 工具」两层，下文「分类 → 工具包」分组的描述作废；② 「现有 Save / Get / Delete 不动」作废——`GET /UserToolConfigurations/{id}` 补全为返回 ToolName / Parameters / Values，并成为编辑弹窗的唯一回填来源（编辑一律调详情，禁止拿卡片行回填）；③ 本文所有裸 `XxxDto` / `XxxListDto` 类型名按 spec 0011 的映射改名。
> 术语遵循根 `CONTEXT.md`（Agent、Agent 工具、用户工具配置、工具参数）；结构决策见 `docs/adr/0008-agent不绑模型与工具绑定合并为单表.md`；值对象直接作 API 契约见 ADR 0003；聚合内子实体写法、校验三层划分、DTO 继承、分页透传见 `Backend/AGENTS.md`。
> 2026-11-27 经设计访谈七轮定稿。仓库现状：`AgentEntity` / `AgentOptions` / `AgentTool` / `AgentToolPackage` 只有实体与 EF 映射，没有任何用例与端点；`AgentsPage` 是占位页；`UserToolConfiguration` 已有单条 Save / Get / Delete。本 spec 是实现的唯一标准。

## Problem Statement

侧栏有「Agent」却只是一张占位页：管理者建不出一个 Agent，更没法告诉它用哪些工具包。数据库里虽然已经有 Agent 表和两张绑定表，但绑定表是围绕「绑定是独立聚合」设计的，前端没有任何入口。另一头，工具的参数值（比如天气服务的 api_key）虽然后端能存，但管理端没有界面：用户既不知道哪些工具需要自己填值，也没地方改。

## Solution

给「Agent」一页列表加一个两步的表单对话框：第一步填名称、描述、指令和模型参数（常用项直接显示，其余折叠成「高级配置」），第二步按「分类 → 工具包」分组勾选工具——勾工具包就是整包绑定，勾单个工具就是单选。保存后如果绑定的工具里有要用户填参数的，自动跳到新的「工具参数」模块：已配置的工具每个一张卡片，敏感值点一下才显示；「新增配置」可以一次配多个工具，编辑只针对单个。

## User Stories

1. 作为管理者，我打开「Agent」看到列表（名称、描述、创建时间、更新时间、操作），可以按名称搜索，以便先知道已经有哪些 Agent。
2. 作为管理者，我点「新建 Agent」打开一个两步的对话框，第一步填名称、描述、指令和模型参数，第二步选工具，一次保存就能建出一个可用的 Agent。
3. 作为管理者，指令是多行大文本框，可以写很长的提示词而不被长度限制拦下。
4. 作为管理者，模型参数默认只看到温度、输出上限、TopP、推理力度、工具模式五项，其余参数折叠在「高级配置」里，以便常用场景表单清爽。
5. 作为管理者，我展开「高级配置」能设置 TopK、频率惩罚、存在惩罚、Seed、停止词、推理输出、响应格式（文本 / JSON + Schema）、允许多次工具调用、允许后台响应，所有项留空就表示不设置。
6. 作为管理者，我填的参数超出范围（比如温度填 3）时被拦下并提示，以便不会把模型不接受的值存进去。
7. 作为管理者，第二步按「分类 → 工具包 → 工具」分组展示全部工具，顶部可以搜索，以便工具多的时候也能快速找到。
8. 作为管理者，我勾某个工具包的「整包」，该包下的工具全部勾上，以便一次绑定整个包。
9. 作为管理者，我在整包状态下取消其中一个工具，整包勾选自动取消，变为只绑定剩下的工具。
10. 作为管理者，我勾选单个工具，保存后这个 Agent 只能用这些工具。
11. 作为管理者，我提交了不存在的工具包或工具时被拒绝且什么都不保存，以便绑定永远指向真实存在的东西。
12. 作为管理者，我在同一个 Agent 里既整包绑定了「天气插件」又单独勾了它下面的工具时被拒绝，以便同一种绑定关系不会重复出现。
13. 作为管理者，我不选任何工具也能保存，以便先建一个只会聊天的 Agent。
14. 作为管理者，我编辑 Agent 时对话框里是它此刻最新的内容和工具勾选状态，以便别人刚改过的不会被我的旧页面覆盖。
15. 作为管理者，编辑时名称、描述、指令、模型参数和工具都可以改，没有不可变字段。
16. 作为管理者，我在同一个租户下用已存在的名称新建或改名 Agent 时被拒绝；不同租户可以同名。
17. 作为管理者，我新建 Agent 保存后，如果绑定的工具里有需要我填参数的，页面自动跳到「工具参数」并提示「来自 Agent「X」：N 个工具待配置」，批量新增弹窗已经把这些工具选好，以便一口气把参数填完。
18. 作为管理者，我编辑 Agent 保存后只是回到列表，不会被跳走，以便修改已有 Agent 不被打断。
19. 作为管理者，我删除 Agent 时看到「已有会话仍可查看，但不能再对话」的确认提示，确认后 Agent 消失；没有任何东西因为「被引用」拦住我。
20. 作为管理者，我在侧栏看到「工具参数」一个独立菜单，排在「工具包分类」后面。
21. 作为管理者，「工具参数」页里每个我已经配置过的工具一张卡片，显示工具名、形态、每个参数的名字和值，分页展示，可以按工具名搜索。
22. 作为管理者，标记为敏感的参数值默认显示为圆点，点眼睛图标才显示明文，以便旁边有人时不会泄露密钥。
23. 作为管理者，某个工具后来新增了必填参数而我没填时，卡片上出现「缺必填」标记，以便知道要去补。
24. 作为管理者，我点卡片「编辑」打开单个工具的参数表单，改完保存立即生效。
25. 作为管理者，我点卡片「删除」并确认后，这个工具的参数配置消失。
26. 作为管理者，我点「新增配置」打开批量弹窗，先多选要配置的工具（只列声明了参数且尚未配置的），每个选中的工具展开自己的参数表单，「全部保存」一次提交。
27. 作为管理者，批量弹窗里有必填项没填时对应工具标红、「全部保存」禁用，以便不会提交一半。
28. 作为管理者，我提交的批量配置里有必填参数缺失时被后端整批拒绝、什么都不保存，以便绕过前端也不会存进不完整的配置。
29. 作为管理者，我只看到自己的工具参数配置，看不到别人的；同一个工具别人配了什么与我无关。
30. 作为前端开发者，我执行 `pnpm gen:api` 后拿到 Agent 的五个端点、工具选项端点、用户工具配置的分页 / 批量保存 / 工具选项端点，组件只从生成层导入，不手写 URL。
31. 作为后端开发者，Agent 是租户级聚合根，Agent 工具是它聚合内唯一的子实体集合，随 Agent 的请求整体同步、没有独立路由，以便聚合形状与生命周期一致。
32. 作为后端开发者，工具模式、推理选项直接用 Microsoft.Extensions.AI 的类型作为契约与持久化形状，不建镜像 DTO 和自建枚举。
33. 作为后端开发者，用户工具配置的分页列表是一条 LINQ 联合查询，一次把工具、工具包、分类的信息带出来，不在循环里查库。
34. 作为测试者，我能在不手写 URL 的情况下验证 Agent 与工具参数的全部路由、请求体（含 `$type` 多态的工具模式）、校验失败与业务拒绝两种 400、404 与租户隔离。
35. 作为测试者，我能在实体映射冒烟测试里确认 Agent 工具随 Agent 保存、从集合移除后软删、删 Agent 连带软删、一级预加载能查全、模型选项里的工具模式能 JSON round-trip。
36. 作为未来的实现 agent，我拿到本 spec 即可独立完成全部实现（决策均已定稿），不必再做架构裁决。

## Implementation Decisions

### 后端

- **实体**（严格 DDD：聚合根 `private set` + 静态工厂 + 意图方法；实体 / 工厂 / 意图方法内不写单字段基础校验与字段间匹配校验，只有赋值与集合内部不变量）：
  - `AgentEntity`（租户级聚合根，类名不变）：`Name`、`Description`、`Instructions`、`Options`（值对象）、`Tools`（私有 set 的 `AgentTool` 集合）。静态工厂 `Create(name, description, instructions, options, tools)`；意图方法 `Update(name, description, instructions, options)` 只赋值（`Options` 整体替换，让 EF 察觉 json 列变化）；`SyncTools(tools)` 以 `(Type, TargetId)` 为键整体同步——提交的条目里重复是聚合内部不变量，在此断言。意图方法 `internal`。
  - `AgentTool`（从聚合根改为 Agent 聚合内子实体：`FullAuditedEntity<long>` + `IMultiTenant`）：`AgentId`、`Type`（`AgentToolType`）、`TargetId`。工厂 `public static Create(agentId, type, targetId)`，由 Handler 构造后交给聚合的 `Create` / `SyncTools`；没有修改方法（同步即替换）。聚合根不接收应用层的输入类型。
  - 枚举 `AgentToolType { ToolPackage = 1, Tool = 2 }`（Domain.Shared）：`ToolPackage` 表示整包绑定，`TargetId` 指向工具包；`Tool` 表示单个工具，`TargetId` 指向工具。
  - 删除 `AgentToolPackage` 实体、它的 EF 配置与 DbSet。
- **值对象**（沿 ADR 0003，直接作 API 契约与持久化形状）：`AgentOptions` 及其 `ReasoningOptions` / `ChatToolMode` / `AgentChatResponseFormat` 从 Domain 搬到 `Domain.Shared/ValueObjects`，字段一个不增不减；继续引用 Microsoft.Extensions.AI 的 `ReasoningOptions`（`Effort` / `Output` 枚举）与 `ChatToolMode`（STJ 多态，判别键 `$type`，判别值 `none` / `auto` / `required`）。`Domain.Shared` 为此引用 `Microsoft.Extensions.AI.Abstractions`，版本与 `Application` 现有传递依赖一致，统一写在版本属性文件里。不建 `AgentOptionsDto`。
- **用例**（Application，新建 `Agents/` 一套目录：Command / Query / Validator / Handler）：
  - `GetPagedList(Keyword, Paging)`：不加载聚合，投影名称 / 描述 / 审计时间成 `AgentListDto`（继承审计基类），关键字按名称包含过滤，分页排序透传。
  - `Get(id)`：连同 `Tools` 一级预加载、不跟踪，`Adapt` 成 `AgentDto`，工具按 `Type`、`TargetId` 排序。
  - `Create` / `Update(id)`：校验后调工厂 / 意图方法；更新时连同 `Tools` 加载（跟踪）再同步，不存在抛「Agent 不存在」。
  - `Delete(id)`：连同 `Tools` 加载（跟踪）后删除聚合，工具随之软删。没有任何守护。
  - 跨聚合校验收敛为一个私有 `ValidateAsync`，创建 / 更新共用（`excludeId`）：名称同租户唯一（一次计数）；`Type = ToolPackage` 的 `TargetId` 去重后一次计数核对都存在；`Type = Tool` 的 `TargetId` 去重后一次计数核对都存在；再把这些工具的所属工具包 Id 一次投影取回，与整包绑定的集合有交集则拒绝（「工具包已整包绑定，不能再单独绑定其中的工具」）。被拒时什么都不保存。
  - `GetToolOptions`：供 Agent 第二步的选择列表——三个投影查询（分类、工具包、工具各取 Id / 名称 / 形态），内存里组装成「分类 → 工具包 → 工具」三层，只保留有工具的包与有包的分类，各层按名称排序；不限工具包状态。
- **端点**（Service.Open，一个 `AgentService` 端点类，约定路由）：`GET /Agents/PagedList`（`keyword` + 分页）、`GET /Agents/ToolOptions`、`GET /Agents/{id}`（不存在 404）、`POST /Agents`（返回新 Id）、`PUT /Agents/{id}`、`DELETE /Agents/{id}`。响应形状沿现状：FluentValidation 失败 400 + ProblemDetails，业务拒绝 400 + 消息文本，取不到单条资源 404。
- **DTO**（Application.Contracts，`Agents/Dtos/`）：
  - 输出 `AgentDto`：继承审计基类 + 名称、描述、指令、`Options`（`AgentOptions`）、`Tools: List<AgentToolDto>`；`AgentToolDto` 继承审计基类 + `Type`、`TargetId`。
  - 列表 `AgentListDto`：继承审计基类 + 名称、描述。
  - 输入 `CreateAgentDto` 与 `UpdateAgentDto` 同形：名称、描述、指令、`Options`、`Tools: List<SaveAgentToolDto>`；`SaveAgentToolDto { Type, TargetId }` 平铺不继承；更新 DTO 不含 Id。
  - `AgentToolOptionDto`：分类 Id / 名称、`Packages`（工具包 Id / 名称、`Tools`（工具 Id / 名称 / 形态）），平铺不继承。
- **Validator**（FluentValidation，类级 `[AutoValidation]`；规则已逐条经用户确认）：
  - 新建 / 更新（两个 Validator 规则相同）：名称必填 ≤ 128；描述可选 ≤ 512；指令必填、不限长度；`Options` 不能为 null 并挂子 Validator；`Tools` 不能为 null、可为空列表，每项 `Type` 必须是合法枚举值、`TargetId` > 0。
  - `Options` 子 Validator：温度 0–2；TopP 0–1；TopK ≥ 1；输出上限 ≥ 1；频率惩罚 / 存在惩罚 −2–2；Seed ≥ 0；停止词最多 16 条、每条非空 ≤ 64；推理力度 / 推理输出必须是合法枚举值；响应格式类型必须合法，为 Json 时 `JsonSchemaDescriptor` 必填且必须是合法 JSON，`JsonSchemaName` 可选不限长；工具模式为 null 时放行，非 null 时必须是 `none` / `auto` / `required` 三种之一；允许多次工具调用 / 允许后台响应无规则。全部可空，空 = 不设置。
  - 不写 `Trim()`，body 字符串去空白由框架负责。
- **用户工具配置的新增用例**（Application `UserToolConfigurations/`，现有 Save / Get / Delete 不动）：
  - `GetPagedList(Keyword, Paging)`：以当前用户的配置为起点，一条 LINQ 联合查询（配置 ⋈ 工具 ⋈ 工具包 ⋈ 工具包分类，全走各自仓储的 `GetQueryable()`，不跟踪），关键字按工具名过滤，投影成含配置 Id / 审计时间、工具 Id / 名称 / 形态 / 描述 / 参数定义、工具包名、分类名、参数值的匿名类型，用框架的 `IQueryable` 分页扩展物化（排序作用于投影后的列），结果 `Adapt` 成 `UserToolConfigurationListDto`（继承审计基类 + 上述字段）。内连接天然隐藏已删工具的残留配置。
  - `SaveBatch(items)`：一次性按工具 Id 批量取工具、一次性取当前用户在这些工具上的已有配置，先对每条做参数解析（必填缺失即抛业务异常，整批不保存），再分别 `InsertManyAsync` / `UpdateManyAsync`；循环里只做内存赋值。
  - `GetToolOptions`：全部声明了参数的工具（Id / 名称 / 形态 / 所属工具包名 / 参数定义）+ 当前用户是否已配置 + 配置的更新时间，不分页，按工具名排序。
  - 对应端点（现有 `UserToolConfigurationService` 端点类里新增三条约定路由）：`GET /UserToolConfigurations/PagedList`、`GET /UserToolConfigurations/ToolOptions`、`POST /UserToolConfigurations/SaveBatch`（body 为 `SaveUserToolConfigurationDto` 列表）。
  - 批量 Validator：列表不能为 null、至少一项，每项挂现有单条 Validator 的规则，列表内工具 Id 不能重复。
- **既有代码的连带修改**：
  - 工具删除用例的「被 Agent 引用拒删」改为只查 `Type = Tool` 且 `TargetId` 等于该工具的行；整包绑定不计。
  - `UserToolConfiguration` 实体仍按现状（本 spec 不收口它的写法）。
- **EF 与数据**：
  - `AgentEntity` 映射不变（指令列不限长）。`AgentTool` 映射：`HasMany(Tools).WithOne().HasForeignKey(AgentId).OnDelete(Cascade)`；索引 TenantId、AgentId、TargetId，全部非唯一（沿 ADR 0001）。
  - 在现有迁移之后**新增一个 EF 迁移**：删除 `AgentToolPackages` 表；`AgentTools` 表加 `AgentId`（非空）、`Type`，把 `ToolId` 改为 `TargetId`，去掉 `AgentToolPackageId`。
  - **应用该迁移前必须先硬删 dev 库里 `AgentToolPackages` 与 `AgentTools` 两张表的全部行（含已软删的）**：旧行没有所属 Agent，加非空列会失败。不写数据迁移。迁移只能由 `dotnet ef` 生成，不手改；生成后先看内容，出现上面列出的操作之外的改表动作就停下来报告。
- **文档同步**：`Backend/AGENTS.md` 中「实体映射按聚合域分组：Agent / Conversation / Model / Tool 四组」的先例描述补一句 Agent 组只剩 `AgentEntity` 与 `AgentTool`；`Frontend/cike.os/AGENTS.md` 的目录结构、测试说明补 `agents/`、`tool-configurations/` 两个模块目录。

### 前端

- **路由与菜单**（nav 由路由 meta 推导）：`/agents` 路由不变、页面重写；新增 `/tool-configurations`（标题「工具参数」，nav，图标 lucide `SlidersHorizontal`），排在「工具包分类」之后、「标签」之前。侧栏变为八项：对话 / Agent / 模型提供方 / 工具包 / 工具包分类 / 工具参数 / 标签 / 设置。
- **Agent 列表页**：工具条（关键字搜索、「新建 Agent」）+ 表格（名称 / 描述 / 创建时间 / 更新时间 / 操作：编辑、删除）+ 分页，默认 `UpdatedAt desc`。
  - 点「编辑」：先按 Id 取详情，成功后才打开对话框；失败 toast 且不打开。
  - 删除确认文案：「确定删除 Agent「{名称}」？已有会话仍可查看，但不能再对话。」
- **Agent 表单对话框**（新建 / 编辑共用，两步，步骤标题可点击切换，底部始终有「上一步」（第一步隐藏）与「保存」）：
  - 第一步「基本信息」：名称、描述、指令（大号多行）；「模型参数」组：温度、输出上限、TopP、推理力度（列出枚举全部值）、工具模式（自动 / 不使用 / 必须使用）；「高级配置」默认折叠：TopK、频率惩罚、存在惩罚、Seed、停止词（逐条输入，最多 16 条）、推理输出、响应格式（文本 / JSON；JSON 时显示名称与 Schema 多行框）、允许多次工具调用、允许后台响应（开关）。所有模型参数留空 = 不设置。
  - 第二步「工具包」：顶部搜索框；按「分类 → 工具包」分组平铺，分类与工具包可折叠；工具包行是「整包」勾选，下面是该包工具的勾选（名称 + 形态徽标）。勾「整包」= 工具全勾；整包状态下取消其中一个工具 = 整包自动取消、包内其余工具转为单选保留；搜索只过滤工具行，空包 / 空分类跟随隐藏。
  - 「保存」提交整个表单；新建成功后按跳转规则处理，编辑成功后关闭并刷新列表。
  - 跳转规则（仅新建）：取提交的工具行展开成「生效的工具」（`Tool` 行本身 + `ToolPackage` 行对应包下的全部工具），其中有参数定义且当前用户尚未配置的 → toast 并跳到 `/tool-configurations?tools=<逗号分隔 Id>&from=<Agent 名称>`（已配置的不算待配置，前端拿用户工具配置的 ToolOptions 过滤）；没有 → 关闭并刷新列表。
- **工具参数页**：顶部（来自 Agent 的提示条，有 `from` / `tools` 查询参数时显示：「来自 Agent「X」：N 个工具待配置」+「立即配置」+ 关闭；进入页面时自动打开批量弹窗并预选这些 Id）；工具条（关键字搜索、「新增配置」）；卡片网格（每页 12 张，`UpdatedAt desc`）。
  - 卡片：工具名 + 形态徽标 + 「缺必填」徽标（任一必填参数在已存值里缺失）；每个参数一行「参数名：值」，`Sensitive` 的值默认 `••••••`、眼睛图标切换明文；底部「编辑」「删除」。删除确认：「确定删除「{工具名}」的参数配置？」
  - 「编辑」→ 单工具弹窗：按工具的参数定义生成表单（`Name` 标签、`Description` 提示、`Required` 星号、`Sensitive` 密码框），保存走现有单条 Save。
  - 「新增配置」→ 批量弹窗：上半「选择工具」多选（候选 = 工具选项里未配置的，按工具包分组显示）；下半每个选中工具一段表单；必填未填的工具段标红、「全部保存」禁用；保存走 SaveBatch，成功后关闭、刷新列表。
- **纯函数层**（vitest 接缝）：Agent 表单状态 ↔ 新建 / 更新请求体（含工具模式枚举 ↔ `$type` 对象、推理力 / 推理输出 ↔ `Reasoning` 对象、响应格式、停止词列表）；分组勾选状态 ↔ `{ type, targetId }` 行（提交与编辑回填两个方向，含整包 / 单选互斥）；「生效的工具里哪些带参数」；前端自身校验（与后端规则一致）；工具参数的值 ↔ 表单、必填校验、缺必填判定、敏感值遮罩。
- **API 层**：后端跑起后执行 `pnpm gen:api` 重新生成，组件只从生成层导入函数与类型，不手写 URL。
- **组件引入**：复用已引入的 table / dialog / alert-dialog / select / badge / input / label / textarea / popover / command；checkbox、collapsible、card 如未引入则用 shadcn-vue CLI 引入，不手写基础组件、不加新的 pnpm 依赖。表单内所有非提交按钮显式 `type="button"`；可滚动表单容器是 grid 时加 `content-start`；页脚放在滚动区外。

## Testing Decisions

- **测试接缝**（全部沿用现有接缝，不新增）：
  - **后端业务规则**：从发布 Command / Query（经 `ILocalEventBus`）这一最高点进入，使用既有 SQLite 内存测试宿主。先例 `ModelProviderTests`、`TagTests`。
  - **后端校验**：FluentValidation 的 `TestValidate`。先例 `ModelProviderDtoValidatorTests`、`SaveUserToolConfigurationDtoValidatorTests`。
  - **HTTP 形状**：真实宿主 `ServiceOpenTestHost`。先例 `ModelProviderHttpTests`。
  - **EF 行为**：既有实体映射冒烟测试。先例 `EntityConfigurationSmokeTests`。
  - **前端**：纯函数接缝（vitest）+ 浏览器走查兜底，不写组件测试。先例 `provider-form.test.ts`、`tag-form.test.ts`、`nav-items.test.ts`。
- **好的测试**：只断言可观察行为——保存后读出来是什么、什么被拒绝、HTTP 响应的状态码 / 内容类型 / 响应体形状；不断言实体私有方法、不依赖 SQL。
- **Agent 用例清单**（新建 `AgentTests`）：带整包 + 单选两种工具创建 → 详情返回两类行；不带工具创建 → 工具列表为空；同租户重名拒绝且不保存、跨租户同名放行、删掉后同名可重建；更新改全部字段并把工具整体替换（移除的行软删、新增的追加）；提交重复的 `(Type, TargetId)` 被拒；引用不存在的工具包 / 工具被拒且不保存；工具所属包同时被整包绑定被拒；删 Agent 后详情为空；别的租户的 Id 取详情为空、更新 / 删除按「Agent 不存在」被拒；工具选项只含有工具的包与分类、各层按名称排序。
- **用户工具配置用例**（扩展 `UserToolConfigurationTests`）：批量保存一次新增两条、一次更新一条新增一条；必填缺失整批拒绝且什么都不保存；分页列表按工具名关键字过滤、字段（工具 / 包 / 分类名、参数定义、值）正确、只含当前用户的配置；工具选项的已配置标记正确。
- **校验用例**：新建 / 更新 Agent 两个 Validator（名称为空 / 超长、描述超长、指令为空、Options 为 null、Tools 为 null 放行为空列表、枚举非法、TargetId 为 0；温度 / TopP / TopK / 输出上限 / 惩罚 / Seed 越界各一例；停止词超 16 条、空串、超长；响应格式为 Json 时 Schema 缺失 / 非法 JSON；工具模式非三种之一）；批量保存 Validator（列表为 null / 为空、工具 Id 重复、单项规则）。
- **HTTP 用例**（新建 `AgentHttpTests`，扩展 `UserToolConfiguration` 的 HTTP 测试）：动词与路径；新建返回字符串形式的 Id 且详情能按它取回；详情不存在 404；校验失败 400 + ProblemDetails；业务拒绝 400 + 消息文本；工具模式按 `{"$type":"auto"}` 发送并以同样形状返回；按租户头隔离；工具选项、分页、批量保存三条新路由可用。
- **EF 冒烟**（改写 `Agent_Domain_RoundTrip`）：Agent 带两类工具保存时自动生成雪花 Id、写入租户与 `AgentId`；一级预加载能把 Agent 连同工具查全；从集合移除工具后保存即软删；删 Agent 连带工具软删；`AgentOptions` 含工具模式（`Auto`）与推理选项能 JSON round-trip。
- **前端用例**：路由表 nav 为八项且含「工具参数」；Agent 表单纯函数（转新建 / 更新请求体、工具模式与推理的对象映射、勾选状态 ↔ 行的两个方向、整包 / 单选互斥、带参数工具的判定、前端校验）；工具参数纯函数（必填校验、缺必填判定、遮罩）。
- **验收**：`dotnet build` 0 新增警告、`dotnet test` 全绿；抓 swagger 核对工具模式的 `$type` 多态形状；`pnpm gen:api` 后看生成层 diff、`pnpm build`（含 vue-tsc）与 `pnpm test` 通过；dev 库真实 HTTP 冒烟通过；浏览器走查：新建 Agent（整包 + 单选）→ 自动跳到工具参数并预选 → 批量保存两个工具 → 卡片显示、敏感值点眼睛显示 → 编辑一个 → 新增配置第三个 → 回 Agent 编辑（取消整包里的一个工具）→ 重名被拒 → 删除 Agent；另走一遍「新建不带工具不跳转」。

## Out of Scope

- 调用模型的运行时执行链路、Agent 绑定模型、最大迭代次数 / 上下文轮数等运行时设置。
- 会话模块：删除 Agent 后会话详情如何显示 Agent 名称（需会话侧禁用软删过滤读取），由会话模块的 spec 处理。
- 保存 Agent 时校验模型选项是否被模型支持、校验绑定工具的参数是否已配置。
- 工具删除守护的策略本身（只改查询列）；工具包 / 分类删除守护。
- `UserToolConfiguration` 实体的 DDD 收口、敏感参数值的加密存储与读取侧脱敏。
- 权限体系、平台写端点的角色鉴权；「设置」模块。
- 其它聚合的边界复核。

## Further Notes

- **发布通道**：本仓库的 issue tracker 与 triage 标签词表仍未配置（`docs/agents/` 不存在、本机无 gh CLI），本 spec 沿前例以仓库内 markdown 落盘并标注 `ready-for-agent`，tracker 就绪后再搬运。
- **设计来源**：结构与校验规则由 2026-11-27 的设计访谈逐条定稿，`CONTEXT.md` 与 ADR 0008 已先行落盘。被否决的方案见 ADR 0008（绑模型、三层聚合、两张一级表、工具快照展开）。
- **契约纪律**：`AgentOptions` 的字段 = API 承诺形状（沿 ADR 0003）；`ChatToolMode` 的 `$type` 是字串判别，与项目自有的 int 判别多态并存，swashbuckle / `gen:api` 对它的形状要实际生成一次核对（项目现有的 SchemaFilter 只处理 int 判别）。`JsonHelper` 的 `AllowOutOfOrderMetadataProperties` 保证 MySQL json 列重排后 `$type` 不在首位也能读回。
- **风险（迁移）**：`AgentTools` 加非空 `AgentId` 依赖先清空存量行；应用迁移前按上文硬删两张表，不要带 `--no-build`。
- **环境事实**：前端开发环境地址指向 https 端口，后端须以 `https` launch profile 启动；运行中的主服务会锁输出 DLL，编译前先停、验证完再拉起。用户常自己开着 Vite，不要动。
- **验证链**：build + `dotnet test` + 抓 swagger + `pnpm gen:api` 后看生成层 diff + `pnpm typecheck` 一趟过；SQLite 测试不能替代 MySQL 行为验证。
- **实施结果（2026-11-27，未提交待 review）**：
  - 后端：`AgentEntity` 收口（`Create` / `Update` / `SyncTools`，聚合根上的意图方法沿 `ModelProvider` 先例保持 `public`，以便 EF 冒烟测试经仓储驱动同步）；`AgentTool.Create(type, targetId)` 不收 agentId——外键由聚合保存时写入（与 `ModelEntity` 先例一致）；`AgentOptions` 搬 Domain.Shared 并引用 M.E.AI.Abstractions 10.11.0（版本属性 `MicrosoftExtensionsAiVersion`）；`AgentToolType` 枚举；`AgentToolPackage` 删除。迁移 `20261127105243_AgentToolAsAgentSubEntity`（删 `AgentToolPackages` 表，`AgentTools` 的 `ToolId`→`TargetId`、`AgentToolPackageId`→`AgentId`，加 `Type`，加指向 `Agents` 的级联外键），应用前已硬删 dev 两表存量行，已应用到 dev MySQL。
  - 推理选项校验：`Reasoning` 下 `Effort` / `Output` 各自可空（M.E.AI 语义：null = 用提供方默认），只校验设置了的那一个（走查发现「只设力度」被 400 后改）。
  - `AgentToolOptionItemDto` 增加 `ParameterCount`（参数定义是 json 列，取回后内存里数），供前端判定新建后是否跳转填参数。
  - 用户工具配置分页：框架的 `ToPaginationAsync` 扩展物理位于 Cike.Data.EFCore 包、Application 层引用不到，改为「配置仓储上 LINQ 联合工具表 + 仓储 `ToPagedListAsync(IQueryable<TEntity>)` 分页，再按页内工具 Id 批量取工具 / 包 / 分类、`Adapt` 后回填」。现有 `DELETE /UserToolConfigurations/{id}` 的 `{id}` 语义是工具 Id（与 Get / Save 一致），前端删除卡片传 `toolId`。
  - 开发态当前用户：主服务在 Development 下没有认证也就没有 `ICurrentUser.Id`，用户工具配置接口会 400；宿主模块新增 `DevelopmentCurrentUser`（固定 `dev-user`）替换注册。
  - 测试：`CikeOSTestsModule` 补 `DependsOn(CikeFluentValidationModule)`——HTTP 宿主与用例宿主共用进程级的 Validator 类型缓存，否则 ProblemDetails 断言随机失败（新增两个 HTTP 测试类后暴露）；`ServiceOpenTestHost` 增加假当前用户替换与 `PublishAsync` 造数入口。`dotnet test` 229/229。
  - 前端：`lib/agent-options.ts`（枚举 / 选项 / `SELECT_UNSET` 哨兵）、`components/agents/`（`agent-form.ts` + 测试、`AgentToolPicker.vue`、`AgentFormDialog.vue`）、`components/tool-configurations/`（`tool-configuration-form.ts` + 测试、`ToolParameterFields.vue`、`ToolConfigurationCard.vue`、`ToolConfigurationEditDialog.vue`、`ToolConfigurationBatchDialog.vue`）、`pages/AgentsPage.vue`、`pages/ToolConfigurationsPage.vue`；shadcn 新增 `collapsible`、`card`；侧栏八项。vue-tsc 0 错、vitest 54/54、`pnpm build` 通过。
  - 浏览器走查三轮：第一轮抓到 4 个 bug（Select 空串 value 卡死、`type=number` 的 v-model 转成 number 后 `.trim()` 炸、编辑弹窗受控输入没用 `v-model` 导致拿不到输入、删除传了配置 Id 而非工具 Id），第二轮抓到 2 个（推理力度单设被 400、数字枚举 Select 选项与字符串模型类型不一致导致回显空白），第三轮全部通过；顺手把「来自 Agent」提示条改为批量保存后自动收掉（否则刷新会再次弹出）。规则已写入两份 AGENTS.md。
