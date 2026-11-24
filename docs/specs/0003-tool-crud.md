# Spec: 工具包分类 / 工具包 / 工具 增删改查（前后端）

> Triage: `ready-for-agent`
> 术语遵循根 `CONTEXT.md`（工具、工具包、工具包分类、技能包）；存储取舍见 `docs/adr/0002-技能包本地磁盘存储与上传分离.md`；索引非唯一见 ADR 0001。

## Problem Statement

领域层已有 `ToolPackageCategory`、`ToolPackage`、`Tool` 三个实体及 EF 映射，但 Application / Contracts / Service.Open 三层没有任何用例与端点，前端 `/tools` 是占位页。管理者无法在管理端维护工具包分类、工具包及其下的工具。此外技能形态工具依赖一整个目录的文件，而项目没有任何文件上传与存储能力；`ToolType` 枚举只有 `Skill / Mcp` 两值，与术语表的「技能 / HTTP MCP / 标准 IO MCP」三种形态及 `ToolSetting` 的三个多态子类不一致。

## Solution

后端按既有模式（Application 层 Command / Query / Validator + `[LocalEventHandler]`，Contracts 只放 DTO，宿主 `MinimalApiServiceBase` 约定路由）为三个实体各补齐增删改查与列表查询，新增独立的技能包 zip 上传端点；`ToolType` 扩为三值以对齐术语表。前端把「工具」菜单拆为「工具包」与「工具包分类」两个一级菜单：工具包列表创建成功后自动进入工具包详情页，在详情页内维护工具；工具编辑用宽 Sheet，技能形态内嵌 zip 上传。

## User Stories

1. 作为管理者，我在「工具包分类」菜单里能看到全部分类、并能新建 / 重命名 / 删除，以便为工具包准备归属。
2. 作为管理者，删除一个仍被工具包引用的分类时被拒绝并看到明确提示，以便不会产生无归属的工具包。
3. 作为管理者，我在「工具包」菜单里能按关键词搜索、按分类与状态筛选并分页浏览工具包，以便在数量增长后仍能快速定位。
4. 作为管理者，新建工具包成功后自动进入该工具包的详情页，以便立刻继续为其添加工具。
5. 作为管理者，我能编辑工具包的基本信息、描述、Readme、图标、分类与状态，并能在详情页删除它；当它下面还有工具时删除被拒绝。
6. 作为管理者，在工具包详情页里我能看到该包下的全部工具（不分页）并按关键词即时过滤，以便包内工具不多时一眼看全。
7. 作为管理者，我在工具包详情页新建 / 编辑工具时，表单依次是基本信息、参数定义、形态、设置，参数定义可增删行，以便先定义参数再让设置引用它们。
8. 作为管理者，工具形态为「技能」时我选择一个 zip 后立即上传，看到返回的目录路径后才允许保存，以便工具一定引用一个真实存在的技能包。
9. 作为管理者，工具形态为「HTTP MCP」或「标准 IO MCP」时填写对应设置，附加请求头 / 环境变量的值可选用字面量或引用已定义参数，以便凭据等敏感值不写死。
10. 作为管理者，切换工具形态时会先确认并清空设置区，以便不会误把 A 形态的设置提交成 B 形态。
11. 作为管理者，删除工具时它对应的用户工具配置一并失效，以便不留下指向不存在工具的配置。
12. 作为管理者，同一租户内我用重复的工具 / 工具包 Code 创建时被拒绝；软删后同 Code 可重建，以便编码复用不被历史数据阻塞。
13. 作为管理者，上传的 zip 不是合法压缩包、为空、超过大小上限、含路径穿越条目或包根没有 `SKILL.md` 时被拒绝并得到提示，以便坏包不落盘。
14. 作为管理者，编辑技能工具重新上传 zip 后旧目录被清理、不重传则保留原目录，以便替换不会丢文件也不会留垃圾。
15. 作为后端开发者，我读到 `ToolType` 与 `ToolSetting` 子类一一对应，以便不用在两套分类之间换算。
16. 作为前端开发者，我拿到按本 spec 生成的 `src/api` 函数就能调用全部端点，以便不手写 URL。
17. 作为维护者，技能包存储的取舍有 ADR 记录、术语表收录了「技能包」，以便后人不会把 guid 目录与容忍孤儿当 bug「修掉」。
18. 作为未来的实现 agent，我拿到本 spec 即可独立完成全部实现（决策均已定稿），不必再做架构裁决。

## Implementation Decisions

