import type {
  CreateToolDto,
  HttpMcpToolSetting,
  HttpTransportMode,
  SkillToolSetting,
  StdioMcpToolSetting,
  ToolDetailDto,
  ToolParameterDefinition,
  ToolValue,
  ToolValueType,
  UpdateToolDto,
} from '@/api'
import { HTTP_TRANSPORT_MODE } from '@/lib/mcp-transport'
import { secondsToTimeSpan, timeSpanToSeconds } from '@/lib/time-span'
import { TOOL_TYPE, toolTypeFromSetting, type AnyToolSetting, type ToolTypeValue } from '@/lib/tool-type'

/** 值类型：1 字面量 / 2 参数引用，与后端 ToolValueType 一致 */
const TOOL_VALUE_TYPE = {
  Literal: 1,
  ParameterReference: 2,
} as const satisfies Record<string, ToolValueType>

interface SkillSettingForm {
  /** 上传接口返回的相对目录；未上传为空串 */
  directory: string
  /** 最近一次上传的文件名，仅用于展示 */
  fileName: string
}

interface HttpSettingForm {
  name: string
  endpoint: string
  transportMode: HttpTransportMode
  /** 以「秒」编辑，提交时转 TimeSpan 字串 */
  connectionTimeoutSeconds: number
  maxReconnectionAttempts: number
  /** 以「秒」编辑，提交时转 TimeSpan 字串 */
  defaultReconnectionIntervalSeconds: number
  credentialKey: string
  additionalHeaders: Record<string, ToolValue>
}

interface StdioSettingForm {
  name: string
  command: string
  /** 启动参数：每行一条 */
  argumentsText: string
  workingDirectory: string
  environmentVariables: Record<string, ToolValue>
}

/** 工具编辑 Sheet 的表单状态：三形态设置区并存，提交时只取当前形态 */
interface ToolFormState {
  code: string
  name: string
  description: string
  toolPackageId: string
  parameters: ToolParameterDefinition[]
  type: ToolTypeValue
  skill: SkillSettingForm
  http: HttpSettingForm
  stdio: StdioSettingForm
}

function emptyHttpSetting(): HttpSettingForm {
  return {
    name: '',
    endpoint: '',
    transportMode: HTTP_TRANSPORT_MODE.StreamableHttp,
    connectionTimeoutSeconds: 30,
    maxReconnectionAttempts: 5,
    defaultReconnectionIntervalSeconds: 1,
    credentialKey: '',
    additionalHeaders: {},
  }
}

function emptyStdioSetting(): StdioSettingForm {
  return {
    name: '',
    command: '',
    argumentsText: '',
    workingDirectory: '',
    environmentVariables: {},
  }
}

function emptyToolForm(toolPackageId: string): ToolFormState {
  return {
    code: '',
    name: '',
    description: '',
    toolPackageId,
    parameters: [],
    type: TOOL_TYPE.HttpMcp,
    skill: { directory: '', fileName: '' },
    http: emptyHttpSetting(),
    stdio: emptyStdioSetting(),
  }
}

function toolFormFromDto(tool: ToolDetailDto): ToolFormState {
  const state = emptyToolForm(tool.toolPackageId ?? '')
  state.code = tool.code ?? ''
  state.name = tool.name ?? ''
  state.description = tool.description ?? ''
  state.parameters = (tool.parameters ?? []).map(parameter => ({ ...parameter }))
  state.type = toolTypeFromSetting(tool.setting as AnyToolSetting | null | undefined)

  const setting = tool.setting as AnyToolSetting | null | undefined
  if (setting?.type === TOOL_TYPE.Skill) {
    state.skill = { directory: setting.directory ?? '', fileName: '' }
  } else if (setting?.type === TOOL_TYPE.HttpMcp) {
    state.http = {
      name: setting.name ?? '',
      endpoint: setting.endpoint ?? '',
      transportMode: setting.transportMode ?? HTTP_TRANSPORT_MODE.StreamableHttp,
      connectionTimeoutSeconds: timeSpanToSeconds(setting.connectionTimeout) || 30,
      maxReconnectionAttempts: setting.maxReconnectionAttempts ?? 5,
      defaultReconnectionIntervalSeconds: timeSpanToSeconds(setting.defaultReconnectionInterval) || 1,
      credentialKey: setting.credentialKey ?? '',
      additionalHeaders: { ...(setting.additionalHeaders ?? {}) },
    }
  } else if (setting?.type === TOOL_TYPE.StdioMcp) {
    state.stdio = {
      name: setting.name ?? '',
      command: setting.command ?? '',
      argumentsText: (setting.arguments ?? []).join('\n'),
      workingDirectory: setting.workingDirectory ?? '',
      environmentVariables: { ...(setting.environmentVariables ?? {}) },
    }
  }

  return state
}

/** 设置区是否有任何已填内容（三形态任一），用于切换形态前的确认判断 */
function isSettingDirty(state: ToolFormState): boolean {
  if (state.skill.directory || state.skill.fileName) return true
  if (state.http.name || state.http.endpoint || state.http.credentialKey || Object.keys(state.http.additionalHeaders).length > 0) return true
  return Boolean(
    state.stdio.name
    || state.stdio.command
    || state.stdio.argumentsText
    || state.stdio.workingDirectory
    || Object.keys(state.stdio.environmentVariables).length > 0,
  )
}

