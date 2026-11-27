import type {
  AnthropicModelProviderEndpointSetting,
  ModelProviderProtocol,
  OpenAiChatCompletionModelProviderEndpointSetting,
  OpenAiResponseModelProviderEndpointSetting,
} from '@/api'

type AnyEndpointSetting =
  | OpenAiChatCompletionModelProviderEndpointSetting
  | OpenAiResponseModelProviderEndpointSetting
  | AnthropicModelProviderEndpointSetting

/** 协议：数值与后端 ModelProviderProtocol 一致（即协议专属设置的判别键 type），不引入 enum（erasableSyntaxOnly） */
const MODEL_PROVIDER_PROTOCOL = {
  OpenAIChatCompletion: 1,
  Anthropic: 2,
  OpenAIResponse: 3,
} as const satisfies Record<string, ModelProviderProtocol>

type ProtocolValue = ModelProviderProtocol

const PROTOCOL_LABELS: Record<ProtocolValue, string> = {
  [MODEL_PROVIDER_PROTOCOL.OpenAIChatCompletion]: 'OpenAI Chat Completion 兼容',
  [MODEL_PROVIDER_PROTOCOL.Anthropic]: 'Anthropic 兼容',
  [MODEL_PROVIDER_PROTOCOL.OpenAIResponse]: 'OpenAI Response 兼容',
}

const PROTOCOL_OPTIONS: ReadonlyArray<{ value: ProtocolValue; label: string }> = [
  { value: MODEL_PROVIDER_PROTOCOL.OpenAIChatCompletion, label: PROTOCOL_LABELS[MODEL_PROVIDER_PROTOCOL.OpenAIChatCompletion] },
  { value: MODEL_PROVIDER_PROTOCOL.Anthropic, label: PROTOCOL_LABELS[MODEL_PROVIDER_PROTOCOL.Anthropic] },
  { value: MODEL_PROVIDER_PROTOCOL.OpenAIResponse, label: PROTOCOL_LABELS[MODEL_PROVIDER_PROTOCOL.OpenAIResponse] },
]

/** 两种 OpenAI 协议（Chat Completion / Response）的专属设置是两个子类，字段同为组织标识 */
function isOpenAiProtocol(protocol: number): boolean {
  return protocol === MODEL_PROVIDER_PROTOCOL.OpenAIChatCompletion || protocol === MODEL_PROVIDER_PROTOCOL.OpenAIResponse
}

function protocolLabel(protocol: number | undefined): string {
  return (protocol != null && PROTOCOL_LABELS[protocol as ProtocolValue]) || '未知'
}

export {
  MODEL_PROVIDER_PROTOCOL,
  PROTOCOL_LABELS,
  PROTOCOL_OPTIONS,
  isOpenAiProtocol,
  protocolLabel,
  type AnyEndpointSetting,
  type ProtocolValue,
}
