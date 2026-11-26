import type { AnthropicModelProviderEndpointSetting, ModelProviderProtocol, OpenAiModelProviderEndpointSetting } from '@/api'

type AnyEndpointSetting = OpenAiModelProviderEndpointSetting | AnthropicModelProviderEndpointSetting

/** 协议：数值与后端 ModelProviderProtocol 一致（即协议专属设置的判别键 type），不引入 enum（erasableSyntaxOnly） */
const MODEL_PROVIDER_PROTOCOL = {
  OpenAI: 1,
  Anthropic: 2,
} as const satisfies Record<string, ModelProviderProtocol>

type ProtocolValue = ModelProviderProtocol

const PROTOCOL_LABELS: Record<ProtocolValue, string> = {
  [MODEL_PROVIDER_PROTOCOL.OpenAI]: 'OpenAI 兼容',
  [MODEL_PROVIDER_PROTOCOL.Anthropic]: 'Anthropic 兼容',
}

const PROTOCOL_OPTIONS: ReadonlyArray<{ value: ProtocolValue; label: string }> = [
  { value: MODEL_PROVIDER_PROTOCOL.OpenAI, label: PROTOCOL_LABELS[MODEL_PROVIDER_PROTOCOL.OpenAI] },
  { value: MODEL_PROVIDER_PROTOCOL.Anthropic, label: PROTOCOL_LABELS[MODEL_PROVIDER_PROTOCOL.Anthropic] },
]

function protocolLabel(protocol: number | undefined): string {
  return (protocol != null && PROTOCOL_LABELS[protocol as ProtocolValue]) || '未知'
}

export {
  MODEL_PROVIDER_PROTOCOL,
  PROTOCOL_LABELS,
  PROTOCOL_OPTIONS,
  protocolLabel,
  type AnyEndpointSetting,
  type ProtocolValue,
}