/** 清空三形态设置区，返回新状态（参数定义区不受影响） */
function resetSettings(state: ToolFormState): ToolFormState {
  return {
    ...state,
    skill: { directory: '', fileName: '' },
    http: emptyHttpSetting(),
    stdio: emptyStdioSetting(),
  }
}

function parameterCodes(state: ToolFormState): string[] {
  return state.parameters.map(parameter => (parameter.code ?? '').trim()).filter(Boolean)
}

/** 前端轻校验：必填项 + 参数编码唯一 + 引用型取值必须命中已定义参数。返回「字段键 → 文案」。 */
function validateToolForm(state: ToolFormState): Record<string, string> {
  const errors: Record<string, string> = {}

  if (!state.code.trim()) errors.code = '请填写工具编码。'
  if (!state.name.trim()) errors.name = '请填写工具名称。'
  if (!state.toolPackageId) errors.toolPackageId = '缺少所属工具包。'

  const codes = parameterCodes(state)
  if (codes.length !== state.parameters.length) errors.parameters = '参数编码不能为空白。'
  if (new Set(codes).size !== codes.length) errors.parameters = '参数编码不能重复。'

  const validateReferences = (values: Record<string, ToolValue>, key: string) => {
    for (const [name, value] of Object.entries(values)) {
      if (value.type === TOOL_VALUE_TYPE.ParameterReference && !codes.includes(value.value ?? '')) {
        errors[key] = `「${name}」引用了未定义的参数。`
      }
    }
  }

  switch (state.type) {
    case TOOL_TYPE.Skill:
      if (!state.skill.directory) errors['skill.directory'] = '请先上传技能包。'
      break
    case TOOL_TYPE.HttpMcp:
      if (!state.http.endpoint.trim()) errors['http.endpoint'] = '请填写端点地址。'
      if (state.http.connectionTimeoutSeconds <= 0) errors['http.connectionTimeoutSeconds'] = '连接超时必须大于 0。'
      if (state.http.defaultReconnectionIntervalSeconds <= 0) errors['http.defaultReconnectionIntervalSeconds'] = '重连间隔必须大于 0。'
      validateReferences(state.http.additionalHeaders, 'http.additionalHeaders')
      break
    case TOOL_TYPE.StdioMcp:
      if (!state.stdio.command.trim()) errors['stdio.command'] = '请填写启动命令。'
      validateReferences(state.stdio.environmentVariables, 'stdio.environmentVariables')
      break
  }

  return errors
}

/** 创建与更新共用的可变字段载荷（不含形态：形态创建后不可变） */
function toToolPayload(state: ToolFormState): Omit<CreateToolDto, 'type'> {
  return {
    code: state.code.trim(),
    name: state.name.trim(),
    description: state.description,
    toolPackageId: state.toolPackageId,
    parameters: state.parameters.map(parameter => ({
      code: (parameter.code ?? '').trim(),
      name: (parameter.name ?? '').trim(),
      description: parameter.description ?? null,
      required: parameter.required ?? false,
      sensitive: parameter.sensitive ?? false,
    })),
    setting: toSetting(state),
  }
}

function toCreateToolDto(state: ToolFormState): CreateToolDto {
  return { ...toToolPayload(state), type: state.type }
}

/** 更新载荷：Id 在路由，形态不可变，其余字段与创建一致 */
function toUpdateToolDto(state: ToolFormState): UpdateToolDto {
  return toToolPayload(state)
}

/** 按当前形态生成值对象；判别键 type 即 ToolType 数值 */
function toSetting(state: ToolFormState): AnyToolSetting {
  switch (state.type) {
    case TOOL_TYPE.Skill:
      return { type: TOOL_TYPE.Skill, directory: state.skill.directory } satisfies SkillToolSetting
    case TOOL_TYPE.HttpMcp:
      return {
        type: TOOL_TYPE.HttpMcp,
        name: state.http.name,
        endpoint: state.http.endpoint.trim(),
        transportMode: state.http.transportMode,
        connectionTimeout: secondsToTimeSpan(state.http.connectionTimeoutSeconds),
        maxReconnectionAttempts: state.http.maxReconnectionAttempts,
        defaultReconnectionInterval: secondsToTimeSpan(state.http.defaultReconnectionIntervalSeconds),
        credentialKey: state.http.credentialKey.trim() || null,
        additionalHeaders: Object.keys(state.http.additionalHeaders).length > 0 ? state.http.additionalHeaders : null,
      } satisfies HttpMcpToolSetting
    default:
      return {
        type: TOOL_TYPE.StdioMcp,
        name: state.stdio.name,
        command: state.stdio.command.trim(),
        arguments: state.stdio.argumentsText
          .split('\n')
          .map(line => line.trim())
          .filter(Boolean),
        workingDirectory: state.stdio.workingDirectory.trim() || null,
        environmentVariables: Object.keys(state.stdio.environmentVariables).length > 0 ? state.stdio.environmentVariables : null,
      } satisfies StdioMcpToolSetting
  }
}
export {
  TOOL_VALUE_TYPE,
  emptyToolForm,
  isSettingDirty,
  parameterCodes,
  resetSettings,
  toCreateToolDto,
  toUpdateToolDto,
  toolFormFromDto,
  validateToolForm,
  type HttpSettingForm,
  type SkillSettingForm,
  type StdioSettingForm,
  type ToolFormState,
}
