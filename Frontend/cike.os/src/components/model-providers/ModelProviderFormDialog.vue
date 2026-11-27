<script setup lang="ts">
import type { ModelProviderDto, TagDto } from '@/api'
import { postApiV1ModelProviders, putApiV1ModelProvidersById } from '@/api'
import { LoaderCircle, Pencil, Plus, Trash } from '@lucide/vue'
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
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import ModelRowEditor from '@/components/model-providers/ModelRowEditor.vue'
import {
  emptyModelRow,
  emptyProviderForm,
  providerFormFromDto,
  providerFormToBody,
  protocolFieldsHaveInput,
  removeModelRow,
  resetProtocolFields,
  upsertModelRow,
  validateModelRow,
  validateProviderForm,
  type ModelRow,
  type ProviderFormState,
} from '@/components/model-providers/provider-form'
import { getErrorMessage } from '@/lib/errors'
import { truncate } from '@/lib/format'
import { PROTOCOL_OPTIONS, isOpenAiProtocol, type ProtocolValue } from '@/lib/model-provider-protocol'

const props = defineProps<{
  open: boolean
  /** 平台标签列表，供模型的标签多选 */
  tags: TagDto[]
  /** 标签列表是否已成功加载：未就绪时不过滤回填、也不允许保存（否则会把模型上的标签全部丢掉） */
  tagsReady: boolean
  /** 传入为编辑（列表页先按 Id 取到的详情），null 为新建 */
  provider?: ModelProviderDto | null
}>()

const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: []
}>()

const form = reactive<ProviderFormState>(emptyProviderForm())
const errors = ref<Record<string, string>>({})
const saving = ref(false)

const isEdit = computed(() => Boolean(props.provider))

// 标签只有 id 列表，名称从这里查表回显
const tagNameById = computed(() => new Map(props.tags.map(tag => [tag.id ?? '', tag.name ?? ''])))

/** 模型的标签名称：只渲染能查到名称的（已删标签的残留引用不显示） */
function tagNames(row: ModelRow): string[] {
  return row.tagIds.map(tagId => tagNameById.value.get(tagId)).filter((name): name is string => Boolean(name))
}

watch(
  () => props.open,
  open => {
    if (!open) return
    errors.value = {}
    pendingProtocol.value = null
    editingRow.value = null
    rowErrors.value = {}
    Object.assign(form, props.provider ? providerFormFromDto(props.provider, props.tags, props.tagsReady) : emptyProviderForm())
  },
)

// 切换协议（新建与编辑都可以）：协议专属字段已有输入时先二次确认，确认后清空（名称、地址、密钥、模型表保留）
const pendingProtocol = ref<ProtocolValue | null>(null)

const protocolValue = computed({
  get: () => String(form.protocol),
  set: (value: string) => {
    const next = Number(value) as ProtocolValue
    if (next === form.protocol) return

    if (protocolFieldsHaveInput(form)) {
      pendingProtocol.value = next
      return
    }

    applyProtocol(next)
  },
})

function applyProtocol(protocol: ProtocolValue) {
  form.protocol = protocol
  resetProtocolFields(form)
  errors.value = {}
}

// 模型条目在表格行内编辑：同一时间只编辑一条，确定后写回表单，不发请求
const editingRow = ref<ModelRow | null>(null)
const rowErrors = ref<Record<string, string>>({})

/** 正在编辑的是表单里已有的条目（名称只读），还是刚建的空行 */
const editingExisting = computed(() => editingRow.value !== null && form.models.some(x => x.rowKey === editingRow.value?.rowKey))

/** 表格要渲染的行：新增时把草稿行放在最上面 */
const tableRows = computed<ModelRow[]>(() =>
  editingRow.value && !editingExisting.value ? [editingRow.value, ...form.models] : form.models,
)

function startAddModel() {
  if (editingRow.value) return
  editingRow.value = emptyModelRow()
  rowErrors.value = {}
}

function startEditModel(row: ModelRow) {
  if (editingRow.value) return
  editingRow.value = { ...row, tagIds: [...row.tagIds] }
  rowErrors.value = {}
}

function cancelEditModel() {
  editingRow.value = null
  rowErrors.value = {}
}

