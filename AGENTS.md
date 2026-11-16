
## 路由：改哪块读哪份

| 改动位置 | 必读 |
|---|---|
| `Backend/**`（.NET / DDD / EF Core / Cike.Framework） | `Backend/AGENTS.md` |
| `Frontend/cike.os/**`（管理端 SPA） | `Frontend/cike.os/AGENTS.md` |

跨栈改动（如后端 API 契约变更影响前端调用）：涉及的几份都过一遍。

## Agent skills

### Issue tracker

Issues and PRDs live in GitHub Issues; external PRs are not a triage surface. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five-label vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context repo: one `CONTEXT.md` at the root plus `docs/adr/`. See `docs/agents/domain.md`.