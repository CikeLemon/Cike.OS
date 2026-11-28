<script setup lang="ts">
import type { UserToolConfigurationDetailDto } from '@/api'
import { postApiV1UserToolConfigurationsSave } from '@/api'
import { LoaderCircle } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import ToolParameterFields from '@/components/tool-configurations/ToolParameterFields.vue'
import {
  configurationFormFromValues,
  configurationValuesToBody,
  validateConfigurationForm,
  type ConfigurationValues,
} from '@/components/tool-configurations/tool-configuration-form'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { getErrorMessage } from '@/lib/errors'

/** 单个工具的参数编辑：按详情里的参数定义生成表单，回填已存的值，保存走现有单条 Save */
const props = defineProps<{
  open: boolean
  /** 点编辑时按工具 Id 取到的详情（带参数定义与已存值）；关闭时为 null */
  item?: UserToolConfigurationDetailDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const values = reactive<ConfigurationValues>({})
const errors = ref<Record<string, string>>({})
const saving = ref(false)

const toolId = computed(() => props.item?.toolId ?? '')

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    Object.keys(values).forEach(key => delete values[key])
    Object.assign(values, configurationFormFromValues(props.item?.parameters, props.item?.values))
  },
)

function setValue(code: string, value: string) {
  values[code] = value
}

function clearError(code: string) {
  delete errors.value[code]
}

async function submit() {
  if (!toolId.value || saving.value) return
  errors.value = validateConfigurationForm(props.item?.parameters, values)
  if (Object.keys(errors.value).length > 0) return

  saving.value = true
  try {
    const result = await postApiV1UserToolConfigurationsSave({ body: { toolId: toolId.value, values: configurationValuesToBody(values) } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('参数已保存。')
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
        <DialogTitle>配置参数：{{ item?.toolName }}</DialogTitle>
        <DialogDescription>按用户保存，你的所有 Agent 共用这一份参数值。</DialogDescription>
      </DialogHeader>
      <form class="grid gap-4" @submit.prevent="submit">
        <ToolParameterFields
          :parameters="item?.parameters"
          :values="values"
          :errors="errors"
          id-prefix="tc-edit"
          @update:value="setValue"
          @clear-error="clearError"
        />
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
