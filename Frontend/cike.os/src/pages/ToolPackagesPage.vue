<script setup lang="ts">
import type { ToolPackageCategoryDto, ToolPackageDto, ToolPackageStatus } from '@/api'
import { deleteApiV1ToolPackagesById, getApiV1ToolPackageCategoriesList, getApiV1ToolPackagesPagedList } from '@/api'
import { Pencil, Plus, Search, Trash } from '@lucide/vue'
import { computed, onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectGroup, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import TablePagination from '@/components/shared/TablePagination.vue'
import ToolPackageFormDialog from '@/components/tool-packages/ToolPackageFormDialog.vue'
import ToolPackageIcon from '@/components/tool-packages/ToolPackageIcon.vue'
import ToolPackageStatusBadge from '@/components/tool-packages/ToolPackageStatusBadge.vue'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime, truncate } from '@/lib/format'
import { TOOL_PACKAGE_STATUS_OPTIONS } from '@/lib/tool-status'

const ALL = 'all'

const router = useRouter()

const PAGE_SIZE = 10

const categories = ref<ToolPackageCategoryDto[]>([])
const items = ref<ToolPackageDto[]>([])
const total = ref(0)
const loading = ref(true)

// 工具条状态：keyword 输入后回车 / 点搜索才生效，分类与状态即时生效
const keywordInput = ref('')
const keyword = ref('')
const categoryFilter = ref<string>(ALL)
const statusFilter = ref<string>(ALL)
const page = ref(1)

const dialogOpen = ref(false)
const editing = ref<ToolPackageDto | null>(null)
const deleting = ref<ToolPackageDto | null>(null)
const deleteBusy = ref(false)

async function loadCategories() {
  const result = await getApiV1ToolPackageCategoriesList()
  if (result.error) {
    toast.error(getErrorMessage(result))
    return
  }
  categories.value = result.data ?? []
}

async function load() {
  loading.value = true
  try {
    const result = await getApiV1ToolPackagesPagedList({
      query: {
        Keyword: keyword.value || undefined,
        ToolPackageCategoryId: categoryFilter.value === ALL ? undefined : categoryFilter.value,
        Status: statusFilter.value === ALL ? undefined : (Number(statusFilter.value) as ToolPackageStatus),
        Page: page.value,
        PageSize: PAGE_SIZE,
        Sorting: 'UpdatedAt desc',
      },
    })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }
    items.value = result.data?.items ?? []
    total.value = Number(result.data?.total ?? 0)
  } finally {
    loading.value = false
  }
}

function applySearch() {
  keyword.value = keywordInput.value
  page.value = 1
  load()
}

watch([categoryFilter, statusFilter], () => {
  page.value = 1
  load()
})

function openCreate() {
  editing.value = null
  dialogOpen.value = true
}

function openEdit(toolPackage: ToolPackageDto) {
  editing.value = toolPackage
  dialogOpen.value = true
}

/** 新建成功 → 自动进入详情页维护工具；编辑成功 → 刷新列表 */
function onSaved(id: string) {
  if (!editing.value && id) {
    router.push({ name: 'tool-package-detail', params: { id } })
    return
  }
  load()
}

