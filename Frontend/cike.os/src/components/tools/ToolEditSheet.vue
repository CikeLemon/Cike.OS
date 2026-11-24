<script setup lang="ts">
import type { ToolDto } from '@/api'
import { postApiV1Tools, putApiV1ToolsById } from '@/api'
import { LoaderCircle } from '@lucide/vue'
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
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Sheet, SheetContent, SheetDescription, SheetFooter, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { Textarea } from '@/components/ui/textarea'
import HttpMcpSettingFields from '@/components/tools/HttpMcpSettingFields.vue'
import SkillSettingFields from '@/components/tools/SkillSettingFields.vue'
import StdioMcpSettingFields from '@/components/tools/StdioMcpSettingFields.vue'
import ToolParameterRows from '@/components/tools/ToolParameterRows.vue'
import {
  emptyToolForm,
  isSettingDirty,
  parameterCodes,
  resetSettings,
  toCreateToolDto,
  toUpdateToolDto,
  toolFormFromDto,
  validateToolForm,
  type ToolFormState,
} from '@/components/tools/tool-form'
import { getErrorMessage } from '@/lib/errors'
import { TOOL_TYPE, TOOL_TYPE_OPTIONS, type ToolTypeValue } from '@/lib/tool-type'

const props = defineProps<{
  open: boolean
  toolPackageId: string
  /** 传入为编辑，null 为新建 */
  tool?: ToolDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const form = reactive<ToolFormState>(emptyToolForm(''))
const errors = ref<Record<string, string>>({})
const saving = ref(false)

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    Object.assign(form, props.tool ? toolFormFromDto(props.tool) : emptyToolForm(props.toolPackageId))
  },
)

const isEdit = computed(() => Boolean(props.tool))
const codes = computed(() => parameterCodes(form))

// 形态切换：设置区已有内容时先确认再清空，参数定义区不受影响
const confirmTypeChange = ref<ToolTypeValue | null>(null)

function requestTypeChange(value: unknown) {
  // 形态创建后不可变：编辑态的单选组已禁用，这里只是兜底
  if (isEdit.value) return
  const next = Number(value) as ToolTypeValue
  if (next === form.type) return
  if (isSettingDirty(form)) {
    confirmTypeChange.value = next
    return
  }
  applyTypeChange(next)
}

function applyTypeChange(next: ToolTypeValue) {
  Object.assign(form, resetSettings(form), { type: next })
  errors.value = {}
}

function confirmTypeSwitch() {
  if (confirmTypeChange.value !== null) {
    applyTypeChange(confirmTypeChange.value)
  }
  confirmTypeChange.value = null
}

/** 新建技能工具未上传技能包时禁用保存 */
const saveDisabled = computed(
  () => saving.value || (form.type === TOOL_TYPE.Skill && !form.skill.directory),
)

async function submit() {
  const nextErrors = validateToolForm(form)
  errors.value = nextErrors
  if (Object.keys(nextErrors).length > 0) {
    toast.error('请先修正表单中的问题。')
    return
  }

  saving.value = true
  try {
    // 形态创建后不可变：更新载荷不带 type，后端用现有形态校验设置
    const result = props.tool
      ? await putApiV1ToolsById({ path: { id: props.tool.id ?? '' }, body: toUpdateToolDto(form) })
      : await postApiV1Tools({ body: toCreateToolDto(form) })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(isEdit.value ? '工具已更新。' : '工具已创建。')
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
        <SheetTitle>{{ isEdit ? '编辑工具' : '新建工具' }}</SheetTitle>
        <SheetDescription>依次填写基本信息、参数定义、形态与设置；参数定义可被设置中的取值引用。</SheetDescription>
      </SheetHeader>

      <form id="tool-form" class="grid flex-1 gap-6 overflow-y-auto px-6 py-4" @submit.prevent="submit">
        <!-- 基本信息 -->
        <section class="grid gap-3">
          <h3 class="text-sm font-semibold">基本信息</h3>
          <div class="grid gap-3 sm:grid-cols-2">
            <div class="grid gap-1.5">
              <Label for="tool-code">编码 <span class="text-destructive">*</span></Label>
              <Input id="tool-code" v-model="form.code" placeholder="包内唯一，如：search" :aria-invalid="Boolean(errors.code)" autocomplete="off" />
              <p v-if="errors.code" class="text-destructive text-sm">{{ errors.code }}</p>
            </div>
            <div class="grid gap-1.5">
              <Label for="tool-name">名称 <span class="text-destructive">*</span></Label>
              <Input id="tool-name" v-model="form.name" placeholder="如：网页搜索" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
              <p v-if="errors.name" class="text-destructive text-sm">{{ errors.name }}</p>
            </div>
          </div>
          <div class="grid gap-1.5">
            <Label for="tool-description">描述</Label>
            <Textarea id="tool-description" v-model="form.description" placeholder="这个工具做什么、什么时候用" class="min-h-16" />
          </div>
        </section>

        <!-- 参数定义 -->
        <section class="grid gap-3">
          <h3 class="text-sm font-semibold">参数定义</h3>
          <ToolParameterRows v-model="form.parameters" :error="errors.parameters" />
        </section>

        <!-- 形态 -->
        <section class="grid gap-3">
          <h3 class="text-sm font-semibold">形态</h3>
          <RadioGroup
            :model-value="String(form.type)"
            class="flex gap-6"
            :disabled="isEdit"
            @update:model-value="requestTypeChange"
          >
            <label
              v-for="option in TOOL_TYPE_OPTIONS"
              :key="option.value"
              class="flex items-center gap-2 text-sm"
              :class="isEdit ? 'text-muted-foreground cursor-not-allowed' : ''"
            >
              <RadioGroupItem :value="String(option.value)" :disabled="isEdit" />
              {{ option.label }}
            </label>
          </RadioGroup>
          <p class="text-muted-foreground text-xs">
            {{ isEdit ? '形态创建后不可修改。' : '切换形态会清空下方设置区（参数定义不受影响）。' }}
          </p>
        </section>

        <!-- 设置 -->
        <section class="grid gap-3">
          <h3 class="text-sm font-semibold">设置</h3>
          <template v-if="form.type === TOOL_TYPE.Skill">
            <SkillSettingFields v-model="form.skill" :error="errors['skill.directory']" />
          </template>
          <template v-else-if="form.type === TOOL_TYPE.HttpMcp">
            <HttpMcpSettingFields v-model="form.http" :parameter-codes="codes" :errors="errors" />
          </template>
          <template v-else>
            <StdioMcpSettingFields v-model="form.stdio" :parameter-codes="codes" :errors="errors" />
          </template>
        </section>
      </form>

      <SheetFooter class="border-t px-6 py-4">
        <Button type="button" variant="outline" :disabled="saving" @click="emit('update:open', false)">取消</Button>
        <Button type="submit" form="tool-form" :disabled="saveDisabled">
          <LoaderCircle v-if="saving" class="animate-spin" />
          保存
        </Button>
      </SheetFooter>
    </SheetContent>

    <AlertDialog :open="confirmTypeChange !== null" @update:open="confirmTypeChange = null">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>切换工具形态</AlertDialogTitle>
          <AlertDialogDescription>设置区已有内容，切换形态将清空当前设置（含已上传的技能包引用）。是否继续？</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel @click="confirmTypeChange = null">取消</AlertDialogCancel>
          <!-- 不用 AlertDialogAction：它会先关闭对话框再触发 click，导致待切换的形态被提前清空 -->
          <Button @click="confirmTypeSwitch">继续切换</Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </Sheet>
</template>
