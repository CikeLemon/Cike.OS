<script setup lang="ts">
import type { ModelProviderDto, ModelProviderEndpointDto } from '@/api'
import {
  deleteApiV1ModelProviderEndpointsById,
  getApiV1ModelProviderEndpointsList,
  getApiV1ModelProvidersById,
} from '@/api'
import { ArrowLeft, Pencil, Plus, Trash } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import EndpointEditSheet from '@/components/model-providers/EndpointEditSheet.vue'
import ModelProviderFormDialog from '@/components/model-providers/ModelProviderFormDialog.vue'
import { endpointModelNames } from '@/components/model-providers/provider-summary'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime } from '@/lib/format'
import { protocolLabel } from '@/lib/model-provider-protocol'

const route = useRoute()
const router = useRouter()

const providerId = computed(() => String(route.params.id ?? ''))

// 提供方只管自己的名称；接入点是独立的聚合，列表按提供方 Id 过滤
const provider = ref<ModelProviderDto | null>(null)
const endpoints = ref<ModelProviderEndpointDto[]>([])
const loading = ref(true)
const notFound = ref(false)

const editNameOpen = ref(false)

const sheetOpen = ref(false)
const editingEndpoint = ref<ModelProviderEndpointDto | null>(null)

const deletingEndpoint = ref<ModelProviderEndpointDto | null>(null)
const deletingEndpointName = ref('')
const deleteEndpointBusy = ref(false)

async function loadProvider() {
  const result = await getApiV1ModelProvidersById({ path: { id: providerId.value } })
  if (result.error) {
    // 提供方不存在与不属于本租户都表现为取不到：详情接口返回 404
    if (result.response?.status === 404) {
      notFound.value = true
      return
    }
    toast.error(getErrorMessage(result))
    return
  }
  provider.value = result.data ?? null
}

async function loadEndpoints() {
  const result = await getApiV1ModelProviderEndpointsList({ query: { modelProviderId: providerId.value } })
  if (result.error) {
    toast.error(getErrorMessage(result))
    return
  }
  endpoints.value = result.data ?? []
}

function openCreateEndpoint() {
  editingEndpoint.value = null
  sheetOpen.value = true
}

function openEditEndpoint(endpoint: ModelProviderEndpointDto) {
  editingEndpoint.value = endpoint
  sheetOpen.value = true
}

async function confirmDeleteEndpoint() {
  const target = deletingEndpoint.value
  if (!target?.id || deleteEndpointBusy.value) return

  deleteEndpointBusy.value = true
  try {
    // 删接入点走它自己的接口：它的映射一并删除，其余接入点不受影响
    const result = await deleteApiV1ModelProviderEndpointsById({ path: { id: target.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('接入点已删除。')
    deletingEndpoint.value = null
    await loadEndpoints()
  } finally {
    deleteEndpointBusy.value = false
  }
}

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([loadProvider(), loadEndpoints()])
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="flex flex-col gap-6 p-6">
    <Button variant="ghost" size="sm" class="-ml-2 w-fit" @click="router.push({ name: 'model-providers' })">
      <ArrowLeft />
      返回模型提供方列表
    </Button>

    <div v-if="loading" class="grid gap-6">
      <Skeleton class="h-24 w-full" />
      <Skeleton class="h-40 w-full" />
    </div>

    <div v-else-if="notFound || !provider" class="text-muted-foreground grid place-items-center gap-2 py-16 text-sm">
      <p>模型提供方不存在或已被删除。</p>
      <Button variant="outline" size="sm" @click="router.push({ name: 'model-providers' })">返回列表</Button>
    </div>

    <template v-else>
      <!-- 详情头部：提供方本身只有名称 -->
      <section class="grid gap-4 rounded-lg border p-5">
        <div class="flex flex-wrap items-start gap-4">
          <div class="grid flex-1 gap-1">
            <h2 class="text-lg font-semibold">{{ provider.name }}</h2>
            <p class="text-muted-foreground text-xs">创建于 {{ formatDateTime(provider.createdAt) }} · 更新于 {{ formatDateTime(provider.updatedAt) }}</p>
          </div>
          <div class="flex gap-2">
            <Button size="sm" variant="outline" @click="editNameOpen = true">
              <Pencil />
              编辑名称
            </Button>
          </div>
        </div>
      </section>

      <!-- 接入点区 -->
      <section class="grid gap-4">
        <div class="flex flex-wrap items-center gap-2">
          <h3 class="text-sm font-semibold">接入点</h3>
          <span class="text-muted-foreground text-sm">共 {{ endpoints.length }} 个</span>
          <Button size="sm" class="ml-auto" @click="openCreateEndpoint">
            <Plus />
            新建接入点
          </Button>
        </div>

        <div class="overflow-hidden rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead class="w-44">名称</TableHead>
                <TableHead class="w-36">协议</TableHead>
                <TableHead class="min-w-56">接入地址</TableHead>
                <TableHead class="min-w-48">支持的模型</TableHead>
                <TableHead class="w-28 text-right">操作</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              <TableEmpty v-if="!endpoints.length" :colspan="5">
                该模型提供方下尚无接入点，点击「新建接入点」添加（协议、接入地址、密钥与模型映射）。
              </TableEmpty>
              <TableRow v-for="endpoint in endpoints" v-else :key="endpoint.id">
                <TableCell class="font-medium">{{ endpoint.name }}</TableCell>
                <TableCell><Badge variant="outline">{{ protocolLabel(endpoint.protocol) }}</Badge></TableCell>
                <TableCell class="text-muted-foreground font-mono text-xs">{{ endpoint.baseUrl || '—' }}</TableCell>
                <TableCell>
                  <div v-if="endpointModelNames(endpoint).length" class="flex flex-wrap gap-1">
                    <Badge v-for="name in endpointModelNames(endpoint)" :key="name" variant="secondary">{{ name }}</Badge>
                  </div>
                  <span v-else class="text-muted-foreground">—</span>
                </TableCell>
                <TableCell class="text-right">
                  <div class="flex justify-end gap-1">
                    <Button size="icon-sm" variant="ghost" aria-label="编辑接入点" @click="openEditEndpoint(endpoint)">
                      <Pencil />
                    </Button>
                    <Button size="icon-sm" variant="ghost" aria-label="删除接入点" @click="deletingEndpoint = endpoint; deletingEndpointName = endpoint.name ?? ''">
                      <Trash />
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            </TableBody>
          </Table>
        </div>
      </section>
    </template>

    <ModelProviderFormDialog v-model:open="editNameOpen" :provider="provider" @saved="loadProvider" />
    <EndpointEditSheet v-model:open="sheetOpen" :provider-id="providerId" :endpoint="editingEndpoint" @saved="loadEndpoints" />

    <DeleteConfirmDialog
      :open="deletingEndpoint !== null"
      title="删除接入点"
      :description="`确定删除接入点「${deletingEndpointName}」？该接入点的模型映射会一并删除。`"
      :loading="deleteEndpointBusy"
      @update:open="deletingEndpoint = null"
      @confirm="confirmDeleteEndpoint"
    />
  </div>
</template>