async function confirmDelete() {
  const target = deleting.value
  if (!target?.id || deleteBusy.value) return

  deleteBusy.value = true
  try {
    const result = await deleteApiV1ToolPackagesById({ path: { id: target.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('工具包已删除。')
    deleting.value = null
    await load()
  } finally {
    deleteBusy.value = false
  }
}

const hasFilters = computed(() => Boolean(keyword.value) || categoryFilter.value !== ALL || statusFilter.value !== ALL)

onMounted(() => {
  loadCategories()
  load()
})
</script>

<template>
  <div class="flex flex-col gap-4 p-6">
    <!-- 页面工具条：搜索 / 筛选 / 主操作 -->
    <div class="flex flex-wrap items-center gap-2">
      <div class="relative w-64">
        <Search class="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
        <Input v-model="keywordInput" placeholder="搜索名称或编码，回车确认" class="h-9 pl-8" autocomplete="off" @keydown.enter="applySearch" />
      </div>
      <Select v-model="categoryFilter">
        <SelectTrigger class="w-40">
          <SelectValue placeholder="全部分类" />
        </SelectTrigger>
        <SelectContent>
          <SelectGroup>
            <SelectItem :value="ALL">全部分类</SelectItem>
            <SelectItem v-for="category in categories" :key="category.id" :value="category.id ?? ''">{{ category.name }}</SelectItem>
          </SelectGroup>
        </SelectContent>
      </Select>
      <Select v-model="statusFilter">
        <SelectTrigger class="w-36">
          <SelectValue placeholder="全部状态" />
        </SelectTrigger>
        <SelectContent>
          <SelectGroup>
            <SelectItem :value="ALL">全部状态</SelectItem>
            <SelectItem v-for="option in TOOL_PACKAGE_STATUS_OPTIONS" :key="option.value" :value="String(option.value)">{{ option.label }}</SelectItem>
          </SelectGroup>
        </SelectContent>
      </Select>
      <Button variant="outline" @click="applySearch">
        <Search />
        搜索
      </Button>
      <Button class="ml-auto" @click="openCreate">
        <Plus />
        新建工具包
      </Button>
    </div>

    <div class="overflow-hidden rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead class="w-64">工具包</TableHead>
            <TableHead class="w-40">编码</TableHead>
            <TableHead class="w-32">分类</TableHead>
            <TableHead class="w-24">状态</TableHead>
            <TableHead>描述</TableHead>
            <TableHead class="w-44">创建时间</TableHead>
            <TableHead class="w-44">更新时间</TableHead>
            <TableHead class="w-28 text-right">操作</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <template v-if="loading">
            <TableRow v-for="i in 5" :key="i">
              <TableCell colspan="8"><Skeleton class="h-5 w-1/2" /></TableCell>
            </TableRow>
          </template>
          <TableEmpty v-else-if="items.length === 0" :colspan="8">
            {{ hasFilters ? '没有符合条件的工具包。' : '尚无工具包，点击右上角新建。' }}
          </TableEmpty>
          <TableRow v-for="toolPackage in items" v-else :key="toolPackage.id" class="cursor-pointer" @click="router.push({ name: 'tool-package-detail', params: { id: toolPackage.id } })">
            <TableCell>
              <div class="flex items-center gap-3">
                <ToolPackageIcon :icon="toolPackage.icon" />
                <span class="font-medium">{{ toolPackage.name }}</span>
              </div>
            </TableCell>
            <TableCell class="font-mono text-xs">{{ toolPackage.code }}</TableCell>
            <TableCell>{{ toolPackage.toolPackageCategoryName || '—' }}</TableCell>
            <TableCell><ToolPackageStatusBadge :status="toolPackage.status" /></TableCell>
            <TableCell class="text-muted-foreground max-w-xs truncate">{{ truncate(toolPackage.description, 60) }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(toolPackage.createdAt) }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(toolPackage.updatedAt) }}</TableCell>
            <TableCell class="text-right" @click.stop>
              <div class="flex justify-end gap-1">
                <Button size="icon-sm" variant="ghost" aria-label="编辑" @click="openEdit(toolPackage)">
                  <Pencil />
                </Button>
                <Button size="icon-sm" variant="ghost" aria-label="删除" @click="deleting = toolPackage">
                  <Trash />
                </Button>
              </div>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>

    <TablePagination v-model:page="page" :page-size="PAGE_SIZE" :total="total" @update:page="load" />

    <ToolPackageFormDialog v-model:open="dialogOpen" :categories="categories" :tool-package="editing" @saved="onSaved" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除工具包"
      :description="`确定删除工具包「${deleting?.name ?? ''}」？工具包下仍有工具时无法删除。`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
