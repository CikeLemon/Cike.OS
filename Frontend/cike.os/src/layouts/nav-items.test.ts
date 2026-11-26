import { describe, expect, it } from 'vitest'
import { defineComponent, type Component } from 'vue'
import { pageRoutes } from '@/router/routes'
import { deriveNavItems } from './nav-items'

const iconA = defineComponent({ name: 'IconA' })
const iconB = defineComponent({ name: 'IconB' })

describe('deriveNavItems', () => {
  it('只收录带 nav 标志的路由，按声明顺序输出，title / icon 透传自 meta', () => {
    const routes = [
      { path: '/a', meta: { title: '甲', nav: true, icon: iconA as Component } },
      { path: '/', redirect: '/a' },
      { path: '/:pathMatch(.*)*', meta: { title: '页面未找到' } },
      { path: '/b', meta: { title: '乙', nav: true, icon: iconB as Component } },
      { path: '/c', meta: { title: '丙' } },
    ]

    expect(deriveNavItems(routes)).toEqual([
      { label: '甲', path: '/a', icon: iconA },
      { label: '乙', path: '/b', icon: iconB },
    ])
  })

  it('真实路由表推导出八个模块，每个都带图标，详情页不进侧栏', () => {
    const items = deriveNavItems(pageRoutes)

    expect(items.map(({ label, path }) => ({ label, path }))).toEqual([
      { label: '对话', path: '/conversations' },
      { label: 'Agent', path: '/agents' },
      { label: '模型', path: '/models' },
      { label: '模型提供方', path: '/model-providers' },
      { label: '工具包', path: '/tool-packages' },
      { label: '工具包分类', path: '/tool-package-categories' },
      { label: '标签', path: '/tags' },
      { label: '设置', path: '/settings' },
    ])
    expect(items.every(item => item.icon != null)).toBe(true)
  })
})
