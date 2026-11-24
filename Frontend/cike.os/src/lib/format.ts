/** ISO 时间 → 本地 `YYYY-MM-DD HH:mm`；空值返回占位符 */
function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—'

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return '—'

  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/** 按字符数截断并追加省略号；空值返回占位符 */
function truncate(value: string | null | undefined, max: number): string {
  if (!value) return '—'
  return value.length > max ? `${value.slice(0, max)}…` : value
}

export { formatDateTime, truncate }
