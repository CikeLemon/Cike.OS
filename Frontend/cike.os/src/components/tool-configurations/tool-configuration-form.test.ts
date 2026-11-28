import { describe, expect, it } from 'vitest'
import {
  batchItems,
  configurationFormFromValues,
  configurationValuesToBody,
  maskValue,
  missingRequiredParameters,
  validateConfigurationForm,
} from './tool-configuration-form'

const parameters = [
  { code: 'ak', name: 'Access key', required: true, sensitive: true },
  { code: 'unit', name: '单位', required: false, sensitive: false },
]

describe('tool-configuration-form', () => {
  it('已存值 → 表单：每个参数一行，已存回填、没有给空串', () => {
    expect(configurationFormFromValues(parameters, { ak: 'sk-1' })).toEqual({ ak: 'sk-1', unit: '' })
    expect(configurationFormFromValues(parameters, null)).toEqual({ ak: '', unit: '' })
    expect(configurationFormFromValues(null, { ak: 'x' })).toEqual({})
  })

  it('缺必填：必填项没有非空白值就算缺', () => {
    expect(missingRequiredParameters(parameters, { ak: 'v', unit: '' })).toEqual([])
    expect(missingRequiredParameters(parameters, { ak: ' ', unit: 'x' }).map(x => x.code)).toEqual(['ak'])
    expect(missingRequiredParameters(parameters, null).map(x => x.code)).toEqual(['ak'])
  })

  it('校验：必填不能为空，填了的值不能是纯空白', () => {
    expect(validateConfigurationForm(parameters, { ak: 'v', unit: '' })).toEqual({})
    expect(validateConfigurationForm(parameters, { ak: '', unit: '' })).toEqual({ ak: 'Access key 为必填项。' })
    expect(validateConfigurationForm(parameters, { ak: 'v', unit: '   ' })).toEqual({ unit: '单位 不能为空白。' })
  })

  it('表单 → 请求：空串不传', () => {
    expect(configurationValuesToBody({ ak: 'sk-1', unit: '' })).toEqual({ ak: 'sk-1' })
  })

  it('批量：每个选中工具一条', () => {
    expect(batchItems({ '1': { ak: 'a', unit: '' }, '2': { ak: 'b' } })).toEqual([
      { toolId: '1', values: { ak: 'a' } },
      { toolId: '2', values: { ak: 'b' } },
    ])
  })

  it('遮罩固定为圆点串', () => {
    expect(maskValue()).toBe('••••••')
  })
})
