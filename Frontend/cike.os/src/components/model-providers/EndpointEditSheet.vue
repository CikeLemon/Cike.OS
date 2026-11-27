<script setup lang="ts">
import type { ModelDto, ModelProviderDto, ModelProviderEndpointDto } from '@/api'
import { getApiV1ModelsPagedList, putApiV1ModelProvidersById } from '@/api'
import { LoaderCircle, Plus, Trash } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import {
  emptyEndpointForm,
  emptyMappingRow,
  endpointFormFromDto,
  endpointFormToSaveDto,
  protocolFieldsHaveInput,
  resetProtocolFields,
  validateEndpointForm,
  type EndpointFormState,
} from '@/components/model-providers/endpoint-form'
import { providerUpdateBody, upsertEndpoint } from '@/components/model-providers/provider-form'
import { getErrorMessage } from '@/lib/errors'
import { PROTOCOL_LABELS, PROTOCOL_OPTIONS, isOpenAiProtocol, type ProtocolValue } from '@/lib/model-provider-protocol'

const props = defineProps<{
  open: boolean
  /** 所属模型提供方（含名下全部接入点）：接入点随提供方整体提交 */
  provider: ModelProviderDto | null
  /** 传入为编辑，null 为新建；编辑态协议只读（创建后不可变） */
  endpoint?: ModelProviderEndpointDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const form = reactive<EndpointFormState>(emptyEndpointForm())
const errors = ref<Record<string, string>>({})
const saving = ref(false)

/** 平台模型目录，供映射行下拉 */
const models = ref<ModelDto[]>([])

const isEdit = computed(() => Boolean(props.endpoint))

// 新建态切换协议：协议专属字段已有输入时先二次确认，确认后清空（名称、地址、密钥、映射保留）
const pendingProtocol = ref<ProtocolValue | null>(null)

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    pendingProtocol.value = null
    Object.assign(form, props.endpoint ? endpointFormFromDto(props.endpoint) : emptyEndpointForm())
    loadModels()
  },
)

async function loadModels() {
  const result = await getApiV1ModelsPagedList({ query: { Page: 1, PageSize: 200, Sorting: 'DisplayName asc' } })
  if (result.error) {
    toast.error(getErrorMessage(result))
    return
  }
  models.value = result.data?.items ?? []
}

function mappingError(rowKey: number, field: 'modelId' | 'priority' | 'providerModelName') {
  return errors.value[`mappings.${rowKey}.${field}`]
}

const protocolValue = computed({
  get: () => String(form.protocol),
  set: (value: string) => {
    const next = Number(value) as ProtocolValue
    if (isEdit.value || next === form.protocol) return

    if (protocolFieldsHaveInput(form)) {
      pendingProtocol.value = next
      return
    }

    applyProtocol(next)
  },
})

function applyProtocol(protocol: ProtocolValue) {
  form.protocol = protocol
  resetProtocolFields(form)
  errors.value = {}
}

