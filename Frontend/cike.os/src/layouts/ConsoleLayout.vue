<script setup lang="ts">
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar'
import AppSidebar from './AppSidebar.vue'
import ContentHeader from './ContentHeader.vue'
</script>

<template>
  <SidebarProvider>
    <AppSidebar />
    <SidebarInset class="h-svh overflow-hidden">
      <ContentHeader />
      <div class="flex-1 overflow-y-auto">
        <RouterView />
      </div>
    </SidebarInset>
  </SidebarProvider>
</template>
