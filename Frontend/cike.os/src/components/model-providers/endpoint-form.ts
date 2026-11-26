import type { ModelProviderEndpointDto, SaveModelProviderEndpointDto, SaveModelProviderMappingDto } from '@/api'
import { MODEL_PROVIDER_PROTOCOL, type AnyEndpointSetting, type ProtocolValue } from '@/lib/model-provider-protocol'

/** 表单里的一行模型映射：modelId 既是提交键也是聚合内的唯一键（一个接入点对一个模型只映射一次） */
interface MappingRow {
  /** 行的稳定标识：供 v-for 作 key、校验错误按键；与行内容无关，删行后其余行的错误提示不串位 */
  rowKey: number
  modelId: string
  priority: string
  providerModelName: string
}

/** 接入点表单状态：两种协议的专属字段并存，提交时只取当前协议；映射随接入点整体提交 */
interface EndpointFormState {
  name: string
  protocol: ProtocolValue
  baseUrl: string
  apiKey: string
  organization: string
  version: string
  mappings: MappingRow[]
}

/** 新增映射行的默认分数：分数只是排序依据，高的先用 */
const DEFAULT_MAPPING_PRIORITY = '0'

let nextRowKey = 0

function emptyMappingRow(): MappingRow {
  return {
    rowKey: ++nextRowKey,
    modelId: '',
    priority: DEFAULT_MAPPING_PRIORITY,
    providerModelName: '',
  }
}

function emptyEndpointForm(): EndpointFormState {
  return {
    name: '',
    protocol: MODEL_PROVIDER_PROTOCOL.OpenAI,
    baseUrl: '',
    apiKey: '',
    organization: '',
    version: '',
    mappings: [],
  }
}

function endpointFormFromDto(dto: ModelProviderEndpointDto | null | undefined): EndpointFormState {
  const setting = dto?.setting
  return {
    name: dto?.name ?? '',
    protocol: dto?.protocol ?? MODEL_PROVIDER_PROTOCOL.OpenAI,
    baseUrl: dto?.baseUrl ?? '',
    apiKey: dto?.apiKey ?? '',
    organization: setting?.type === MODEL_PROVIDER_PROTOCOL.OpenAI ? (setting.organization ?? '') : '',
    version: setting?.type === MODEL_PROVIDER_PROTOCOL.Anthropic ? (setting.version ?? '') : '',
    mappings: (dto?.mappings ?? []).map(mapping => ({
      rowKey: ++nextRowKey,
      modelId: mapping.modelId ?? '',
      priority: String(mapping.priority ?? 0),
      providerModelName: mapping.providerModelName ?? '',
    })),
  }
}

/** 协议专属设置：只带当前协议的字段，未填即 null */
function protocolSetting(state: EndpointFormState): AnyEndpointSetting {
  if (state.protocol === MODEL_PROVIDER_PROTOCOL.Anthropic) {
    return { type: MODEL_PROVIDER_PROTOCOL.Anthropic, version: state.version.trim() || null }
  }

  return { type: MODEL_PROVIDER_PROTOCOL.OpenAI, organization: state.organization.trim() || null }
}

/** 映射行转提交项：分数转数字，空白提供商侧模型名归 null（表示回落模型名称） */
function mappingRowsToDtos(rows: ReadonlyArray<MappingRow>): SaveModelProviderMappingDto[] {
  return rows.map(row => ({
    modelId: row.modelId,
    priority: Number(row.priority),
    providerModelName: row.providerModelName.trim() || null,
  }))
}

/** 只校验表单自身；接入点重名、模型是否存在、同一模型是否重复由后端裁决 */
function validateEndpointForm(state: EndpointFormState): Record<string, string> {
  const errors: Record<string, string> = {}
  if (!state.name.trim()) errors.name = '请填写接入点名称。'
  else if (state.name.trim().length > 128) errors.name = '接入点名称不能超过 128 个字符。'
  if (!state.baseUrl.trim()) errors.baseUrl = '请填写接入地址。'
  if (!state.apiKey.trim()) errors.apiKey = '请填写 API 密钥。'

  state.mappings.forEach(row => {
    if (!row.modelId) errors[`mappings.${row.rowKey}.modelId`] = '请选择模型。'
    if (!Number.isInteger(Number(row.priority))) errors[`mappings.${row.rowKey}.priority`] = '分数必须是整数。'
    if (row.providerModelName.trim().length > 128) errors[`mappings.${row.rowKey}.providerModelName`] = '提供商侧模型名不能超过 128 个字符。'
  })
  return errors
}

/** 协议专属字段是否已有输入：切换协议前据此决定要不要二次确认 */
function protocolFieldsHaveInput(state: EndpointFormState): boolean {
  return Boolean(state.organization.trim() || state.version.trim())
}

/** 切换协议后清空协议专属字段（名称、地址、密钥、映射保留） */
function resetProtocolFields(state: EndpointFormState): void {
  state.organization = ''
  state.version = ''
}

/**
 * 接入点表单 → 随提供方整体提交的输入项。已有接入点带上 Id（后端按 Id 更新，协议不可变），新建不带 Id。
 * 协议每次都带：新建时生效，已有接入点必须与现有协议一致。
 */
function endpointFormToSaveDto(state: EndpointFormState, id?: string): SaveModelProviderEndpointDto {
  return {
    ...(id ? { id } : {}),
    name: state.name.trim(),
    protocol: state.protocol,
    baseUrl: state.baseUrl.trim(),
    apiKey: state.apiKey.trim(),
    setting: protocolSetting(state),
    mappings: mappingRowsToDtos(state.mappings),
  }
}

export {
  emptyEndpointForm,
  emptyMappingRow,
  endpointFormFromDto,
  endpointFormToSaveDto,
  mappingRowsToDtos,
  protocolFieldsHaveInput,
  protocolSetting,
  resetProtocolFields,
  validateEndpointForm,
  type EndpointFormState,
  type MappingRow,
}