### 后端

- **枚举**：`ToolType` 改为 `Skill = 1, HttpMcp = 2, StdioMcp = 3`（库中无存量数据；列未显式配置类型，按 int 存储，**不需要迁移**）。`ToolPackageStatus` 不动。
- **用例目录**（均含 `Commands/`、`Queries/`、`Validators/`，一类型一文件，Handler 以 `Handler` 结尾）：
  - `Application/ToolPackageCategories/`：`GetList`（全量，按 Name 排序）、`Get`、`Create`、`Update`、`Delete`。
  - `Application/ToolPackages/`：`GetPagedList`、`Get`、`Create`、`Update`、`Delete`。
  - `Application/Tools/`：`GetList`（按 `ToolPackageId` 全量，按 Code 排序）、`Get`、`Create`、`Update`、`Delete`。
  - `Application/SkillPackages/`：`Upload`（接收 `Stream` + 文件名 + 长度，不含 HTTP 类型）。
- **端点**（`Service.Open/Services/`，约定路由，前缀 `api/v1`）：
  - `ToolPackageCategoryService`：`GetListAsync` → `GET /ToolPackageCategories/List`；`GetAsync(id)` → `GET /ToolPackageCategories/{id}`；`CreateAsync(dto)` → `POST`；`UpdateAsync(dto)` → `PUT`；`DeleteAsync(id)` → `DELETE /{id}`。
  - `ToolPackageService`：`GetPagedListAsync([AsParameters] GetToolPackagePagedListDto)` → `GET /ToolPackages/PagedList`；其余同上形态。
  - `ToolService`：`GetListAsync(long toolPackageId)` → `GET /Tools/List?toolPackageId=`；其余同上形态。
  - `SkillPackageService`：`UploadAsync(IFormFile file)` → `POST /SkillPackages/Upload`，返回 `{ directory }`。端点只负责打开流并转发 Command；若 .NET 表单绑定要求防伪造令牌，仅在该端点关闭（实施时读框架源码确认是否已全局处理，不凭记忆）。
- **DTO**（`Application.Contracts/<聚合>/Dtos/`）：
  - 分类：`ToolPackageCategoryDto : AuditedEntityDto<long>`（Name）、`CreateToolPackageCategoryDto`、`UpdateToolPackageCategoryDto`。
  - 工具包：`ToolPackageDto`（含 `ToolPackageCategoryName`，列表与详情在 Handler 内一次性批量取分类回填，不逐行查库）、`GetToolPackagePagedListDto : PagedAndSortedResultRequest`（Keyword / ToolPackageCategoryId? / Status?）、`CreateToolPackageDto`、`UpdateToolPackageDto`。
  - 工具：`ToolDto`（Id / Code / Name / Description / Type / ToolPackageId / Parameters / Setting）、`ToolParameterDefinitionDto`、`ToolValueDto`（Type / Value）、`ToolSettingDto` 多态（`[JsonPolymorphic]` 判别键 `type`：`skill` / `httpMcp` / `stdioMcp`，字段与领域值对象一一对应，TimeSpan 对外暴露为「秒」整数）、`CreateToolDto`、`UpdateToolDto`。
  - 技能包：`UploadSkillPackageResultDto`（Directory）。
- **Validator**（FluentValidation）：Code / Name 必填与长度；`Type` 在枚举范围内且 `Setting` 的判别类型与之匹配；`Parameters` 内 Code 非空且不重复；引用型 `ToolValue` 的 Value 必须命中某个参数 Code；技能形态 `Directory` 必填。统一走类级 `[AutoValidation]`。
- **写侧规则**（Handler 内）：
  - 引用校验：建工具包时分类必须存在；建工具时工具包必须存在。
  - Code 唯一：同租户内 `GetCountAsync(x => x.Code == code && x.Id != id)` > 0 即抛 `UserFriendlyException`（软删过滤生效；并发窗口按 ADR 0001 接受）。分类 Name 不查重。
  - 级联：删分类 → `GetCountAsync` 子包 > 0 拒绝；删包 → 子工具 > 0 拒绝；删工具 → 先查 UserToolConfiguration 列表批量软删，再查 AgentTool 引用数 > 0 拒绝。
  - 状态：`ToolPackageStatus` 仅校验枚举合法，不做流转限制。
  - 技能目录：Update 后若 Directory 变化则删旧目录；Delete 技能工具后删目录；删除失败只记日志。
