/** 生成 client 返回的错误：后端体已解析（JSON 对象或字串），并带 request/response */
interface ApiErrorShape {
  error?: unknown
  response?: Response
}

/**
 * 从生成 client 的失败结果里提取可读的中文文案。
 * 后端三种形态：业务异常（JSON 字串）、FluentValidation（ProblemDetails.errors）、其他对象（message/title）。
 */
export function getErrorMessage(result: ApiErrorShape | unknown): string {
  const error = (result as ApiErrorShape | undefined)?.error

  if (typeof error === 'string') return error || '请求失败。'
  if (error == null || typeof error !== 'object') return '请求失败，请稍后重试。'

  const record = error as Record<string, unknown>

  const errors = record.errors
  if (errors && typeof errors === 'object') {
    const messages = Object.values(errors as Record<string, unknown>)
      .flatMap(value => (Array.isArray(value) ? value : [value]))
      .filter((value): value is string => typeof value === 'string')
    if (messages.length > 0) return messages.join('；')
  }

  if (typeof record.message === 'string' && record.message) return record.message
  if (typeof record.title === 'string' && record.title) return record.title

  const response = (result as ApiErrorShape).response
  if (response?.status === 404) return '请求的资源不存在。'
  if (response && response.status >= 500) return '服务器处理失败，请稍后重试。'

  return '请求失败，请稍后重试。'
}
