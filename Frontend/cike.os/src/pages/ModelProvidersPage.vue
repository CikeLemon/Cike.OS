<script setup lang="ts">
import type { ModelProviderDto, ModelProviderListDto, TagDto } from '@/api'
import { deleteApiV1ModelProvidersById, getApiV1ModelProvidersById, getApiV1ModelProvidersList, getApiV1TagsList } from '@/api'
import { Pencil, Plus, Trash } from '@lucide/vue'
import { onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import ModelProviderFormDialog from '@/components/model-providers/ModelProviderFormDialog.vue'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime } from '@/lib/format'
import { protocolLabel } from '@/lib/model-provider-protocol'

const providers = ref<ModelProviderListDto[]>([])
const loading = ref(true)

// 标签只有 id 列表，表单里的名称从这里查表回显
const tags = ref<TagDto[]>([])
// 标签列表没加载成功时：编辑框不过滤回填、不允许保存
const tagsReady = ref(false)

const dialogOpen = ref(false)
/** 编辑对象：点编辑时按 Id 取到的详情（连接字段 + 全部模型），null 为新建 */
const editing = ref<ModelProviderDto | null>(null)
const opening = ref(false)

const deleting = ref<ModelProviderListDto | null>(null)
/** 名称单独存：确认框关闭动画期间 deleting 已被清空，直接读它会闪成空串 */
const deletingName = ref('')
const deleteBusy = ref(false)

async function loadTags() {
  const result = await getApiV1TagsList()
  if (result.error) {
    toast.error(getErrorMessage(result))
    return
  }
  tags.value = result.data ?? []
  tagsReady.value = true
}

async function load() {
  loading.value = true
  try {
    const result = await getApiV1ModelProvidersList()
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }
    providers.value = result.data ?? []
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editing.value = null
  dialogOpen.value = true
}

/** 编辑前先按 Id 取详情：表单里是这个提供方此刻最新的连接信息和全部模型，不拿列表行回填。标签列表没就绪时按钮禁用，与保存同一口径 */
async function openEdit(provider: ModelProviderListDto) {
  if (opening.value) return

  opening.value = true
  try {
    const result = await getApiV1ModelProvidersById({ path: { id: provider.id ?? '' } })
    if (result.error || !result.data) {
      toast.error(getErrorMessage(result))
      return
    }
    editing.value = result.data
    dialogOpen.value = true
  } finally {
    opening.value = false
  }
}

async function confirmDelete() {
  const target = deleting.value
  if (!target?.id || deleteBusy.value) return

  deleteBusy.value = true
  try {
    const result = await deleteApiV1ModelProvidersById({ path: { id: target.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('模型提供方已删除。')
    deleting.value = null
    await load()
  } finally {
    deleteBusy.value = false
  }
}

onMounted(() => {
  loadTags()
  load()
})
</script>

<template>
  <div class="flex flex-col gap-4 p-6">
    <!-- 页面工具条：主操作 -->
    <div class="flex flex-wrap items-center gap-2">
      <Button class="ml-auto" @click="openCreate">
        <Plus />
        新建模型提供方
      </Button>
    </div>

    <div class="overflow-hidden rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead class="w-52">名称</TableHead>
            <TableHead class="w-56">协议</TableHead>
            <TableHead class="min-w-64">模型</TableHead>
            <TableHead class="w-44">创建时间</TableHead>
            <TableHead class="w-44">更新时间</TableHead>
            <TableHead class="w-24 text-right">操作</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <template v-if="loading">
            <TableRow v-for="i in 5" :key="i">
              <TableCell colspan="6"><Skeleton class="h-5 w-1/2" /></TableCell>
            </TableRow>
          </template>
          <TableEmpty v-else-if="providers.length === 0" :colspan="6">尚无模型提供方，点击右上角新建。</TableEmpty>
          <TableRow v-for="provider in providers" v-else :key="provider.id">
            <TableCell class="font-medium">{{ provider.name }}</TableCell>
            <TableCell>
              <Badge variant="outline">{{ protocolLabel(provider.protocol) }}</Badge>
            </TableCell>
            <TableCell>
              <!-- 后端已按提供方汇总并按名称排序 -->
              <div v-if="provider.modelNames?.length" class="flex flex-wrap gap-1">
                <Badge v-for="name in provider.modelNames" :key="name" variant="secondary">{{ name }}</Badge>
              </div>
              <span v-else class="text-muted-foreground">—</span>
            </TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(provider.createdAt) }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(provider.updatedAt) }}</TableCell>
            <TableCell class="text-right">
              <div class="flex justify-end gap-1">
                <Button size="icon-sm" variant="ghost" aria-label="编辑" :disabled="opening || !tagsReady" @click="openEdit(provider)">
                  <Pencil />
                </Button>
                <Button size="icon-sm" variant="ghost" aria-label="删除" @click="deleting = provider; deletingName = provider.name ?? ''">
                  <Trash />
                </Button>
              </div>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>

    <ModelProviderFormDialog v-model:open="dialogOpen" :provider="editing" :tags="tags" :tags-ready="tagsReady" @saved="load" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除模型提供方"
      :description="`确定删除模型提供方「${deletingName}」？名下全部模型会一并删除。`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
