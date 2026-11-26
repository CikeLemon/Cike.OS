<script setup lang="ts">
import type { ModelDto, TagDto } from '@/api'
import { deleteApiV1ModelsById, getApiV1ModelsPagedList, getApiV1TagsList } from '@/api'
import { Pencil, Plus, Search, Trash } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import ModelFormDialog from '@/components/models/ModelFormDialog.vue'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import TablePagination from '@/components/shared/TablePagination.vue'
import { getErrorMessage } from '@/lib/errors'
import { truncate } from '@/lib/format'

const PAGE_SIZE = 10

// 标签只有 id 列表，名称从这里查表回显
const tags = ref<TagDto[]>([])
const tagNameById = computed(() => new Map(tags.value.map(tag => [tag.id ?? '', tag.name ?? ''])))
const items = ref<ModelDto[]>([])
const total = ref(0)
const loading = ref(true)

// keyword 输入后回车 / 点搜索才生效
const keywordInput = ref('')
const keyword = ref('')
const page = ref(1)

const dialogOpen = ref(false)
const editing = ref<ModelDto | null>(null)
const deleting = ref<ModelDto | null>(null)
const deleteBusy = ref(false)

async function loadTags() {
  const result = await getApiV1TagsList()
  if (result.error) {
    toast.error(getErrorMessage(result))
    return
  }
  tags.value = result.data ?? []
}

async function load() {
  loading.value = true
  try {
    const result = await getApiV1ModelsPagedList({
      query: {
        Keyword: keyword.value || undefined,
        Page: page.value,
        PageSize: PAGE_SIZE,
        Sorting: 'DisplayName asc',
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

function openEdit(model: ModelDto) {
  editing.value = model
  dialogOpen.value = true
}

async function confirmDelete() {
  const target = deleting.value
  if (!target?.id || deleteBusy.value) return

  deleteBusy.value = true
  try {
    const result = await deleteApiV1ModelsById({ path: { id: target.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('模型已删除。')
    deleting.value = null
    await load()
  } finally {
    deleteBusy.value = false
  }
}

const hasFilters = computed(() => Boolean(keyword.value))

onMounted(() => {
  loadTags()
  load()
})
</script>

<template>
  <div class="flex flex-col gap-4 p-6">
    <!-- 页面工具条：搜索 / 主操作 -->
    <div class="flex flex-wrap items-center gap-2">
      <div class="relative w-64">
        <Search class="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
        <Input v-model="keywordInput" placeholder="搜索模型名称或显示名称，回车确认" class="h-9 pl-8" autocomplete="off" @keydown.enter="applySearch" />
      </div>
      <Button variant="outline" @click="applySearch">
        <Search />
        搜索
      </Button>
      <Button class="ml-auto" @click="openCreate">
        <Plus />
        新建模型
      </Button>
    </div>

    <div class="overflow-hidden rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead class="w-64">模型</TableHead>
            <TableHead class="w-64">标签</TableHead>
            <TableHead>描述</TableHead>
            <TableHead class="w-28 text-right">操作</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <template v-if="loading">
            <TableRow v-for="i in 5" :key="i">
              <TableCell colspan="4"><Skeleton class="h-5 w-1/2" /></TableCell>
            </TableRow>
          </template>
          <TableEmpty v-else-if="items.length === 0" :colspan="4">
            {{ hasFilters ? '没有符合条件的模型。' : '模型目录为空，点击右上角新建。' }}
          </TableEmpty>
          <TableRow v-for="model in items" v-else :key="model.id">
            <TableCell>
              <div class="grid gap-0.5">
                <span class="font-medium">{{ model.displayName }}</span>
                <code class="text-muted-foreground font-mono text-xs">{{ model.name }}</code>
              </div>
            </TableCell>
            <TableCell>
              <div class="flex flex-wrap gap-1">
                <Badge v-for="tagId in model.tagIds ?? []" :key="tagId" variant="secondary">{{ tagNameById.get(tagId) ?? '未知标签' }}</Badge>
                <span v-if="(model.tagIds?.length ?? 0) === 0" class="text-muted-foreground">—</span>
              </div>
            </TableCell>
            <TableCell class="text-muted-foreground max-w-md truncate">{{ truncate(model.description, 80) }}</TableCell>
            <TableCell class="text-right">
              <div class="flex justify-end gap-1">
                <Button size="icon-sm" variant="ghost" aria-label="编辑" @click="openEdit(model)">
                  <Pencil />
                </Button>
                <Button size="icon-sm" variant="ghost" aria-label="删除" @click="deleting = model">
                  <Trash />
                </Button>
              </div>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>

    <TablePagination v-model:page="page" :page-size="PAGE_SIZE" :total="total" @update:page="load" />

    <ModelFormDialog v-model:open="dialogOpen" :tags="tags" :model="editing" @saved="load" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除模型"
      :description="`确定删除模型「${deleting?.displayName ?? ''}」？仍有提供商映射时无法删除。`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
