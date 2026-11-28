<script setup lang="ts">
import type { ToolParameterDefinition } from '@/api'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { ConfigurationValues } from '@/components/tool-configurations/tool-configuration-form'

/** 按工具的参数定义生成的表单：Name 标签、Description 提示、Required 星号、Sensitive 密码框 */
defineProps<{
  parameters: Array<ToolParameterDefinition> | null | undefined
  values: ConfigurationValues
  errors?: Record<string, string>
  idPrefix?: string
}>()

const emit = defineEmits<{
  'update:value': [code: string, value: string]
  clearError: [code: string]
}>()

function onInput(code: string, value: string | number) {
  emit('update:value', code, String(value))
  emit('clearError', code)
}
</script>

<template>
  <div class="grid gap-3">
    <p v-if="(parameters ?? []).length === 0" class="text-muted-foreground text-sm">这个工具没有声明参数。</p>
    <div v-for="parameter in parameters" :key="parameter.code ?? ''" class="grid gap-1.5">
      <Label :for="`${idPrefix}-${parameter.code}`">
        {{ parameter.name || parameter.code }}
        <span v-if="parameter.required" class="text-destructive">*</span>
      </Label>
      <Input
        :id="`${idPrefix}-${parameter.code}`"
        :model-value="values[parameter.code ?? ''] ?? ''"
        :type="parameter.sensitive ? 'password' : 'text'"
        :autocomplete="parameter.sensitive ? 'new-password' : 'off'"
        :aria-invalid="Boolean(errors?.[parameter.code ?? ''])"
        @update:model-value="onInput(parameter.code ?? '', $event)"
      />
      <p v-if="errors?.[parameter.code ?? '']" class="text-destructive text-sm">{{ errors[parameter.code ?? ''] }}</p>
      <p v-else-if="parameter.description" class="text-muted-foreground text-xs">{{ parameter.description }}</p>
    </div>
  </div>
</template>
