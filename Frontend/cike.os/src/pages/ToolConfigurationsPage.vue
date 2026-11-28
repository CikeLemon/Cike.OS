<script setup lang="ts">
import type { UserToolConfigurationDetailDto, UserToolConfigurationItemDto } from '@/api'
import { deleteApiV1UserToolConfigurationsById, getApiV1UserToolConfigurationsById, getApiV1UserToolConfigurationsPagedList } from '@/api'
import { Plus, Search, X } from '@lucide/vue'
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import ToolConfigurationBatchDialog from '@/components/tool-configurations/ToolConfigurationBatchDialog.vue'
import ToolConfigurationCard from '@/components/tool-configurations/ToolConfigurationCard.vue'
import ToolConfigurationEditDialog from '@/components/tool-configurations/ToolConfigurationEditDialog.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import DeleteConfirmDialog from '@/components/shared/DeleteConfirmDialog.vue'
import TablePagination from '@/components/shared/TablePagination.vue'
import { getErrorMessage } from '@/lib/errors'

const PAGE_SIZE = 12

const route = useRoute()
const router = useRouter()

const items = ref<UserToolConfigurationItemDto[]>([])
const total = ref(0)
const loading = ref(true)

// keyword 输入后回车 / 点搜索才生效
const keywordInput = ref('')
const keyword = ref('')
const page = ref(1)

// 编辑对象：点编辑时按工具 Id 取到的详情（带参数定义与已存值），null 为关闭
const editing = ref<UserToolConfigurationDetailDto | null>(null)
const editOpen = ref(false)
const detailBusy = ref(false)
const batchOpen = ref(false)
const deleting = ref<UserToolConfigurationItemDto | null>(null)
// 关闭动画期间 deleting 已清空，文案里的名称单独留一份，避免闪成空串
const deletingName = ref('')
const deleteBusy = ref(false)

/** 来自 Agent 新建的跳转：/tool-configurations?tools=1,2,3&from=Agent名 */
const pendingFromAgent = computed(() => {
  const tools = (route.query.tools as string | undefined) ?? ''
  const ids = tools
    .split(',')
    .map(x => x.trim())
    .filter(Boolean)
  return ids.length > 0 ? { from: (route.query.from as string | undefined) ?? '', toolIds: ids } : null
})

async function load() {
  loading.value = true
  try {
    const result = await getApiV1UserToolConfigurationsPagedList({
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

function dismissBanner() {
  router.replace({ path: route.path })
}

/** 批量保存后刷新；来自 Agent 的提示条随之收掉（否则刷新会再次弹出） */
function onBatchSaved() {
  if (pendingFromAgent.value) router.replace({ path: route.path })
  load()
}

/** 编辑前先按工具 Id 取详情（参数定义以工具此刻的为准），成功后才打开弹窗；失败 toast 且不打开 */
async function openEdit(item: UserToolConfigurationItemDto) {
  if (!item.toolId || detailBusy.value) return
  detailBusy.value = true
  try {
    const result = await getApiV1UserToolConfigurationsById({ path: { id: item.toolId } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }
    editing.value = result.data ?? null
    editOpen.value = true
  } finally {
    detailBusy.value = false
  }
}

function openDelete(item: UserToolConfigurationItemDto) {
  deleting.value = item
  deletingName.value = item.toolName ?? ''
}

async function confirmDelete() {
  const target = deleting.value
  if (!target?.id || deleteBusy.value) return

  deleteBusy.value = true
  try {
    // 现有 Delete 接口的 {id} 语义是工具 Id（与 Get / Save 一致），不是配置 Id
    const result = await deleteApiV1UserToolConfigurationsById({ path: { id: target.toolId ?? '' } })
    if (result.error) {
      toast.error(getErrorMessage(result))
      return
    }

    toast.success('参数配置已删除。')
    deleting.value = null
    await load()
  } finally {
    deleteBusy.value = false
  }
}

const hasFilters = computed(() => Boolean(keyword.value))

onMounted(() => {
  load()
  // 从 Agent 跳过来：直接打开批量新增并预选
  if (pendingFromAgent.value) batchOpen.value = true
})
</script>

<template>
  <div class="flex flex-col gap-4 p-6">
    <!-- 来自 Agent 的提示条 -->
    <div v-if="pendingFromAgent" class="bg-muted text-muted-foreground flex items-center gap-3 rounded-lg border px-4 py-2.5 text-sm">
      <span>
        来自 Agent「{{ pendingFromAgent.from }}」：{{ pendingFromAgent.toolIds.length }} 个工具待配置
      </span>
      <Button size="sm" variant="outline" @click="batchOpen = true">立即配置</Button>
      <Button size="icon-sm" variant="ghost" aria-label="关闭提示" class="ml-auto" @click="dismissBanner">
        <X />
      </Button>
    </div>

    <!-- 页面工具条：搜索 / 主操作 -->
    <div class="flex flex-wrap items-center gap-2">
      <div class="relative w-64">
        <Search class="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
        <Input v-model="keywordInput" placeholder="搜索工具名称，回车确认" class="h-9 pl-8" autocomplete="off" @keydown.enter="applySearch" />
      </div>
      <Button variant="outline" @click="applySearch">
        <Search />
        搜索
      </Button>
      <Button class="ml-auto" @click="batchOpen = true">
        <Plus />
        新增配置
      </Button>
    </div>

    <!-- 卡片网格：已配置的工具每个一张 -->
    <div v-if="loading" class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      <Skeleton v-for="i in 6" :key="i" class="h-40 rounded-xl" />
    </div>
    <div v-else-if="items.length === 0" class="text-muted-foreground rounded-lg border border-dashed p-10 text-center text-sm">
      {{ hasFilters ? '没有符合条件的配置。' : '还没有配置过任何工具的参数，点击右上角「新增配置」。' }}
    </div>
    <div v-else class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      <ToolConfigurationCard v-for="item in items" :key="item.id" :item="item" :edit-busy="detailBusy" @edit="openEdit" @remove="openDelete" />
    </div>

    <TablePagination v-model:page="page" :page-size="PAGE_SIZE" :total="total" @update:page="load" />

    <ToolConfigurationEditDialog v-model:open="editOpen" :item="editing" @saved="load" />
    <ToolConfigurationBatchDialog v-model:open="batchOpen" :preselected-tool-ids="pendingFromAgent?.toolIds" @saved="onBatchSaved" />
    <DeleteConfirmDialog
      :open="deleting !== null"
      title="删除参数配置"
      :description="`确定删除「${deletingName}」的参数配置？使用它的 Agent 在补齐参数前无法调用该工具。`"
      :loading="deleteBusy"
      @update:open="deleting = null"
      @confirm="confirmDelete"
    />
  </div>
</template>
