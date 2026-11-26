import { describe, expect, it } from 'vitest'
import type { ModelProviderDto, ModelProviderEndpointDto, SaveModelProviderEndpointDto } from '@/api'
import {
  emptyProviderForm,
  endpointToSaveDto,
  providerFormFromDto,
  providerFormToBody,
  providerUpdateBody,
  removeEndpoint,
  upsertEndpoint,
  validateProviderForm,
} from './provider-form'

const endpoints: ModelProviderEndpointDto[] = [
  {
    id: '1',
    name: '官方 OpenAI',
    protocol: 1,
    baseUrl: 'https://api.openai.com/v1',
    apiKey: 'sk-1',
    setting: { type: 1, organization: 'org-1' },
    mappings: [{ modelId: '7', modelDisplayName: 'GLM 5.3', priority: 10, providerModelName: '' }],
  },
  {
    id: '2',
    name: '官方 Anthropic',
    protocol: 2,
    baseUrl: 'https://api.anthropic.com',
    apiKey: 'ak-1',
    setting: { type: 2, version: '2023-06-01' },
    mappings: [{ modelId: '7', priority: 5, providerModelName: 'glm-5.3' }],
  },
]

const provider: ModelProviderDto = { id: '100', name: '智谱', endpoints }

describe('provider-form', () => {
  it('提供方表单只有名称：新建请求体去首尾空白', () => {
    expect(providerFormToBody({ name: ' 智谱 ' })).toEqual({ name: '智谱' })
  })

  it('校验：名称必填且不超过 128 个字符', () => {
    expect(validateProviderForm({ name: '智谱' })).toEqual({})
    expect(validateProviderForm(emptyProviderForm())).toEqual({ name: '请填写模型提供方名称。' })
    expect(validateProviderForm({ name: 'x'.repeat(129) })).toEqual({ name: '模型提供方名称不能超过 128 个字符。' })
  })

  it('从 DTO 回填：只取名称，接入点不进表单', () => {
    expect(providerFormFromDto({ name: 'P', endpoints: [] })).toEqual({ name: 'P' })
    expect(providerFormFromDto(null)).toEqual({ name: '' })
  })
})

describe('提供方整体提交（接入点随提供方保存）', () => {
  it('没改的接入点原样转回输入：去掉界面用的模型显示名称，空字符串的提供商侧模型名归 null', () => {
    expect(endpoints.map(endpointToSaveDto)).toEqual([
      {
        id: '1',
        name: '官方 OpenAI',
        protocol: 1,
        baseUrl: 'https://api.openai.com/v1',
        apiKey: 'sk-1',
        setting: { type: 1, organization: 'org-1' },
        mappings: [{ modelId: '7', priority: 10, providerModelName: null }],
      },
      {
        id: '2',
        name: '官方 Anthropic',
        protocol: 2,
        baseUrl: 'https://api.anthropic.com',
        apiKey: 'ak-1',
        setting: { type: 2, version: '2023-06-01' },
        mappings: [{ modelId: '7', priority: 5, providerModelName: 'glm-5.3' }],
      },
    ])
  })

  it('更新请求体 = 名称 + 全部接入点', () => {
    const body = providerUpdateBody(provider.name, endpoints.map(endpointToSaveDto))
    expect(body.name).toBe('智谱')
    expect(body.endpoints).toHaveLength(2)
  })

  it('新建接入点：其余原样保留，新的追加在后面', () => {
    const created: SaveModelProviderEndpointDto = { name: '中转', protocol: 1, baseUrl: 'https://relay', apiKey: 'sk-2', setting: { type: 1 }, mappings: [] }
    const result = upsertEndpoint(endpoints, created)
    expect(result.map(x => x.id)).toEqual(['1', '2', undefined])
    expect(result[2]).toBe(created)
  })

  it('编辑接入点：同 Id 的被替换，其余原样保留', () => {
    const edited: SaveModelProviderEndpointDto = { id: '1', name: '官方（改）', protocol: 1, baseUrl: 'https://x', apiKey: 'k', setting: { type: 1 }, mappings: [] }
    const result = upsertEndpoint(endpoints, edited)
    expect(result.map(x => x.id)).toEqual(['2', '1'])
    expect(result[1]).toBe(edited)
  })

  it('删除接入点：提交时不带它', () => {
    expect(removeEndpoint(endpoints, '1').map(x => x.id)).toEqual(['2'])
    expect(removeEndpoint(undefined, '1')).toEqual([])
  })
})
