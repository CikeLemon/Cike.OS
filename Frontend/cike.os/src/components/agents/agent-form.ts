import type {
  AgentDetailDto,
  AgentOptions,
  AgentToolPackageOptionItemDto,
  AgentToolOptionItemDto,
  CreateAgentDto,
  SaveAgentToolDto,
  UpdateAgentDto,
} from '@/api'
import { AGENT_TOOL_TYPE, SELECT_UNSET, TOOL_MODE, type ReasoningEffortValue, type ReasoningOutputValue, type ResponseFormatTypeValue, type ToolModeValue, type TriStateValue } from '@/lib/agent-options'

/** 数值类模型参数在表单里都是字符串：空串 = 不设置，提交时转数字 */
interface AgentFormState {
  name: string
  description: string
  instructions: string

  // 模型参数（常用）
  temperature: string
  maxOutputTokens: string
  topP: string
  reasoningEffort: ReasoningEffortValue | null
  toolMode: ToolModeValue | null

  // 高级配置
  topK: string
  frequencyPenalty: string
  presencePenalty: string
  seed: string
  stopSequences: string[]
  reasoningOutput: ReasoningOutputValue | null
  responseFormatType: ResponseFormatTypeValue | null
  jsonSchemaName: string
  jsonSchemaDescriptor: string
  allowMultipleToolCalls: TriStateValue
  allowBackgroundResponses: TriStateValue

  // 第二步：工具勾选。整包勾选的工具包 Id，与单选的工具 Id（两者互斥：整包下的工具不能再单选）
  wholePackages: string[]
  toolIds: string[]
}

/** 新建 / 更新的请求体同形（没有不可变字段） */
type AgentRequestBody = CreateAgentDto & UpdateAgentDto

interface AgentToolSelection {
  wholePackages: string[]
  toolIds: string[]
}

function emptyAgentForm(): AgentFormState {
  return {
    name: '',
    description: '',
    instructions: '',
    temperature: '',
    maxOutputTokens: '',
    topP: '',
    reasoningEffort: null,
    toolMode: null,
    topK: '',
    frequencyPenalty: '',
    presencePenalty: '',
    seed: '',
    stopSequences: [],
    reasoningOutput: null,
    responseFormatType: null,
    jsonSchemaName: '',
    jsonSchemaDescriptor: '',
    allowMultipleToolCalls: SELECT_UNSET,
    allowBackgroundResponses: SELECT_UNSET,
    wholePackages: [],
    toolIds: [],
  }
}

/** 数值 → 表单字符串：空值给空串 */
function numberToInput(value: number | string | null | undefined): string {
  if (value === null || value === undefined || value === '') return ''
  return String(value)
}

/**
 * 表单字段 → 数值：空（空串 / null）给 null；其余转数字，不是数字时返回 NaN（由校验拦下）。
 * `Input type="number"` 的 v-model 会把值转成 number，所以这里同时接受 number 与 string。
 */
function inputToNumber(value: string | number | null | undefined): number | null {
  if (value === null || value === undefined) return null
  const text = String(value).trim()
  if (!text) return null
  return Number(text)
}

/** Seed 是 long：请求里按字符串传，空 = null */
function seedToString(value: string | number | null | undefined): string | null {
  const number = inputToNumber(value)
  return number === null ? null : String(number)
}

/** 三态下拉 → 可空布尔：不设置 / 是 / 否 */
function triStateToBoolean(value: TriStateValue): boolean | null {
  if (value === 'true') return true
  if (value === 'false') return false
  return null
}

function booleanToTriState(value: boolean | null | undefined): TriStateValue {
  if (value === true) return 'true'
  if (value === false) return 'false'
  return SELECT_UNSET
}

/** 可空枚举 ↔ 下拉的字符串值：null 用哨兵表示「不设置」 */
function toSelectValue(value: string | number | null | undefined): string {
  return value === null || value === undefined ? SELECT_UNSET : String(value)
}

function fromSelectValue(raw: string): string | null {
  return raw === SELECT_UNSET ? null : raw
}

/** M.E.AI 工具模式对象 → 下拉枚举值（只认三种预定义模式，其它情况视为未设置） */
function toolModeFromOptions(options: AgentOptions): ToolModeValue | null {
  const type = options.toolMode?.$type
  if (type === TOOL_MODE.Auto || type === TOOL_MODE.None || type === TOOL_MODE.Required) return type
  return null
}

