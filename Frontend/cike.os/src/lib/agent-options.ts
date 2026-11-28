import type { AgentToolType } from '@/api'

/** Agent 工具条目的类型：数值与后端 AgentToolType 枚举一致，不引入 enum（erasableSyntaxOnly） */
const AGENT_TOOL_TYPE = {
  ToolPackage: 1,
  Tool: 2,
} as const satisfies Record<string, AgentToolType>

type AgentToolTypeValue = AgentToolType

/**
 * 工具模式（M.E.AI 的多态对象，判别键 $type）：
 * auto = 自动决定是否用工具；none = 不使用工具；required = 必须使用工具。
 */
const TOOL_MODE = {
  Auto: 'auto',
  None: 'none',
  Required: 'required',
} as const

type ToolModeValue = (typeof TOOL_MODE)[keyof typeof TOOL_MODE]

const TOOL_MODE_OPTIONS: ReadonlyArray<{ value: ToolModeValue; label: string }> = [
  { value: TOOL_MODE.Auto, label: '自动' },
  { value: TOOL_MODE.None, label: '不使用' },
  { value: TOOL_MODE.Required, label: '必须使用' },
]

/** 推理力度：数值与 M.E.AI ReasoningEffort 一致（None=0 … ExtraHigh=4） */
const REASONING_EFFORT = {
  None: 0,
  Low: 1,
  Medium: 2,
  High: 3,
  ExtraHigh: 4,
} as const

type ReasoningEffortValue = (typeof REASONING_EFFORT)[keyof typeof REASONING_EFFORT]

const REASONING_EFFORT_OPTIONS: ReadonlyArray<{ value: ReasoningEffortValue; label: string }> = [
  { value: REASONING_EFFORT.None, label: '不推理' },
  { value: REASONING_EFFORT.Low, label: '低' },
  { value: REASONING_EFFORT.Medium, label: '中' },
  { value: REASONING_EFFORT.High, label: '高' },
  { value: REASONING_EFFORT.ExtraHigh, label: '极高' },
]

/** 推理输出：数值与 M.E.AI ReasoningOutput 一致（None=0 / Summary=1 / Full=2） */
const REASONING_OUTPUT = {
  None: 0,
  Summary: 1,
  Full: 2,
} as const

type ReasoningOutputValue = (typeof REASONING_OUTPUT)[keyof typeof REASONING_OUTPUT]

const REASONING_OUTPUT_OPTIONS: ReadonlyArray<{ value: ReasoningOutputValue; label: string }> = [
  { value: REASONING_OUTPUT.None, label: '不输出' },
  { value: REASONING_OUTPUT.Summary, label: '摘要' },
  { value: REASONING_OUTPUT.Full, label: '完整' },
]

/** 响应格式：数值与后端 AgentChatResponseFormatType 枚举一致 */
const RESPONSE_FORMAT_TYPE = {
  Text: 1,
  Json: 2,
} as const

type ResponseFormatTypeValue = (typeof RESPONSE_FORMAT_TYPE)[keyof typeof RESPONSE_FORMAT_TYPE]

const RESPONSE_FORMAT_OPTIONS: ReadonlyArray<{ value: ResponseFormatTypeValue; label: string }> = [
  { value: RESPONSE_FORMAT_TYPE.Text, label: '文本' },
  { value: RESPONSE_FORMAT_TYPE.Json, label: 'JSON（按 Schema 输出）' },
]

/**
 * 可空下拉的「不设置」哨兵值：reka-ui 的 SelectItem 不允许空串 value（空串被保留给清除选择），
 * 表单里的可空枚举 / 三态布尔统一用它在下拉里表示「不设置」。
 */
const SELECT_UNSET = '__unset__'

/** 可空布尔的三态下拉：不设置 / 是 / 否 */
const TRI_STATE_OPTIONS: ReadonlyArray<{ value: typeof SELECT_UNSET | 'true' | 'false'; label: string }> = [
  { value: SELECT_UNSET, label: '不设置' },
  { value: 'true', label: '是' },
  { value: 'false', label: '否' },
]

type TriStateValue = (typeof TRI_STATE_OPTIONS)[number]['value']

export {
  AGENT_TOOL_TYPE,
  REASONING_EFFORT,
  REASONING_EFFORT_OPTIONS,
  REASONING_OUTPUT,
  REASONING_OUTPUT_OPTIONS,
  RESPONSE_FORMAT_OPTIONS,
  RESPONSE_FORMAT_TYPE,
  SELECT_UNSET,
  TOOL_MODE,
  TOOL_MODE_OPTIONS,
  TRI_STATE_OPTIONS,
  type AgentToolTypeValue,
  type ReasoningEffortValue,
  type ReasoningOutputValue,
  type ResponseFormatTypeValue,
  type ToolModeValue,
  type TriStateValue,
}
