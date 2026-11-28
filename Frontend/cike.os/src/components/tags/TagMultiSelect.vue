<script setup lang="ts">
import type { TagItemDto } from '@/api'
import { Check, ChevronsUpDown } from '@lucide/vue'
import { computed, ref } from 'vue'
import { Button } from '@/components/ui/button'
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '@/components/ui/command'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'

const props = withDefaults(
  defineProps<{
    /** 可选项：平台标签列表 */
    tags: TagItemDto[]
    disabled?: boolean
    placeholder?: string
    /** 列表为空时的提示（如「暂无可选标签」或「标签列表加载失败」） */
    emptyText?: string
    invalid?: boolean
  }>(),
  { disabled: false, placeholder: '选择标签', emptyText: '暂无可选标签', invalid: false },
)

/** 选中的标签 Id 列表 */
const model = defineModel<string[]>({ default: () => [] })

const open = ref(false)

/** 触发按钮上显示已选标签的名称，用顿号连成一行（名称由父级查表）；表格行内不放徽标，免得撑高行 */
const selectedText = computed(() =>
  model.value.map(id => props.tags.find(tag => tag.id === id)?.name).filter((name): name is string => Boolean(name)).join('、'),
)

function toggle(id: string) {
  model.value = model.value.includes(id) ? model.value.filter(x => x !== id) : [...model.value, id]
}
</script>

<template>
  <Popover v-model:open="open">
    <PopoverTrigger as-child>
      <Button
        type="button"
        variant="outline"
        role="combobox"
        :aria-expanded="open"
        :aria-invalid="invalid"
        :disabled="disabled || tags.length === 0"
        class="w-full justify-between px-3 font-normal"
      >
        <span v-if="selectedText" class="truncate">{{ selectedText }}</span>
        <span v-else class="text-muted-foreground truncate">{{ tags.length === 0 ? emptyText : placeholder }}</span>
        <ChevronsUpDown class="ml-2 size-4 shrink-0 opacity-50" />
      </Button>
    </PopoverTrigger>
    <PopoverContent class="p-0" align="start" :style="{ width: 'var(--reka-popover-trigger-width)' }">
      <Command>
        <CommandInput placeholder="搜索标签…" />
        <CommandList>
          <CommandEmpty>没有匹配的标签。</CommandEmpty>
          <CommandGroup>
            <CommandItem v-for="tag in tags" :key="tag.id" :value="tag.name ?? ''" @select="toggle(tag.id ?? '')">
              <Check :class="['mr-2 size-4', model.includes(tag.id ?? '') ? 'opacity-100' : 'opacity-0']" />
              {{ tag.name }}
            </CommandItem>
          </CommandGroup>
        </CommandList>
      </Command>
    </PopoverContent>
  </Popover>
</template>
