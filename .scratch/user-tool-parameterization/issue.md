# 用户级工具参数化配置

Labels: ready-for-agent

## Problem Statement

用户在 Agent 里使用 MCP / OpenAPI / Skill 工具时，经常需要提供自己的身份或认证参数（例如 AK、Token、自定义请求头的值）。当前模型里没有一个地方让“工具声明可配什么、用户填什么、运行时注入什么”形成闭环，导致这些值要么没法配置，要么会被迫混进结构性连接配置里。

## Solution

引入三个领域概念，并保持平台不预设认证形态：

- **工具参数（Tool Parameter）**：工具声明自己的身份/认证类可配置项，只描述“可配什么”，不保存真实值，也不设默认值。
- **参数引用（Parameter Reference）**：工具配置里的值槽直接绑定某个已声明参数；一个值槽只绑一个参数，同一参数可被多个值槽复用。
- **用户工具配置（User Tool Configuration）**：某个租户内某个用户为某个工具填写的参数值集合，归属粒度是租户 × 用户 × 工具。

运行时按当前用户动态注入参数值，不缓存全局连接；缺失必填参数时，在首次真正调用该工具前拦截，不在 Agent 安装或会话创建时拦截。插件卸载或工具删除时，该用户对应的工具配置随之删除；工具仅停用时保留。

## User Stories

1. As a tool author, I want to declare identity/authentication parameters on a tool, so that users can provide their own credentials without me hardcoding them.
2. As a tool author, I want to mark a parameter as required, so that the platform blocks a tool call before it starts when the user has not filled it.
3. As a tool author, I want to keep structural connection settings separate from user parameters, so that users cannot change a tool’s endpoint, transport mode, timeout, or command.
4. As a tool author, I want to bind any value slot in my tool configuration to a declared parameter, so that the platform does not restrict where a value may be used.
5. As a tool author, I want to reuse the same parameter across multiple value slots, so that one AK can feed both a request header and an environment variable.
6. As a tool author, I want to update a tool’s parameter declarations without invalidating users’ existing values, so that users do not have to refill compatible parameters.
7. As a tool author, I want the platform not to impose a fixed authentication scheme layer, so that I can shape Bearer, Basic, custom headers, or any future form myself.
8. As a tool author, I want no default values on parameters, so that empty means “not filled” rather than “inherited from someone else”.
9. As a user, I want to fill my own parameter values for a tool, so that my credentials are not shared with other users.
10. As a user, I want to fill a tool’s parameters once and reuse them across all Agents that use that tool, so that I do not have to repeat the same AK in every Agent.
11. As a user, I want to update or delete my own parameter values for a tool, so that I can rotate credentials or stop using a tool.
12. As a user, I want to be prompted only when I actually try to use a tool that requires a missing parameter, so that installing an Agent does not force me to configure tools I may never call.
13. As a user, I want my values injected at runtime for each call, so that the platform never caches my credentials inside a global connection.
14. As a user, I want the same tool to behave with my credentials only when I call it, so that another user’s call cannot reuse my authentication state.
15. As a user, I want my parameter values isolated by tenant, so that the same user identity in different tenants cannot leak credentials across tenants.
16. As a user, I want parameter definitions to live on the tool rather than the plugin, so that a plugin cannot expose one tool’s credentials to another tool in the same package.
17. As an administrator, I want to see only whether a user has configured a tool and when it was last updated, so that I can support troubleshooting without seeing credential values.
18. As an administrator, I want to delete a user’s tool configuration when the tool or plugin is removed, so that stale credentials do not remain in the system.
19. As an administrator, I want a disabled tool to retain user configuration, so that users do not have to refill credentials when the tool is re-enabled.
20. As a platform operator, I want the parameter system to remain independent of specific tool types, so that adding a new tool type does not require changing the parameter model.
21. As a platform operator, I want the platform not to whitelist which value slots may bind parameters, so that each tool type can define its own slots.
22. As a platform operator, I want one value slot to bind at most one parameter, so that configuration remains unambiguous.
23. As a platform operator, I want a parameter to be referenceable by multiple value slots, so that one value can be reused without duplication.
24. As a platform operator, I want runtime injection to happen per call and per user, so that concurrent users of the same tool never share authentication state.
25. As a platform operator, I want parameter definition changes to be evaluated against the current declaration at runtime, so that extra old values are ignored and compatible values continue to work.
26. As a platform operator, I want the domain model not to encode a parameter type whitelist, so that future tool types can introduce richer value types without schema migration.
27. As a platform operator, I want the model not to use string interpolation placeholders, so that binding is represented structurally and cannot be spoofed by template text.
28. As a platform operator, I want the system to store current parameter values as plaintext for now, so that this iteration does not introduce encryption or key-management complexity.
29. As a platform operator, I want the system not to provide default parameter values, so that configuration state remains explicit.
30. As a platform operator, I want the tool parameter and user tool configuration to remain separate from plugin installation, so that user credentials are not conflated with tenant-level bindings.

