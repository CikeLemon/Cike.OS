import { describe, expect, it } from 'vitest'
import { pageRoutes } from '@/router/routes'
import { deriveNavItems } from './nav-items'

describe('deriveNavItems', () => {
  it('只收录带 nav 标志的路由，按声明顺序输出，title 传入 label', () => {
    const routes = [
      { path: '/a', meta: { title: '甲', nav: true } },
      { path: '/', redirect: '/a' },
      { path: '/:pathMatch(.*)*', meta: { title: '页面未找到' } },
      { path: '/b', meta: { title: '乙', nav: true } },
      { path: '/c', meta: { title: '丙' } },
    ]

    expect(deriveNavItems(routes)).toEqual([
      { label: '甲', path: '/a' },
      { label: '乙', path: '/b' },
    ])
  })

  it('真实路由表推导出六个模块，顺序与路由表一致', () => {
    expect(deriveNavItems(pageRoutes)).toEqual([
      { label: '对话', path: '/conversations' },
      { label: 'Agent', path: '/agents' },
      { label: '模型', path: '/models' },
      { label: '工具', path: '/tools' },
      { label: '标签', path: '/tags' },
      { label: '设置', path: '/settings' },
    ])
  })
})
