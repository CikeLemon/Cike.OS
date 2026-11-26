import type {
  CreateModelProviderDto,
  ModelProviderDto,
  ModelProviderEndpointDto,
  SaveModelProviderEndpointDto,
  UpdateModelProviderDto,
} from '@/api'

/** 提供方表单状态：提供方本身只有名称，连接与映射都在它的接入点上 */
interface ProviderFormState {
  name: string
}

function emptyProviderForm(): ProviderFormState {
  return { name: '' }
}

function providerFormFromDto(dto: ModelProviderDto | null | undefined): ProviderFormState {
  return { name: dto?.name ?? '' }
}

/** 名称同租户内是否重复由后端裁决 */
function validateProviderForm(state: ProviderFormState): Record<string, string> {
  const errors: Record<string, string> = {}
  if (!state.name.trim()) errors.name = '请填写模型提供方名称。'
  else if (state.name.trim().length > 128) errors.name = '模型提供方名称不能超过 128 个字符。'
  return errors
}

/** 新建请求体：提供方创建时只有名称 */
function providerFormToBody(state: ProviderFormState): CreateModelProviderDto {
  return { name: state.name.trim() }
}

/**
 * 提供方的更新请求体：名称 + 名下全部接入点。
 * 接入点与映射随提供方整体提交，后端按这份完整状态同步——没提交的接入点会被删除，所以没改的也必须原样带上。
 */
function providerUpdateBody(name: string | null | undefined, endpoints: SaveModelProviderEndpointDto[]): UpdateModelProviderDto {
  return { name: name ?? '', endpoints }
}

/** 没改的接入点原样转回输入 */
function endpointToSaveDto(endpoint: ModelProviderEndpointDto): SaveModelProviderEndpointDto {
  return {
    id: endpoint.id ?? '',
    name: endpoint.name ?? '',
    protocol: endpoint.protocol,
    baseUrl: endpoint.baseUrl ?? '',
    apiKey: endpoint.apiKey ?? '',
    setting: endpoint.setting ?? null,
    mappings: (endpoint.mappings ?? []).map(mapping => ({
      modelId: mapping.modelId ?? '',
      priority: mapping.priority ?? 0,
      providerModelName: mapping.providerModelName || null,
    })),
  }
}

/** 新增或替换一个接入点：有同 Id 的替换，没有的追加 */
function upsertEndpoint(
  endpoints: ModelProviderEndpointDto[] | null | undefined,
  endpoint: SaveModelProviderEndpointDto,
): SaveModelProviderEndpointDto[] {
  const others = (endpoints ?? []).filter(x => !endpoint.id || x.id !== endpoint.id).map(endpointToSaveDto)
  return [...others, endpoint]
}

/** 删掉一个接入点：提交时带上其余接入点、不带它 */
function removeEndpoint(
  endpoints: ModelProviderEndpointDto[] | null | undefined,
  endpointId: string,
): SaveModelProviderEndpointDto[] {
  return (endpoints ?? []).filter(x => x.id !== endpointId).map(endpointToSaveDto)
}

export {
  emptyProviderForm,
  endpointToSaveDto,
  providerFormFromDto,
  providerFormToBody,
  providerUpdateBody,
  removeEndpoint,
  upsertEndpoint,
  validateProviderForm,
  type ProviderFormState,
}
