<script setup lang="ts">
import type { AgentToolPackageOptionItemDto } from '@/api'
import { Search } from '@lucide/vue'
import { computed, ref } from 'vue'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { TOOL_TYPE_LABELS, type ToolTypeValue } from '@/lib/tool-type'
import { selectWholePackage, toggleTool, unselectToolUnderWholePackage, unselectWholePackage } from '@/components/agents/agent-form'

const props = defineProps<{
  /** 工具包 → 工具 的选择列表（每个工具包带它下面的工具） */
  options: AgentToolPackageOptionItemDto[]
  /** 整包勾选的工具包 Id */
  wholePackages: string[]
  /** 单选的工具 Id */
  toolIds: string[]
  disabled?: boolean
}>()

const emit = defineEmits<{
  'update:wholePackages': [value: string[]]
  'update:toolIds': [value: string[]]
}>()

const keyword = ref('')

/** 搜索只过滤工具行；工具包名命中时显示它全部的工具；空包跟随隐藏 */
const visible = computed(() => {
  const text = keyword.value.trim().toLowerCase()
  return (props.options ?? [])
    .map(pkg => {
      const tools = pkg.tools ?? []
      const packageHit = Boolean(text) && (pkg.name ?? '').toLowerCase().includes(text)
      return { ...pkg, tools: packageHit || !text ? tools : tools.filter(tool => (tool.name ?? '').toLowerCase().includes(text)) }
    })
    .filter(pkg => pkg.tools.length > 0)
})

const selectedCount = computed(() => props.wholePackages.length + props.toolIds.length)

function isWhole(toolPackageId: string): boolean {
  return props.wholePackages.includes(toolPackageId)
}

function isToolSelected(toolId: string): boolean {
  return props.toolIds.includes(toolId)
}

function typeLabel(type: ToolTypeValue | undefined): string {
  return type ? TOOL_TYPE_LABELS[type] : ''
}

function onPackageChange(toolPackageId: string, toolIds: string[], value: boolean | 'indeterminate') {
  const next = { wholePackages: [...props.wholePackages], toolIds: [...props.toolIds] }
  if (value === true) selectWholePackage(next, toolPackageId, toolIds)
  else unselectWholePackage(next, toolPackageId)
  emit('update:wholePackages', next.wholePackages)
  emit('update:toolIds', next.toolIds)
}

function onToolChange(toolPackageId: string, toolIds: string[], toolId: string, value: boolean | 'indeterminate') {
  const next = { wholePackages: [...props.wholePackages], toolIds: [...props.toolIds] }
  // 整包状态下取消其中一个：整包自动取消，包内其余工具转为单选保留
  if (isWhole(toolPackageId) && value !== true) unselectToolUnderWholePackage(next, toolPackageId, toolIds, toolId)
  else toggleTool(next, toolId, value === true)
  emit('update:wholePackages', next.wholePackages)
  emit('update:toolIds', next.toolIds)
}
</script>

<template>
  <div class="grid gap-3">
    <div class="relative w-64">
      <Search class="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
      <Input v-model="keyword" placeholder="搜索工具" class="h-9 pl-8" autocomplete="off" />
    </div>
    <p class="text-muted-foreground text-xs">勾「整包」= 包内全部工具（含之后新增的）都生效，取消其中一个即改为只绑定剩下的；勾单个工具 = 只用选中的。已选 {{ selectedCount }} 项。</p>

    <p v-if="visible.length === 0" class="text-muted-foreground rounded-lg border border-dashed p-6 text-center text-sm">没有可选的工具。</p>

    <div v-for="pkg in visible" :key="pkg.toolPackageId" class="rounded-lg border px-4 py-2.5">
      <label class="flex cursor-pointer items-center gap-2 text-sm">
        <Checkbox
          :model-value="isWhole(pkg.toolPackageId ?? '')"
          :disabled="disabled"
          @update:model-value="onPackageChange(pkg.toolPackageId ?? '', (pkg.tools ?? []).map(t => t.toolId ?? ''), $event)"
        />
        <span class="font-semibold">{{ pkg.name }}</span>
        <span class="text-muted-foreground text-xs">（整包）</span>
      </label>
      <div class="mt-2 grid gap-1.5 pl-6">
        <label
          v-for="tool in pkg.tools"
          :key="tool.toolId"
          class="flex cursor-pointer items-center gap-2 text-sm"
        >
          <Checkbox
            :model-value="isWhole(pkg.toolPackageId ?? '') || isToolSelected(tool.toolId ?? '')"
            :disabled="disabled"
            @update:model-value="onToolChange(pkg.toolPackageId ?? '', (pkg.tools ?? []).map(t => t.toolId ?? ''), tool.toolId ?? '', $event)"
          />
          <span>{{ tool.name }}</span>
          <Badge v-if="tool.type" variant="secondary">{{ typeLabel(tool.type) }}</Badge>
        </label>
      </div>
    </div>
  </div>
</template>
