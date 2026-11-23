<script setup lang="ts">
import { computed } from 'vue'
import { Moon, Sun } from '@lucide/vue'
import { useRoute } from 'vue-router'
import { Button } from '@/components/ui/button'
import { SidebarTrigger } from '@/components/ui/sidebar'
import { useTheme } from '@/composables/useTheme'

const route = useRoute()
const { theme, toggleTheme } = useTheme()

const title = computed(() => (route.meta.title ?? '').trim())
</script>

<template>
  <header class="border-b flex h-12 shrink-0 items-center gap-2 px-4">
    <SidebarTrigger />
    <h1 class="text-sm font-medium">{{ title }}</h1>
    <Button
      variant="ghost"
      size="icon"
      class="ml-auto"
      @click="toggleTheme"
    >
      <Sun v-if="theme === 'dark'" />
      <Moon v-else />
      <span class="sr-only">切换主题</span>
    </Button>
  </header>
</template>
