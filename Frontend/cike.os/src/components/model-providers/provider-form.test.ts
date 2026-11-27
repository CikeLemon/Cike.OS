import { describe, expect, it } from 'vitest'
import type {
  AnthropicModelProviderSetting,
  ModelProviderDto,
  OpenAiChatCompletionModelProviderSetting,
  OpenAiResponseModelProviderSetting,
  TagDto,
} from '@/api'
import {
  emptyModelRow,
  emptyProviderForm,
  existingTagIds,
  modelRowsToDtos,
  protocolFieldsHaveInput,
  providerFormFromDto,
  providerFormToBody,
  removeModelRow,
  resetProtocolFields,
  upsertModelRow,
  validateModelRow,
  validateProviderForm,
  type ProviderFormState,
} from './provider-form'

function validOpenAi(): ProviderFormState {
  return { ...emptyProviderForm(), name: ' 智谱 (OpenAI) ', baseUrl: ' https://api.example.com/v1 ', apiKey: ' sk-1 ', organization: ' org-1 ' }
}

function row(name: string, description = '', tagIds: string[] = []) {
  return { ...emptyModelRow(), name, description, tagIds }
}

const tags: TagDto[] = [{ id: '1', key: 'vision', name: '视觉' }, { id: '2', key: 'chat', name: '对话' }]