function confirmEditModel() {
  const row = editingRow.value
  if (!row) return

  rowErrors.value = validateModelRow(row, form.models)
  if (Object.keys(rowErrors.value).length > 0) return

  upsertModelRow(form, row)
  editingRow.value = null
  rowErrors.value = {}
}

async function submit() {
  if (editingRow.value) return
  // 标签列表没加载成功时不保存：回填没过滤掉已删标签，会被后端「标签不存在」拦下
  if (!props.tagsReady) return
  errors.value = validateProviderForm(form)
  if (Object.keys(errors.value).length > 0) return

  saving.value = true
  try {
    // 新建与更新的请求体同形：连接字段 + 期望的完整模型列表
    const body = providerFormToBody(form)
    const result = props.provider
      ? await putApiV1ModelProvidersById({ path: { id: props.provider.id ?? '' }, body })
      : await postApiV1ModelProviders({ body })

    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success(isEdit.value ? '模型提供方已更新。' : '模型提供方已创建。')
    emit('saved')
    emit('update:open', false)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-4xl">
      <DialogHeader>
        <DialogTitle>{{ isEdit ? '编辑模型提供方' : '新建模型提供方' }}</DialogTitle>
        <DialogDescription>一个模型提供方 = 一种协议 + 一个接入地址 + 一把密钥，以及它提供的模型。同一家供应商有几种协议就建几个提供方。</DialogDescription>
      </DialogHeader>

      <!-- content-start：grid 的隐式行默认拉伸填满高度，会把各分组撑出大段空白 -->
      <form id="provider-form" class="grid max-h-[70vh] content-start gap-5 overflow-y-auto" @submit.prevent="submit">
        <!-- 连接 -->
        <section class="grid gap-3">
          <h3 class="text-sm font-semibold">连接</h3>
          <div class="grid gap-3 sm:grid-cols-2 sm:items-start">
            <div class="grid gap-1.5">
              <Label for="provider-name">名称 <span class="text-destructive">*</span></Label>
              <Input id="provider-name" v-model="form.name" placeholder="如：智谱 (OpenAI)" :aria-invalid="Boolean(errors.name)" autocomplete="off" />
              <p v-if="errors.name" class="text-destructive text-sm">{{ errors.name }}</p>
            </div>
            <div class="grid gap-1.5">
              <Label for="provider-protocol">协议 <span class="text-destructive">*</span></Label>
              <Select v-model="protocolValue" :disabled="saving">
                <SelectTrigger id="provider-protocol" class="w-full">
                  <SelectValue placeholder="选择协议" />
                </SelectTrigger>
                <SelectContent>
                  <SelectGroup>
                    <SelectItem v-for="option in PROTOCOL_OPTIONS" :key="option.value" :value="String(option.value)">{{ option.label }}</SelectItem>
                  </SelectGroup>
                </SelectContent>
              </Select>
              <p class="text-muted-foreground text-xs">决定用哪种接口约定调用模型服务。</p>
            </div>
          </div>
          <div class="grid gap-3 sm:grid-cols-2 sm:items-start">
            <div class="grid gap-1.5">
              <Label for="provider-base-url">接入地址 <span class="text-destructive">*</span></Label>
              <Input id="provider-base-url" v-model="form.baseUrl" placeholder="https://…" :aria-invalid="Boolean(errors.baseUrl)" autocomplete="off" />
              <p v-if="errors.baseUrl" class="text-destructive text-sm">{{ errors.baseUrl }}</p>
            </div>
            <div class="grid gap-1.5">
              <Label for="provider-api-key">API 密钥 <span class="text-destructive">*</span></Label>
              <Input id="provider-api-key" v-model="form.apiKey" type="password" :aria-invalid="Boolean(errors.apiKey)" autocomplete="off" />
              <p v-if="errors.apiKey" class="text-destructive text-sm">{{ errors.apiKey }}</p>
            </div>
          </div>
          <!-- 协议专属字段：均为可选；两种 OpenAI 协议共用组织标识 -->
          <div v-if="isOpenAiProtocol(form.protocol)" class="grid gap-1.5 sm:max-w-[calc(50%-0.375rem)]">
            <Label for="provider-organization">组织标识</Label>
            <Input id="provider-organization" v-model="form.organization" placeholder="可选，请求头 OpenAI-Organization" autocomplete="off" />
          </div>
          <div v-else class="grid gap-1.5 sm:max-w-[calc(50%-0.375rem)]">
            <Label for="provider-version">API 版本</Label>
            <Input id="provider-version" v-model="form.version" placeholder="可选，请求头 anthropic-version" autocomplete="off" />
          </div>
        </section>

        <!-- 模型：在表格行内增删改，先留在表单里，点「保存」随提供方一起提交 -->
        <section class="grid gap-3 border-t pt-5">
          <div class="flex items-center justify-between gap-2">
            <h3 class="text-sm font-semibold">模型</h3>
            <Button type="button" size="sm" variant="outline" :disabled="saving || editingRow !== null" @click="startAddModel">
              <Plus />
              添加模型
            </Button>
          </div>
          <p class="text-muted-foreground text-xs">这个提供方提供哪些模型。模型名称是调用时发给提供方的名字，创建后不可修改。</p>

          <!-- table-fixed：列宽按表头给定的宽度走，不随已选标签的文字长度漂移 -->
          <div class="overflow-hidden rounded-lg border">
            <Table class="table-fixed">
              <TableHeader>
                <TableRow>
                  <TableHead class="w-56">名称</TableHead>
                  <TableHead class="w-64">标签</TableHead>
                  <TableHead>描述</TableHead>
                  <TableHead class="w-24 text-right">操作</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                <TableEmpty v-if="form.models.length === 0 && editingRow === null" :colspan="4">还没有模型，点击「添加模型」。</TableEmpty>
                <template v-for="row in tableRows" :key="row.rowKey">
                  <ModelRowEditor
                    v-if="editingRow?.rowKey === row.rowKey"
                    :row="editingRow"
                    :name-locked="editingExisting"
                    :errors="rowErrors"
                    :tags="tags"
                    :tags-ready="tagsReady"
                    @confirm="confirmEditModel"
                    @cancel="cancelEditModel"
                    @clear="rowErrors = {}"
                  />
                  <TableRow v-else>
                    <TableCell class="font-mono text-xs">{{ row.name }}</TableCell>
                    <TableCell>
                      <div class="flex flex-wrap gap-1">
                        <Badge v-for="name in tagNames(row)" :key="name" variant="secondary">{{ name }}</Badge>
                        <span v-if="tagNames(row).length === 0" class="text-muted-foreground">—</span>
                      </div>
                    </TableCell>
                    <TableCell class="text-muted-foreground max-w-md truncate">{{ truncate(row.description, 60) }}</TableCell>
                    <TableCell class="text-right">
                      <div class="flex justify-end gap-1">
                        <Button type="button" size="icon-sm" variant="ghost" aria-label="编辑模型" :disabled="saving || editingRow !== null" @click="startEditModel(row)">
                          <Pencil />
                        </Button>
                        <Button type="button" size="icon-sm" variant="ghost" aria-label="移除模型" :disabled="saving || editingRow !== null" @click="removeModelRow(form, row.rowKey)">
                          <Trash />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                </template>
              </TableBody>
            </Table>
          </div>
        </section>
      </form>

      <!-- 页脚放在滚动区外：模型多时不用滚到底就能看到按钮；提交按钮用 form 属性挂到上面的表单 -->
      <DialogFooter>
        <p v-if="!tagsReady" class="text-destructive text-sm">标签列表加载失败，请刷新页面后再编辑。</p>
        <Button type="button" variant="outline" :disabled="saving" @click="emit('update:open', false)">取消</Button>
        <Button type="submit" form="provider-form" :disabled="saving || !tagsReady || editingRow !== null">
          <LoaderCircle v-if="saving" class="animate-spin" />
          保存
        </Button>
      </DialogFooter>
    </DialogContent>

    <AlertDialog :open="pendingProtocol !== null" @update:open="pendingProtocol = null">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>切换协议</AlertDialogTitle>
          <AlertDialogDescription>协议专属字段已填写，切换协议将清空它们（名称、接入地址、密钥与模型保留）。是否继续？</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel @click="pendingProtocol = null">取消</AlertDialogCancel>
          <!-- 不用 AlertDialogAction：它会先关闭对话框再触发 click，导致待切换的协议被提前清空 -->
          <Button @click="applyProtocol(pendingProtocol ?? form.protocol); pendingProtocol = null">继续切换</Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </Dialog>
</template>
