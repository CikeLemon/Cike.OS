# Cike.OS

基于 Microsoft Agent Framework 的 Agent 管理平台基建，统一管理 Agent 及其可挂载的能力，后续支撑办公与业务流程场景。

## Language

### Agent 与模型

**Agent**:
由提示词、推荐模型与挂载的插件构成的智能体定义；定义本身不决定实际使用哪个模型。
_Avoid_: 智能体应用、助手、专家、Bot

**模型服务（Model Provider）**:
租户自行配置的一个大模型接入（类型、地址、密钥），归该租户私有。
_Avoid_: 模型供应商配置、模型渠道

**模型（Model）**:
某个模型服务下一个具体可用的模型及其能力标记（如是否支持工具调用、识图）。
_Avoid_: 模型配置、部署

**推荐模型（Recommended Model）**:
Agent 定义上记录的建议模型，仅作提示；实际模型由使用方租户在 Agent 安装时绑定。
_Avoid_: 默认模型

### 能力

**工具（Tool）**:
可挂载到 Agent 上的一项能力配置，按类型分为技能、MCP 服务、OpenAPI 服务；平台只管理到这一层，其内部具体可调用的函数在运行时获取。工具总是属于某个插件，不独立上架，也不单独挂载。
_Avoid_: 能力、函数

**技能（Skill）**:
一类工具：遵循 Agent Skills 开放格式的指令、脚本与资源包（以 `SKILL.md` 为入口），由 Agent 按需渐进加载。
_Avoid_: 技能包、Prompt 模板、知识包

**MCP 服务（MCP Server）**:
一类工具：连接某个 MCP Server 所需的配置（传输方式、地址、认证等），其暴露的函数清单在运行时向服务端获取。
_Avoid_: MCP 工具、MCP 插件

**OpenAPI 服务（OpenAPI Service）**:
一类工具：由一份 OpenAPI 描述文档及其调用配置（服务地址、认证等）构成的 HTTP 接口集合。
_Avoid_: API 插件、HTTP 工具

**插件（Plugin）**:
由 1 到 N 个工具打包而成、作为整体上架与安装的单位，不包含 Agent。暂无版本概念，归属租户的修改对所有已安装租户即时生效。
_Avoid_: 工具包、扩展、Extension

**挂载（Mount）**:
Agent 与插件之间的关联，Agent 运行时可用其所挂载插件当前包含的全部工具；权限边界靠插件的拆分粒度保证，而非在挂载时挑选工具。
_Avoid_: 绑定、引用、依赖

### 市场

**插件市场（Plugin Market）**:
面向 Agent 搭建者分发插件的市场，上架单位是插件。
_Avoid_: 工具市场、技能市场

**Agent 市场（Agent Market）**:
面向最终使用者分发 Agent 的市场，上架单位是 Agent；Agent 通过其挂载的插件与插件市场关联。
_Avoid_: 应用市场、助手广场

**归属租户（Owner Tenant）**:
创建并拥有某个插件或 Agent 的租户，只有它能修改、发布该插件或 Agent。
_Avoid_: 发布方、作者租户

**平台租户（Platform Tenant）**:
代表平台方自身的特殊租户，官方插件与官方 Agent 归属于它。
_Avoid_: 宿主、系统租户

**可见范围（Visibility）**:
插件或 Agent 在市场中对谁可见：「租户内」仅归属租户可见，「公开」对所有租户可见；目前只有平台租户能公开。
_Avoid_: 权限、共享范围

**安装（Installation）**:
某个租户启用一个插件或 Agent 的记录，承载该租户自己的密钥与绑定；分为插件安装与 Agent 安装。被安装的定义本身不存放任何租户的配置值。
_Avoid_: 订阅、引用、实例

**插件安装（Plugin Installation）**:
租户对插件的安装，填写该插件声明所需的配置项（如认证信息）。

**Agent 安装（Agent Installation）**:
租户对 Agent 的安装，绑定本租户的一个模型；前提是该 Agent 挂载的插件在本租户均已安装。

### 运行

**会话（Session）**:
使用方与某个 Agent 安装之间的一段持续上下文，对应框架的 `AgentSession`；人工聊天与工作流/API 调用都形成会话。与 Cike.Auth 中的登录会话无关。
_Avoid_: 对话、Thread、Chat

**会话记录（Session Message）**:
会话中按顺序追加的一条消息，可以是用户输入、助手回复、工具调用或工具结果；恢复上下文与界面展示都用它。
_Avoid_: 聊天记录、轮次

**会话事件（Session Event）**:
会话中每次调用 Agent 的过程记录，如开始、完成（含模型、Token、耗时）、失败（含原因）、取消；用于追溯、计量与排障。
_Avoid_: 运行日志、轮次
