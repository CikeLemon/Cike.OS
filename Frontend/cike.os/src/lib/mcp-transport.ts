import type { HttpTransportMode } from '@/api'

/** MCP 传输模式：数值与 MCP SDK 的 HttpTransportMode 一致（直接复用值对象出 API，不另造枚举） */
const HTTP_TRANSPORT_MODE = {
  AutoDetect: 0,
  StreamableHttp: 1,
  Sse: 2,
} as const satisfies Record<string, HttpTransportMode>

const HTTP_TRANSPORT_MODE_OPTIONS: ReadonlyArray<{ value: HttpTransportMode; label: string }> = [
  { value: HTTP_TRANSPORT_MODE.AutoDetect, label: 'AutoDetect（自动探测）' },
  { value: HTTP_TRANSPORT_MODE.StreamableHttp, label: 'StreamableHttp' },
  { value: HTTP_TRANSPORT_MODE.Sse, label: 'Sse' },
]

export { HTTP_TRANSPORT_MODE, HTTP_TRANSPORT_MODE_OPTIONS }
