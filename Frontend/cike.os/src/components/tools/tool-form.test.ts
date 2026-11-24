import { describe, expect, it } from 'vitest'
import { TOOL_TYPE } from '@/lib/tool-type'
import {
  TOOL_VALUE_TYPE,
  emptyToolForm,
  isSettingDirty,
  parameterCodes,
  resetSettings,
  toCreateToolDto,
  toolFormFromDto,
  validateToolForm,
  toUpdateToolDto,
} from '@/components/tools/tool-form'
import { HTTP_TRANSPORT_MODE } from '@/lib/mcp-transport'
import { secondsToTimeSpan } from '@/lib/time-span'

function validSkillForm() {
  const form = emptyToolForm('100')
  form.code = 'skill-tool'
  form.name = '技能工具'
  form.type = TOOL_TYPE.Skill
  form.skill = { directory: 'skills/0/abc', fileName: 'demo.zip' }
  return form
}

function validHttpForm() {
  const form = emptyToolForm('100')
  form.code = 'http-tool'
  form.name = 'HTTP 工具'
  form.type = TOOL_TYPE.HttpMcp
  form.http.endpoint = 'http://localhost/mcp'
  return form
}

describe('validateToolForm', () => {
  it('合法技能形态无错误', () => {
    expect(validateToolForm(validSkillForm())).toEqual({})
  })

  it('必填项缺失与未上传技能包都有对应错误', () => {
    const form = validSkillForm()
    form.code = ' '
    form.name = ''
    form.skill.directory = ''
    const errors = validateToolForm(form)
    expect(errors.code).toBeDefined()
    expect(errors.name).toBeDefined()
    expect(errors['skill.directory']).toBeDefined()
  })

  it('参数编码空白或重复时报错', () => {
    const blank = validHttpForm()
    blank.parameters = [{ code: ' ', name: 'x' }]
    expect(validateToolForm(blank).parameters).toBeDefined()

    const duplicated = validHttpForm()
    duplicated.parameters = [{ code: 'a', name: 'x' }, { code: 'a', name: 'y' }]
    expect(validateToolForm(duplicated).parameters).toBeDefined()
  })

  it('引用未定义参数时报错，引用已定义参数时通过', () => {
    const form = validHttpForm()
    form.parameters = [{ code: 'ak', name: 'Key' }]
    form.http.additionalHeaders = { 'X-Key': { type: TOOL_VALUE_TYPE.ParameterReference, value: 'missing' } }
    expect(validateToolForm(form)['http.additionalHeaders']).toBeDefined()

    form.http.additionalHeaders = { 'X-Key': { type: TOOL_VALUE_TYPE.ParameterReference, value: 'ak' } }
    expect(validateToolForm(form)).toEqual({})
  })

  it('标准 IO 形态缺少命令时报错', () => {
    const form = emptyToolForm('100')
    form.code = 'stdio'
    form.name = 'stdio'
    form.type = TOOL_TYPE.StdioMcp
    form.stdio.command = ''
    expect(validateToolForm(form)['stdio.command']).toBeDefined()
  })
})

describe('toCreateToolDto', () => {
  it('按当前形态只输出对应的值对象，判别键为 ToolType 数值', () => {
    const skill = toCreateToolDto(validSkillForm())
    expect(skill.setting).toEqual({ type: 1, directory: 'skills/0/abc' })

    const http = toCreateToolDto(validHttpForm())
    expect(http.setting).toMatchObject({ type: 2, endpoint: 'http://localhost/mcp', transportMode: HTTP_TRANSPORT_MODE.StreamableHttp })
    // 表单里的「秒」在提交时转成 TimeSpan 字串
    expect(http.setting).toMatchObject({ connectionTimeout: '00:00:30', defaultReconnectionInterval: '00:00:01' })
  })

  it('标准 IO 参数按行拆分并去掉空行', () => {
    const form = emptyToolForm('100')
    form.code = 'stdio'
    form.name = 'stdio'
    form.type = TOOL_TYPE.StdioMcp
    form.stdio.command = 'npx'
    form.stdio.argumentsText = ' -y \n\n server \n'
    expect(toCreateToolDto(form).setting).toMatchObject({ type: 3, arguments: ['-y', 'server'] })
  })
})

describe('toUpdateToolDto', () => {
  it('更新载荷不带 type（形态创建后不可变）与 id（id 在路由），其余字段与创建一致', () => {
    const form = validHttpForm()
    form.parameters = [{ code: 'ak', name: 'Key' }]

    const update = toUpdateToolDto(form)
    const create = toCreateToolDto(form)

    expect(update).not.toHaveProperty('type')
    expect(update).not.toHaveProperty('id')
    expect(update).toEqual({ code: create.code, name: create.name, description: create.description, toolPackageId: create.toolPackageId, parameters: create.parameters, setting: create.setting })
  })
})

describe('toolFormFromDto / resetSettings', () => {
  it('从 DTO 还原表单：识别形态、TimeSpan 转秒', () => {
    const form = toolFormFromDto({
      id: '1',
      code: 'http-tool',
      name: 'x',
      type: TOOL_TYPE.HttpMcp,
      toolPackageId: '100',
      parameters: [{ code: 'ak', name: 'Key' }],
      setting: {
        type: 2,
        endpoint: 'http://localhost/mcp',
        transportMode: HTTP_TRANSPORT_MODE.Sse,
        connectionTimeout: secondsToTimeSpan(15),
        defaultReconnectionInterval: '00:00:02',
        maxReconnectionAttempts: 3,
        additionalHeaders: { a: { type: TOOL_VALUE_TYPE.Literal, value: '1' } },
      },
    })
    expect(form.type).toBe(TOOL_TYPE.HttpMcp)
    expect(form.http.transportMode).toBe(HTTP_TRANSPORT_MODE.Sse)
    expect(form.http.connectionTimeoutSeconds).toBe(15)
    expect(form.http.defaultReconnectionIntervalSeconds).toBe(2)
    expect(form.http.maxReconnectionAttempts).toBe(3)
    expect(form.http.additionalHeaders).toEqual({ a: { type: TOOL_VALUE_TYPE.Literal, value: '1' } })
    expect(parameterCodes(form)).toEqual(['ak'])
  })

  it('设置区有内容时 isSettingDirty 为真，resetSettings 后为假且参数定义保留', () => {
    const form = validHttpForm()
    form.parameters = [{ code: 'ak', name: 'Key' }]
    expect(isSettingDirty(form)).toBe(true)

    const reset = resetSettings(form)
    expect(isSettingDirty(reset)).toBe(false)
    expect(reset.parameters).toEqual([{ code: 'ak', name: 'Key' }])
    expect(form.http.endpoint).toBe('http://localhost/mcp')
  })
})
