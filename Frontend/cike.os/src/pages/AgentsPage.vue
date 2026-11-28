<script setup lang="ts">
import type { AgentDetailDto, AgentItemDto, AgentToolPackageOptionItemDto } from '@/api'
import { deleteApiV1AgentsById, getApiV1AgentsById, getApiV1AgentsPagedList, getApiV1AgentsToolOptions, getApiV1UserToolConfigurationsToolOptions } from '@/api'
import { Pencil, Plus, Search, Trash } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import AgentFormDialog from '@/components/agents/AgentFormDialog.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableEmpty, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import TablePagination from '@/components/shared/TablePagination.vue'
import { getErrorMessage } from '@/lib/errors'
import { formatDateTime } from '@/lib/format'

const PAGE_SIZE = 10

const router = useRouter()

const items = ref<AgentItemDto[]>([])
const total = ref(0)
const loading = ref(true)

// keyword 输入后回车 / 点搜索才生效
const keywordInput = ref('')
const keyword = ref('')
const page = ref(1)

// 第二步的选择列表：进页面就取一次（工具包 → 工具，不随 Agent 变）
const toolOptions = ref<AgentToolPackageOptionItemDto[]>([])

const dialogOpen = ref(false)
const editing = ref<AgentDetailDto | null>(null)
const deleting = ref<AgentItemDto | null>(null)
// 关闭动画期间 deleting 已清空，文案里的名称单独留一份，避免闪成空串
const deletingName = ref('')
const deleteBusy = ref(false)
const detailBusy = ref(false)

async function load() {
  loading.value = true
  try {
    const result = await getApiV1AgentsPagedList({
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

async function loadToolOptions() {
  const result = await getApiV1AgentsToolOptions()
  if (result.error) {
    toast.error(getErrorMessage(result))
    return
  }
  toolOptions.value = result.data ?? []
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

async function openEdit(agent: AgentItemDto) {
  // 先按 Id 取详情，成功后才打开对话框；失败 toast 且不打开
  if (!agent.id || detailBusy.value) return
  detailBusy.value = true
  try {
    const result = await getApiV1AgentsById({ path: { id: agent.id } })
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

function openDelete(agent: AgentItemDto) {
  deleting.value = agent
  deletingName.value = agent.name ?? ''
}

async function confirmDelete() {
  const target = deleting.value
  if (!target?.id || deleteBusy.value) return

  deleteBusy.value = true
  try {
    const result = await deleteApiV1AgentsById({ path: { id: target.id } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('Agent 已删除。')
    deleting.value = null
    await load()
  } finally {
    deleteBusy.value = false
  }
}

/** 新建后绑定的工具里有「要填且还没填」参数的 → 跳到「工具参数」并预选；编辑只刷新列表 */
async function onSaved(payload: { created: boolean; name: string; pendingToolIds: string[] }) {
  if (payload.created && payload.pendingToolIds.length > 0) {
    // 已经配过参数的工具不算待配置（否则跳过去预选是空的）
    const options = await getApiV1UserToolConfigurationsToolOptions()
    const configured = new Set((options.data ?? []).filter(x => x.configured).map(x => x.toolId ?? ''))
    const pending = payload.pendingToolIds.filter(id => !configured.has(id))
    if (pending.length > 0) {
      toast.info(`有 ${pending.length} 个工具需要配置参数。`)
      router.push({ path: '/tool-configurations', query: { tools: pending.join(','), from: payload.name } })
      return
    }
  }
  load()
}

const hasFilters = computed(() => Boolean(keyword.value))

onMounted(() => {
  load()
  loadToolOptions()
})
</script>

<template>
  <div class="flex flex-col gap-4 p-6">
    <!-- 页面工具条：搜索 / 主操作 -->
    <div class="flex flex-wrap items-center gap-2">
      <div class="relative w-64">
        <Search class="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
        <Input v-model="keywordInput" placeholder="搜索名称，回车确认" class="h-9 pl-8" autocomplete="off" @keydown.enter="applySearch" />
      </div>
      <Button variant="outline" @click="applySearch">
        <Search />
        搜索
      </Button>
      <Button class="ml-auto" @click="openCreate">
        <Plus />
        新建 Agent
      </Button>
    </div>

    <div class="overflow-hidden rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead class="w-56">名称</TableHead>
            <TableHead>描述</TableHead>
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
            {{ hasFilters ? '没有符合条件的 Agent。' : 'Agent 为空，点击右上角新建。' }}
          </TableEmpty>
          <TableRow v-for="agent in items" v-else :key="agent.id">
            <TableCell class="font-medium">{{ agent.name }}</TableCell>
            <TableCell class="text-muted-foreground max-w-md truncate">{{ agent.description || '—' }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(agent.createdAt) }}</TableCell>
            <TableCell class="text-muted-foreground">{{ formatDateTime(agent.updatedAt) }}</TableCell>
            <TableCell class="text-right">
              <div class="flex justify-end gap-1">
                <Button size="icon-sm" variant="ghost" aria-label="编辑" :disabled="detailBusy" @click="openEdit(agent)">
                  <Pencil />
                </Button>
                <Button size="icon-sm" variant="ghost" aria-label="删除" @click="openDelete(agent)">
                  <Trash />
                </Button>
              </div>
            </TableCell>
          </TableRow>
        </TableBody>
      </Table>
    </div>

    <TablePagination v-model:page="page" :page-size="PAGE_SIZE" :total="total" @update:page="load" />

    <AgentFormDialog v-model:open="dialogOpen" :agent="editing" :tool-options="toolOptions" @saved="onSaved" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除 Agent"
      :description="`确定删除 Agent「${deletingName}」？已有会话仍可查看，但不能再对话。`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
