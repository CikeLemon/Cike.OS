<script setup lang="ts">
import type { UserToolConfigurationToolOptionItemDto } from '@/api'
import { getApiV1UserToolConfigurationsToolOptions, postApiV1UserToolConfigurationsSaveBatch } from '@/api'
import { LoaderCircle } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import ToolParameterFields from '@/components/tool-configurations/ToolParameterFields.vue'
import {
  batchItems,
  configurationFormFromValues,
  validateConfigurationForm,
  type ConfigurationValues,
} from '@/components/tool-configurations/tool-configuration-form'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { TOOL_TYPE_LABELS } from '@/lib/tool-type'
import { getErrorMessage } from '@/lib/errors'
import { cn } from '@/lib/utils'

/**
 * 批量新增：上半多选要配置的工具（只列声明了参数且尚未配置的，按工具包分组），下半每个选中工具一段表单，一个请求全部保存。
 * 从 Agent 新建跳转过来时由页面传入预选的工具 Id。
 */
const props = defineProps<{
  open: boolean
  /** 打开时预选的工具 Id（如来自 Agent 的跳转） */
  preselectedToolIds?: string[]
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const candidates = ref<UserToolConfigurationToolOptionItemDto[]>([])
const loading = ref(false)
const selected = ref<string[]>([])
// 每个选中工具的表单：toolId → 参数值
const forms = reactive<Record<string, ConfigurationValues>>({})
const saving = ref(false)

watch(
  () => props.open,
  async open => {
    if (!open) return
    selected.value = []
    Object.keys(forms).forEach(key => delete forms[key])
    await load()
    const ids = new Set(candidates.value.map(x => x.toolId ?? ''))
    selected.value = (props.preselectedToolIds ?? []).filter(id => ids.has(id))
    ensureForms()
  },
)

async function load() {
  loading.value = true
  try {
    const result = await getApiV1UserToolConfigurationsToolOptions()
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }
    // 候选 = 未配置的
    candidates.value = (result.data ?? []).filter(x => !x.configured)
  } finally {
    loading.value = false
  }
}

/** 候选按工具包分组（保持后端按工具名排序后的顺序，组内不再排） */
const candidateGroups = computed(() => {
  const groups = new Map<string, UserToolConfigurationToolOptionItemDto[]>()
  for (const tool of candidates.value) {
    const name = tool.toolPackageName ?? ''
    if (!groups.has(name)) groups.set(name, [])
    groups.get(name)!.push(tool)
  }
  return [...groups.entries()].map(([packageName, tools]) => ({ packageName, tools }))
})

const candidateById = computed(() => new Map(candidates.value.map(x => [x.toolId ?? '', x])))

/** 选中工具 → 表单 */
function ensureForms() {
  for (const toolId of selected.value) {
    if (!forms[toolId]) forms[toolId] = configurationFormFromValues(candidateById.value.get(toolId)?.parameters, null)
  }
  for (const key of Object.keys(forms)) {
    if (!selected.value.includes(key)) delete forms[key]
  }
}

function toggle(toolId: string, checked: boolean | 'indeterminate') {
  const next = new Set(selected.value)
  if (checked === true) next.add(toolId)
  else next.delete(toolId)
  selected.value = [...next]
  ensureForms()
}

function setValue(toolId: string, code: string, value: string) {
  if (!forms[toolId]) return
  forms[toolId][code] = value
}

/** 每个选中工具的实时校验结果：直接驱动字段红字与「全部保存」是否可用 */
const validation = computed(() => {
  const result: Record<string, Record<string, string>> = {}
  for (const toolId of selected.value) {
    result[toolId] = validateConfigurationForm(candidateById.value.get(toolId)?.parameters, forms[toolId] ?? {})
  }
  return result
})

const allValid = computed(() => selected.value.length > 0 && Object.values(validation.value).every(x => Object.keys(x).length === 0))

async function submit() {
  if (!allValid.value || saving.value) return
  saving.value = true
  try {
    const result = await postApiV1UserToolConfigurationsSaveBatch({ body: batchItems(forms) })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(`已保存 ${selected.value.length} 个工具的参数。`)
    emit('saved')
    emit('update:open', false)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-2xl">
      <DialogHeader>
        <DialogTitle>新增配置</DialogTitle>
        <DialogDescription>先选要配置的工具，再逐个填写参数；一次全部保存。按用户保存，你的所有 Agent 共用。</DialogDescription>
      </DialogHeader>

      <form id="tc-batch-form" class="grid max-h-[65vh] content-start gap-5 overflow-y-auto" @submit.prevent="submit">
        <!-- 选择工具：按工具包分组 -->
        <section class="grid gap-2">
          <h3 class="text-sm font-semibold">选择工具</h3>
          <p v-if="!loading && candidates.length === 0" class="text-muted-foreground rounded-lg border border-dashed p-4 text-center text-sm">没有待配置的工具。</p>
          <div v-for="group in candidateGroups" v-else :key="group.packageName" class="grid gap-1">
            <p class="text-muted-foreground text-xs">{{ group.packageName }}</p>
            <label v-for="tool in group.tools" :key="tool.toolId" class="hover:bg-accent flex cursor-pointer items-center gap-2 rounded-md px-2 py-1.5 text-sm">
              <Checkbox :model-value="selected.includes(tool.toolId ?? '')" :disabled="saving" @update:model-value="toggle(tool.toolId ?? '', $event)" />
              <span class="font-medium">{{ tool.toolName }}</span>
              <span class="text-muted-foreground text-xs">{{ tool.toolType ? TOOL_TYPE_LABELS[tool.toolType] : '' }}</span>
            </label>
          </div>
        </section>

        <!-- 每个选中工具一段表单；必填未填的工具标题标红 -->
        <section
          v-for="toolId in selected"
          :key="toolId"
          :class="cn('grid gap-3 border-t pt-4', Object.keys(validation[toolId] ?? {}).length > 0 && 'text-destructive')"
        >
          <h3 class="text-sm font-semibold">{{ candidateById.get(toolId)?.toolName }}</h3>
          <ToolParameterFields
            :parameters="candidateById.get(toolId)?.parameters"
            :values="forms[toolId] ?? {}"
            :errors="validation[toolId]"
            :id-prefix="`tc-batch-${toolId}`"
            @update:value="(code, value) => setValue(toolId, code, value)"
          />
        </section>
      </form>

      <!-- 页脚放在滚动区外：提交按钮用 form 属性挂到上面的表单 -->
      <DialogFooter>
        <Button type="button" variant="outline" :disabled="saving" @click="emit('update:open', false)">取消</Button>
        <Button type="submit" form="tc-batch-form" :disabled="saving || !allValid">
          <LoaderCircle v-if="saving" class="animate-spin" />
          全部保存
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
