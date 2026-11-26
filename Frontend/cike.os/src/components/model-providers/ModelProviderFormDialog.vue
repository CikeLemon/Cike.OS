<script setup lang="ts">
import type { ModelDto, ModelProviderDto } from '@/api'
import { getApiV1ModelsPagedList, postApiV1ModelProviders, putApiV1ModelProvidersById } from '@/api'
import { LoaderCircle, Plus, Trash } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { AlertDialog, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { emptyMappingRow, emptyProviderForm, providerFormFromDto, providerFormToBody, resetSettingFields, settingHasInput, validateProviderForm, type ProviderFormState } from '@/components/model-providers/provider-form'
import { getErrorMessage } from '@/lib/errors'
import { MODEL_PROVIDER_TYPE, MODEL_PROVIDER_TYPE_OPTIONS, type ProviderTypeValue } from '@/lib/model-provider-type'

const props = defineProps<{
  open: boolean
  /** 传入为编辑，null 为新建；编辑态类型只读（创建后不可变） */
  provider?: ModelProviderDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const form = reactive<ProviderFormState>(emptyProviderForm())
const errors = ref<Record<string, string>>({})
const saving = ref(false)

/** 平台模型目录，供映射行下拉 */
const models = ref<ModelDto[]>([])

const isEdit = computed(() => Boolean(props.provider))

// 切换类型：设置区已有输入时先二次确认，确认后清空设置区（名称与映射保留）
const pendingType = ref<ProviderTypeValue | null>(null)
const switchConfirmOpen = computed({
  get: () => pendingType.value !== null,
  set: value => {
    if (!value) pendingType.value = null
  },
})

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    pendingType.value = null
    Object.assign(form, props.provider ? providerFormFromDto(props.provider) : emptyProviderForm())
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

function mappingError(index: number, field: 'modelId' | 'priority' | 'providerModelName') {
  return errors.value[`mappings.${index}.${field}`]
}

function onTypeChange(value: unknown) {
  const next = Number(value) as ProviderTypeValue
  if (next === form.type) return

  if (settingHasInput(form)) {
    pendingType.value = next
    return
  }

  applyType(next)
}

function applyType(type: ProviderTypeValue) {
  form.type = type
  resetSettingFields(form)
  errors.value = {}
}

async function submit() {
  errors.value = validateProviderForm(form)
  if (Object.keys(errors.value).length > 0) return

  saving.value = true
  try {
    const body = providerFormToBody(form)
    const result = props.provider
      ? await putApiV1ModelProvidersById({ path: { id: props.provider.id ?? '' }, body })
      : await postApiV1ModelProviders({ body })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(isEdit.value ? '模型提供方已更新。' : '模型提供方已创建。')
    emit('saved')
    emit('update:open', false)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
      <DialogHeader>
        <DialogTitle>{{ isEdit ? '编辑模型提供方' : '新建模型提供方' }}</DialogTitle>
        <DialogDescription>提供方持有接入地址与密钥，并记录它能提供哪些模型；类型创建后不可修改，要换类型请删除后重建。</DialogDescription>
      </DialogHeader>
      <form class="grid gap-4" @submit.prevent="submit">
        <div class="grid gap-2">
          <Label for="provider-name">名称</Label>
          <Input id="provider-name" v-model="form.name" placeholder="如：智谱官方" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
          <p v-if="errors.name" class="text-destructive text-sm">{{ errors.name }}</p>
        </div>

        <div class="grid gap-2">
          <Label>类型</Label>
          <RadioGroup :model-value="String(form.type)" :disabled="isEdit || saving" @update:model-value="onTypeChange">
            <label v-for="option in MODEL_PROVIDER_TYPE_OPTIONS" :key="option.value" class="flex items-center gap-2 text-sm" :class="{ 'text-muted-foreground': isEdit }">
              <RadioGroupItem :value="String(option.value)" />
              {{ option.label }}
            </label>
          </RadioGroup>
          <p v-if="isEdit" class="text-muted-foreground text-xs">类型创建后不可修改。</p>
        </div>

        <div class="grid gap-4 sm:grid-cols-2 sm:items-start">
          <div class="grid gap-2">
            <Label for="provider-base-url">接入地址</Label>
            <Input id="provider-base-url" v-model="form.baseUrl" placeholder="https://…" :aria-invalid="Boolean(errors.baseUrl)" autocomplete="off" />
            <p v-if="errors.baseUrl" class="text-destructive text-sm">{{ errors.baseUrl }}</p>
          </div>
          <div class="grid gap-2">
            <Label for="provider-api-key">API 密钥</Label>
            <Input id="provider-api-key" v-model="form.apiKey" type="password" :aria-invalid="Boolean(errors.apiKey)" autocomplete="off" />
            <p v-if="errors.apiKey" class="text-destructive text-sm">{{ errors.apiKey }}</p>
          </div>
          <div v-if="form.type === MODEL_PROVIDER_TYPE.OpenAI" class="grid gap-2">
            <Label for="provider-organization">组织标识</Label>
            <Input id="provider-organization" v-model="form.organization" placeholder="如：org-xxxx" :aria-invalid="Boolean(errors.organization)" autocomplete="off" />
            <p v-if="errors.organization" class="text-destructive text-sm">{{ errors.organization }}</p>
          </div>
          <div v-if="form.type === MODEL_PROVIDER_TYPE.Anthropic" class="grid gap-2">
            <Label for="provider-version">API 版本</Label>
            <Input id="provider-version" v-model="form.version" placeholder="如：2023-06-01" :aria-invalid="Boolean(errors.version)" autocomplete="off" />
            <p v-if="errors.version" class="text-destructive text-sm">{{ errors.version }}</p>
          </div>
        </div>

        <!-- 模型映射：随提供方整体保存，没有单独的增删接口 -->
        <div class="grid gap-2">
          <div class="flex items-center justify-between gap-2">
            <Label>模型映射</Label>
            <Button type="button" size="sm" variant="outline" :disabled="saving" @click="form.mappings.push(emptyMappingRow())">
              <Plus />
              添加映射
            </Button>
          </div>
          <p class="text-muted-foreground text-xs">这个提供方能提供哪些模型。同一个模型被多个提供方接入时，分数高的先用。</p>

          <div v-for="(row, index) in form.mappings" :key="index" class="grid gap-2 sm:grid-cols-[minmax(0,1fr)_6rem_minmax(0,1fr)_auto] sm:items-start">
            <div class="grid gap-1">
              <Select v-model="row.modelId" :disabled="saving">
                <SelectTrigger class="w-full" :aria-invalid="Boolean(mappingError(index, 'modelId'))">
                  <SelectValue placeholder="选择模型" />
                </SelectTrigger>
                <SelectContent>
                  <SelectGroup>
                    <SelectItem v-for="model in models" :key="model.id" :value="model.id ?? ''">{{ model.displayName }}</SelectItem>
                  </SelectGroup>
                </SelectContent>
              </Select>
              <p v-if="mappingError(index, 'modelId')" class="text-destructive text-sm">{{ mappingError(index, 'modelId') }}</p>
            </div>
            <div class="grid gap-1">
              <Input v-model="row.priority" type="number" step="1" placeholder="分数" :aria-invalid="Boolean(mappingError(index, 'priority'))" />
              <p v-if="mappingError(index, 'priority')" class="text-destructive text-sm">{{ mappingError(index, 'priority') }}</p>
            </div>
            <div class="grid gap-1">
              <Input v-model="row.providerModelName" placeholder="提供商侧模型名，留空则用模型名称" :aria-invalid="Boolean(mappingError(index, 'providerModelName'))" autocomplete="off" />
              <p v-if="mappingError(index, 'providerModelName')" class="text-destructive text-sm">{{ mappingError(index, 'providerModelName') }}</p>
            </div>
            <Button type="button" size="icon-sm" variant="ghost" aria-label="移除映射" :disabled="saving" @click="form.mappings.splice(index, 1)">
              <Trash />
            </Button>
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" :disabled="saving" @click="emit('update:open', false)">取消</Button>
          <Button type="submit" :disabled="saving">
            <LoaderCircle v-if="saving" class="animate-spin" />
            保存
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>

    <AlertDialog v-model:open="switchConfirmOpen">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>切换提供商类型？</AlertDialogTitle>
          <AlertDialogDescription>设置区已填写的内容会被清空（名称与模型映射保留）。</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>取消</AlertDialogCancel>
          <Button @click="applyType(pendingType ?? form.type); pendingType = null">确认切换</Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </Dialog>
</template>
