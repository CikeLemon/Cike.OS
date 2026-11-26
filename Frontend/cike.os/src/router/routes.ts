import type { Component } from 'vue'
import type { RouteRecordRaw } from 'vue-router'
import { Bot, Cpu, FolderOpen, MessageSquare, Package, Server, Settings, Tag } from '@lucide/vue'

declare module 'vue-router' {
  interface RouteMeta {
    /** 页面标题：内容头部与浏览器标签页共用的单一来源 */
    title?: string
    /** 是否出现在侧栏一级导航 */
    nav?: boolean
    /** 侧栏导航图标（仅 nav 路由需要） */
    icon?: Component
    /** 页面使用的布局组件；未指定走默认布局 */
    layout?: Component
  }
}

/** 页面路由（未包裹布局）；导航、标题与图标的单一来源 */
const pageRoutes: RouteRecordRaw[] = [
  {
    path: '/conversations',
    component: () => import('@/pages/ConversationsPage.vue'),
    meta: { title: '对话', nav: true, icon: MessageSquare },
  },
  {
    path: '/agents',
    component: () => import('@/pages/AgentsPage.vue'),
    meta: { title: 'Agent', nav: true, icon: Bot },
  },
  {
    name: 'models',
    path: '/models',
    component: () => import('@/pages/ModelsPage.vue'),
    meta: { title: '模型', nav: true, icon: Cpu },
  },
  {
    name: 'model-providers',
    path: '/model-providers',
    component: () => import('@/pages/ModelProvidersPage.vue'),
    meta: { title: '模型提供方', nav: true, icon: Server },
  },
  {
    name: 'model-provider-detail',
    path: '/model-providers/:id',
    component: () => import('@/pages/ModelProviderDetailPage.vue'),
    meta: { title: '模型提供方详情' },
  },
  {
    name: 'tool-packages',
    path: '/tool-packages',
    component: () => import('@/pages/ToolPackagesPage.vue'),
    meta: { title: '工具包', nav: true, icon: Package },
  },
  {
    name: 'tool-package-detail',
    path: '/tool-packages/:id',
    component: () => import('@/pages/ToolPackageDetailPage.vue'),
    meta: { title: '工具包详情' },
  },
  {
    path: '/tool-package-categories',
    component: () => import('@/pages/ToolPackageCategoriesPage.vue'),
    meta: { title: '工具包分类', nav: true, icon: FolderOpen },
  },
  {
    path: '/tags',
    component: () => import('@/pages/TagsPage.vue'),
    meta: { title: '标签', nav: true, icon: Tag },
  },
  {
    path: '/settings',
    component: () => import('@/pages/SettingsPage.vue'),
    meta: { title: '设置', nav: true, icon: Settings },
  },
  {
    path: '/:pathMatch(.*)*',
    component: () => import('@/pages/NotFoundPage.vue'),
    meta: { title: '页面未找到' },
  },
]

/** 按布局注册体系装配路由：相邻且同布局的页面归入同一布局父路由 */
function buildRoutes(pages: RouteRecordRaw[], defaultLayout: Component): RouteRecordRaw[] {
  const routes: RouteRecordRaw[] = [{ path: '/', redirect: '/conversations' }]
  let current: { layout: Component; children: RouteRecordRaw[] } | undefined
  for (const page of pages) {
    const layout = page.meta?.layout ?? defaultLayout
    if (!current || current.layout !== layout) {
      current = { layout, children: [] }
      routes.push({ path: '/', component: layout, children: current.children })
    }
    current.children.push(page)
  }
  return routes
}

export { buildRoutes, pageRoutes }