describe('provider-form', () => {
  it('请求体：去首尾空白、带协议与全部模型；设置只带当前协议的字段', () => {
    const state = { ...validOpenAi(), models: [row(' glm-5.3 ', ' 旗舰 ', ['1']), row('qwen')] }
    expect(providerFormToBody(state)).toEqual({
      name: '智谱 (OpenAI)',
      protocol: 1,
      baseUrl: 'https://api.example.com/v1',
      apiKey: 'sk-1',
      setting: { type: 1, organization: 'org-1' },
      models: [
        { name: 'glm-5.3', description: '旗舰', tagIds: ['1'] },
        { name: 'qwen', description: '', tagIds: [] },
      ],
    })
  })

  it('请求体：Anthropic 与 OpenAI Response 协议各自的设置；未填的协议专属字段为 null', () => {
    expect(providerFormToBody({ ...validOpenAi(), protocol: 2, version: ' ' }).setting).toEqual({ type: 2, version: null })
    expect(providerFormToBody({ ...validOpenAi(), protocol: 3 }).setting).toEqual({ type: 3, organization: 'org-1' })
  })

  it('校验：连接字段必填，协议专属字段不设必填；模型可以为空', () => {
    expect(validateProviderForm({ ...validOpenAi(), organization: '' })).toEqual({})
    expect(validateProviderForm(emptyProviderForm())).toEqual({
      name: '请填写模型提供方名称。',
      baseUrl: '请填写接入地址。',
      apiKey: '请填写 API 密钥。',
    })
    expect(validateProviderForm({ ...validOpenAi(), name: 'x'.repeat(129) })).toEqual({ name: '模型提供方名称不能超过 128 个字符。' })
  })

  it('校验模型条目：名称必填、不超长、不与其它条目重名；描述不超长', () => {
    const state = { ...validOpenAi(), models: [row('glm-5.3'), row('qwen')] }

    expect(validateModelRow(row('glm-4'), state.models)).toEqual({})
    expect(validateModelRow(row(''), state.models)).toEqual({ name: '请填写模型名称。' })
    expect(validateModelRow(row('x'.repeat(129)), state.models)).toEqual({ name: '模型名称不能超过 128 个字符。' })
    expect(validateModelRow(row(' glm-5.3 '), state.models)).toEqual({ name: '该模型已添加。' })
    // 编辑自己：同名的正是这一条，不算重复
    expect(validateModelRow({ ...state.models[0], description: '改描述' }, state.models)).toEqual({})
    expect(validateModelRow(row('glm-4', 'd'.repeat(2049)), state.models)).toEqual({ description: '描述不能超过 2048 个字符。' })
  })

  it('模型条目的加入 / 替换 / 移除：以 rowKey 定位', () => {
    const state = { ...validOpenAi(), models: [row('a'), row('b')] }
    const [a, b] = state.models

    // 新增：rowKey 不在表单里 → 追加
    const added = row('c')
    upsertModelRow(state, added)
    expect(state.models.map(x => x.name)).toEqual(['a', 'b', 'c'])

    // 编辑：rowKey 已在表单里 → 原位替换
    upsertModelRow(state, { ...b, description: '改了' })
    expect(state.models[1]).toMatchObject({ rowKey: b.rowKey, name: 'b', description: '改了' })

    removeModelRow(state, a.rowKey)
    expect(state.models.map(x => x.name)).toEqual(['b', 'c'])
    removeModelRow(state, a.rowKey)
    expect(state.models.map(x => x.name)).toEqual(['b', 'c'])
  })

  it('模型条目转提交项：以名称为键、不带 Id', () => {
    expect(modelRowsToDtos([row(' a ', ' d ', ['2'])])).toEqual([{ name: 'a', description: 'd', tagIds: ['2'] }])
  })

  it('从详情回填：按设置的判别键取对应专属字段，模型按详情顺序还原', () => {
    const openAi: ModelProviderDto = {
      id: '5',
      name: 'E',
      protocol: 1,
      baseUrl: 'u',
      apiKey: 'k',
      setting: { type: 1, organization: 'o' } satisfies OpenAiChatCompletionModelProviderSetting,
      models: [{ name: 'b', description: '', tagIds: ['2'] }, { name: 'a', description: '甲', tagIds: ['1', '999'] }],
    }
    const form = providerFormFromDto(openAi, tags, true)
    expect({ ...form, models: form.models.map(x => ({ name: x.name, description: x.description, tagIds: x.tagIds })) }).toEqual({
      name: 'E',
      protocol: 1,
      baseUrl: 'u',
      apiKey: 'k',
      organization: 'o',
      version: '',
      // 已删标签的残留引用（999）不带进表单
      models: [
        { name: 'b', description: '', tagIds: ['2'] },
        { name: 'a', description: '甲', tagIds: ['1'] },
      ],
    })
    expect(new Set(form.models.map(x => x.rowKey)).size).toBe(2)

    const anthropic: ModelProviderDto = {
      name: 'F',
      protocol: 2,
      baseUrl: 'u',
      apiKey: 'k',
      setting: { type: 2, version: 'v' } satisfies AnthropicModelProviderSetting,
    }
    expect(providerFormFromDto(anthropic, tags, true)).toEqual({
      name: 'F',
      protocol: 2,
      baseUrl: 'u',
      apiKey: 'k',
      organization: '',
      version: 'v',
      models: [],
    })

    // OpenAI Response：组织标识回填到同一个输入框
    const response: ModelProviderDto = {
      name: 'R',
      protocol: 3,
      baseUrl: 'u',
      apiKey: 'k',
      setting: { type: 3, organization: 'org-r' } satisfies OpenAiResponseModelProviderSetting,
    }
    expect(providerFormFromDto(response, tags, true)).toEqual({
      name: 'R',
      protocol: 3,
      baseUrl: 'u',
      apiKey: 'k',
      organization: 'org-r',
      version: '',
      models: [],
    })
  })

  it('从详情回填：标签列表没加载成功时不过滤标签 Id（原样保留）', () => {
    const dto: ModelProviderDto = { name: 'E', protocol: 1, baseUrl: 'u', apiKey: 'k', setting: { type: 1 }, models: [{ name: 'a', tagIds: ['1', '999'] }] }
    expect(providerFormFromDto(dto, tags, false).models[0].tagIds).toEqual(['1', '999'])
  })

  it('existingTagIds：只保留当前标签列表里能匹配到的 Id，顺序不变', () => {
    expect(existingTagIds(['2', '999', '1'], tags)).toEqual(['2', '1'])
    expect(existingTagIds(['999'], tags)).toEqual([])
    expect(existingTagIds(null, tags)).toEqual([])
    expect(existingTagIds(['1'], [])).toEqual([])
  })

  it('协议专属字段是否已有输入：只有名称、地址、密钥、模型时为 false', () => {
    expect(protocolFieldsHaveInput({ ...validOpenAi(), organization: '', models: [row('a')] })).toBe(false)
    expect(protocolFieldsHaveInput({ ...validOpenAi(), organization: '', version: 'v' })).toBe(true)
  })

  it('切换协议只清空协议专属字段', () => {
    const kept = row('a')
    const state = { ...validOpenAi(), version: 'v', models: [kept] }
    resetProtocolFields(state)
    expect(state.organization).toBe('')
    expect(state.version).toBe('')
    // 名称、地址、密钥、模型保留
    expect(state.name).toBe(' 智谱 (OpenAI) ')
    expect(state.models).toEqual([kept])
  })
})
