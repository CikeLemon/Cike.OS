<script setup lang="ts">
import type { ToolPackageCategoryDto } from '@/api'
import { deleteApiV1ToolPackageCategoriesById, getApiV1ToolPackageCategoriesList } from '@/api'
import { Pencil, Plus, Trash } from '@lucide/vue'
import { onMounted, ref } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import ToolPackageCategoryFormDialog from '@/components/tool-package-categories/ToolPackageCategoryFormDialog.vue'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime } from '@/lib/format'

const categories = ref<ToolPackageCategoryDto[]>([])
const loading = ref(true)
const dialogOpen = ref(false)
const editing = ref<ToolPackageCategoryDto | null>(null)
const deleting = ref<ToolPackageCategoryDto | null>(null)
const deleteBusy = ref(false)

async function load() {
  loading.value = true
  try {
    const result = await getApiV1ToolPackageCategoriesList()
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }
    categories.value = result.data ?? []
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editing.value = null
  dialogOpen.value = true
}

function openEdit(category: ToolPackageCategoryDto) {
  editing.value = category
  dialogOpen.value = true
}

async function confirmDelete() {
  const target = deleting.value
  if (!target?.id || deleteBusy.value) return

  deleteBusy.value = true
  try {
    const result = await deleteApiV1ToolPackageCategoriesById({ path: { id: target.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('分类已删除。')
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
    <!-- 页面工具条：说明 + 主操作 -->
    <div class="flex items-center justify-between gap-4">
      <p class="text-muted-foreground text-sm">共 {{ categories.length }} 个分类；分类下仍有工具包时不可删除。</p>
      <Button @click="openCreate">
        <Plus />
        新建分类
      </Button>
    </div>

    <div class="overflow-hidden rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead class="w-48">名称</TableHead>
            <TableHead>创建时间</TableHead>
            <TableHead>更新时间</TableHead>
            <TableHead class="w-28 text-right">操作</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <template v-if="loading">
            <TableRow v-for="i in 3" :key="i">
              <TableCell colspan="4"><Skeleton class="h-5 w-1/3" /></TableCell>
            </TableRow>
          </template>
          <TableEmpty v-else-if="categories.length === 0" :colspan="4">尚无分类，点击右上角新建。</TableEmpty>
          <TableRow v-for="category in categories" v-else :key="category.id">
            <TableCell class="font-medium">{{ category.name }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(category.createdAt) }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(category.updatedAt) }}</TableCell>
            <TableCell class="text-right">
              <div class="flex justify-end gap-1">
                <Button size="icon-sm" variant="ghost" aria-label="重命名" @click="openEdit(category)">
                  <Pencil />
                </Button>
                <Button size="icon-sm" variant="ghost" aria-label="删除" @click="deleting = category">
                  <Trash />
                </Button>
              </div>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>

    <ToolPackageCategoryFormDialog v-model:open="dialogOpen" :category="editing" @saved="load" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除分类"
      :description="`确定删除分类「${deleting?.name ?? ''}」？`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
