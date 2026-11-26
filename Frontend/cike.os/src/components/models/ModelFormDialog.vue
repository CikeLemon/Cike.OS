<script setup lang="ts">
import type { ModelDto, TagDto } from '@/api'
import { postApiV1Models, putApiV1ModelsById } from '@/api'
import { LoaderCircle } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { getErrorMessage } from '@/lib/errors'

const props = defineProps<{
  open: boolean
  /** 平台标签列表，供多选 */
  tags: TagDto[]
  /** 传入为编辑，null 为新建；模型名称创建后不可变，编辑态禁用 */
  model?: ModelDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const form = reactive({
  name: '',
  displayName: '',
  description: '',
  tagIds: [] as string[],
})
const errors = ref<Record<string, string>>({})
const saving = ref(false)

const isEdit = computed(() => Boolean(props.model))

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    const model = props.model
    form.name = model?.name ?? ''
    form.displayName = model?.displayName ?? ''
    form.description = model?.description ?? ''
    form.tagIds = [...(model?.tagIds ?? [])]
  },
)

function toggleTag(id: string, checked: boolean) {
  form.tagIds = checked ? [...form.tagIds, id] : form.tagIds.filter(x => x !== id)
}

/** 模型名称只在创建时必填（创建后不可变）；描述可选 */
function validate(): boolean {
  const next: Record<string, string> = {}
  if (!isEdit.value && !form.name.trim()) next.name = '请填写模型名称。'
  if (!form.displayName.trim()) next.displayName = '请填写显示名称。'
  errors.value = next
  return Object.keys(next).length === 0
}

async function submit() {
  if (!validate()) return

  saving.value = true
  try {
    // 模型名称创建后不可变，不进 UpdateDto
    const editable = {
      displayName: form.displayName.trim(),
      description: form.description.trim(),
      tagIds: form.tagIds,
    }
    const result = props.model
      ? await putApiV1ModelsById({ path: { id: props.model.id ?? '' }, body: editable })
      : await postApiV1Models({ body: { name: form.name.trim(), ...editable } })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(props.model ? '模型已更新。' : '模型已创建。')
    emit('saved')
    emit('update:open', false)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-lg">
      <DialogHeader>
        <DialogTitle>{{ isEdit ? '编辑模型' : '新建模型' }}</DialogTitle>
        <DialogDescription>模型是平台级目录条目，定义市面上的模型本身；它能由哪些提供方提供，在「模型提供方」的模型映射里维护。</DialogDescription>
      </DialogHeader>
      <form class="grid gap-4" @submit.prevent="submit">
        <!-- items-start：一侧出现提示 / 报错时另一侧不跟着拉伸下沉 -->
        <div class="grid gap-4 sm:grid-cols-2 sm:items-start">
          <div class="grid gap-2">
            <Label for="model-name">模型名称</Label>
            <Input id="model-name" v-model="form.name" placeholder="如：glm-5.3" :disabled="isEdit" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
            <p v-if="errors.name" class="text-destructive text-sm">{{ errors.name }}</p>
            <p v-else-if="isEdit" class="text-muted-foreground text-xs">模型名称创建后不可修改。</p>
          </div>
          <div class="grid gap-2">
            <Label for="model-display-name">显示名称</Label>
            <Input id="model-display-name" v-model="form.displayName" placeholder="如：GLM 5.3" :aria-invalid="Boolean(errors.displayName)" autocomplete="off" />
            <p v-if="errors.displayName" class="text-destructive text-sm">{{ errors.displayName }}</p>
          </div>
        </div>
        <div class="grid gap-2">
          <Label for="model-description">描述</Label>
          <Textarea id="model-description" v-model="form.description" placeholder="可选：说明这个模型的定位与能力" class="min-h-24" />
        </div>
        <div class="grid gap-2">
          <Label>标签</Label>
          <p v-if="tags.length === 0" class="text-muted-foreground text-sm">暂无可选标签。</p>
          <div v-else class="flex flex-wrap gap-x-6 gap-y-2">
            <label v-for="tag in tags" :key="tag.id" class="flex items-center gap-2 text-sm">
              <Checkbox :model-value="form.tagIds.includes(tag.id ?? '')" @update:model-value="toggleTag(tag.id ?? '', Boolean($event))" />
              {{ tag.name }}
            </label>
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
  </Dialog>
</template>
