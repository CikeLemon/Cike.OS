import type { ToolPackageStatus } from '@/api'

/** 工具包状态：数值与后端 ToolPackageStatus 枚举一致，不引入 enum（erasableSyntaxOnly） */
const TOOL_PACKAGE_STATUS = {
  Draft: 1,
  Published: 2,
  Offline: 3,
} as const satisfies Record<string, ToolPackageStatus>

type ToolPackageStatusValue = ToolPackageStatus

const TOOL_PACKAGE_STATUS_LABELS: Record<ToolPackageStatusValue, string> = {
  [TOOL_PACKAGE_STATUS.Draft]: '草稿',
  [TOOL_PACKAGE_STATUS.Published]: '已发布',
  [TOOL_PACKAGE_STATUS.Offline]: '已下线',
}

const TOOL_PACKAGE_STATUS_OPTIONS = (Object.keys(TOOL_PACKAGE_STATUS) as (keyof typeof TOOL_PACKAGE_STATUS)[]).map(
  key => ({ value: TOOL_PACKAGE_STATUS[key] as ToolPackageStatus, label: TOOL_PACKAGE_STATUS_LABELS[TOOL_PACKAGE_STATUS[key] as ToolPackageStatus] }),
)

/** 状态徽标变体：只用既有语义 token 的变体，不新增色相 */
function getStatusBadgeVariant(status: ToolPackageStatusValue | undefined): 'secondary' | 'outline' | 'default' {
  switch (status) {
    case TOOL_PACKAGE_STATUS.Published:
      return 'default'
    case TOOL_PACKAGE_STATUS.Offline:
      return 'outline'
    default:
      return 'secondary'
  }
}

export {
  TOOL_PACKAGE_STATUS,
  TOOL_PACKAGE_STATUS_LABELS,
  TOOL_PACKAGE_STATUS_OPTIONS,
  getStatusBadgeVariant,
  type ToolPackageStatusValue,
}
