<script setup lang="ts">
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from '@lucide/vue'
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
} from '@/components/ui/pagination'
import { cn } from '@/lib/utils'

const props = withDefaults(
  defineProps<{
    page: number
    pageSize: number
    total: number
    class?: string
  }>(),
  { class: '' },
)

const emit = defineEmits<{ 'update:page': [value: number] }>()

function onPageChange(value: number) {
  if (value !== props.page && value >= 1 && value <= Math.max(1, Math.ceil(props.total / props.pageSize))) {
    emit('update:page', value)
  }
}
</script>

<template>
  <div v-if="total > pageSize" :class="cn('flex items-center justify-between gap-4', props.class)">
    <p class="text-muted-foreground text-sm">共 {{ total }} 条</p>
    <Pagination
      :page="page"
      :total="total"
      :items-per-page="pageSize"
      :sibling-count="1"
      show-edges
      @update:page="onPageChange"
    >
      <PaginationContent v-slot="{ items }">
        <PaginationItem :value="1">
          <button type="button" class="hover:bg-accent hover:text-accent-foreground inline-flex size-8 items-center justify-center rounded-md" @click="onPageChange(1)">
            <ChevronsLeft />
            <span class="sr-only">首页</span>
          </button>
        </PaginationItem>
        <PaginationItem :value="page - 1">
          <button type="button" class="hover:bg-accent hover:text-accent-foreground inline-flex size-8 items-center justify-center rounded-md" @click="onPageChange(page - 1)">
            <ChevronLeft />
            <span class="sr-only">上一页</span>
          </button>
        </PaginationItem>
        <template v-for="(item, index) in items" :key="index">
          <PaginationItem v-if="item.type === 'page'" :value="item.value" :is-active="item.value === page">
            <button
              type="button"
              :class="cn(
                'inline-flex size-8 items-center justify-center rounded-md text-sm',
                item.value === page ? 'bg-primary text-primary-foreground' : 'hover:bg-accent hover:text-accent-foreground',
              )"
              @click="onPageChange(item.value)"
            >
              {{ item.value }}
            </button>
          </PaginationItem>
          <PaginationEllipsis v-else :index="index">
            <span class="inline-flex size-8 items-center justify-center">…</span>
          </PaginationEllipsis>
        </template>
        <PaginationItem :value="page + 1">
          <button type="button" class="hover:bg-accent hover:text-accent-foreground inline-flex size-8 items-center justify-center rounded-md" @click="onPageChange(page + 1)">
            <ChevronRight />
            <span class="sr-only">下一页</span>
          </button>
        </PaginationItem>
        <PaginationItem :value="Math.ceil(total / pageSize)">
          <button type="button" class="hover:bg-accent hover:text-accent-foreground inline-flex size-8 items-center justify-center rounded-md" @click="onPageChange(Math.ceil(total / pageSize))">
            <ChevronsRight />
            <span class="sr-only">末页</span>
          </button>
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  </div>
</template>
