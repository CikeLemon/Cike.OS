import { describe, expect, it } from 'vitest'
import type { AgentDetailDto, AgentToolPackageOptionItemDto } from '@/api'
import { AGENT_TOOL_TYPE, TOOL_MODE } from '@/lib/agent-options'
import {
  agentFormFromDto,
  agentFormToBody,
  emptyAgentForm,
  inputToNumber,
  pendingParameterToolIds,
  selectWholePackage,
  toggleTool,
  toolSelectionFromRows,
  toolSelectionToRows,
  unselectToolUnderWholePackage,
  unselectWholePackage,
  validateAgentForm,
} from './agent-form'

function validForm() {
  const form = emptyAgentForm()
  form.name = '客服助手'
  form.instructions = '你是一名客服。'
  return form
}

const options: AgentToolPackageOptionItemDto[] = [
  {
    toolPackageId: '100',
    name: '天气插件',
    tools: [
      { toolId: '1001', name: '天气查询', type: 2, parameterCount: 1 },
      { toolId: '1002', name: '空气质量', type: 2, parameterCount: 0 },
    ],
  },
  {
    toolPackageId: '101',
    name: '搜索套件',
    tools: [{ toolId: '1011', name: '网页搜索', type: 2, parameterCount: 2 }],
  },
]

