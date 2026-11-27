<script setup lang="ts">
import type { ModelProviderDto } from '@/api'
import { postApiV1ModelProviders, putApiV1ModelProvidersById } from '@/api'
import { LoaderCircle } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { emptyProviderForm, providerFormFromDto, providerFormToBody, providerUpdateBody, validateProviderForm, type ProviderFormState } from '@/components/model-providers/provider-form'
import { getErrorMessage } from '@/lib/errors'

const props = defineProps<{
  open: boolean
  /** 传入为编辑（改名），null 为新建 */
  provider?: ModelProviderDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  /** 新建成功时带上新提供方 id，供列表页跳转详情；编辑时为空串 */
  saved: [id: string]
}>()

const form = reactive<ProviderFormState>(emptyProviderForm())
const errors = ref<Record<string, string>>({})
const saving = ref(false)

const isEdit = computed(() => Boolean(props.provider))

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    Object.assign(form, props.provider ? providerFormFromDto(props.provider) : emptyProviderForm())
  },
)

async function submit() {
  errors.value = validateProviderForm(form)
  if (Object.keys(errors.value).length > 0) return

  saving.value = true
  try {
    // 改名只提交名称：提供方只管自己，接入点有它们自己的接口
    const result = props.provider
      ? await putApiV1ModelProvidersById({
          path: { id: props.provider.id ?? '' },
          body: providerUpdateBody(form.name),
        })
      : await postApiV1ModelProviders({ body: providerFormToBody(form) })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(isEdit.value ? '模型提供方已更新。' : '模型提供方已创建。')
    // 新建返回新提供方 id（后端 Ok<long>）：生成层把这个响应推成 {}，运行时实际是字符串
    emit('saved', props.provider ? '' : typeof result.data === 'string' ? result.data : '')
    emit('update:open', false)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>{{ isEdit ? '编辑模型提供方' : '新建模型提供方' }}</DialogTitle>
        <DialogDescription>{{ isEdit ? '模型提供方本身只有名称；接入地址、密钥与模型映射都在它的接入点上。' : '先建提供方，再到详情页里添加接入点（协议、地址、密钥与模型映射）。' }}</DialogDescription>
      </DialogHeader>
      <form class="grid gap-4" @submit.prevent="submit">
        <div class="grid gap-2">
          <Label for="provider-name">名称</Label>
          <Input id="provider-name" v-model="form.name" placeholder="如：智谱" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
          <p v-if="errors.name" class="text-destructive text-sm">{{ errors.name }}</p>
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
  </Dialog>
</template>
