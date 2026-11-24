import type { HttpMcpToolSetting, SkillToolSetting, StdioMcpToolSetting, ToolType } from '@/api'
import { HTTP_TRANSPORT_MODE } from '@/lib/mcp-transport'
import { secondsToTimeSpan } from '@/lib/time-span'

/** 工具形态：数值与后端 ToolType 枚举一致，不引入 enum（erasableSyntaxOnly） */
const TOOL_TYPE = {
  Skill: 1,
  HttpMcp: 2,
  StdioMcp: 3,
} as const satisfies Record<string, ToolType>

type ToolTypeValue = ToolType

const TOOL_TYPE_LABELS: Record<ToolTypeValue, string> = {
  [TOOL_TYPE.Skill]: '技能',
  [TOOL_TYPE.HttpMcp]: 'HTTP MCP',
  [TOOL_TYPE.StdioMcp]: '标准 IO MCP',
}

const TOOL_TYPE_OPTIONS: ReadonlyArray<{ value: ToolTypeValue; label: string }> = [
  { value: TOOL_TYPE.Skill, label: TOOL_TYPE_LABELS[TOOL_TYPE.Skill] },
  { value: TOOL_TYPE.HttpMcp, label: TOOL_TYPE_LABELS[TOOL_TYPE.HttpMcp] },
  { value: TOOL_TYPE.StdioMcp, label: TOOL_TYPE_LABELS[TOOL_TYPE.StdioMcp] },
]

type AnyToolSetting = SkillToolSetting | HttpMcpToolSetting | StdioMcpToolSetting

/** 由多态设置的判别键反推工具形态 */
function toolTypeFromSetting(setting: AnyToolSetting | null | undefined): ToolTypeValue {
  switch (setting?.type) {
    case TOOL_TYPE.Skill:
      return TOOL_TYPE.Skill
    case TOOL_TYPE.HttpMcp:
      return TOOL_TYPE.HttpMcp
    default:
      return TOOL_TYPE.StdioMcp
  }
}

/** 各形态设置的默认值（字段与后端值对象默认值一致） */
function defaultSettingForType(type: ToolTypeValue): AnyToolSetting {
  switch (type) {
    case TOOL_TYPE.Skill:
      return { type: TOOL_TYPE.Skill, directory: '' } satisfies SkillToolSetting
    case TOOL_TYPE.HttpMcp:
      return {
        type: TOOL_TYPE.HttpMcp,
        name: '',
        endpoint: '',
        transportMode: HTTP_TRANSPORT_MODE.StreamableHttp,
        connectionTimeout: secondsToTimeSpan(30),
        maxReconnectionAttempts: 5,
        defaultReconnectionInterval: secondsToTimeSpan(1),
        credentialKey: null,
        additionalHeaders: null,
      } satisfies HttpMcpToolSetting
    default:
      return {
        type: TOOL_TYPE.StdioMcp,
        name: '',
        command: '',
        arguments: null,
        workingDirectory: null,
        environmentVariables: null,
      } satisfies StdioMcpToolSetting
  }
}

export {
  TOOL_TYPE,
  TOOL_TYPE_LABELS,
  TOOL_TYPE_OPTIONS,
  defaultSettingForType,
  toolTypeFromSetting,
  type AnyToolSetting,
  type ToolTypeValue,
}
