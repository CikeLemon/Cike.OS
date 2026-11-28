<script setup lang="ts">
import type { TagItemDto } from '@/api'
import { Check, X } from '@lucide/vue'
import { watch } from 'vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { TableCell, TableRow } from '@/components/ui/table'
import TagMultiSelect from '@/components/tags/TagMultiSelect.vue'
import type { ModelRow } from '@/components/model-providers/provider-form'

const props = defineProps<{
  /** 正在编辑的那一条：父级持有的草稿（新增时是空行，编辑时是该行的副本），输入直接写在它上面 */
  row: ModelRow
  /** 编辑已有条目时名称只读（创建后不可变） */
  nameLocked: boolean
  /** 校验错误，按字段名给 */
  errors: Record<string, string>
  tags: TagItemDto[]
  tagsReady: boolean
}>()

const emit = defineEmits<{
  confirm: []
  cancel: []
  /** 用户改了输入：父级据此清掉行上的红字 */
  clear: []
}>()

const row = props.row

// 红字不随输入变化会一直挂着：一有输入就通知父级清掉，下次点确定时重新校验
watch(
  () => [row.name, row.description, row.tagIds.length],
  () => emit('clear'),
)
</script>

<template>
  <!-- align-top：名称 / 描述下面会多一行提示文字，单元格垂直居中会把同行的其它控件顶得错位 -->
  <TableRow>
    <TableCell class="align-top">
      <div class="grid gap-1">
        <Input
          v-model="row.name"
          placeholder="如：glm-5.3"
          :disabled="nameLocked"
          :aria-invalid="Boolean(errors.name)"
          autocomplete="off"
          @keydown.enter.prevent="emit('confirm')"
        />
        <p v-if="errors.name" class="text-destructive text-xs">{{ errors.name }}</p>
        <p v-else-if="nameLocked" class="text-muted-foreground text-xs">名称创建后不可修改</p>
      </div>
    </TableCell>
    <TableCell class="align-top">
      <TagMultiSelect v-model="row.tagIds" :tags="tags" :disabled="!tagsReady" :empty-text="tagsReady ? '暂无可选标签' : '标签列表加载失败'" />
    </TableCell>
    <TableCell class="align-top">
      <div class="grid gap-1">
        <Input v-model="row.description" placeholder="可选" :aria-invalid="Boolean(errors.description)" autocomplete="off" @keydown.enter.prevent="emit('confirm')" />
        <p v-if="errors.description" class="text-destructive text-xs">{{ errors.description }}</p>
      </div>
    </TableCell>
    <TableCell class="text-right align-top">
      <div class="flex justify-end gap-1">
        <Button type="button" size="icon-sm" aria-label="确定" @click="emit('confirm')">
          <Check />
        </Button>
        <Button type="button" size="icon-sm" variant="ghost" aria-label="取消" @click="emit('cancel')">
          <X />
        </Button>
      </div>
    </TableCell>
  </TableRow>
</template>