- **技能包存储**：`Application/SkillPackages/` 内定义 `ISkillPackageStorage` 接口 + 本地磁盘实现（标记接口自动注册），选项 `SkillStorageOptions { RootPath, MaxUploadBytes = 50MB }` 在宿主 `appsettings` 的 `SkillStorage` 节绑定，开发默认 `./data/skills`。目录布局 `{RootPath}/{tenantId}/{guid}`，返回相对路径 `skills/{tenantId}/{guid}`。解压用 `System.IO.Compression`（不新增 NuGet 依赖），解压前校验：长度不超上限、zip 可打开且至少一个条目、每个条目的规范化完整路径位于目标目录内（zip-slip）、根目录存在 `SKILL.md`；任一失败清理临时目录后抛 `UserFriendlyException`。
- **实体写法**：沿用项目既有风格（Handler 内对象初始化 + 仓储），不引入静态工厂改造；查询侧用 `IReadOnlyRepository` + `BeginAsNoTracking()`，实体 → DTO 用 Mapster `Adapt`，非平凡映射（TimeSpan ↔ 秒）在 Application 模块注册 `TypeAdapterConfig`。

### 前端

- **路由与菜单**（`src/router/routes.ts`，nav 由 meta 推导）：删除 `/tools` 与 `ToolsPage.vue`；新增 `/tool-packages`（「工具包」，nav）、`/tool-packages/:id`（「工具包详情」，非 nav）、`/tool-package-categories`（「工具包分类」，nav）。侧栏顺序：对话 / Agent / 模型 / 工具包 / 工具包分类 / 标签 / 设置。`nav-items.test.ts` 的真实路由表用例同步改为七项。
- **页面**（`src/pages/`）：
  - `ToolPackagesPage.vue`：工具条（keyword 输入、分类下拉、状态下拉、新建按钮）+ 表格（Icon / 名称 / Code / 分类 / 状态 badge / 描述截断 / 操作：编辑、删除）+ 分页；空态与骨架屏。创建成功后 `router.push` 到详情页。
  - `ToolPackageDetailPage.vue`：头部（Icon、名称、Code、状态 badge、分类名、描述、Readme 折叠、编辑 / 删除按钮、返回列表）；工具区（keyword 输入 + 新建工具 + 表格：Code / 名称 / 形态 / 参数数 / 操作：编辑、删除）。keyword 在内存过滤，不调接口。路由参数对应的工具包不存在时显示空态并提供返回。
  - `ToolPackageCategoriesPage.vue`：新建按钮 + 表格（名称 / 创建时间 / 操作：重命名、删除），全量不分页。
- **业务组件**（`src/components/`，PascalCase，表单 `*Form*.vue`、对话框 `*Dialog.vue`）：
  - `tool-package-categories/ToolPackageCategoryFormDialog.vue`（新建 / 重命名共用）。
  - `tool-packages/ToolPackageFormDialog.vue`（基本信息 + 描述 + Readme textarea；新建 / 编辑共用）。
  - `tools/ToolEditSheet.vue`（右滑 ≥560px，内部滚动；区块顺序：基本信息 → 参数定义 → 形态 → 设置 → 底部取消 / 保存）、`tools/ToolParameterRows.vue`、`tools/SkillSettingFields.vue`、`tools/HttpMcpSettingFields.vue`、`tools/StdioMcpSettingFields.vue`、`tools/ToolValueRows.vue`（键值行，值类型下拉：字面量 / 参数引用）。
  - 所有删除确认用 `AlertDialog`；错误统一 `toast.error(getErrorMessage(error))`。
- **交互细则**：
  - 形态用 RadioGroup；切换时若设置区已有内容先 `AlertDialog` 确认，确认后清空设置区（技能区清空已上传目录）。参数定义区不受影响。
  - 技能区：选择 zip 即调用上传接口，上传中显示进行态，成功后显示目录路径与「重新选择 zip」；新建且未上传时保存按钮禁用；编辑时显示当前目录，不重传则保留。
  - 保存前前端轻校验：引用型 ToolValue 的参数 Code 必须在参数定义中存在；必填项未填时定位到对应字段。
  - Icon：值为 `http(s)://` 时渲染 `img`，否则显示 lucide `Package` 图标（按名称动态解析 lucide 图标的能力在实施时核实 `@lucide/vue` 是否提供，有则启用，无则维持 fallback）。
  - 状态 badge：Draft / Published / Offline 三态映射为 const 对象 + 字面量联合类型（`erasableSyntaxOnly` 禁 enum），颜色只用既有语义 token 变体，不新增色相。
