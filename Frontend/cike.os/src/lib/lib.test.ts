import { describe, expect, it } from 'vitest'
import { filterToolsByKeyword } from '@/lib/filter'
import { formatDateTime, truncate } from '@/lib/format'
import { TOOL_PACKAGE_STATUS, getStatusBadgeVariant } from '@/lib/tool-status'
import { defaultSettingForType, toolTypeFromSetting, TOOL_TYPE } from '@/lib/tool-type'
import { secondsToTimeSpan, timeSpanToSeconds } from '@/lib/time-span'
import { HTTP_TRANSPORT_MODE_OPTIONS } from '@/lib/mcp-transport'

describe('filterToolsByKeyword', () => {
  const tools = [
    { code: 'search', name: '网页搜索', description: 'Search the web' },
    { code: 'fetch', name: '抓取', description: 'Fetch a URL' },
    { code: 'calc', name: '计算器', description: null },
  ]

  it('空关键词原样返回', () => {
    expect(filterToolsByKeyword(tools, '   ')).toEqual(tools)
  })

  it('按编码 / 名称 / 描述大小写不敏感匹配', () => {
    expect(filterToolsByKeyword(tools, 'SEARCH').map(x => x.code)).toEqual(['search'])
    expect(filterToolsByKeyword(tools, '抓')).toEqual([tools[1]])
    expect(filterToolsByKeyword(tools, 'url').map(x => x.code)).toEqual(['fetch'])
  })

  it('无命中返回空数组', () => {
    expect(filterToolsByKeyword(tools, 'nothing')).toEqual([])
  })
})

describe('getStatusBadgeVariant', () => {
  it('已发布用 default，已下线用 outline，草稿 / 未知用 secondary', () => {
    expect(getStatusBadgeVariant(TOOL_PACKAGE_STATUS.Published)).toBe('default')
    expect(getStatusBadgeVariant(TOOL_PACKAGE_STATUS.Offline)).toBe('outline')
    expect(getStatusBadgeVariant(TOOL_PACKAGE_STATUS.Draft)).toBe('secondary')
    expect(getStatusBadgeVariant(undefined)).toBe('secondary')
  })
})

describe('format helpers', () => {
  it('formatDateTime 补零并使用本地时区', () => {
    expect(formatDateTime('2026-11-24T03:05:00')).toBe('2026-11-24 03:05')
    expect(formatDateTime(null)).toBe('—')
    expect(formatDateTime('not-a-date')).toBe('—')
  })

  it('truncate 超长截断并追加省略号', () => {
    expect(truncate('abcdef', 3)).toBe('abc…')
    expect(truncate('abc', 3)).toBe('abc')
    expect(truncate('', 3)).toBe('—')
  })
})

describe('tool-type helpers', () => {
  it('由设置判别键（ToolType 数值）反推形态', () => {
    expect(toolTypeFromSetting({ type: 1, directory: 'x' })).toBe(TOOL_TYPE.Skill)
    expect(toolTypeFromSetting(defaultSettingForType(TOOL_TYPE.HttpMcp))).toBe(TOOL_TYPE.HttpMcp)
    expect(toolTypeFromSetting(defaultSettingForType(TOOL_TYPE.StdioMcp))).toBe(TOOL_TYPE.StdioMcp)
    expect(toolTypeFromSetting(null)).toBe(TOOL_TYPE.StdioMcp)
  })
})

describe('time-span helpers', () => {
  it('秒转 TimeSpan 字串，超过一天带天数段', () => {
    expect(secondsToTimeSpan(30)).toBe('00:00:30')
    expect(secondsToTimeSpan(3661)).toBe('01:01:01')
    expect(secondsToTimeSpan(90061)).toBe('1.01:01:01')
    expect(secondsToTimeSpan(-5)).toBe('00:00:00')
  })

  it('TimeSpan 字串转整秒，容忍毫秒与天数，非法返回 0', () => {
    expect(timeSpanToSeconds('00:00:30')).toBe(30)
    expect(timeSpanToSeconds('1.01:01:01.5000000')).toBe(90061)
    expect(timeSpanToSeconds('')).toBe(0)
    expect(timeSpanToSeconds('abc')).toBe(0)
  })

  it('往返一致', () => {
    expect(timeSpanToSeconds(secondsToTimeSpan(123456))).toBe(123456)
  })
})

describe('mcp transport mode options', () => {
  it('三个选项与 MCP SDK 枚举值一致', () => {
    expect(HTTP_TRANSPORT_MODE_OPTIONS.map(o => o.value)).toEqual([0, 1, 2])
  })
})
