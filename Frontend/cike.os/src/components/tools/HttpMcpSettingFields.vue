<script setup lang="ts">
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import ToolValueRows from '@/components/tools/ToolValueRows.vue'
import type { HttpSettingForm } from '@/components/tools/tool-form'
import type { HttpTransportMode } from '@/api'
import { HTTP_TRANSPORT_MODE_OPTIONS } from '@/lib/mcp-transport'

defineProps<{
  parameterCodes: string[]
  errors: Record<string, string>
}>()

const model = defineModel<HttpSettingForm>({ required: true })
</script>

<template>
  <div class="grid gap-3 rounded-md border p-3 sm:grid-cols-2">
    <div class="grid gap-1.5">
      <Label for="http-name" class="text-xs">服务名</Label>
      <Input id="http-name" v-model="model.name" class="h-8" autocomplete="off" />
    </div>
    <div class="grid gap-1.5">
      <Label for="http-endpoint" class="text-xs">端点 <span class="text-destructive">*</span></Label>
      <Input id="http-endpoint" v-model="model.endpoint" placeholder="https://example.com/mcp" class="h-8" :aria-invalid="Boolean(errors['http.endpoint'])" autocomplete="off" />
    </div>
    <div class="grid gap-1.5">
      <Label class="text-xs">传输模式</Label>
      <Select
        :model-value="String(model.transportMode)"
        @update:model-value="model.transportMode = Number($event) as HttpTransportMode"
      >
        <SelectTrigger class="h-8 w-full">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectGroup>
            <SelectItem v-for="mode in HTTP_TRANSPORT_MODE_OPTIONS" :key="mode.value" :value="String(mode.value)">{{ mode.label }}</SelectItem>
          </SelectGroup>
        </SelectContent>
      </Select>
    </div>
    <div class="grid gap-1.5">
      <Label for="http-credential" class="text-xs">凭据引用</Label>
      <Input id="http-credential" v-model="model.credentialKey" placeholder="可选" class="h-8" autocomplete="off" />
    </div>
    <div class="grid gap-1.5">
      <Label for="http-timeout" class="text-xs">连接超时（秒）</Label>
      <Input id="http-timeout" v-model.number="model.connectionTimeoutSeconds" type="number" min="1" class="h-8" :aria-invalid="Boolean(errors['http.connectionTimeoutSeconds'])" />
    </div>
    <div class="grid gap-1.5">
      <Label for="http-reconnect" class="text-xs">最大重连次数</Label>
      <Input id="http-reconnect" v-model.number="model.maxReconnectionAttempts" type="number" min="0" class="h-8" />
    </div>
    <div class="grid gap-1.5">
      <Label for="http-interval" class="text-xs">重连间隔（秒）</Label>
      <Input id="http-interval" v-model.number="model.defaultReconnectionIntervalSeconds" type="number" min="1" class="h-8" :aria-invalid="Boolean(errors['http.defaultReconnectionIntervalSeconds'])" />
    </div>
    <div class="sm:col-span-2">
      <ToolValueRows v-model="model.additionalHeaders" :parameter-codes="parameterCodes" title="附加请求头" :error="errors['http.additionalHeaders']" />
    </div>
    <p v-if="errors['http.endpoint']" class="text-destructive text-sm sm:col-span-2">{{ errors['http.endpoint'] }}</p>
    <p v-if="errors['http.connectionTimeoutSeconds']" class="text-destructive text-sm sm:col-span-2">{{ errors['http.connectionTimeoutSeconds'] }}</p>
    <p v-if="errors['http.defaultReconnectionIntervalSeconds']" class="text-destructive text-sm sm:col-span-2">{{ errors['http.defaultReconnectionIntervalSeconds'] }}</p>
  </div>
</template>
