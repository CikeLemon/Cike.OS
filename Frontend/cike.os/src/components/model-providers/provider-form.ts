import type { CreateModelProviderDto, ModelProviderDto, SaveModelProviderMappingDto, UpdateModelProviderDto } from '@/api'
import type { ProviderTypeValue } from '@/lib/model-provider-type'
import { MODEL_PROVIDER_TYPE } from '@/lib/model-provider-type'

/** 表单里的一行模型映射：modelId 既是提交键也是行标识（一个提供方对一个模型只映射一次） */
interface MappingRow {
  modelId: string
  priority: string
  providerModelName: string
}

/** 提供商表单状态：两种类型的专属字段并存，提交时只取当前类型；映射随提供方整体提交 */
interface ProviderFormState {
  name: string
  type: ProviderTypeValue
  baseUrl: string
  apiKey: string
  organization: string
  version: string
  mappings: MappingRow[]
}

/** 新增映射行的默认分数：分数只是排序依据，高的先用 */
const DEFAULT_MAPPING_PRIORITY = '0'

function emptyMappingRow(): MappingRow {
  return {
    modelId: '',
    priority: DEFAULT_MAPPING_PRIORITY,
    providerModelName: '',
  }
}

function emptyProviderForm(): ProviderFormState {
  return {
    name: '',
    type: MODEL_PROVIDER_TYPE.OpenAI,
    baseUrl: '',
    apiKey: '',
    organization: '',
    version: '',
    mappings: [],
  }
}

function providerFormFromDto(dto: ModelProviderDto): ProviderFormState {
  const setting = dto.setting
  return {
    name: dto.name ?? '',
    type: setting?.type ?? MODEL_PROVIDER_TYPE.OpenAI,
    baseUrl: setting?.baseUrl ?? '',
    apiKey: setting?.apiKey ?? '',
    organization: setting?.type === MODEL_PROVIDER_TYPE.OpenAI ? (setting.organization ?? '') : '',
    version: setting?.type === MODEL_PROVIDER_TYPE.Anthropic ? (setting.version ?? '') : '',
    mappings: (dto.mappings ?? []).map(mapping => ({
      modelId: mapping.modelId ?? '',
      priority: String(mapping.priority ?? 0),
      providerModelName: mapping.providerModelName ?? '',
    })),
  }
}

/** 只校验表单自身；名称唯一、模型是否存在、同一模型是否重复由后端裁决 */
function validateProviderForm(state: ProviderFormState): Record<string, string> {
  const errors: Record<string, string> = {}
  if (!state.name.trim()) errors.name = '请填写提供商名称。'
  if (!state.baseUrl.trim()) errors.baseUrl = '请填写接入地址。'
  if (!state.apiKey.trim()) errors.apiKey = '请填写 API 密钥。'
  if (state.type === MODEL_PROVIDER_TYPE.OpenAI && !state.organization.trim()) errors.organization = '请填写组织标识。'
  if (state.type === MODEL_PROVIDER_TYPE.Anthropic && !state.version.trim()) errors.version = '请填写 API 版本。'

  state.mappings.forEach((row, index) => {
    if (!row.modelId) errors[`mappings.${index}.modelId`] = '请选择模型。'
    if (!Number.isInteger(Number(row.priority))) errors[`mappings.${index}.priority`] = '分数必须是整数。'
    if (row.providerModelName.trim().length > 128) errors[`mappings.${index}.providerModelName`] = '提供商侧模型名不能超过 128 个字符。'
  })
  return errors
}

function providerFormToBody(state: ProviderFormState): CreateModelProviderDto & UpdateModelProviderDto {
  const name = state.name.trim()
  const mappings: SaveModelProviderMappingDto[] = state.mappings.map(row => ({
    modelId: row.modelId,
    priority: Number(row.priority),
    providerModelName: row.providerModelName.trim() || null,
  }))
  if (state.type === MODEL_PROVIDER_TYPE.OpenAI) {
    return {
      name,
      mappings,
      setting: {
        type: MODEL_PROVIDER_TYPE.OpenAI,
        baseUrl: state.baseUrl.trim(),
        apiKey: state.apiKey.trim(),
        organization: state.organization.trim(),
      },
    }
  }

  return {
    name,
    mappings,
    setting: {
      type: MODEL_PROVIDER_TYPE.Anthropic,
      baseUrl: state.baseUrl.trim(),
      apiKey: state.apiKey.trim(),
      version: state.version.trim(),
    },
  }
}

/** 设置区是否已有输入：切换类型前据此决定要不要二次确认 */
function settingHasInput(state: ProviderFormState): boolean {
  return Boolean(state.baseUrl.trim() || state.apiKey.trim() || state.organization.trim() || state.version.trim())
}

/** 切换类型后清空设置区专属输入（名称与映射保留） */
function resetSettingFields(state: ProviderFormState): void {
  state.baseUrl = ''
  state.apiKey = ''
  state.organization = ''
  state.version = ''
}

export {
  emptyMappingRow,
  emptyProviderForm,
  providerFormFromDto,
  providerFormToBody,
  resetSettingFields,
  settingHasInput,
  validateProviderForm,
  type MappingRow,
  type ProviderFormState,
}
