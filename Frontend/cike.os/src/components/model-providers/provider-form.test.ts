import { describe, expect, it } from 'vitest'
import type { AnthropicModelProviderSetting, ModelProviderDto, OpenAiModelProviderSetting } from '@/api'
import { emptyMappingRow, emptyProviderForm, providerFormFromDto, providerFormToBody, settingHasInput, validateProviderForm } from './provider-form'

function validOpenAi() {
  return { ...emptyProviderForm(), name: ' 智谱 ', baseUrl: ' https://api.example.com/v1 ', apiKey: ' sk-1 ', organization: ' org-1 ' }
}

describe('provider-form', () => {
  it('OpenAI 表单转 DTO：去首尾空白、type 为 1、只带组织标识，没有映射时为空数组', () => {
    const body = providerFormToBody(validOpenAi())
    expect(body.name).toBe('智谱')
    expect(body.setting).toEqual({ type: 1, baseUrl: 'https://api.example.com/v1', apiKey: 'sk-1', organization: 'org-1' })
    expect('version' in body.setting!).toBe(false)
    expect(body.mappings).toEqual([])
  })

  it('Anthropic 表单转 DTO：type 为 2、只带版本', () => {
    const body = providerFormToBody({ ...emptyProviderForm(), type: 2, name: 'A', baseUrl: 'u', apiKey: 'k', version: '2023-06-01' })
    expect(body.setting).toEqual({ type: 2, baseUrl: 'u', apiKey: 'k', version: '2023-06-01' })
  })

  it('映射转 DTO：分数转数字，空白提供商侧模型名归 null（表示回落模型名称）', () => {
    const state = { ...validOpenAi(), mappings: [{ modelId: '7', priority: '30', providerModelName: ' zhipu/glm ' }, { ...emptyMappingRow(), modelId: '9' }] }
    expect(providerFormToBody(state).mappings).toEqual([
      { modelId: '7', priority: 30, providerModelName: 'zhipu/glm' },
      { modelId: '9', priority: 0, providerModelName: null },
    ])
  })

  it('校验：公共字段必填，专属字段只看当前类型', () => {
    expect(validateProviderForm(validOpenAi())).toEqual({})

    const empty = emptyProviderForm()
    expect(validateProviderForm(empty)).toEqual({
      name: '请填写提供商名称。',
      baseUrl: '请填写接入地址。',
      apiKey: '请填写 API 密钥。',
      organization: '请填写组织标识。',
    })

    // Anthropic 类型不要求组织标识，但要求版本
    const anthropic = { ...validOpenAi(), type: 2 as const, organization: '', version: '' }
    expect(validateProviderForm(anthropic)).toEqual({ version: '请填写 API 版本。' })
  })

  it('校验：映射行必须选模型、分数必须是整数', () => {
    const state = { ...validOpenAi(), mappings: [{ ...emptyMappingRow(), priority: 'abc', providerModelName: 'x'.repeat(129) }] }
    expect(validateProviderForm(state)).toEqual({
      'mappings.0.modelId': '请选择模型。',
      'mappings.0.priority': '分数必须是整数。',
      'mappings.0.providerModelName': '提供商侧模型名不能超过 128 个字符。',
    })
  })

  it('从 DTO 回填：按设置的判别键取对应专属字段，映射行按 DTO 顺序还原', () => {
    const openAi: ModelProviderDto = {
      name: 'P',
      setting: { type: 1, baseUrl: 'u', apiKey: 'k', organization: 'o' } satisfies OpenAiModelProviderSetting,
      mappings: [{ modelId: '7', priority: 30, providerModelName: 'zhipu/glm' }],
    }
    expect(providerFormFromDto(openAi)).toEqual({
      name: 'P',
      type: 1,
      baseUrl: 'u',
      apiKey: 'k',
      organization: 'o',
      version: '',
      mappings: [{ modelId: '7', priority: '30', providerModelName: 'zhipu/glm' }],
    })

    const anthropic: ModelProviderDto = {
      name: 'Q',
      setting: { type: 2, baseUrl: 'u', apiKey: 'k', version: 'v' } satisfies AnthropicModelProviderSetting,
    }
    expect(providerFormFromDto(anthropic)).toEqual({
      name: 'Q',
      type: 2,
      baseUrl: 'u',
      apiKey: 'k',
      organization: '',
      version: 'v',
      mappings: [],
    })
  })

  it('设置区是否已有输入：只有名称和映射时为 false', () => {
    expect(settingHasInput({ ...emptyProviderForm(), name: 'x', mappings: [{ ...emptyMappingRow(), modelId: '1' }] })).toBe(false)
    expect(settingHasInput({ ...emptyProviderForm(), apiKey: 'k' })).toBe(true)
  })
})
