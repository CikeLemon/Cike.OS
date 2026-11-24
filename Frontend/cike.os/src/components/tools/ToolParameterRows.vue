<script setup lang="ts">
import type { ToolParameterDefinition } from '@/api'
import { Plus, Trash } from '@lucide/vue'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

/** 参数定义行编辑：Code / 名称 / 描述 / 必填 / 敏感，可增删 */
const model = defineModel<ToolParameterDefinition[]>({ required: true })

defineProps<{
  error?: string
}>()

function addRow() {
  model.value = [...model.value, { code: '', name: '', description: null, required: false, sensitive: false }]
}

function removeRow(index: number) {
  model.value = model.value.filter((_, i) => i !== index)
}

/** 生成 DTO 的字符串字段可空，输入框只接受非空值 */
function setField(index: number, field: 'code' | 'name' | 'description', value: string | number) {
  const row = model.value[index]
  if (row) row[field] = String(value)
}
</script>

<template>
  <div class="grid gap-3">
    <div class="flex items-center justify-between">
      <Label>参数定义</Label>
      <Button type="button" size="sm" variant="outline" @click="addRow">
        <Plus />
        添加参数
      </Button>
    </div>
    <p v-if="error" class="text-destructive text-sm">{{ error }}</p>
    <p v-if="model.length === 0" class="text-muted-foreground text-sm">尚无参数。设置中的附加请求头 / 环境变量可以引用这里定义的参数。</p>

    <div
      v-for="(row, index) in model"
      :key="index"
      class="grid gap-3 rounded-md border p-3 sm:grid-cols-[1fr_1fr_auto]"
    >
      <div class="grid gap-1.5">
        <Label :for="`param-code-${index}`" class="text-xs">编码</Label>
        <Input :id="`param-code-${index}`" :model-value="row.code ?? ''" placeholder="如：api_key" class="h-8" autocomplete="off" @update:model-value="setField(index, 'code', $event)" />
      </div>
      <div class="grid gap-1.5">
        <Label :for="`param-name-${index}`" class="text-xs">名称</Label>
        <Input :id="`param-name-${index}`" :model-value="row.name ?? ''" placeholder="如：API Key" class="h-8" autocomplete="off" @update:model-value="setField(index, 'name', $event)" />
      </div>
      <div class="flex items-end">
        <Button type="button" size="icon-sm" variant="ghost" :aria-label="`删除参数 ${index + 1}`" @click="removeRow(index)">
          <Trash />
        </Button>
      </div>
      <div class="grid gap-1.5 sm:col-span-2">
        <Label :for="`param-desc-${index}`" class="text-xs">描述</Label>
        <Input :id="`param-desc-${index}`" :model-value="row.description ?? ''" placeholder="可选" class="h-8" @update:model-value="setField(index, 'description', $event)" />
      </div>
      <div class="flex items-center gap-4 sm:col-span-3">
        <label class="flex items-center gap-2 text-sm">
          <Checkbox v-model="row.required" />
          必填
        </label>
        <label class="flex items-center gap-2 text-sm">
          <Checkbox v-model="row.sensitive" />
          敏感
        </label>
      </div>
    </div>
  </div>
</template>
