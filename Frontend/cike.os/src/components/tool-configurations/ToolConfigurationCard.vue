<script setup lang="ts">
import type { UserToolConfigurationItemDto } from '@/api'
import { Eye, EyeOff, Pencil, Trash, TriangleAlert } from '@lucide/vue'
import { ref } from 'vue'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { maskValue, missingRequiredParameters } from '@/components/tool-configurations/tool-configuration-form'
import { TOOL_TYPE_LABELS } from '@/lib/tool-type'

/** 工具参数页的一张卡片：当前用户对一个工具的配置（显示态）。敏感值默认遮罩，点眼睛才显示 */
defineProps<{
  item: UserToolConfigurationItemDto
  /** 详情请求进行中：编辑按钮禁用，防重入 */
  editBusy?: boolean
}>()

const emit = defineEmits<{
  edit: [item: UserToolConfigurationItemDto]
  remove: [item: UserToolConfigurationItemDto]
}>()

// 已点开明文的参数 code
const revealed = ref(new Set<string>())

function isRevealed(code: string): boolean {
  return revealed.value.has(code)
}

function toggleRevealed(code: string) {
  const next = new Set(revealed.value)
  if (next.has(code)) next.delete(code)
  else next.add(code)
  revealed.value = next
}
</script>

<template>
  <Card>
    <CardHeader class="flex flex-row items-start justify-between gap-2">
      <div class="grid gap-1">
        <CardTitle class="text-base">{{ item.toolName }}</CardTitle>
        <p class="text-muted-foreground text-xs">{{ item.toolPackageCategoryName }} / {{ item.toolPackageName }}</p>
      </div>
      <Badge variant="secondary">{{ item.toolType ? TOOL_TYPE_LABELS[item.toolType] : '' }}</Badge>
    </CardHeader>
    <CardContent class="grid gap-1.5 text-sm">
      <p v-if="missingRequiredParameters(item.parameters, item.values).length > 0" class="text-destructive flex items-center gap-1">
        <TriangleAlert class="size-4" />
        缺必填
      </p>
      <p v-if="(item.parameters ?? []).length === 0" class="text-muted-foreground">这个工具没有声明参数。</p>
      <div v-for="parameter in item.parameters" :key="parameter.code ?? ''" class="grid grid-cols-[8rem_1fr_auto] items-center gap-2">
        <span class="text-muted-foreground truncate">{{ parameter.name || parameter.code }}</span>
        <span class="truncate font-mono text-xs">
          {{ parameter.sensitive && !isRevealed(parameter.code ?? '') ? maskValue() : (item.values?.[parameter.code ?? ''] ?? '—') }}
        </span>
        <Button
          v-if="parameter.sensitive && item.values?.[parameter.code ?? '']"
          type="button"
          size="icon-sm"
          variant="ghost"
          :aria-label="isRevealed(parameter.code ?? '') ? '隐藏' : '显示'"
          @click="toggleRevealed(parameter.code ?? '')"
        >
          <EyeOff v-if="isRevealed(parameter.code ?? '')" />
          <Eye v-else />
        </Button>
      </div>
    </CardContent>
    <CardFooter class="justify-end gap-1">
      <Button size="sm" variant="ghost" :disabled="editBusy" @click="emit('edit', item)">
        <Pencil />
        编辑
      </Button>
      <Button size="sm" variant="ghost" @click="emit('remove', item)">
        <Trash />
        删除
      </Button>
    </CardFooter>
  </Card>
</template>
