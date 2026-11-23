# Cike.OS 领域术语表

| 术语 | 定义 |
|---|---|
| 管理端（Admin Console） | 前端唯一的应用外壳。对话工作台、Agent / 模型 / 工具等资源管理都作为其中的功能模块存在，不存在与它并列的独立界面形态。 |
| 模块（Module） | 管理端侧栏的一级导航单元，与领域实体域对应：对话、Agent、模型、工具、标签、设置。 |
| Agent | 具备指令（Instructions）与推理选项（温度、输出上限等）的智能代理，可被会话引用执行任务。 |
| 模型（Model） | 挂在某个模型提供方下的具体模型条目，可携带多个标签。 |
| 模型提供方（Model Provider） | 模型的接入方（如 OpenAI、Anthropic），持有连接设置（地址、密钥等）。 |
| 工具（Tool） | 可被调用的能力单元，分技能（Skill）、HTTP MCP、标准 IO MCP 三种形态，声明参数定义。 |
| 工具包（Tool Package） | 工具的打包分发单元，归属于一个工具包分类，有草稿/发布/下线状态。 |
| 工具包分类（Tool Package Category） | 工具包的分类目录。 |
| 标签（Tag） | 以键（Key）为标识的标注词条，用于模型等资源的标记。 |
| 会话（Conversation） | 围绕某个 Agent 展开的对话单元，按类型区分（Agent、工作流等）。 |
| 会话成员（Conversation Member） | 参与某个会话的成员及其角色（成员、管理员）。 |
| 会话消息（Conversation Message） | 会话中的一条消息，发送者带角色（如 user / assistant）与标识。 |
| 会话消息事件（Conversation Message Event） | 消息执行过程中产生的事件记录（动作、内容、耗时）。 |
| 会话文件（Conversation File） | 会话中传输的文件（名称、大小、MIME 类型、存储路径）。 |
| 用户工具配置（User Tool Configuration） | 用户对某个工具的参数取值设定。 |
| Agent 工具包绑定（Agent Tool Package） | Agent 与工具包的绑定关系，可标记为全量绑定。 |
| Agent 工具（Agent Tool） | 绑定关系下的具体工具项。 |