function agentFormFromDto(agent: AgentDetailDto | null | undefined): AgentFormState {
  const options = agent?.options
  return {
    name: agent?.name ?? '',
    description: agent?.description ?? '',
    instructions: agent?.instructions ?? '',
    temperature: numberToInput(options?.temperature),
    maxOutputTokens: numberToInput(options?.maxOutputTokens),
    topP: numberToInput(options?.topP),
    reasoningEffort: (options?.reasoning?.effort as ReasoningEffortValue | undefined) ?? null,
    toolMode: toolModeFromOptions(options ?? {}),
    topK: numberToInput(options?.topK),
    frequencyPenalty: numberToInput(options?.frequencyPenalty),
    presencePenalty: numberToInput(options?.presencePenalty),
    seed: numberToInput(options?.seed),
    stopSequences: [...(options?.stopSequences ?? [])],
    reasoningOutput: (options?.reasoning?.output as ReasoningOutputValue | undefined) ?? null,
    responseFormatType: options?.responseFormat ? (options.responseFormat.type as ResponseFormatTypeValue) : null,
    jsonSchemaName: options?.responseFormat?.jsonSchemaName ?? '',
    jsonSchemaDescriptor: options?.responseFormat?.jsonSchemaDescriptor ?? '',
    allowMultipleToolCalls: booleanToTriState(options?.allowMultipleToolCalls),
    allowBackgroundResponses: booleanToTriState(options?.allowBackgroundResponses),
    ...toolSelectionFromRows(agent?.tools ?? []),
  }
}

/** 详情里的工具条目 → 勾选状态：整包行进 wholePackages，单个工具行进 toolIds */
function toolSelectionFromRows(rows: Array<{ type?: number | null; targetId?: string | null }>): AgentToolSelection {
  const wholePackages: string[] = []
  const toolIds: string[] = []
  for (const row of rows) {
    if (!row.targetId) continue
    if (row.type === AGENT_TOOL_TYPE.ToolPackage) wholePackages.push(row.targetId)
    else toolIds.push(row.targetId)
  }
  return { wholePackages, toolIds }
}

/** 勾选状态 → 提交的条目：整包一行，单选工具每个一行；按类型、目标排序，与后端返回顺序一致 */
function toolSelectionToRows(selection: AgentToolSelection): SaveAgentToolDto[] {
  const rows: SaveAgentToolDto[] = [
    ...selection.wholePackages.map(targetId => ({ type: AGENT_TOOL_TYPE.ToolPackage, targetId })),
    ...selection.toolIds.map(targetId => ({ type: AGENT_TOOL_TYPE.Tool, targetId })),
  ]
  return rows.sort((a, b) => Number(a.type) - Number(b.type) || Number(a.targetId) - Number(b.targetId))
}

/** 勾「整包」：工具包进 wholePackages，它下面的工具从单选里移除 */
function selectWholePackage(selection: AgentToolSelection, toolPackageId: string, toolsOfPackage: string[]): void {
  if (!selection.wholePackages.includes(toolPackageId)) selection.wholePackages.push(toolPackageId)
  const removed = new Set(toolsOfPackage)
  selection.toolIds = selection.toolIds.filter(id => !removed.has(id))
}

/** 取消「整包」：只移除整包标记，工具保持未选中（用户再按需勾） */
function unselectWholePackage(selection: AgentToolSelection, toolPackageId: string): void {
  selection.wholePackages = selection.wholePackages.filter(id => id !== toolPackageId)
}

/** 整包状态下取消其中一个工具：整包自动取消，包内其余工具转为单选保留 */
function unselectToolUnderWholePackage(selection: AgentToolSelection, toolPackageId: string, toolsOfPackage: string[], removedToolId: string): void {
  unselectWholePackage(selection, toolPackageId)
  const kept = toolsOfPackage.filter(id => id !== removedToolId)
  for (const id of kept) {
    if (!selection.toolIds.includes(id)) selection.toolIds.push(id)
  }
}

/** 单选工具：所属包已整包时不动（界面上也不可勾） */
function toggleTool(selection: AgentToolSelection, toolId: string, checked: boolean): void {
  const exists = selection.toolIds.includes(toolId)
  if (checked && !exists) selection.toolIds.push(toolId)
  if (!checked && exists) selection.toolIds = selection.toolIds.filter(id => id !== toolId)
}

/** 表单的模型参数 → AgentOptions（全部可空，空 = 不设置） */
function optionsFromForm(form: AgentFormState): AgentOptions {
  const reasoning =
    form.reasoningEffort !== null || form.reasoningOutput !== null
      ? { effort: form.reasoningEffort ?? undefined, output: form.reasoningOutput ?? undefined }
      : undefined
  const responseFormat = form.responseFormatType
    ? {
        type: form.responseFormatType,
        jsonSchemaName: form.jsonSchemaName.trim() || null,
        jsonSchemaDescriptor: form.responseFormatType === 2 ? form.jsonSchemaDescriptor.trim() : null,
      }
    : undefined
  const toolMode = form.toolMode ? { $type: form.toolMode } : null
  return {
    temperature: inputToNumber(form.temperature),
    maxOutputTokens: inputToNumber(form.maxOutputTokens),
    topP: inputToNumber(form.topP),
    topK: inputToNumber(form.topK),
    frequencyPenalty: inputToNumber(form.frequencyPenalty),
    presencePenalty: inputToNumber(form.presencePenalty),
    // Seed 是 long：前端按字符串原样传，空 = 不设置
    seed: seedToString(form.seed),
    reasoning,
    responseFormat,
    stopSequences: form.stopSequences.length > 0 ? form.stopSequences : null,
    allowMultipleToolCalls: triStateToBoolean(form.allowMultipleToolCalls),
    toolMode: toolMode as AgentOptions['toolMode'],
    allowBackgroundResponses: triStateToBoolean(form.allowBackgroundResponses),
  }
}