## Implementation Decisions

- Introduce three domain concepts: **Tool Parameter**, **Parameter Reference**, and **User Tool Configuration**.
- Parameter declarations belong to the **Tool** aggregate, not to the plugin.
- Parameter declarations describe name, type, requiredness, and sensitivity; they do **not** include default values.
- Only identity/authentication values are parameterizable. Structural connection settings such as endpoint, transport mode, timeout, reconnection, or process command remain outside the parameter system.
- Parameter references are structured direct bindings between a tool configuration value slot and a declared parameter. The platform does **not** use `${xxx}` string placeholders.
- A value slot binds exactly one parameter. A parameter may be referenced by multiple value slots.
- The platform does not whitelist which value slots may bind parameters. Value slot kinds are determined by each tool type’s own configuration structure.
- User tool configuration is keyed by tenant, user, and tool.
- User tool configuration is owned and maintained by the user. Administrators can see configuration status and last update time, but not values.
- Current iteration stores parameter values as plaintext; encryption, redaction, and viewing behavior are out of scope.
- Runtime injection is performed per tool invocation and per user. The platform does not cache a global connection that carries a user’s credentials.
- A missing required parameter blocks the first real invocation of that tool, not Agent installation or session creation.
- Tool parameter definition changes do not invalidate existing user configuration. At runtime, validation is performed against the current declaration; obsolete values are ignored, and compatible values continue to be used.
- Plugin uninstall or tool deletion deletes the corresponding user tool configuration. Tool disablement retains it.
- The platform does not introduce an authentication scheme layer and does not predefine Bearer, Basic, OAuth, or similar forms.
- The domain model does not encode a parameter type whitelist. Initial implementation may treat values as strings, but the model is not restricted to strings.
- The feature spans the domain layer, application command/query handlers, application contracts, EF Core entity configuration and migration, and the HTTP host’s minimal API endpoints.
- The single test seam is the application command/query handler boundary. Tests exercise external behavior through that seam and do not inspect entity internals, EF configuration, or private domain state.
- If HTTP endpoints are added, they should only forward parameters to the existing command/query seam and should not become a second testing surface.

## Testing Decisions

- A good test exercises external behavior only: what the caller can create, read, update, delete, or invoke. It should not assert private entity internals, EF mappings, or repository implementation details.
- The single test seam is the application command/query handler boundary, following the framework’s CQRS conventions.
- The repository currently has no existing tests, so this feature introduces the first integration tests for this area.
- Test cases should cover:
  1. Declaring a tool parameter and binding it to one or more value slots.
  2. Creating, updating, and deleting a user tool configuration.
  3. Tenant isolation of the same user identity across different tenants.
  4. User isolation of parameter values across users.
  5. A required parameter blocking the first tool invocation.
  6. Runtime injection using the current user’s values without a shared global connection.
  7. Parameter definition changes leaving compatible user values intact and ignoring obsolete values.
  8. Plugin uninstall or tool deletion removing user tool configuration.
  9. Tool disablement preserving user tool configuration.
  10. One value slot binding exactly one parameter and one parameter binding multiple value slots.
- Tests should not depend on a specific HTTP surface, since the HTTP layer is not the chosen seam.
- Tests should use the framework’s standard in-memory or test database approach and should not require a real MCP server.

## Out of Scope

- Encryption, key management, secret rotation, redaction, or viewing behavior for parameter values.
- OAuth, mTLS, HMAC, AWS SigV4, or any specific authentication protocol.
- Plugin-level parameter definitions.
- Tenant-level shared credentials.
- Multiple parameter value sets per user per tool.
- Default parameter values.
- Parameter type system beyond what the initial implementation needs.
- Agent installation flow changes beyond the pre-call interception rule.
- Tool runtime connection management beyond parameter injection semantics.
- Any UI/UX work beyond the data model and API contracts.
- Import or export of user tool configurations.
- Multi-language localization of parameter metadata.

## Further Notes

- This spec intentionally does not prescribe which value slots are supported. The binding mechanism is generic, and each tool type defines its own value slots.
- The domain glossary in `CONTEXT.md` is the source of truth for the three introduced terms and should remain synchronized with this spec.
- No additional ADR is required for this iteration; the model is captured in the domain glossary and this spec.
