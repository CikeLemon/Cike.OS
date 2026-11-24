import type { Component } from 'vue'
import type { RouteMeta } from 'vue-router'

export interface NavItem {
  label: string
  path: string
  /** 来自路由 meta.icon；未配置时由视图层兜底 */
  icon?: Component
}

interface RouteLike {
  path: string
  meta?: RouteMeta
}

/** 由路由表推导侧栏一级导航：只收录带 nav 标志的路由，按声明顺序输出，图标透传自 meta.icon */
export function deriveNavItems(routes: readonly RouteLike[]): NavItem[] {
  return routes
    .filter(route => route.meta?.nav === true)
    .map(route => ({
      label: route.meta?.title ?? '',
      path: route.path,
      icon: route.meta?.icon,
    }))
}