describe('agent-form', () => {
  it('数值输入同时接受字符串与 number（type=number 的 v-model 会转成 number）', () => {
    expect(inputToNumber('')).toBeNull()
    expect(inputToNumber(null)).toBeNull()
    expect(inputToNumber(' 2048 ')).toBe(2048)
    expect(inputToNumber(0.7)).toBe(0.7)
    expect(Number.isNaN(inputToNumber('abc'))).toBe(true)
  })
  it('表单 → 请求体：全部模型参数留空 = 不设置，工具按类型、目标排序', () => {
    const form = validForm()
    form.wholePackages = ['101']
    form.toolIds = ['1002']

    expect(agentFormToBody(form)).toEqual({
      name: '客服助手',
      description: null,
      instructions: '你是一名客服。',
      options: {
        temperature: null,
        maxOutputTokens: null,
        topP: null,
        topK: null,
        frequencyPenalty: null,
        presencePenalty: null,
        seed: null,
        reasoning: undefined,
        responseFormat: undefined,
        stopSequences: null,
        allowMultipleToolCalls: null,
        toolMode: null,
        allowBackgroundResponses: null,
      },
      tools: [
        { type: AGENT_TOOL_TYPE.ToolPackage, targetId: '101' },
        { type: AGENT_TOOL_TYPE.Tool, targetId: '1002' },
      ],
    })
  })

  it('表单 → 请求体：数值、工具模式（$type 多态）、推理、响应格式、三态布尔', () => {
    const form = validForm()
    form.temperature = '0.7'
    form.maxOutputTokens = ' 2048 '
    form.toolMode = TOOL_MODE.Required
    form.reasoningEffort = 3
    form.reasoningOutput = 1
    form.responseFormatType = 2
    form.jsonSchemaName = 'answer'
    form.jsonSchemaDescriptor = '{"type":"object"}'
    form.stopSequences = ['END', 'STOP']
    form.allowMultipleToolCalls = 'false'
    form.allowBackgroundResponses = 'true'

    const body = agentFormToBody(form)
    expect(body.options).toEqual({
      temperature: 0.7,
      maxOutputTokens: 2048,
      topP: null,
      topK: null,
      frequencyPenalty: null,
      presencePenalty: null,
      seed: null,
      reasoning: { effort: 3, output: 1 },
      responseFormat: { type: 2, jsonSchemaName: 'answer', jsonSchemaDescriptor: '{"type":"object"}' },
      stopSequences: ['END', 'STOP'],
      allowMultipleToolCalls: false,
      toolMode: { $type: 'required' },
      allowBackgroundResponses: true,
    })
  })

  it('详情 → 表单：模型选项与工具勾选状态两个方向都能回填', () => {
    const dto: AgentDetailDto = {
      id: '1',
      name: '客服助手',
      description: '描述',
      instructions: '指令',
      options: {
        temperature: 0.3,
        topP: 1,
        seed: '42',
        reasoning: { effort: 2, output: 0 },
        responseFormat: { type: 1, jsonSchemaName: null, jsonSchemaDescriptor: null },
        toolMode: { $type: 'auto' },
        allowBackgroundResponses: false,
        stopSequences: ['A'],
      },
      tools: [
        { type: 2, targetId: '1002' },
        { type: 1, targetId: '100' },
      ],
    }

    expect(agentFormFromDto(dto)).toMatchObject({
      name: '客服助手',
      description: '描述',
      instructions: '指令',
      temperature: '0.3',
      topP: '1',
      seed: '42',
      reasoningEffort: 2,
      reasoningOutput: 0,
      responseFormatType: 1,
      toolMode: 'auto',
      allowBackgroundResponses: 'false',
      stopSequences: ['A'],
      wholePackages: ['100'],
      toolIds: ['1002'],
    })

    // 再提交回去：与详情的工具条目一致（按类型、目标排序）
    const again = agentFormToBody(agentFormFromDto(dto))
    expect(again.tools).toEqual([
      { type: 1, targetId: '100' },
      { type: 2, targetId: '1002' },
    ])
    expect(again.options?.toolMode).toEqual({ $type: 'auto' })
  })

  it('工具模式只认三种预定义模式，其它情况视为未设置', () => {
    expect(agentFormFromDto({ options: { toolMode: { $type: 'whatever' } } })).toMatchObject({ toolMode: null })
    expect(agentFormFromDto({ options: { toolMode: { $type: 'none' } } })).toMatchObject({ toolMode: 'none' })
  })

  it('勾选互斥：勾整包后包内工具从单选移除；取消整包只去标记；整包下的工具不能单选', () => {
    const selection = { wholePackages: [], toolIds: ['1001'] }

    selectWholePackage(selection, '100', ['1001', '1002'])
    expect(selection).toEqual({ wholePackages: ['100'], toolIds: [] })

    unselectWholePackage(selection, '100')
    expect(selection).toEqual({ wholePackages: [], toolIds: [] })

    toggleTool(selection, '1002', true)
    toggleTool(selection, '1002', true)
    toggleTool(selection, '1001', true)
    expect(selection.toolIds).toEqual(['1002', '1001'])
    toggleTool(selection, '1002', false)
    expect(selection.toolIds).toEqual(['1001'])
  })

  it('整包状态下取消其中一个工具：整包自动取消，包内其余工具转为单选保留', () => {
    const selection = { wholePackages: ['100'], toolIds: ['1011'] }
    unselectToolUnderWholePackage(selection, '100', ['1001', '1002', '1003'], '1002')
    expect(selection).toEqual({ wholePackages: [], toolIds: ['1011', '1001', '1003'] })
  })

  it('勾选状态 ↔ 条目：按类型、目标排序，两个方向互逆', () => {
    const rows = toolSelectionToRows({ wholePackages: ['101', '100'], toolIds: ['1011', '1002'] })
    expect(rows.map(x => `${x.type}:${x.targetId}`)).toEqual(['1:100', '1:101', '2:1002', '2:1011'])
    expect(toolSelectionFromRows(rows)).toEqual({ wholePackages: ['100', '101'], toolIds: ['1002', '1011'] })
  })

  it('新建后要跳去填参数的工具：单选行本身 + 整包行展开成包内全部工具，只留带参数的', () => {
    expect(pendingParameterToolIds(toolSelectionToRows({ wholePackages: ['100'], toolIds: ['1002'] }), options)).toEqual(['1001'])
    expect(pendingParameterToolIds(toolSelectionToRows({ wholePackages: [], toolIds: ['1011'] }), options)).toEqual(['1011'])
    expect(pendingParameterToolIds(toolSelectionToRows({ wholePackages: [], toolIds: ['1002'] }), options)).toEqual([])
    expect(pendingParameterToolIds([], options)).toEqual([])
  })

  it('校验：名称 / 指令必填，长度上限，数值范围，停止词，JSON Schema', () => {
    expect(validateAgentForm(validForm())).toEqual({})

    const form = validForm()
    form.name = ''
    form.instructions = ' '
    form.description = '描'.repeat(513)
    form.temperature = '3'
    form.topP = '-0.1'
    form.topK = '0'
    form.maxOutputTokens = 'abc'
    form.frequencyPenalty = '-2.5'
    form.presencePenalty = '2.1'
    form.seed = '-1'
    form.stopSequences = ['', 'x'.repeat(65)]
    form.responseFormatType = 2
    form.jsonSchemaDescriptor = '{not json'
    expect(Object.keys(validateAgentForm(form)).sort()).toEqual(
      ['description', 'frequencyPenalty', 'instructions', 'jsonSchemaDescriptor', 'maxOutputTokens', 'name', 'presencePenalty', 'seed', 'stopSequences', 'temperature', 'topK', 'topP'].sort(),
    )

    // 文本格式不要求 Schema；边界值放行
    const ok = validForm()
    ok.temperature = '2'
    ok.topP = '1'
    ok.topK = '1'
    ok.frequencyPenalty = '-2'
    ok.seed = '0'
    ok.stopSequences = Array.from({ length: 16 }, () => 'END')
    ok.responseFormatType = 1
    expect(validateAgentForm(ok)).toEqual({})
  })
})
