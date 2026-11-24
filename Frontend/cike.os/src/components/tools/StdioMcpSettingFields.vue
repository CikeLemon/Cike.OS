<script setup lang="ts">
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import ToolValueRows from '@/components/tools/ToolValueRows.vue'
import type { StdioSettingForm } from '@/components/tools/tool-form'

defineProps<{
  parameterCodes: string[]
  errors: Record<string, string>
}>()

const model = defineModel<StdioSettingForm>({ required: true })
</script>

<template>
  <div class="grid gap-3 rounded-md border p-3 sm:grid-cols-2">
    <div class="grid gap-1.5">
      <Label for="stdio-name" class="text-xs">服务名</Label>
      <Input id="stdio-name" v-model="model.name" class="h-8" autocomplete="off" />
    </div>
    <div class="grid gap-1.5">
      <Label for="stdio-command" class="text-xs">命令 <span class="text-destructive">*</span></Label>
      <Input id="stdio-command" v-model="model.command" placeholder="如：npx" class="h-8" :aria-invalid="Boolean(errors['stdio.command'])" autocomplete="off" />
    </div>
    <div class="grid gap-1.5 sm:col-span-2">
      <Label for="stdio-args" class="text-xs">启动参数（每行一条）</Label>
      <Textarea id="stdio-args" v-model="model.argumentsText" placeholder="-y&#10;@modelcontextprotocol/server-filesystem" class="min-h-20 font-mono text-sm" />
    </div>
    <div class="grid gap-1.5 sm:col-span-2">
      <Label for="stdio-workdir" class="text-xs">工作目录</Label>
      <Input id="stdio-workdir" v-model="model.workingDirectory" placeholder="可选" class="h-8" autocomplete="off" />
    </div>
    <div class="sm:col-span-2">
      <ToolValueRows v-model="model.environmentVariables" :parameter-codes="parameterCodes" title="环境变量" :error="errors['stdio.environmentVariables']" />
    </div>
    <p v-if="errors['stdio.command']" class="text-destructive text-sm sm:col-span-2">{{ errors['stdio.command'] }}</p>
  </div>
</template>
