<script setup lang="ts">
import type { ToolDto, ToolPackageCategoryDto, ToolPackageDto } from '@/api'
import { deleteApiV1ToolPackagesById, deleteApiV1ToolsById, getApiV1ToolPackageCategoriesList, getApiV1ToolPackagesById, getApiV1ToolsList } from '@/api'
import { ArrowLeft, Pencil, Plus, Search, Trash } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import ToolPackageFormDialog from '@/components/tool-packages/ToolPackageFormDialog.vue'
import ToolPackageIcon from '@/components/tool-packages/ToolPackageIcon.vue'
import ToolPackageStatusBadge from '@/components/tool-packages/ToolPackageStatusBadge.vue'
import ToolEditSheet from '@/components/tools/ToolEditSheet.vue'
import { filterToolsByKeyword } from '@/lib/filter'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime } from '@/lib/format'
import { TOOL_TYPE_LABELS, type ToolTypeValue } from '@/lib/tool-type'

const route = useRoute()
const router = useRouter()

const packageId = computed(() => String(route.params.id ?? ''))

const toolPackage = ref<ToolPackageDto | null>(null)
const categories = ref<ToolPackageCategoryDto[]>([])
const tools = ref<ToolDto[]>([])
const loading = ref(true)
const notFound = ref(false)

const keyword = ref('')
const filteredTools = computed(() => filterToolsByKeyword(tools.value, keyword.value))

const editPackageOpen = ref(false)
const deletingPackage = ref(false)
const toolSheetOpen = ref(false)
const editingTool = ref<ToolDto | null>(null)
const deletingTool = ref<ToolDto | null>(null)
const deleteToolBusy = ref(false)
const deletePackageBusy = ref(false)

const readmeOpen = ref(false)

async function loadPackage() {
  const result = await getApiV1ToolPackagesById({ path: { id: packageId.value } })
  if (result.error) {
    if (result.response?.status === 404) {
      notFound.value = true
      return
    }
    toast.error(getErrorMessage(result))
    return
  }
  toolPackage.value = result.data ?? null
}

async function loadCategories() {
  const result = await getApiV1ToolPackageCategoriesList()
  if (result.error) {
    toast.error(getErrorMessage(result))
    return
  }
  categories.value = result.data ?? []
}

async function loadTools() {
  const result = await getApiV1ToolsList({ query: { toolPackageId: packageId.value } })
  if (result.error) {
    toast.error(getErrorMessage(result))
    return
  }
  tools.value = result.data ?? []
}

async function loadAll() {
  loading.value = true
  try {
    await Promise.all([loadPackage(), loadCategories(), loadTools()])
  } finally {
    loading.value = false
  }
}

function openCreateTool() {
  editingTool.value = null
  toolSheetOpen.value = true
}

function openEditTool(tool: ToolDto) {
  editingTool.value = tool
  toolSheetOpen.value = true
}

async function onToolSaved() {
  await loadTools()
}