function agentFormToBody(form: AgentFormState): AgentRequestBody {
  return {
    name: form.name.trim(),
    description: form.description.trim() || null,
    instructions: form.instructions.trim(),
    options: optionsFromForm(form),
    tools: toolSelectionToRows(form),
  }
}

function agentFormToCreateBody(form: AgentFormState): CreateAgentDto {
  return agentFormToBody(form)
}

function agentFormToUpdateBody(form: AgentFormState): UpdateAgentDto {
  return agentFormToBody(form)
}

/** 与后端 CreateAgentDtoValidator 一致的请求级校验；名称是否重名、引用是否存在由后端裁决 */
function validateAgentForm(form: AgentFormState): Record<string, string> {
  const errors: Record<string, string> = {}
  const name = form.name.trim()
  const description = form.description.trim()
  if (!name) errors.name = '请填写名称。'
  else if (name.length > 128) errors.name = '名称不能超过 128 个字符。'
  if (description.length > 512) errors.description = '描述不能超过 512 个字符。'
  if (!form.instructions.trim()) errors.instructions = '请填写指令。'

  const ranges: Array<[keyof AgentFormState, number, number, string]> = [
    ['temperature', 0, 2, '温度须在 0 到 2 之间。'],
    ['topP', 0, 1, 'TopP 须在 0 到 1 之间。'],
    ['frequencyPenalty', -2, 2, '频率惩罚须在 -2 到 2 之间。'],
    ['presencePenalty', -2, 2, '存在惩罚须在 -2 到 2 之间。'],
  ]
  for (const [key, min, max, message] of ranges) {
    const value = inputToNumber(form[key] as string | number | null)
    if (value !== null && (Number.isNaN(value) || value < min || value > max)) errors[key] = message
  }
  for (const [key, message] of [
    ['topK', 'TopK 须大于等于 1。'],
    ['maxOutputTokens', '输出上限须大于等于 1。'],
    ['seed', 'Seed 须大于等于 0。'],
  ] as const) {
    const value = inputToNumber(form[key] as string | number | null)
    if (value !== null && (Number.isNaN(value) || value < (key === 'seed' ? 0 : 1))) errors[key] = message
  }

  const stops = form.stopSequences.map(x => x.trim())
  if (stops.length > 16) errors.stopSequences = '停止词最多 16 条。'
  else if (stops.some(x => !x)) errors.stopSequences = '停止词不能为空。'
  else if (stops.some(x => x.length > 64)) errors.stopSequences = '每个停止词不能超过 64 个字符。'

  if (form.responseFormatType === 2) {
    const schema = form.jsonSchemaDescriptor.trim()
    if (!schema) errors.jsonSchemaDescriptor = '响应格式为 JSON 时必须填写 JSON Schema。'
    else {
      try {
        JSON.parse(schema)
      } catch {
        errors.jsonSchemaDescriptor = 'JSON Schema 不是合法的 JSON。'
      }
    }
  }

  return errors
}

/**
 * 新建后要跳去填参数的工具：提交的条目展开成「生效的工具」（单选行本身 + 整包行对应包下的全部工具），
 * 其中声明了参数的。
 */
function pendingParameterToolIds(rows: SaveAgentToolDto[], options: AgentToolPackageOptionItemDto[]): string[] {
  const toolsById = new Map<string, AgentToolOptionItemDto>()
  const toolsByPackage = new Map<string, AgentToolOptionItemDto[]>()
  for (const pkg of options) {
    toolsByPackage.set(pkg.toolPackageId ?? '', pkg.tools ?? [])
    for (const tool of pkg.tools ?? []) toolsById.set(tool.toolId ?? '', tool)
  }

  const effective = new Set<string>()
  for (const row of rows) {
    const targetId = row.targetId ?? ''
    if (row.type === AGENT_TOOL_TYPE.ToolPackage) {
      for (const tool of toolsByPackage.get(targetId) ?? []) effective.add(tool.toolId ?? '')
    } else if (toolsById.has(targetId)) {
      effective.add(targetId)
    }
  }

  return [...effective].filter(id => (toolsById.get(id)?.parameterCount ?? 0) > 0)
}

export {
  agentFormFromDto,
  agentFormToBody,
  agentFormToCreateBody,
  agentFormToUpdateBody,
  emptyAgentForm,
  fromSelectValue,
  inputToNumber,
  optionsFromForm,
  pendingParameterToolIds,
  selectWholePackage,
  toggleTool,
  toSelectValue,
  toolSelectionFromRows,
  toolSelectionToRows,
  unselectToolUnderWholePackage,
  unselectWholePackage,
  validateAgentForm,
  type AgentFormState,
  type AgentRequestBody,
  type AgentToolSelection,
}
