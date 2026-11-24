<script setup lang="ts">
import { Package } from '@lucide/vue'
import { computed, ref, watch } from 'vue'

/** Icon 为 http(s) 地址时渲染图片，其余（lucide 名 / 空）回退到 Package 图标 */
const props = withDefaults(
  defineProps<{
    icon?: string | null
    size?: number
  }>(),
  { icon: '', size: 20 },
)

const failed = ref(false)
watch(() => props.icon, () => { failed.value = false })

const imageUrl = computed(() => {
  const value = (props.icon ?? '').trim()
  return /^https?:\/\//i.test(value) ? value : null
})
</script>

<template>
  <img
    v-if="imageUrl && !failed"
    :src="imageUrl"
    alt=""
    :style="{ width: `${size}px`, height: `${size}px` }"
    class="shrink-0 rounded-md object-contain"
    @error="failed = true"
  >
  <Package v-else :style="{ width: `${size}px`, height: `${size}px` }" class="text-muted-foreground shrink-0" />
</template>