async function confirmDeleteTool() {
  const target = deletingTool.value
  if (!target?.id || deleteToolBusy.value) return

  deleteToolBusy.value = true
  try {
    const result = await deleteApiV1ToolsById({ path: { id: target.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('工具已删除。')
    deletingTool.value = null
    await loadTools()
  } finally {
    deleteToolBusy.value = false
  }
}

async function confirmDeletePackage() {
  if (deletePackageBusy.value) return

  deletePackageBusy.value = true
  try {
    const result = await deleteApiV1ToolPackagesById({ path: { id: packageId.value } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('工具包已删除。')
    router.push({ name: 'tool-packages' })
  } finally {
    deletePackageBusy.value = false
  }
}

function typeLabel(type: ToolTypeValue | undefined): string {
  return (type != null && TOOL_TYPE_LABELS[type]) || '未知'
}

onMounted(loadAll)
</script>

<template>
  <div class="flex flex-col gap-6 p-6">
    <Button variant="ghost" size="sm" class="-ml-2 w-fit" @click="router.push({ name: 'tool-packages' })">
      <ArrowLeft />
      返回工具包列表
    </Button>

    <div v-if="loading" class="grid gap-6">
      <Skeleton class="h-24 w-full" />
      <Skeleton class="h-40 w-full" />
    </div>

    <div v-else-if="notFound || !toolPackage" class="text-muted-foreground grid place-items-center gap-2 py-16 text-sm">
      <p>工具包不存在或已被删除。</p>
      <Button variant="outline" size="sm" @click="router.push({ name: 'tool-packages' })">返回列表</Button>
    </div>

    <template v-else>
      <!-- 详情头部 -->
      <section class="grid gap-4 rounded-lg border p-5">
        <div class="flex flex-wrap items-start gap-4">
          <ToolPackageIcon :icon="toolPackage.icon" :size="40" />
          <div class="grid flex-1 gap-1">
            <div class="flex flex-wrap items-center gap-3">
              <h2 class="text-lg font-semibold">{{ toolPackage.name }}</h2>
              <code class="text-muted-foreground bg-muted rounded px-1.5 py-0.5 font-mono text-xs">{{ toolPackage.code }}</code>
              <ToolPackageStatusBadge :status="toolPackage.status" />
              <Badge variant="outline">{{ toolPackage.toolPackageCategoryName || '未分类' }}</Badge>
            </div>
            <p v-if="toolPackage.description" class="text-muted-foreground text-sm">{{ toolPackage.description }}</p>
            <p class="text-muted-foreground text-xs">创建于 {{ formatDateTime(toolPackage.createdAt) }} · 更新于 {{ formatDateTime(toolPackage.updatedAt) }}</p>
          </div>
          <div class="flex gap-2">
            <Button size="sm" variant="outline" @click="editPackageOpen = true">
              <Pencil />
              编辑
            </Button>
            <Button size="sm" variant="outline" @click="deletingPackage = true">
              <Trash />
              删除
            </Button>
          </div>
        </div>
        <div v-if="toolPackage.readme" class="grid gap-2">
          <button type="button" class="text-muted-foreground text-xs hover:text-foreground w-fit" @click="readmeOpen = !readmeOpen">
            {{ readmeOpen ? '收起 Readme' : '展开 Readme' }}
          </button>
          <pre v-if="readmeOpen" class="bg-muted text-foreground max-h-80 overflow-auto rounded-md p-4 font-mono text-xs whitespace-pre-wrap">{{ toolPackage.readme }}</pre>
        </div>
      </section>

      <!-- 工具区 -->
      <section class="grid gap-4">
        <div class="flex flex-wrap items-center gap-2">
          <h3 class="text-sm font-semibold">工具</h3>
          <span class="text-muted-foreground text-sm">共 {{ tools.length }} 个</span>
          <div class="relative ml-4 w-60">
            <Search class="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
            <Input v-model="keyword" placeholder="按编码 / 名称 / 描述过滤" class="h-9 pl-8" autocomplete="off" />
          </div>
          <Button size="sm" class="ml-auto" @click="openCreateTool">
            <Plus />
            新建工具
          </Button>
        </div>

        <div class="overflow-hidden rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead class="w-48">编码</TableHead>
                <TableHead class="w-48">名称</TableHead>
                <TableHead class="w-32">形态</TableHead>
                <TableHead class="w-20">参数数</TableHead>
                <TableHead>描述</TableHead>
                <TableHead class="w-28 text-right">操作</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              <TableEmpty v-if="filteredTools.length === 0" :colspan="6">
                {{ tools.length === 0 ? '该工具包下尚无工具，点击右上角新建。' : '没有符合条件的工具。' }}
              </TableEmpty>
              <TableRow v-for="tool in filteredTools" v-else :key="tool.id">
                <TableCell class="font-mono text-xs">{{ tool.code }}</TableCell>
                <TableCell class="font-medium">{{ tool.name }}</TableCell>
                <TableCell><Badge variant="secondary">{{ typeLabel(tool.type) }}</Badge></TableCell>
                <TableCell class="text-muted-foreground">{{ tool.parameters?.length ?? 0 }}</TableCell>
                <TableCell class="text-muted-foreground max-w-xs truncate">{{ tool.description || '—' }}</TableCell>
                <TableCell class="text-right">
                  <div class="flex justify-end gap-1">
                    <Button size="icon-sm" variant="ghost" aria-label="编辑工具" @click="openEditTool(tool)">
                      <Pencil />
                    </Button>
                    <Button size="icon-sm" variant="ghost" aria-label="删除工具" @click="deletingTool = tool">
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

    <ToolPackageFormDialog v-model:open="editPackageOpen" :categories="categories" :tool-package="toolPackage" @saved="loadAll" />
    <ToolEditSheet v-model:open="toolSheetOpen" :tool-package-id="packageId" :tool="editingTool" @saved="onToolSaved" />

    <DeleteConfirmDialog
      :open="deletingTool !== null"
      title="删除工具"
      :description="`确定删除工具「${deletingTool?.name ?? ''}」？其用户工具配置将一并失效。`"
      :loading="deleteToolBusy"
      @update:open="deletingTool = null"
      @confirm="confirmDeleteTool"
    />
    <DeleteConfirmDialog
      :open="deletingPackage"
      title="删除工具包"
      :description="`确定删除工具包「${toolPackage?.name ?? ''}」？工具包下仍有工具时无法删除。`"
      :loading="deletePackageBusy"
      @update:open="deletingPackage = false"
      @confirm="confirmDeletePackage"
    />
  </div>
</template>
