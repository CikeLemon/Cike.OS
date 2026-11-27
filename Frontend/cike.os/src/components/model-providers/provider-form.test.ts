import { describe, expect, it } from 'vitest'
import { emptyProviderForm, providerFormFromDto, providerFormToBody, providerUpdateBody, validateProviderForm } from './provider-form'

describe('provider-form', () => {
  it('提供方表单只有名称：新建请求体去首尾空白', () => {
    expect(providerFormToBody({ name: ' 智谱 ' })).toEqual({ name: '智谱' })
  })

  it('更新请求体只有名称：接入点不随提供方提交', () => {
    expect(providerUpdateBody(' 智谱 ')).toEqual({ name: '智谱' })
    expect(providerUpdateBody(undefined)).toEqual({ name: '' })
  })

  it('校验：名称必填且不超过 128 个字符', () => {
    expect(validateProviderForm({ name: '智谱' })).toEqual({})
    expect(validateProviderForm(emptyProviderForm())).toEqual({ name: '请填写模型提供方名称。' })
    expect(validateProviderForm({ name: 'x'.repeat(129) })).toEqual({ name: '模型提供方名称不能超过 128 个字符。' })
  })

  it('从 DTO 回填：只取名称', () => {
    expect(providerFormFromDto({ name: 'P' })).toEqual({ name: 'P' })
    expect(providerFormFromDto(null)).toEqual({ name: '' })
  })
})
