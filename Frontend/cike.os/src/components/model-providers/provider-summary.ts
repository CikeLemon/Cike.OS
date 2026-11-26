import type { ModelProviderEndpointDto } from '@/api'

/** 一个接入点支持的模型显示名称：分数高的排前面（详情页「支持的模型」列） */
function endpointModelNames(endpoint: ModelProviderEndpointDto | null | undefined): string[] {
  return (endpoint?.mappings ?? []).map(mapping => mapping.modelDisplayName ?? '').filter(name => name !== '')
}

export { endpointModelNames }
