import type { AnthropicModelProviderSetting, OpenAiModelProviderSetting } from '@/api'

type AnyProviderSetting = OpenAiModelProviderSetting | AnthropicModelProviderSetting

/** 提供商类型：数值与后端 ModelProviderType 一致（即设置的判别键 type），不引入 enum（erasableSyntaxOnly） */
const MODEL_PROVIDER_TYPE = {
  OpenAI: 1,
  Anthropic: 2,
} as const satisfies Record<string, AnyProviderSetting['type']>

type ProviderTypeValue = AnyProviderSetting['type']

const MODEL_PROVIDER_TYPE_LABELS: Record<ProviderTypeValue, string> = {
  [MODEL_PROVIDER_TYPE.OpenAI]: 'OpenAI 兼容',
  [MODEL_PROVIDER_TYPE.Anthropic]: 'Anthropic',
}

const MODEL_PROVIDER_TYPE_OPTIONS = (Object.keys(MODEL_PROVIDER_TYPE) as (keyof typeof MODEL_PROVIDER_TYPE)[]).map(
  key => ({ value: MODEL_PROVIDER_TYPE[key], label: MODEL_PROVIDER_TYPE_LABELS[MODEL_PROVIDER_TYPE[key]] }),
)

function providerTypeLabel(type: number | undefined): string {
  return (type != null && MODEL_PROVIDER_TYPE_LABELS[type as ProviderTypeValue]) || '未知'
}

export {
  MODEL_PROVIDER_TYPE,
  MODEL_PROVIDER_TYPE_LABELS,
  MODEL_PROVIDER_TYPE_OPTIONS,
  providerTypeLabel,
  type AnyProviderSetting,
  type ProviderTypeValue,
}
