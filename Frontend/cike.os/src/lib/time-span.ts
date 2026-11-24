/**
 * 值对象直接出 API 后，TimeSpan 以 System.Text.Json 的 ISO 形状传输（"00:00:30"）。
 * 表单用「秒」编辑，这里做双向换算。
 */

/** 秒 → "d.hh:mm:ss" 形式的 TimeSpan 字串（不足一天不带天数段） */
function secondsToTimeSpan(totalSeconds: number): string {
  const whole = Math.max(0, Math.floor(totalSeconds))
  const days = Math.floor(whole / 86400)
  const hours = Math.floor((whole % 86400) / 3600)
  const minutes = Math.floor((whole % 3600) / 60)
  const seconds = whole % 60
  const pad = (n: number) => String(n).padStart(2, '0')
  const time = `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`
  return days > 0 ? `${days}.${time}` : time
}

/** "[-]d.hh:mm:ss[.fff]" → 整秒；无法解析返回 0 */
function timeSpanToSeconds(value: string | null | undefined): number {
  if (!value) return 0

  const match = /^-?(?:(\d+)\.)?(\d{1,2}):(\d{2}):(\d{2})(?:\.(\d+))?$/.exec(value.trim())
  if (!match) return 0

  const [, days, hours, minutes, seconds] = match
  const total = Number(days ?? 0) * 86400 + Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds)
  return value.trim().startsWith('-') ? -total : total
}

export { secondsToTimeSpan, timeSpanToSeconds }
