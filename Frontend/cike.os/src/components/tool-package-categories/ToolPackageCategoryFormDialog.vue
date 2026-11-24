<script setup lang="ts">
import type { ToolPackageCategoryDto } from '@/api'
import { postApiV1ToolPackageCategories, putApiV1ToolPackageCategoriesById } from '@/api'
import { LoaderCircle } from '@lucide/vue'
import { ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { getErrorMessage } from '@/lib/errors'

const props = defineProps<{
  open: boolean
  /** 传入为编辑，null 为新建 */
  category?: ToolPackageCategoryDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const name = ref('')
const error = ref('')
const saving = ref(false)

watch(
  () => props.open,
  open => {
    if (open) {
      name.value = props.category?.name ?? ''
      error.value = ''
    }
  },
)

async function submit() {
  const trimmed = name.value.trim()
  if (!trimmed) {
    error.value = '请填写分类名称。'
    return
  }

  saving.value = true
  try {
    const result = props.category
      ? await putApiV1ToolPackageCategoriesById({ path: { id: props.category.id ?? '' }, body: { name: trimmed } })
      : await postApiV1ToolPackageCategories({ body: { name: trimmed } })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(props.category ? '分类已更新。' : '分类已创建。')
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
        <DialogTitle>{{ category ? '重命名分类' : '新建工具包分类' }}</DialogTitle>
        <DialogDescription>分类只有名称一个字段，用于归类工具包。</DialogDescription>
      </DialogHeader>
      <form class="grid gap-4" @submit.prevent="submit">
        <div class="grid gap-2">
          <Label for="category-name">名称</Label>
          <Input
            id="category-name"
            v-model="name"
            placeholder="如：生产力"
            :aria-invalid="Boolean(error)"
            autocomplete="off"
          />
          <p v-if="error" class="text-destructive text-sm">{{ error }}</p>
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
