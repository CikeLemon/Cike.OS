<script setup lang="ts">
import { postApiV1SkillPackagesUpload } from '@/api'
import { FileArchive, LoaderCircle, Upload } from '@lucide/vue'
import { ref } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { getErrorMessage } from '@/lib/errors'
import { type SkillSettingForm } from '@/components/tools/tool-form'

/** 技能区：选择 zip 即上传，成功后持有目录路径；未上传时由父级禁用保存 */
const model = defineModel<SkillSettingForm>({ required: true })

defineProps<{ error?: string }>()

const fileInput = ref<HTMLInputElement | null>(null)
const uploading = ref(false)

function pickFile() {
  fileInput.value?.click()
}

async function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return

  uploading.value = true
  try {
    const result = await postApiV1SkillPackagesUpload({ body: { file } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }
    model.value = { directory: result.data?.directory ?? '', fileName: file.name }
    toast.success('技能包已上传。')
  } finally {
    uploading.value = false
  }
}
</script>

<template>
  <div class="grid gap-3 rounded-md border p-3">
    <div class="flex items-center justify-between gap-3">
      <div class="grid gap-1">
        <span class="text-sm font-medium">技能包</span>
        <span class="text-muted-foreground text-xs">根目录须包含 SKILL.md 的 zip 压缩包，选择后立即上传。</span>
      </div>
      <input ref="fileInput" type="file" accept=".zip,application/zip" class="hidden" @change="onFileChange" />
      <Button type="button" size="sm" variant="outline" :disabled="uploading" @click="pickFile">
        <LoaderCircle v-if="uploading" class="animate-spin" />
        <Upload v-else />
        {{ model.directory ? '重新选择 zip' : '选择 zip' }}
      </Button>
    </div>
    <p v-if="error" class="text-destructive text-sm">{{ error }}</p>
    <p v-if="model.directory" class="text-muted-foreground flex items-center gap-2 text-sm">
      <FileArchive class="size-4" />
      <span class="truncate">
        已上传<span v-if="model.fileName" class="ml-1 font-medium text-foreground">{{ model.fileName }}</span>
        <span class="ml-2 font-mono text-xs">{{ model.directory }}</span>
      </span>
    </p>
    <p v-else-if="!uploading" class="text-muted-foreground text-sm">尚未上传，保存前必须先上传技能包。</p>
  </div>
</template>
