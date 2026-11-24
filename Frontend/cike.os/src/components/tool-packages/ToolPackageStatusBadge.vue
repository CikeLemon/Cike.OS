<script setup lang="ts">
import type { ToolPackageStatus } from '@/api'
import { Badge } from '@/components/ui/badge'
import { TOOL_PACKAGE_STATUS_LABELS, getStatusBadgeVariant } from '@/lib/tool-status'

const props = defineProps<{
  status?: ToolPackageStatus | null
}>()

function labelOf(status: ToolPackageStatus | null | undefined): string {
  return (status != null && TOOL_PACKAGE_STATUS_LABELS[status]) || '未知'
}
</script>

<template>
  <Badge :variant="getStatusBadgeVariant(props.status ?? undefined)">
    {{ labelOf(props.status) }}
  </Badge>
</template>
