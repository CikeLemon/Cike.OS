<script setup lang="ts">
import type { ToolPackageCategoryDto, ToolPackageDto, ToolPackageStatus } from '@/api'
import { postApiV1ToolPackages, putApiV1ToolPackagesById } from '@/api'
import { LoaderCircle } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { getErrorMessage } from '@/lib/errors'
import { TOOL_PACKAGE_STATUS, TOOL_PACKAGE_STATUS_OPTIONS } from '@/lib/tool-status'

const props = defineProps<{
  open: boolean
  categories: ToolPackageCategoryDto[]
  /** 传入为编辑，null 为新建 */
  toolPackage?: ToolPackageDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: [id: string]
}>()

const form = reactive({
  code: '',
  name: '',
  description: '',
  icon: '',
  readme: '',
  toolPackageCategoryId: '',
  status: String(TOOL_PACKAGE_STATUS.Draft) as ToolPackageStatus | string,
})
const errors = ref<Record<string, string>>({})
const saving = ref(false)

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    const pkg = props.toolPackage
    form.code = pkg?.code ?? ''
    form.name = pkg?.name ?? ''
    form.description = pkg?.description ?? ''
    form.icon = pkg?.icon ?? ''
    form.readme = pkg?.readme ?? ''
    form.toolPackageCategoryId = pkg?.toolPackageCategoryId ?? props.categories[0]?.id ?? ''
    form.status = pkg?.status != null ? String(pkg.status) : String(TOOL_PACKAGE_STATUS.Draft)
  },
)

const isEdit = computed(() => Boolean(props.toolPackage))

function validate(): boolean {
  const next: Record<string, string> = {}
  if (!form.code.trim()) next.code = '请填写编码。'
  if (!form.name.trim()) next.name = '请填写名称。'
  if (!form.toolPackageCategoryId) next.toolPackageCategoryId = '请选择分类。'
  errors.value = next
  return Object.keys(next).length === 0
}

async function submit() {
  if (!validate()) return

  saving.value = true
  try {
    const body = {
      code: form.code.trim(),
      name: form.name.trim(),
      description: form.description,
      icon: form.icon.trim(),
      readme: form.readme,
      toolPackageCategoryId: form.toolPackageCategoryId,
      status: Number(form.status) as ToolPackageStatus,
    }

    const result = props.toolPackage
      ? await putApiV1ToolPackagesById({ path: { id: props.toolPackage.id ?? '' }, body })
      : await postApiV1ToolPackages({ body })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    const id = props.toolPackage ? props.toolPackage.id : (result.data as unknown as string)
    toast.success(isEdit.value ? '工具包已更新。' : '工具包已创建。')
    emit('saved', id ?? '')
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
        <DialogTitle>{{ isEdit ? '编辑工具包' : '新建工具包' }}</DialogTitle>
        <DialogDescription>工具包是工具的打包分发单元，创建成功后可在详情页添加工具。</DialogDescription>
      </DialogHeader>
      <form class="grid gap-4" @submit.prevent="submit">
        <div class="grid gap-4 sm:grid-cols-2 sm:items-start">
          <div class="grid gap-2">
            <Label for="package-code">编码</Label>
            <Input id="package-code" v-model="form.code" placeholder="如：web-search" :aria-invalid="Boolean(errors.code)" autocomplete="off" />
            <p v-if="errors.code" class="text-destructive text-sm">{{ errors.code }}</p>
          </div>
          <div class="grid gap-2">
            <Label for="package-name">名称</Label>
            <Input id="package-name" v-model="form.name" placeholder="如：网页搜索" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
            <p v-if="errors.name" class="text-destructive text-sm">{{ errors.name }}</p>
          </div>
          <div class="grid gap-2">
            <Label>分类</Label>
            <Select v-model="form.toolPackageCategoryId">
              <SelectTrigger class="w-full" :aria-invalid="Boolean(errors.toolPackageCategoryId)">
                <SelectValue placeholder="选择分类" />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectItem v-for="category in categories" :key="category.id" :value="category.id ?? ''">{{ category.name }}</SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
            <p v-if="errors.toolPackageCategoryId" class="text-destructive text-sm">{{ errors.toolPackageCategoryId }}</p>
          </div>
          <div class="grid gap-2">
            <Label>状态</Label>
            <Select v-model="form.status">
              <SelectTrigger class="w-full">
                <SelectValue placeholder="选择状态" />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectItem v-for="option in TOOL_PACKAGE_STATUS_OPTIONS" :key="option.value" :value="String(option.value)">{{ option.label }}</SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>
          <div class="grid gap-2 sm:col-span-2">
            <Label for="package-icon">图标</Label>
            <Input id="package-icon" v-model="form.icon" placeholder="图片地址（https://…）或留空" autocomplete="off" />
          </div>
        </div>
        <div class="grid gap-2">
          <Label for="package-description">描述</Label>
          <Input id="package-description" v-model="form.description" placeholder="一句话说明这个工具包做什么" />
        </div>
        <div class="grid gap-2">
          <Label for="package-readme">Readme</Label>
          <Textarea id="package-readme" v-model="form.readme" placeholder="支持 Markdown" class="min-h-32" />
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