- **API 层**：后端跑起后执行 `pnpm gen:api` 生成 `src/api`，组件只从 `@/api` 导入生成函数与类型，不手写 URL。
- **组件引入**：`pnpm dlx shadcn-vue@latest add` 引入 table / dialog / alert-dialog / select / form / label / textarea / badge / radio-group / pagination（及其依赖），不在 `ui/` 生成物上反复打补丁。
- **模块页骨架约定**：本页确立「页面工具条（搜索 / 筛选 / 主操作）+ 内容区 + 空态 / 骨架屏」结构作为后续模块页参照，**不抽共享组件**（单样本不抽象）。

## Testing Decisions

- **后端**（沿用 `Cike.OS.Tests` 的 SQLite 内存测试宿主，覆盖外部行为）：
  - 三个实体各一组：创建 / 读取 / 更新 / 删除 / 列表筛选与分页。
  - 写侧规则：Code 重复拒绝（含软删后重建放行）、删分类 / 删包有子项拒绝、删工具级联软删用户工具配置且被 AgentTool 引用时拒绝、Type 与 Setting 不匹配被 Validator 拒绝。
  - 技能包：合法 zip（含 `SKILL.md`）落盘并返回相对路径；缺 `SKILL.md`、空包、非法 zip、zip-slip 条目、超大小各一例拒绝且不残留目录。测试把 `RootPath` 指向临时目录。
  - 回归：现有全部测试通过。
- **前端**：只为抽出的纯函数加 vitest——工具列表 keyword 过滤、状态 badge 映射、表单校验（引用参数存在性）；不写组件测试，界面行为由浏览器走查验收。
- **验收**：`dotnet build Cike.OS.slnx` 0 警告、`dotnet test` 全绿；`pnpm build`（含 `vue-tsc`）通过；`pnpm test` 通过；浏览器走查三条用户路径（分类管理、建包 → 详情 → 建三种形态工具、编辑重传技能包）。

## Out of Scope

- 孤儿技能包目录的回收任务。
- 对象存储 / 多实例共享卷。
- Agent 与工具包的绑定管理（AgentToolPackage / AgentTool 的用例），本 spec 只在删工具时检查其引用。
- 用户工具配置的界面（既有后端用例不动）。
- 技能包内容的运行时消费（如何被 Agent 执行读取）。
- 工具包的发布审核流、版本管理、状态机流转限制。
- 登录态、权限守卫、移动端适配。
- 把模块页骨架抽成共享组件（等第二个模块页出现再评估）。

## Further Notes

- **契约形状已变更（实现后用户决定）**：工具设置的 6 个 `*Dto` 镜像与 `ToolSettingMapper` 已删除，值对象直接作为 API 契约；判别键 `type` 为 `ToolType` 数值，`TimeSpan` 以 ISO 字串传输。详见 `docs/adr/0003-工具值对象直接作为API契约.md`，本节以下关于多态 DTO / 秒字段 / Mapster 的描述以 ADR 0003 为准。
- **测试接缝**（全仓只复用两个既有接缝，不新建）：后端唯一接缝是 `Cike.OS.Tests` 的测试宿主（SQLite 内存库，既有先例：用户工具配置集成测试、实体映射冒烟测试），所有后端用例从「发布 Command / Query」这一最高点进入；前端唯一接缝是 vitest 纯函数（既有先例：`nav-items` 推导），只覆盖 keyword 过滤、badge 映射、表单校验三个纯函数。
- **实施顺序**：1. 后端：`ToolType` 扩值 → 三实体用例 + Validator + 端点 → 技能包存储与上传 → 测试。
2. 前端：起后端 → `pnpm gen:api` → add 组件 → 路由与三页面 → Sheet 与子表单 → 纯函数测试。
3. 联调走查；`Frontend/cike.os/AGENTS.md`「尚未搭建」段中关于 vue-router 的描述已过时，仅在本次触碰范围内顺手修正。
- **实施期待核实**（读源码后定，不是待裁决项）：.NET 10 Minimal API `IFormFile` 表单绑定是否需要防伪造令牌及框架是否已全局处理；`@lucide/vue` 是否提供按名称动态解析图标。
- **发布通道**：本仓库的 issue tracker 与 triage 标签词表尚未配置（`docs/agents/issue-tracker.md` 不存在），本 spec 先以仓库内 markdown 落盘并标注 `ready-for-agent`，tracker 就绪后再搬运。
