<script setup lang="ts">
import type { ModelProviderDto } from '@/api'
import { deleteApiV1ModelProvidersById, getApiV1ModelProvidersList } from '@/api'
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
import { providerTypeLabel } from '@/lib/model-provider-type'

const providers = ref<ModelProviderDto[]>([])
const loading = ref(true)

const dialogOpen = ref(false)
const editing = ref<ModelProviderDto | null>(null)
const deleting = ref<ModelProviderDto | null>(null)
const deleteBusy = ref(false)

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

function openEdit(provider: ModelProviderDto) {
  editing.value = provider
  dialogOpen.value = true
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

onMounted(load)
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
            <TableHead class="w-44">名称</TableHead>
            <TableHead class="w-32">类型</TableHead>
            <TableHead class="min-w-64">模型映射</TableHead>
            <TableHead>接入地址</TableHead>
            <TableHead class="w-44">更新时间</TableHead>
            <TableHead class="w-28 text-right">操作</TableHead>
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
            <TableCell><Badge variant="outline">{{ providerTypeLabel(provider.setting?.type) }}</Badge></TableCell>
            <TableCell>
              <!-- 一个模型只映射一次，以 modelId 作 key；分数越高越先用 -->
              <div v-if="provider.mappings?.length" class="flex flex-wrap gap-1">
                <Badge v-for="mapping in provider.mappings" :key="mapping.modelId" variant="secondary">
                  {{ mapping.modelDisplayName }} · {{ mapping.priority }}
                </Badge>
              </div>
              <span v-else class="text-muted-foreground">—</span>
            </TableCell>
            <TableCell class="text-muted-foreground font-mono text-xs">{{ provider.setting?.baseUrl || '—' }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(provider.updatedAt) }}</TableCell>
            <TableCell class="text-right">
              <div class="flex justify-end gap-1">
                <Button size="icon-sm" variant="ghost" aria-label="编辑" @click="openEdit(provider)">
                  <Pencil />
                </Button>
                <Button size="icon-sm" variant="ghost" aria-label="删除" @click="deleting = provider">
                  <Trash />
                </Button>
              </div>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>

    <ModelProviderFormDialog v-model:open="dialogOpen" :provider="editing" @saved="load" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除模型提供方"
      :description="`确定删除模型提供方「${deleting?.name ?? ''}」？它名下的全部映射会一并删除。`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
