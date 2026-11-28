# Agent 不绑定模型，工具绑定合并为 AgentTool 单表

主流产品（Dify、Coze、Open WebUI、Microsoft Agent Framework）的 Agent 都绑定模型；本系统**不绑**：模型在运行时选择，Agent 上的模型选项（`AgentOptions`，直接复用 Microsoft.Extensions.AI 的类型）只是默认值，运行时可调整，不支持的选项由框架层按模型能力做 best-effort 映射。工具绑定原来拆成 `AgentToolPackage`（独立聚合根）→ `AgentTool` 两层；决定合并为 Agent 聚合内**唯一的一级子集合** `AgentTool`（`AgentId` / `Type` / `TargetId`）：`Type = ToolPackage` 表示整包绑定，包内全部工具（含之后新增的）生效、不记录具体工具；`Type = Tool` 表示只绑定单个工具。形状与 OpenAI Responses 的 `mcp_server` + `allowed_tools`（不填 = 该服务全部工具）同构。

## Considered Options

- **Agent 绑定模型**（主流做法）：本系统的模型在运行时选择，绑了反而要回答「绑哪个提供方、哪种协议」。
- **三层 Agent → AgentToolPackage → AgentTool**：最直觉，但聚合三层需要两级预加载，是 ADR 0006 否决过的结构。
- **两张表都挂在 Agent 一级，`AgentTool` 靠 `ToolPackageId` 隐式归属于绑定行**：多一张表、一个集合，绑定行与工具行的从属关系靠外部 id 隐式表达。
- **只留 `AgentTool(AgentId, ToolId)`，整包绑定在绑定时展开为快照**（Dify / Claude Code 形态）：包以后新增的工具不生效，与「全选即整包」的语义矛盾。

## Consequences

- `AgentTool` 从聚合根改为 Agent 聚合内子实体（`FullAuditedEntity`），`AgentToolPackage` 表删除；dev 库旧数据清空，不写数据迁移。
- 聚合内不变量只有「同一 `(Type, TargetId)` 不重复」；「引用的工具包 / 工具存在」「同一工具包没有既整包绑定又单选其工具」放 Handler。
- 接口与前端：Agent 表单第二步行是「分类 → 工具包」分组平铺的勾选列表，工具包行勾「整包」即绑定全部工具，勾单个工具即单选；提交时拍平成 `{ type, targetId }` 行。（2026-11-28 修订，spec 0011：选择树改为「工具包 → 工具」两层，不再按分类分组——分类只是工具包列表的检索维度，勾选工具时用不上。）
- 工具删除时的「被 Agent 引用拒删」改为只查 `Type = Tool` 的行；整包绑定不计——包里少一个工具只是不生效，不需要守护。
- 保存 Agent 时不校验模型选项是否被模型支持，也不校验绑定工具的参数是否已配置。新建 Agent 保存后若绑定的工具里有声明了参数的，前端跳转到独立的「工具参数」模块并自动打开批量新增弹窗预选这些工具；编辑不检测。用户工具配置是「用户 + 工具」维度的独立聚合，与 Agent 无关。
