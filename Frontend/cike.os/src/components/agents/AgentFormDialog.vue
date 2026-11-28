<script setup lang="ts">
import type { AgentDetailDto, AgentToolPackageOptionItemDto } from '@/api'
import { postApiV1Agents, putApiV1AgentsById } from '@/api'
import { ChevronDown, LoaderCircle, Plus, Trash, X } from '@lucide/vue'
import { computed, reactive, ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import AgentToolPicker from '@/components/agents/AgentToolPicker.vue'
import {
  agentFormFromDto,
  agentFormToBody,
  emptyAgentForm,
  fromSelectValue,
  pendingParameterToolIds,
  toSelectValue,
  toolSelectionToRows,
  validateAgentForm,
  type AgentFormState,
} from '@/components/agents/agent-form'
import { Button } from '@/components/ui/button'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { getErrorMessage } from '@/lib/errors'
import {
  REASONING_EFFORT_OPTIONS,
  REASONING_OUTPUT_OPTIONS,
  RESPONSE_FORMAT_OPTIONS,
  RESPONSE_FORMAT_TYPE,
  SELECT_UNSET,
  TOOL_MODE_OPTIONS,
  TRI_STATE_OPTIONS,
  type ReasoningEffortValue,
  type ReasoningOutputValue,
  type ResponseFormatTypeValue,
  type ToolModeValue,
} from '@/lib/agent-options'

/** 保存成功后交给页面决定去哪：新建且绑定了带参数的工具时，页面带着这些工具 Id 跳去「工具参数」 */
interface AgentSavedPayload {
  created: boolean
  name: string
  pendingToolIds: string[]
}

const props = defineProps<{
  open: boolean
  /** 传入为编辑（列表页先按 Id 取到的详情），null 为新建 */
  agent?: AgentDetailDto | null
  /** 工具包 → 工具 的选择列表（第二步用） */
  toolOptions: AgentToolPackageOptionItemDto[]
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: [payload: AgentSavedPayload]
}>()

const STEP_BASIC = 1
const STEP_TOOLS = 2

const form = reactive<AgentFormState>(emptyAgentForm())
const errors = ref<Record<string, string>>({})
const saving = ref(false)
const step = ref<number>(STEP_BASIC)
const advancedOpen = ref(false)

const isEdit = computed(() => Boolean(props.agent))

// 可空枚举 ↔ 下拉的字符串值：null 用哨兵表示「不设置」（SelectItem 不允许空串 value）
const reasoningEffortModel = computed({
  get: () => toSelectValue(form.reasoningEffort),
  set: (raw: string) => {
    form.reasoningEffort = raw === SELECT_UNSET ? null : (Number(raw) as ReasoningEffortValue)
  },
})
const toolModeModel = computed({
  get: () => toSelectValue(form.toolMode),
  set: (raw: string) => {
    form.toolMode = fromSelectValue(raw) as ToolModeValue | null
  },
})
const reasoningOutputModel = computed({
  get: () => toSelectValue(form.reasoningOutput),
  set: (raw: string) => {
    form.reasoningOutput = raw === SELECT_UNSET ? null : (Number(raw) as ReasoningOutputValue)
  },
})
const responseFormatModel = computed({
  get: () => toSelectValue(form.responseFormatType),
  set: (raw: string) => {
    form.responseFormatType = raw === SELECT_UNSET ? null : (Number(raw) as ResponseFormatTypeValue)
  },
})

// 两步都可以随意切换；有校验错误时切回出错的那一步
watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    advancedOpen.value = false
    step.value = STEP_BASIC
    Object.assign(form, props.agent ? agentFormFromDto(props.agent) : emptyAgentForm())
  },
)

function addStopSequence() {
  if (form.stopSequences.length >= 16) return
  form.stopSequences.push('')
}

function removeStopSequence(index: number) {
  form.stopSequences.splice(index, 1)
}

