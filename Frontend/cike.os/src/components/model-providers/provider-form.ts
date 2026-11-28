import type { CreateModelProviderDto, ModelProviderDetailDto, SaveModelProviderModelDto, TagItemDto, UpdateModelProviderDto } from '@/api'
import { MODEL_PROVIDER_PROTOCOL, type AnyProviderSetting, type ProtocolValue } from '@/lib/model-provider-protocol'

/**
 * 表单里的一条模型：名称既是提交键（后端以名称为键同步，创建后不可变）也是表单内的唯一键。
 * rowKey 是行的稳定标识：供 v-for 作 key、校验错误按键；与行内容无关，删行后其余行的错误提示不串位。
 */
interface ModelRow {
  rowKey: number
  name: string
  description: string
  tagIds: string[]
}

/** 模型提供方表单状态：连接字段 + 它提供的模型；各协议的专属字段并存，提交时只取当前协议 */
interface ProviderFormState {
  name: string
  protocol: ProtocolValue
  baseUrl: string
  apiKey: string
  organization: string
  version: string
  models: ModelRow[]
}

let nextRowKey = 0

function emptyModelRow(): ModelRow {
  return {
    rowKey: ++nextRowKey,
    name: '',
    description: '',
    tagIds: [],
  }
}

function emptyProviderForm(): ProviderFormState {
  return {
    name: '',
    protocol: MODEL_PROVIDER_PROTOCOL.OpenAIChatCompletion,
    baseUrl: '',
    apiKey: '',
    organization: '',
    version: '',
    models: [],
  }
}

/**
 * 回填用：标签删除不清理模型上的引用，只保留当前标签列表里能匹配到的 Id。
 * 匹配不到的是已删标签，不带进表单，保存时自然不会提交、也不会被后端「标签不存在」拦下。
 */
function existingTagIds(tagIds: ReadonlyArray<string> | null | undefined, tags: ReadonlyArray<TagItemDto>): string[] {
  const existing = new Set(tags.map(tag => tag.id))
  return (tagIds ?? []).filter(id => existing.has(id))
}

/** 从详情回填；标签列表没加载成功时不过滤标签 Id（原样保留，保存由对话框拦下） */
function providerFormFromDto(dto: ModelProviderDetailDto | null | undefined, tags: TagItemDto[], tagsReady: boolean): ProviderFormState {
  const setting = dto?.setting
  return {
    name: dto?.name ?? '',
    protocol: (dto?.protocol ?? MODEL_PROVIDER_PROTOCOL.OpenAIChatCompletion) as ProtocolValue,
    baseUrl: dto?.baseUrl ?? '',
    apiKey: dto?.apiKey ?? '',
    // 两种 OpenAI 协议的设置子类不同，专属字段同为组织标识（按判别键收窄类型）
    organization:
      setting?.type === MODEL_PROVIDER_PROTOCOL.OpenAIChatCompletion || setting?.type === MODEL_PROVIDER_PROTOCOL.OpenAIResponse
        ? (setting.organization ?? '')
        : '',
    version: setting?.type === MODEL_PROVIDER_PROTOCOL.Anthropic ? (setting.version ?? '') : '',
    models: (dto?.models ?? []).map(model => ({
      rowKey: ++nextRowKey,
      name: model.name ?? '',
      description: model.description ?? '',
      tagIds: tagsReady ? existingTagIds(model.tagIds, tags) : [...(model.tagIds ?? [])],
    })),
  }
}

/** 协议专属设置：只带当前协议的字段，未填即 null；判别键与协议一致 */
function protocolSetting(state: ProviderFormState): AnyProviderSetting {
  if (state.protocol === MODEL_PROVIDER_PROTOCOL.Anthropic) {
    return { type: MODEL_PROVIDER_PROTOCOL.Anthropic, version: state.version.trim() || null }
  }

  if (state.protocol === MODEL_PROVIDER_PROTOCOL.OpenAIResponse) {
    return { type: MODEL_PROVIDER_PROTOCOL.OpenAIResponse, organization: state.organization.trim() || null }
  }

  return { type: MODEL_PROVIDER_PROTOCOL.OpenAIChatCompletion, organization: state.organization.trim() || null }
}

