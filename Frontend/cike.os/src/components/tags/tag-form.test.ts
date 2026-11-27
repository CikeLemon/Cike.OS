import { describe, expect, it } from 'vitest'
import {
  TAG_KEY_PATTERN,
  emptyTagForm,
  tagFormToCreateBody,
  tagFormToUpdateBody,
  tagFormFromDto,
  validateTagForm,
} from './tag-form'

describe('tag-form', () => {
  it('从 DTO 回填 / 空表单', () => {
    expect(tagFormFromDto({ id: '1', key: 'vision', name: '视觉' })).toEqual({ key: 'vision', name: '视觉' })
    expect(emptyTagForm()).toEqual({ key: '', name: '' })
    expect(tagFormFromDto(null)).toEqual(emptyTagForm())
  })

  it('校验：键必填、格式、长度', () => {
    expect(validateTagForm({ key: 'vision', name: '视觉' })).toEqual({})
    expect(validateTagForm({ key: 'vision-2_x', name: '视觉' })).toEqual({})
    expect(validateTagForm({ key: '  ', name: '视觉' })).toEqual({ key: '请填写键。' })
    expect(validateTagForm({ key: 'Vision', name: '视觉' })).toEqual({ key: '键只能由小写字母、数字、- 和 _ 组成，且以字母开头。' })
    expect(validateTagForm({ key: '2vision', name: '视觉' })).toEqual({ key: '键只能由小写字母、数字、- 和 _ 组成，且以字母开头。' })
    expect(validateTagForm({ key: 'vis ion', name: '视觉' })).toEqual({ key: '键只能由小写字母、数字、- 和 _ 组成，且以字母开头。' })
    expect(validateTagForm({ key: 'a'.repeat(65), name: '视觉' })).toEqual({ key: '键不能超过 64 个字符。' })
    expect(validateTagForm({ key: 'a'.repeat(64), name: '视觉' })).toEqual({})
  })

  it('校验：名称必填、长度；键与名称同时出错时两条都报', () => {
    expect(validateTagForm({ key: 'vision', name: '' })).toEqual({ name: '请填写名称。' })
    expect(validateTagForm({ key: 'vision', name: '长'.repeat(129) })).toEqual({ name: '名称不能超过 128 个字符。' })
    expect(validateTagForm({ key: '', name: '' })).toEqual({ key: '请填写键。', name: '请填写名称。' })
  })

  it('请求体：去首尾空白；更新体不带键', () => {
    expect(tagFormToCreateBody({ key: ' vision ', name: ' 视觉 ' })).toEqual({ key: 'vision', name: '视觉' })
    expect(tagFormToUpdateBody({ key: 'vision', name: ' 视觉 ' })).toEqual({ name: '视觉' })
  })

  it('键正则与后端一致', () => {
    expect(TAG_KEY_PATTERN.test('a')).toBe(true)
    expect(TAG_KEY_PATTERN.test('a-b_c1')).toBe(true)
    expect(TAG_KEY_PATTERN.test('A')).toBe(false)
    expect(TAG_KEY_PATTERN.test('_a')).toBe(false)
    expect(TAG_KEY_PATTERN.test('a.b')).toBe(false)
    expect(TAG_KEY_PATTERN.test('')).toBe(false)
  })
})
