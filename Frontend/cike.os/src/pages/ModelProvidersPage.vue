<script setup lang="ts">
import type { ModelProviderListDto } from '@/api'
import { deleteApiV1ModelProvidersById, getApiV1ModelProvidersList } from '@/api'
import { Plus, Trash } from '@lucide/vue'
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
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

const router = useRouter()

const providers = ref<ModelProviderListDto[]>([])
const loading = ref(true)

const dialogOpen = ref(false)
const deleting = ref<ModelProviderListDto | null>(null)
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
  dialogOpen.value = true
}

/** 新建成功后直接进入详情页添加接入点 */
function onCreated(id: string) {
  if (id) {
    router.push({ name: 'model-provider-detail', params: { id } })
    return
  }
  load()
}

function openDetail(provider: ModelProviderListDto) {
  router.push({ name: 'model-provider-detail', params: { id: provider.id ?? '' } })
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
            <TableHead class="w-52">名称</TableHead>
            <TableHead class="w-56">协议</TableHead>
            <TableHead class="min-w-64">支持的模型</TableHead>
            <TableHead class="w-24">接入点</TableHead>
            <TableHead class="w-44">更新时间</TableHead>
            <TableHead class="w-20 text-right">操作</TableHead>
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
            <TableCell class="font-medium">
              <button type="button" class="hover:underline" @click="openDetail(provider)">{{ provider.name }}</button>
            </TableCell>
            <TableCell>
              <!-- 后端已按提供方汇总：协议去重、按枚举顺序 -->
              <div v-if="provider.protocols?.length" class="flex flex-wrap gap-1">
                <Badge v-for="protocol in provider.protocols" :key="protocol" variant="outline">{{ protocolLabel(protocol) }}</Badge>
              </div>
              <span v-else class="text-muted-foreground">—</span>
            </TableCell>
            <TableCell>
              <!-- 跨接入点按模型去重，分数高的在前 -->
              <div v-if="provider.modelDisplayNames?.length" class="flex flex-wrap gap-1">
                <Badge v-for="name in provider.modelDisplayNames" :key="name" variant="secondary">{{ name }}</Badge>
              </div>
              <span v-else class="text-muted-foreground">—</span>
            </TableCell>
            <TableCell class="text-muted-foreground">{{ provider.endpointCount ?? 0 }} 个</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(provider.updatedAt) }}</TableCell>
            <TableCell class="text-right">
              <Button size="icon-sm" variant="ghost" aria-label="删除" @click="deleting = provider">
                <Trash />
              </Button>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>

    <ModelProviderFormDialog v-model:open="dialogOpen" :provider="null" @saved="onCreated" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除模型提供方"
      :description="`确定删除模型提供方「${deleting?.name ?? ''}」？它名下的全部接入点与模型映射会一并删除。`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
