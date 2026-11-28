<script setup lang="ts">
import type { TagDetailDto } from '@/api'
import { postApiV1Tags, putApiV1TagsById } from '@/api'
import { LoaderCircle } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { emptyTagForm, tagFormFromDto, tagFormToCreateBody, tagFormToUpdateBody, validateTagForm, type TagFormState } from '@/components/tags/tag-form'
import { getErrorMessage } from '@/lib/errors'

const props = defineProps<{
  open: boolean
  /** 传入为编辑（改名），null 为新建；键创建后不可变，编辑态禁用 */
  tag?: TagDetailDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const form = reactive<TagFormState>(emptyTagForm())
const errors = ref<Record<string, string>>({})
const saving = ref(false)

const isEdit = computed(() => Boolean(props.tag))

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    Object.assign(form, props.tag ? tagFormFromDto(props.tag) : emptyTagForm())
  },
)

async function submit() {
  errors.value = validateTagForm(form)
  if (Object.keys(errors.value).length > 0) return

  saving.value = true
  try {
    // 键创建后不可变，不进更新体
    const result = props.tag
      ? await putApiV1TagsById({ path: { id: props.tag.id ?? '' }, body: tagFormToUpdateBody(form) })
      : await postApiV1Tags({ body: tagFormToCreateBody(form) })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(props.tag ? '标签已更新。' : '标签已创建。')
    emit('saved')
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
        <DialogTitle>{{ isEdit ? '编辑标签' : '新建标签' }}</DialogTitle>
        <DialogDescription>标签是平台级标注词条，用于给模型等平台目录资源打标；键是机器标识，名称用于显示。</DialogDescription>
      </DialogHeader>
      <form class="grid gap-4" @submit.prevent="submit">
        <div class="grid gap-2">
          <Label for="tag-key">键</Label>
          <Input id="tag-key" v-model="form.key" placeholder="如：vision" class="font-mono" :disabled="isEdit" :aria-invalid="Boolean(errors.key)" autocomplete="off" />
          <p v-if="errors.key" class="text-destructive text-sm">{{ errors.key }}</p>
          <p v-else-if="isEdit" class="text-muted-foreground text-xs">键创建后不可修改。</p>
        </div>
        <div class="grid gap-2">
          <Label for="tag-name">名称</Label>
          <Input id="tag-name" v-model="form.name" placeholder="如：视觉" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
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
