import { describe, expect, it } from 'vitest'
import type { ModelProviderEndpointDto } from '@/api'
import { endpointModelNames } from './provider-summary'

// 列表页的协议 / 支持的模型 / 接入点数由后端按提供方汇总在 ModelProviderListDto 里，前端不再计算

describe('provider-summary', () => {
  it('接入点支持的模型：按接口返回顺序（分数降序），跳过没有显示名称的映射', () => {
    const endpoint: ModelProviderEndpointDto = {
      mappings: [{ modelId: '7', modelDisplayName: 'GLM 5.3', priority: 10 }, { modelId: '8', priority: 8 }, { modelId: '9', modelDisplayName: 'Qwen 3.8', priority: 5 }],
    }
    expect(endpointModelNames(endpoint)).toEqual(['GLM 5.3', 'Qwen 3.8'])
    expect(endpointModelNames(undefined)).toEqual([])
  })
})
