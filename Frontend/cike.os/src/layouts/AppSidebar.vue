<script setup lang="ts">
import { Circle } from '@lucide/vue'
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
// 导航项（标签、路径、图标）全部由路由表推导，这里不维护任何静态映射
const navItems = deriveNavItems(pageRoutes)

// 详情页（/tool-packages/:id）归属其列表菜单，按路径前缀判定激活
function isActive(path: string): boolean {
  return route.path === path || route.path.startsWith(`${path}/`)
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
                :is-active="isActive(item.path)"
                :as-child="true"
              >
                <RouterLink :to="item.path">
                  <component :is="item.icon ?? Circle" />
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
