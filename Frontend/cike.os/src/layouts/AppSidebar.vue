<script setup lang="ts">
import type { Component } from 'vue'
import { Bot, Circle, Cpu, MessageSquare, Settings, Tag, Wrench } from '@lucide/vue'
import { useRoute } from 'vue-router'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/components/ui/sidebar'
import { pageRoutes } from '@/router/routes'
import { deriveNavItems } from './nav-items'

const route = useRoute()
const navItems = deriveNavItems(pageRoutes)

// 图标是视图层关注点，按模块路径映射；路由表只管标题与路径
const moduleIcons: Record<string, Component> = {
  '/conversations': MessageSquare,
  '/agents': Bot,
  '/models': Cpu,
  '/tools': Wrench,
  '/tags': Tag,
  '/settings': Settings,
}

function iconFor(path: string): Component {
  return moduleIcons[path] ?? Circle
}
</script>

<template>
  <Sidebar collapsible="icon">
    <SidebarHeader>
      <SidebarMenu>
        <SidebarMenuItem>
          <SidebarMenuButton size="lg">
            <div class="bg-primary text-primary-foreground flex size-8 shrink-0 items-center justify-center rounded-lg text-sm font-bold">
              C
            </div>
            <span class="truncate text-base font-semibold">Cike.OS</span>
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>
    </SidebarHeader>
    <SidebarContent>
      <SidebarGroup>
        <SidebarGroupContent>
          <SidebarMenu>
            <SidebarMenuItem v-for="item in navItems" :key="item.path">
              <SidebarMenuButton
                :tooltip="item.label"
                :is-active="route.path === item.path"
                :as-child="true"
              >
                <RouterLink :to="item.path">
                  <component :is="iconFor(item.path)" />
                  <span>{{ item.label }}</span>
                </RouterLink>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroupContent>
      </SidebarGroup>
    </SidebarContent>
    <!-- 空 footer 槽位：留给将来的用户菜单（权限提案后填充） -->
    <SidebarFooter />
  </Sidebar>
</template>
