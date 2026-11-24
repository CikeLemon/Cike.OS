<script setup lang="ts">
import type { ToolValue, ToolValueType } from '@/api'
import { Plus, Trash } from '@lucide/vue'
import { ref, watch } from 'vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { TOOL_VALUE_TYPE } from '@/components/tools/tool-form'

/**
 * 键值行：值类型可选字面量或引用已定义参数。
 * 对外以 Record 同步；内部用带稳定 id 的行数组渲染，避免「键」输入时因 :key 变化导致整行重挂载、输入框失焦。
 */
const model = defineModel<Record<string, ToolValue>>({ required: true })

defineProps<{
  /** 已定义的参数编码，供「参数引用」下拉选择 */
  parameterCodes: string[]
  title: string
  error?: string
}>()

interface Row {
  id: number
  key: string
  value: ToolValue
}

let nextId = 1
const rows = ref<Row[]>([])

watch(
  model,
  value => {
    // 只在行集合与外部模型不一致时重建（自身 sync 触发的回灌内容相同，不重建以保住焦点）
    const entries = Object.entries(value)
    const same = entries.length === rows.value.length
      && entries.every(([key, toolValue], index) => rows.value[index]?.key === key && rows.value[index]?.value === toolValue)
    if (!same) {
      rows.value = entries.map(([key, toolValue]) => ({ id: nextId++, key, value: toolValue }))
    }
  },
  { immediate: true },
)

function sync() {
  model.value = Object.fromEntries(rows.value.map(row => [row.key, row.value]))
}

function addRow() {
  let index = rows.value.length + 1
  const keys = new Set(rows.value.map(row => row.key))
  while (keys.has(`key_${index}`)) index++
  rows.value.push({ id: nextId++, key: `key_${index}`, value: { type: TOOL_VALUE_TYPE.Literal, value: '' } })
  sync()
}

function removeRow(id: number) {
  rows.value = rows.value.filter(row => row.id !== id)
  sync()
}

/** 键名改动只在非空白且不重复时同步到模型，输入过程中不触发重建 */
function onKeyInput(row: Row, value: string | number) {
  row.key = String(value)
  if (row.key.trim() && rows.value.filter(r => r.key === row.key).length === 1) {
    sync()
  }
}

function updateValue(row: Row, patch: Partial<ToolValue>) {
  row.value = { ...row.value, ...patch }
  sync()
}

function valueTypeOf(value: ToolValue): ToolValueType {
  return value.type ?? TOOL_VALUE_TYPE.Literal
}
</script>

<template>
  <div class="grid gap-3">
    <div class="flex items-center justify-between">
      <span class="text-sm font-medium">{{ title }}</span>
      <Button type="button" size="sm" variant="outline" @click="addRow">
        <Plus />
        添加
      </Button>
    </div>
    <p v-if="error" class="text-destructive text-sm">{{ error }}</p>

    <div
      v-for="row in rows"
      :key="row.id"
      class="grid gap-2 rounded-md border p-3 sm:grid-cols-[1fr_8rem_1fr_auto]"
    >
      <Input :model-value="row.key" placeholder="键" class="h-8" autocomplete="off" @update:model-value="onKeyInput(row, $event)" />
      <Select
        :model-value="String(valueTypeOf(row.value))"
        @update:model-value="updateValue(row, { type: Number($event) as ToolValueType })"
      >
        <SelectTrigger class="h-8 w-full">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectGroup>
            <SelectItem :value="String(TOOL_VALUE_TYPE.Literal)">字面量</SelectItem>
            <SelectItem :value="String(TOOL_VALUE_TYPE.ParameterReference)">参数引用</SelectItem>
          </SelectGroup>
        </SelectContent>
      </Select>
      <Select
        v-if="valueTypeOf(row.value) === TOOL_VALUE_TYPE.ParameterReference"
        :model-value="row.value.value ?? ''"
        @update:model-value="updateValue(row, { value: String($event) })"
      >
        <SelectTrigger class="h-8 w-full">
          <SelectValue placeholder="选择参数" />
        </SelectTrigger>
        <SelectContent>
          <SelectGroup>
            <SelectItem v-for="code in parameterCodes" :key="code" :value="code">{{ code }}</SelectItem>
          </SelectGroup>
        </SelectContent>
      </Select>
      <Input
        v-else
        :model-value="row.value.value ?? ''"
        placeholder="值"
        class="h-8"
        autocomplete="off"
        @update:model-value="updateValue(row, { value: String($event) })"
      />
      <Button type="button" size="icon-sm" variant="ghost" :aria-label="`删除 ${row.key}`" @click="removeRow(row.id)">
        <Trash />
      </Button>
    </div>
    <p v-if="rows.length === 0" class="text-muted-foreground text-sm">未设置。</p>
  </div>
</template>