async function submit() {
  errors.value = validateEndpointForm(form)
  if (!props.provider || Object.keys(errors.value).length > 0) return

  saving.value = true
  try {
    const provider = props.provider
    // 接入点随提供方整体提交：没改的接入点原样带上，只替换 / 追加这一条；新建的协议随表单提交，已有的协议不可变
    const endpoint = endpointFormToSaveDto(form, props.endpoint?.id)
    const result = await putApiV1ModelProvidersById({
      path: { id: provider.id ?? '' },
      body: providerUpdateBody(provider.name, upsertEndpoint(provider.endpoints, endpoint)),
    })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(isEdit.value ? '接入点已更新。' : '接入点已创建。')
    emit('saved')
    emit('update:open', false)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Sheet :open="open" @update:open="emit('update:open', $event)">
    <SheetContent class="flex w-full flex-col gap-0 p-0 sm:max-w-2xl">
      <SheetHeader class="border-b px-6 py-4">
        <SheetTitle>{{ isEdit ? '编辑接入点' : '新建接入点' }}</SheetTitle>
        <SheetDescription>一个接入点 = 一种协议 + 一个接入地址 + 一把密钥，以及它接入的模型。</SheetDescription>
      </SheetHeader>

      <form id="endpoint-form" class="grid flex-1 gap-6 overflow-y-auto px-6 py-4" @submit.prevent="submit">
        <!-- 连接 -->
        <section class="grid gap-3">
          <h3 class="text-sm font-semibold">连接</h3>
          <div class="grid gap-3 sm:grid-cols-2 sm:items-start">
            <div class="grid gap-1.5">
              <Label for="endpoint-name">名称 <span class="text-destructive">*</span></Label>
              <Input id="endpoint-name" v-model="form.name" placeholder="如：官方、中转" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
              <p v-if="errors.name" class="text-destructive text-sm">{{ errors.name }}</p>
            </div>
            <div class="grid gap-1.5">
              <Label for="endpoint-protocol">协议 <span class="text-destructive">*</span></Label>
              <!-- 协议创建后不可变：编辑态用纯文本展示，不做成输入框的样子 -->
              <p v-if="isEdit" id="endpoint-protocol" class="flex min-h-9 items-center text-sm">{{ PROTOCOL_LABELS[form.protocol] }}</p>
              <Select v-else v-model="protocolValue" :disabled="saving">
                <SelectTrigger id="endpoint-protocol" class="w-full">
                  <SelectValue placeholder="选择协议" />
                </SelectTrigger>
                <SelectContent>
                  <SelectGroup>
                    <SelectItem v-for="option in PROTOCOL_OPTIONS" :key="option.value" :value="String(option.value)">{{ option.label }}</SelectItem>
                  </SelectGroup>
                </SelectContent>
              </Select>
              <p class="text-muted-foreground text-xs">{{ isEdit ? '协议创建后不可修改，要换协议请删除后重建。' : '决定用哪种接口约定调用模型服务。' }}</p>
            </div>
          </div>
          <div class="grid gap-3 sm:grid-cols-2 sm:items-start">
            <div class="grid gap-1.5">
              <Label for="endpoint-base-url">接入地址 <span class="text-destructive">*</span></Label>
              <Input id="endpoint-base-url" v-model="form.baseUrl" placeholder="https://…" :aria-invalid="Boolean(errors.baseUrl)" autocomplete="off" />
              <p v-if="errors.baseUrl" class="text-destructive text-sm">{{ errors.baseUrl }}</p>
            </div>
            <div class="grid gap-1.5">
              <Label for="endpoint-api-key">API 密钥 <span class="text-destructive">*</span></Label>
              <Input id="endpoint-api-key" v-model="form.apiKey" type="password" :aria-invalid="Boolean(errors.apiKey)" autocomplete="off" />
              <p v-if="errors.apiKey" class="text-destructive text-sm">{{ errors.apiKey }}</p>
            </div>
          </div>
          <!-- 协议专属字段：均为可选；两种 OpenAI 协议共用组织标识 -->
          <div v-if="isOpenAiProtocol(form.protocol)" class="grid gap-1.5 sm:max-w-[calc(50%-0.375rem)]">
            <Label for="endpoint-organization">组织标识</Label>
            <Input id="endpoint-organization" v-model="form.organization" placeholder="可选，请求头 OpenAI-Organization" autocomplete="off" />
          </div>
          <div v-else class="grid gap-1.5 sm:max-w-[calc(50%-0.375rem)]">
            <Label for="endpoint-version">API 版本</Label>
            <Input id="endpoint-version" v-model="form.version" placeholder="可选，请求头 anthropic-version" autocomplete="off" />
          </div>
        </section>

        <!-- 模型映射：随接入点整体保存，没有单独的增删接口 -->
        <section class="grid gap-3">
          <div class="flex items-center justify-between gap-2">
            <h3 class="text-sm font-semibold">模型映射</h3>
            <Button type="button" size="sm" variant="outline" :disabled="saving" @click="form.mappings.push(emptyMappingRow())">
              <Plus />
              添加模型
            </Button>
          </div>
          <p class="text-muted-foreground text-xs">这个接入点能提供哪些模型。同一个模型被多个接入点接入时，分数高的先用。</p>

          <!-- 行 key 用稳定的 rowKey：删掉中间一行后，其余行的错误提示不串位 -->
          <div v-for="(row, index) in form.mappings" :key="row.rowKey" class="grid gap-2 sm:grid-cols-[minmax(0,1fr)_6rem_minmax(0,1fr)_auto] sm:items-start">
            <div class="grid gap-1">
              <Select v-model="row.modelId" :disabled="saving">
                <SelectTrigger class="w-full" :aria-invalid="Boolean(mappingError(row.rowKey, 'modelId'))">
                  <SelectValue placeholder="选择模型" />
                </SelectTrigger>
                <SelectContent>
                  <SelectGroup>
                    <SelectItem v-for="model in models" :key="model.id" :value="model.id ?? ''">{{ model.displayName }}</SelectItem>
                  </SelectGroup>
                </SelectContent>
              </Select>
              <p v-if="mappingError(row.rowKey, 'modelId')" class="text-destructive text-sm">{{ mappingError(row.rowKey, 'modelId') }}</p>
            </div>
            <div class="grid gap-1">
              <Input v-model="row.priority" type="number" step="1" placeholder="分数" aria-label="分数" :aria-invalid="Boolean(mappingError(row.rowKey, 'priority'))" />
              <p v-if="mappingError(row.rowKey, 'priority')" class="text-destructive text-sm">{{ mappingError(row.rowKey, 'priority') }}</p>
            </div>
            <div class="grid gap-1">
              <Input v-model="row.providerModelName" placeholder="提供商侧模型名，留空则用模型名称" aria-label="提供商侧模型名" :aria-invalid="Boolean(mappingError(row.rowKey, 'providerModelName'))" autocomplete="off" />
              <p v-if="mappingError(row.rowKey, 'providerModelName')" class="text-destructive text-sm">{{ mappingError(row.rowKey, 'providerModelName') }}</p>
            </div>
            <Button type="button" size="icon-sm" variant="ghost" aria-label="移除映射" :disabled="saving" @click="form.mappings.splice(index, 1)">
              <Trash />
            </Button>
          </div>
        </section>
      </form>

      <SheetFooter class="border-t px-6 py-4">
        <Button type="button" variant="outline" :disabled="saving" @click="emit('update:open', false)">取消</Button>
        <Button type="submit" form="endpoint-form" :disabled="saving">
          <LoaderCircle v-if="saving" class="animate-spin" />
          保存
        </Button>
      </SheetFooter>
    </SheetContent>

    <AlertDialog :open="pendingProtocol !== null" @update:open="pendingProtocol = null">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>切换协议</AlertDialogTitle>
          <AlertDialogDescription>协议专属字段已填写，切换协议将清空它们（名称、接入地址、密钥与模型映射保留）。是否继续？</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel @click="pendingProtocol = null">取消</AlertDialogCancel>
          <!-- 不用 AlertDialogAction：它会先关闭对话框再触发 click，导致待切换的协议被提前清空 -->
          <Button @click="applyProtocol(pendingProtocol ?? form.protocol); pendingProtocol = null">继续切换</Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </Sheet>
</template>
