import { describe, expect, it } from 'vitest'
import type {
  AnthropicModelProviderEndpointSetting,
  ModelProviderEndpointDto,
  OpenAiChatCompletionModelProviderEndpointSetting,
  OpenAiResponseModelProviderEndpointSetting,
} from '@/api'
import {
  emptyEndpointForm,
  emptyMappingRow,
  endpointFormFromDto,
  endpointFormToCreateDto,
  endpointFormToUpdateDto,
  protocolFieldsHaveInput,
  resetProtocolFields,
  validateEndpointForm,
} from './endpoint-form'

function validOpenAi() {
  return { ...emptyEndpointForm(), name: ' 官方 ', baseUrl: ' https://api.example.com/v1 ', apiKey: ' sk-1 ', organization: ' org-1 ' }
}

describe('endpoint-form', () => {
  it('新建请求体：带所属提供方、去首尾空白、协议为 1、设置只带组织标识，没有映射时为空数组', () => {
    expect(endpointFormToCreateDto(validOpenAi(), '100')).toEqual({
      modelProviderId: '100',
      name: '官方',
      protocol: 1,
      baseUrl: 'https://api.example.com/v1',
      apiKey: 'sk-1',
      setting: { type: 1, organization: 'org-1' },
      mappings: [],
    })
  })

  it('更新请求体：不含归属、协议与 id（创建后都不可变，由路由上的 id 指认接入点）；未填的协议专属字段为 null', () => {
    const body = endpointFormToUpdateDto({ ...validOpenAi(), protocol: 2, version: '' })
    expect(body).toEqual({
      name: '官方',
      baseUrl: 'https://api.example.com/v1',
      apiKey: 'sk-1',
      setting: { type: 2, version: null },
      mappings: [],
    })
  })

  it('OpenAI Response 协议：判别键为 3，专属字段同样是组织标识', () => {
    const body = endpointFormToCreateDto({ ...validOpenAi(), protocol: 3 }, '100')
    expect(body.protocol).toBe(3)
    expect(body.setting).toEqual({ type: 3, organization: 'org-1' })
  })

  it('映射转提交项：分数转数字，空白提供商侧模型名归 null（表示回落模型名称）', () => {
    const state = {
      ...validOpenAi(),
      mappings: [{ ...emptyMappingRow(), modelId: '7', priority: '30', providerModelName: ' zhipu/glm ' }, { ...emptyMappingRow(), modelId: '9' }],
    }
    expect(endpointFormToCreateDto(state, '100').mappings).toEqual([
      { modelId: '7', priority: 30, providerModelName: 'zhipu/glm' },
      { modelId: '9', priority: 0, providerModelName: null },
    ])
  })

  it('校验：连接字段必填，协议专属字段不设必填', () => {
    expect(validateEndpointForm(validOpenAi())).toEqual({})
    expect(validateEndpointForm(emptyEndpointForm())).toEqual({
      name: '请填写接入点名称。',
      baseUrl: '请填写接入地址。',
      apiKey: '请填写 API 密钥。',
    })
    // 两种协议都不要求专属字段
    expect(validateEndpointForm({ ...validOpenAi(), organization: '' })).toEqual({})
    expect(validateEndpointForm({ ...validOpenAi(), protocol: 2, version: '' })).toEqual({})
  })

  it('校验：映射行必须选模型、分数必须是整数；错误按行的稳定 rowKey 存，与下标无关', () => {
    const first = { ...emptyMappingRow(), modelId: '1' }
    const bad = { ...emptyMappingRow(), priority: 'abc', providerModelName: 'x'.repeat(129) }
    const errors = validateEndpointForm({ ...validOpenAi(), mappings: [first, bad] })
    expect(Object.keys(errors).every(key => !key.includes(`mappings.${first.rowKey}.`))).toBe(true)
    expect(errors).toEqual({
      [`mappings.${bad.rowKey}.modelId`]: '请选择模型。',
      [`mappings.${bad.rowKey}.priority`]: '分数必须是整数。',
      [`mappings.${bad.rowKey}.providerModelName`]: '提供商侧模型名不能超过 128 个字符。',
    })
  })

  it('从 DTO 回填：按设置的判别键取对应专属字段，映射行按 DTO 顺序还原', () => {
    const openAi: ModelProviderEndpointDto = {
      id: '5',
      name: 'E',
      protocol: 1,
      baseUrl: 'u',
      apiKey: 'k',
      setting: { type: 1, organization: 'o' } satisfies OpenAiChatCompletionModelProviderEndpointSetting,
      mappings: [{ modelId: '7', priority: 30, providerModelName: 'zhipu/glm' }],
    }
    const toRows = (state: ReturnType<typeof endpointFormFromDto>) =>
      state.mappings.map(({ modelId, priority, providerModelName }) => ({ modelId, priority, providerModelName }))
    const openAiForm = endpointFormFromDto(openAi)
    expect({ ...openAiForm, mappings: toRows(openAiForm) }).toEqual({
      name: 'E',
      protocol: 1,
      baseUrl: 'u',
      apiKey: 'k',
      organization: 'o',
      version: '',
      mappings: [{ modelId: '7', priority: '30', providerModelName: 'zhipu/glm' }],
    })
    expect(new Set(openAiForm.mappings.map(row => row.rowKey)).size).toBe(openAiForm.mappings.length)

    const anthropic: ModelProviderEndpointDto = {
      name: 'F',
      protocol: 2,
      baseUrl: 'u',
      apiKey: 'k',
      setting: { type: 2, version: 'v' } satisfies AnthropicModelProviderEndpointSetting,
    }
    expect(endpointFormFromDto(anthropic)).toEqual({
      name: 'F',
      protocol: 2,
      baseUrl: 'u',
      apiKey: 'k',
      organization: '',
      version: 'v',
      mappings: [],
    })

    // OpenAI Response 接入点：组织标识回填到同一个输入框
    const response: ModelProviderEndpointDto = {
      name: 'R',
      protocol: 3,
      baseUrl: 'u',
      apiKey: 'k',
      setting: { type: 3, organization: 'org-r' } satisfies OpenAiResponseModelProviderEndpointSetting,
    }
    expect(endpointFormFromDto(response)).toEqual({
      name: 'R',
      protocol: 3,
      baseUrl: 'u',
      apiKey: 'k',
      organization: 'org-r',
      version: '',
      mappings: [],
    })
  })

  it('协议专属字段是否已有输入：只有名称、地址、密钥、映射时为 false', () => {
    expect(protocolFieldsHaveInput({ ...validOpenAi(), organization: '', mappings: [{ ...emptyMappingRow(), modelId: '1' }] })).toBe(false)
    expect(protocolFieldsHaveInput({ ...validOpenAi(), organization: '', version: 'v' })).toBe(true)
  })

  it('切换协议只清空协议专属字段', () => {
    const kept = { ...emptyMappingRow(), modelId: '1' }
    const state = { ...validOpenAi(), version: 'v', mappings: [kept] }
    resetProtocolFields(state)
    expect(state.organization).toBe('')
    expect(state.version).toBe('')
    // 名称、地址、密钥、映射保留
    expect(state.name).toBe(' 官方 ')
    expect(state.mappings).toEqual([kept])
  })
})