/** 模型条目转提交项：以名称为键，不带 Id；空描述归空字符串 */
function modelRowsToDtos(rows: ReadonlyArray<ModelRow>): SaveModelProviderModelDto[] {
  return rows.map(row => ({
    name: row.name.trim(),
    description: row.description.trim(),
    tagIds: row.tagIds,
  }))
}

/** 只校验连接字段；提供方重名、标签是否存在由后端裁决 */
function validateProviderForm(state: ProviderFormState): Record<string, string> {
  const errors: Record<string, string> = {}
  if (!state.name.trim()) errors.name = '请填写模型提供方名称。'
  else if (state.name.trim().length > 128) errors.name = '模型提供方名称不能超过 128 个字符。'
  if (!state.baseUrl.trim()) errors.baseUrl = '请填写接入地址。'
  if (!state.apiKey.trim()) errors.apiKey = '请填写 API 密钥。'
  return errors
}

/**
 * 校验模型小对话框里的一条：名称必填且不能与表单里其它条目重名（同一提供方下模型名称不重复）。
 * row 自己的 rowKey 不算「其它条目」，编辑时改描述与标签不会被自己挡住。
 */
function validateModelRow(row: ModelRow, siblings: ReadonlyArray<ModelRow>): Record<string, string> {
  const errors: Record<string, string> = {}
  const name = row.name.trim()
  if (!name) errors.name = '请填写模型名称。'
  else if (name.length > 128) errors.name = '模型名称不能超过 128 个字符。'
  else if (siblings.some(x => x.rowKey !== row.rowKey && x.name.trim() === name)) errors.name = '该模型已添加。'

  if (row.description.trim().length > 2048) errors.description = '描述不能超过 2048 个字符。'
  return errors
}

/** 把小对话框里确定的一条写回表单：rowKey 已在表单里是替换（编辑），否则追加（新增） */
function upsertModelRow(state: ProviderFormState, row: ModelRow): void {
  const index = state.models.findIndex(x => x.rowKey === row.rowKey)
  if (index >= 0) {
    state.models.splice(index, 1, row)
  } else {
    state.models.push(row)
  }
}

function removeModelRow(state: ProviderFormState, rowKey: number): void {
  const index = state.models.findIndex(x => x.rowKey === rowKey)
  if (index >= 0) state.models.splice(index, 1)
}

/** 协议专属字段是否已有输入：切换协议前据此决定要不要二次确认 */
function protocolFieldsHaveInput(state: ProviderFormState): boolean {
  return Boolean(state.organization.trim() || state.version.trim())
}

/** 切换协议后清空协议专属字段（名称、地址、密钥、模型表保留） */
function resetProtocolFields(state: ProviderFormState): void {
  state.organization = ''
  state.version = ''
}

/**
 * 表单 → 请求体。新建与更新同形（协议可改，所以更新请求体里也有协议）；模型列表是期望的完整状态，没带的会被移除。
 */
function providerFormToBody(state: ProviderFormState): CreateModelProviderDto & UpdateModelProviderDto {
  return {
    name: state.name.trim(),
    protocol: state.protocol,
    baseUrl: state.baseUrl.trim(),
    apiKey: state.apiKey.trim(),
    setting: protocolSetting(state),
    models: modelRowsToDtos(state.models),
  }
}

export {
  emptyModelRow,
  emptyProviderForm,
  existingTagIds,
  modelRowsToDtos,
  protocolFieldsHaveInput,
  protocolSetting,
  providerFormFromDto,
  providerFormToBody,
  removeModelRow,
  resetProtocolFields,
  upsertModelRow,
  validateModelRow,
  validateProviderForm,
  type ModelRow,
  type ProviderFormState,
}
