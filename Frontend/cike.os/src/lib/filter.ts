/** 工具列表 keyword 过滤：匹配编码 / 名称 / 描述，大小写不敏感；空关键词原样返回 */
interface KeywordMatchable {
  code?: string | null
  name?: string | null
  description?: string | null
}

function filterToolsByKeyword<T extends KeywordMatchable>(tools: readonly T[], keyword: string): T[] {
  const needle = keyword.trim().toLowerCase()
  if (!needle) return [...tools]

  return tools.filter(tool =>
    [tool.code, tool.name, tool.description].some(field => field?.toLowerCase().includes(needle)),
  )
}

export { filterToolsByKeyword, type KeywordMatchable }
