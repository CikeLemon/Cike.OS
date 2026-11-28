import type { SaveUserToolConfigurationDto, ToolParameterDefinition } from '@/api'

/** 参数 code → 用户填的值；全部按字符串处理 */
type ConfigurationValues = Record<string, string>

const MASK = '••••••'

/** 敏感值的遮罩（纯前端，接口原样返回） */
function maskValue(): string {
  return MASK
}

/** 已存值 → 表单：每个声明的参数一行，已存的回填，没有的给空串 */
function configurationFormFromValues(
  parameters: Array<ToolParameterDefinition> | null | undefined,
  values: ConfigurationValues | null | undefined,
): ConfigurationValues {
  const form: ConfigurationValues = {}
  for (const parameter of parameters ?? []) {
    const code = parameter.code ?? ''
    form[code] = values?.[code] ?? ''
  }
  return form
}

/** 必填项没填（空白视为没填） */
function missingRequiredParameters(
  parameters: Array<ToolParameterDefinition> | null | undefined,
  values: ConfigurationValues | null | undefined,
): ToolParameterDefinition[] {
  return (parameters ?? []).filter(parameter => parameter.required && !(values?.[parameter.code ?? ''] ?? '').trim())
}

/** 与后端单条保存一致的请求级校验：必填不能为空，填了的值不能是空白 */
function validateConfigurationForm(
  parameters: Array<ToolParameterDefinition> | null | undefined,
  values: ConfigurationValues,
): Record<string, string> {
  const errors: Record<string, string> = {}
  for (const parameter of parameters ?? []) {
    const code = parameter.code ?? ''
    const raw = values[code] ?? ''
    const value = raw.trim()
    const label = parameter.name || code
    if (value) continue
    if (parameter.required) errors[code] = `${label} 为必填项。`
    else if (raw) errors[code] = `${label} 不能为空白。`
  }
  return errors
}

/** 表单 → 请求里的 values：空串不传（可选项留空 = 不设置） */
function configurationValuesToBody(values: ConfigurationValues): Record<string, string> {
  const body: Record<string, string> = {}
  for (const [code, value] of Object.entries(values)) {
    if (value.trim()) body[code] = value
  }
  return body
}

/** 批量保存：选中工具的表单 → 请求体列表 */
function batchItems(selection: Record<string, ConfigurationValues>): SaveUserToolConfigurationDto[] {
  return Object.entries(selection).map(([toolId, values]) => ({ toolId, values: configurationValuesToBody(values) }))
}

export {
  batchItems,
  configurationFormFromValues,
  configurationValuesToBody,
  maskValue,
  missingRequiredParameters,
  validateConfigurationForm,
  type ConfigurationValues,
}
