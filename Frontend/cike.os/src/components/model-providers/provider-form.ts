import type { CreateModelProviderDto, ModelProviderDto, UpdateModelProviderDto } from '@/api'

/** 提供方表单状态：提供方本身只有名称，连接与映射都在它的接入点上（接入点有自己独立的接口） */
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

/** 更新请求体：也只有名称——提供方只管自己，接入点不随提供方提交 */
function providerUpdateBody(name: string | null | undefined): UpdateModelProviderDto {
  return { name: (name ?? '').trim() }
}

export {
  emptyProviderForm,
  providerFormFromDto,
  providerFormToBody,
  providerUpdateBody,
  validateProviderForm,
  type ProviderFormState,
}
