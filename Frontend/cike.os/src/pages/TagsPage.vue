<script setup lang="ts">
import type { TagDetailDto, TagItemDto } from '@/api'
import { deleteApiV1TagsById, getApiV1TagsById, getApiV1TagsPagedList } from '@/api'
import { Pencil, Plus, Search, Trash } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import TablePagination from '@/components/shared/TablePagination.vue'
import TagFormDialog from '@/components/tags/TagFormDialog.vue'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime } from '@/lib/format'

const PAGE_SIZE = 10

const items = ref<TagItemDto[]>([])
const total = ref(0)
const loading = ref(true)

// keyword 输入后回车 / 点搜索才生效
const keywordInput = ref('')
const keyword = ref('')
const page = ref(1)

const dialogOpen = ref(false)
const editing = ref<TagDetailDto | null>(null)
const deleting = ref<TagItemDto | null>(null)
// 关闭动画期间 deleting 已清空，文案里的名称单独留一份，避免闪成空串
const deletingName = ref('')
const deleteBusy = ref(false)
const detailBusy = ref(false)

async function load() {
  loading.value = true
  try {
    const result = await getApiV1TagsPagedList({
      query: {
        keyword: keyword.value || undefined,
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

function openCreate() {
  editing.value = null
  dialogOpen.value = true
}

/** 编辑前先按 Id 取详情回填表单，不拿列表行；失败 toast 且不打开 */
async function openEdit(tag: TagItemDto) {
  if (!tag.id || detailBusy.value) return
  detailBusy.value = true
  try {
    const result = await getApiV1TagsById({ path: { id: tag.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }
    editing.value = result.data ?? null
    dialogOpen.value = true
  } finally {
    detailBusy.value = false
  }
}

function openDelete(tag: TagItemDto) {
  deleting.value = tag
  deletingName.value = tag.name ?? ''
}

async function confirmDelete() {
  const target = deleting.value
  if (!target?.id || deleteBusy.value) return

  deleteBusy.value = true
  try {
    const result = await deleteApiV1TagsById({ path: { id: target.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('标签已删除。')
    deleting.value = null
    await load()
  } finally {
    deleteBusy.value = false
  }
}

const hasFilters = computed(() => Boolean(keyword.value))

onMounted(load)
</script>

<template>
  <div class="flex flex-col gap-4 p-6">
    <!-- 页面工具条：搜索 / 主操作 -->
    <div class="flex flex-wrap items-center gap-2">
      <div class="relative w-64">
        <Search class="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
        <Input v-model="keywordInput" placeholder="搜索键或名称，回车确认" class="h-9 pl-8" autocomplete="off" @keydown.enter="applySearch" />
      </div>
      <Button variant="outline" @click="applySearch">
        <Search />
        搜索
      </Button>
      <Button class="ml-auto" @click="openCreate">
        <Plus />
        新建标签
      </Button>
    </div>

    <div class="overflow-hidden rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead class="w-56">键</TableHead>
            <TableHead>名称</TableHead>
            <TableHead class="w-44">创建时间</TableHead>
            <TableHead class="w-44">更新时间</TableHead>
            <TableHead class="w-28 text-right">操作</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <template v-if="loading">
            <TableRow v-for="i in 5" :key="i">
              <TableCell colspan="5"><Skeleton class="h-5 w-1/2" /></TableCell>
            </TableRow>
          </template>
          <TableEmpty v-else-if="items.length === 0" :colspan="5">
            {{ hasFilters ? '没有符合条件的标签。' : '标签为空，点击右上角新建。' }}
          </TableEmpty>
          <TableRow v-for="tag in items" v-else :key="tag.id">
            <TableCell class="font-mono text-xs">{{ tag.key }}</TableCell>
            <TableCell class="font-medium">{{ tag.name }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(tag.createdAt) }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(tag.updatedAt) }}</TableCell>
            <TableCell class="text-right">
              <div class="flex justify-end gap-1">
                <Button size="icon-sm" variant="ghost" aria-label="编辑" :disabled="detailBusy" @click="openEdit(tag)">
                  <Pencil />
                </Button>
                <Button size="icon-sm" variant="ghost" aria-label="删除" @click="openDelete(tag)">
                  <Trash />
                </Button>
              </div>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>

    <TablePagination v-model:page="page" :page-size="PAGE_SIZE" :total="total" @update:page="load" />

    <TagFormDialog v-model:open="dialogOpen" :tag="editing" @saved="load" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除标签"
      :description="`确定删除标签「${deletingName}」？引用它的模型将不再显示该标签。`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
