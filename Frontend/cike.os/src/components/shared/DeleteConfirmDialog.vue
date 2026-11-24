<script setup lang="ts">
import { LoaderCircle } from '@lucide/vue'
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

// 「删除」不用 AlertDialogAction：它会在 click 时先关闭对话框，父级保存的目标会先被清空；
// 这里改为普通按钮，关闭时机完全交给父级（请求成功后再关）。
withDefaults(
  defineProps<{
    open: boolean
    title?: string
    description: string
    loading?: boolean
  }>(),
  { title: '确认删除', loading: false },
)

const emit = defineEmits<{
  'update:open': [value: boolean]
  confirm: []
}>()
</script>

<template>
  <AlertDialog :open="open" @update:open="emit('update:open', $event)">
    <AlertDialogContent>
      <AlertDialogHeader>
        <AlertDialogTitle>{{ title }}</AlertDialogTitle>
        <AlertDialogDescription>{{ description }}</AlertDialogDescription>
      </AlertDialogHeader>
      <AlertDialogFooter>
        <AlertDialogCancel :disabled="loading">取消</AlertDialogCancel>
        <Button variant="destructive" :disabled="loading" @click="emit('confirm')">
          <LoaderCircle v-if="loading" class="animate-spin" />
          删除
        </Button>
      </AlertDialogFooter>
    </AlertDialogContent>
  </AlertDialog>
</template>