async function submit() {
  errors.value = validateAgentForm(form)
  if (Object.keys(errors.value).length > 0) {
    step.value = STEP_BASIC
    return
  }

  saving.value = true
  try {
    // 新建与更新的请求体同形：全部字段 + 期望的完整工具条目列表
    const body = agentFormToBody(form)
    const result = props.agent
      ? await putApiV1AgentsById({ path: { id: props.agent.id ?? '' }, body })
      : await postApiV1Agents({ body })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(isEdit.value ? 'Agent 已更新。' : 'Agent 已创建。')
    // 新建后：绑定的工具里有要用户填参数的，页面跳去「工具参数」；编辑不检测
    const pendingToolIds = props.agent ? [] : pendingParameterToolIds(toolSelectionToRows(form), props.toolOptions)
    emit('saved', { created: !props.agent, name: body.name ?? '', pendingToolIds })
    emit('update:open', false)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-3xl">
      <DialogHeader>
        <DialogTitle>{{ isEdit ? '编辑 Agent' : '新建 Agent' }}</DialogTitle>
        <DialogDescription>Agent = 指令 + 模型选项（默认值，运行时可调整）+ 绑定的工具。不绑定模型，模型在运行时选择。</DialogDescription>
      </DialogHeader>

      <!-- 步骤标题可点击切换，底部始终有保存 -->
      <div class="flex items-center gap-2 text-sm">
        <button
          type="button"
          :class="cn('rounded-md px-3 py-1.5', step === STEP_BASIC ? 'bg-primary text-primary-foreground' : 'hover:bg-accent hover:text-accent-foreground')"
          @click="step = STEP_BASIC"
        >
          ① 基本信息
        </button>
        <button
          type="button"
          :class="cn('rounded-md px-3 py-1.5', step === STEP_TOOLS ? 'bg-primary text-primary-foreground' : 'hover:bg-accent hover:text-accent-foreground')"
          @click="step = STEP_TOOLS"
        >
          ② 工具包
        </button>
      </div>

      <form id="agent-form" class="grid max-h-[65vh] content-start gap-5 overflow-y-auto" @submit.prevent="submit">
        <!-- 第一步：基本信息 -->
        <template v-if="step === STEP_BASIC">
          <section class="grid gap-3">
            <div class="grid gap-3 sm:grid-cols-2 sm:items-start">
              <div class="grid gap-1.5">
                <Label for="agent-name">名称 <span class="text-destructive">*</span></Label>
                <Input id="agent-name" v-model="form.name" placeholder="如：客服助手" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
                <p v-if="errors.name" class="text-destructive text-sm">{{ errors.name }}</p>
              </div>
              <div class="grid gap-1.5">
                <Label for="agent-description">描述</Label>
                <Input id="agent-description" v-model="form.description" placeholder="可选，一句话说明它做什么" :aria-invalid="Boolean(errors.description)" autocomplete="off" />
                <p v-if="errors.description" class="text-destructive text-sm">{{ errors.description }}</p>
              </div>
            </div>
            <div class="grid gap-1.5">
              <Label for="agent-instructions">指令 <span class="text-destructive">*</span></Label>
              <Textarea id="agent-instructions" v-model="form.instructions" rows="8" placeholder="Agent 的自我介绍与提示词，不限长度" :aria-invalid="Boolean(errors.instructions)" />
              <p v-if="errors.instructions" class="text-destructive text-sm">{{ errors.instructions }}</p>
            </div>
          </section>

          <!-- 模型参数：留空 = 不设置 -->
          <section class="grid gap-3 border-t pt-5">
            <h3 class="text-sm font-semibold">模型参数</h3>
            <p class="text-muted-foreground text-xs">只是默认值，运行时可调整；留空表示不设置、由模型决定。</p>
            <div class="grid gap-3 sm:grid-cols-3 sm:items-start">
              <div class="grid gap-1.5">
                <Label for="agent-temperature">温度</Label>
                <Input id="agent-temperature" v-model="form.temperature" type="number" step="any" min="0" max="2" placeholder="0 – 2" :aria-invalid="Boolean(errors.temperature)" />
                <p v-if="errors.temperature" class="text-destructive text-sm">{{ errors.temperature }}</p>
              </div>
              <div class="grid gap-1.5">
                <Label for="agent-max-output-tokens">输出上限</Label>
                <Input id="agent-max-output-tokens" v-model="form.maxOutputTokens" type="number" step="1" min="1" placeholder="token 数" :aria-invalid="Boolean(errors.maxOutputTokens)" />
                <p v-if="errors.maxOutputTokens" class="text-destructive text-sm">{{ errors.maxOutputTokens }}</p>
              </div>
              <div class="grid gap-1.5">
                <Label for="agent-top-p">TopP</Label>
                <Input id="agent-top-p" v-model="form.topP" type="number" step="any" min="0" max="1" placeholder="0 – 1" :aria-invalid="Boolean(errors.topP)" />
                <p v-if="errors.topP" class="text-destructive text-sm">{{ errors.topP }}</p>
              </div>
              <div class="grid gap-1.5">
                <Label for="agent-reasoning-effort">推理力度</Label>
                <Select v-model="reasoningEffortModel">
                  <SelectTrigger id="agent-reasoning-effort" class="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectGroup>
                      <SelectItem :value="SELECT_UNSET">不设置</SelectItem>
                      <SelectItem v-for="option in REASONING_EFFORT_OPTIONS" :key="option.value" :value="String(option.value)">{{ option.label }}</SelectItem>
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </div>
              <div class="grid gap-1.5">
                <Label for="agent-tool-mode">工具模式</Label>
                <Select v-model="toolModeModel">
                  <SelectTrigger id="agent-tool-mode" class="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectGroup>
                      <SelectItem :value="SELECT_UNSET">不设置</SelectItem>
                      <SelectItem v-for="option in TOOL_MODE_OPTIONS" :key="option.value" :value="option.value">{{ option.label }}</SelectItem>
                    </SelectGroup>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </section>

          <!-- 高级配置：默认折叠 -->
          <Collapsible v-model:open="advancedOpen" class="border-t pt-5">
            <CollapsibleTrigger class="group flex w-full items-center gap-1 text-sm font-semibold">
              <ChevronDown class="size-4 transition-transform group-data-[state=open]:rotate-180" />
              高级配置
            </CollapsibleTrigger>
            <CollapsibleContent>
              <div class="mt-3 grid gap-3 sm:grid-cols-3 sm:items-start">
                <div class="grid gap-1.5">
                  <Label for="agent-top-k">TopK</Label>
                  <Input id="agent-top-k" v-model="form.topK" type="number" step="1" min="1" placeholder="≥ 1" :aria-invalid="Boolean(errors.topK)" />
                  <p v-if="errors.topK" class="text-destructive text-sm">{{ errors.topK }}</p>
                </div>
                <div class="grid gap-1.5">
                  <Label for="agent-frequency-penalty">频率惩罚</Label>
                  <Input id="agent-frequency-penalty" v-model="form.frequencyPenalty" type="number" step="any" min="-2" max="2" placeholder="-2 – 2" :aria-invalid="Boolean(errors.frequencyPenalty)" />
                  <p v-if="errors.frequencyPenalty" class="text-destructive text-sm">{{ errors.frequencyPenalty }}</p>
                </div>
                <div class="grid gap-1.5">
                  <Label for="agent-presence-penalty">存在惩罚</Label>
                  <Input id="agent-presence-penalty" v-model="form.presencePenalty" type="number" step="any" min="-2" max="2" placeholder="-2 – 2" :aria-invalid="Boolean(errors.presencePenalty)" />
                  <p v-if="errors.presencePenalty" class="text-destructive text-sm">{{ errors.presencePenalty }}</p>
                </div>
                <div class="grid gap-1.5">
                  <Label for="agent-seed">Seed</Label>
                  <Input id="agent-seed" v-model="form.seed" type="number" step="1" min="0" placeholder="≥ 0" :aria-invalid="Boolean(errors.seed)" />
                  <p v-if="errors.seed" class="text-destructive text-sm">{{ errors.seed }}</p>
                </div>
                <div class="grid gap-1.5">
                  <Label for="agent-reasoning-output">推理输出</Label>
                  <Select v-model="reasoningOutputModel">
                    <SelectTrigger id="agent-reasoning-output" class="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectGroup>
                        <SelectItem :value="SELECT_UNSET">不设置</SelectItem>
                        <SelectItem v-for="option in REASONING_OUTPUT_OPTIONS" :key="option.value" :value="String(option.value)">{{ option.label }}</SelectItem>
                      </SelectGroup>
                    </SelectContent>
                  </Select>
                </div>
                <div class="grid gap-1.5">
                  <Label for="agent-response-format">响应格式</Label>
                  <Select v-model="responseFormatModel">
                    <SelectTrigger id="agent-response-format" class="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectGroup>
                        <SelectItem :value="SELECT_UNSET">不设置</SelectItem>
                        <SelectItem v-for="option in RESPONSE_FORMAT_OPTIONS" :key="option.value" :value="String(option.value)">{{ option.label }}</SelectItem>
                      </SelectGroup>
                    </SelectContent>
                  </Select>
                </div>
                <div class="grid gap-1.5">
                  <Label for="agent-allow-multiple-tool-calls">允许多次工具调用</Label>
                  <Select v-model="form.allowMultipleToolCalls">
                    <SelectTrigger id="agent-allow-multiple-tool-calls" class="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectGroup>
                        <SelectItem v-for="option in TRI_STATE_OPTIONS" :key="option.value" :value="option.value">{{ option.label }}</SelectItem>
                      </SelectGroup>
                    </SelectContent>
                  </Select>
                </div>
                <div class="grid gap-1.5">
                  <Label for="agent-allow-background-responses">允许后台响应</Label>
                  <Select v-model="form.allowBackgroundResponses">
                    <SelectTrigger id="agent-allow-background-responses" class="w-full">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectGroup>
                        <SelectItem v-for="option in TRI_STATE_OPTIONS" :key="option.value" :value="option.value">{{ option.label }}</SelectItem>
                      </SelectGroup>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <!-- JSON Schema：只在响应格式为 JSON 时出现 -->
              <div v-if="form.responseFormatType === RESPONSE_FORMAT_TYPE.Json" class="mt-3 grid gap-3">
                <div class="grid gap-1.5">
                  <Label for="agent-json-schema-name">Schema 名称</Label>
                  <Input id="agent-json-schema-name" v-model="form.jsonSchemaName" placeholder="可选" autocomplete="off" />
                </div>
                <div class="grid gap-1.5">
                  <Label for="agent-json-schema-descriptor">JSON Schema <span class="text-destructive">*</span></Label>
                  <Textarea id="agent-json-schema-descriptor" v-model="form.jsonSchemaDescriptor" rows="6" class="font-mono text-xs" placeholder='{"type":"object","properties":{}}' :aria-invalid="Boolean(errors.jsonSchemaDescriptor)" />
                  <p v-if="errors.jsonSchemaDescriptor" class="text-destructive text-sm">{{ errors.jsonSchemaDescriptor }}</p>
                </div>
              </div>

              <!-- 停止词：逐条输入 -->
              <div class="mt-3 grid gap-1.5">
                <div class="flex items-center justify-between">
                  <Label>停止词</Label>
                  <Button type="button" size="sm" variant="outline" :disabled="form.stopSequences.length >= 16" @click="addStopSequence">
                    <Plus />
                    添加停止词
                  </Button>
                </div>
                <div v-for="(_, index) in form.stopSequences" :key="index" class="flex gap-2">
                  <Input v-model="form.stopSequences[index]" placeholder="如：END" class="max-w-xs" autocomplete="off" />
                  <Button type="button" size="icon-sm" variant="ghost" aria-label="移除停止词" @click="removeStopSequence(index)">
                    <Trash />
                  </Button>
                </div>
                <p v-if="errors.stopSequences" class="text-destructive text-sm">{{ errors.stopSequences }}</p>
              </div>
            </CollapsibleContent>
          </Collapsible>
        </template>

        <!-- 第二步：工具包 -->
        <section v-else class="grid gap-3">
          <p v-if="toolOptions.length === 0" class="text-muted-foreground rounded-lg border border-dashed p-6 text-center text-sm">还没有可选的工具，先去「工具包」里建工具。</p>
          <AgentToolPicker
            v-else
            v-model:whole-packages="form.wholePackages"
            v-model:tool-ids="form.toolIds"
            :options="toolOptions"
            :disabled="saving"
          />
        </section>
      </form>

      <!-- 页脚放在滚动区外：提交按钮用 form 属性挂到上面的表单 -->
      <DialogFooter>
        <p v-if="Object.keys(errors).length > 0" class="text-destructive text-sm">
          <template v-if="step !== STEP_BASIC">基本信息有未通过校验的项。</template>
          <template v-else>请检查标红的字段。</template>
        </p>
        <Button v-if="step === STEP_TOOLS" type="button" variant="outline" :disabled="saving" @click="step = STEP_BASIC">
          <X />
          上一步
        </Button>
        <Button type="button" variant="outline" :disabled="saving" @click="emit('update:open', false)">取消</Button>
        <Button type="submit" form="agent-form" :disabled="saving">
          <LoaderCircle v-if="saving" class="animate-spin" />
          保存
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
