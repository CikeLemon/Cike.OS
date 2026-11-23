import { createRouter, createWebHistory } from 'vue-router'
import ConsoleLayout from '@/layouts/ConsoleLayout.vue'
import { buildRoutes, pageRoutes } from './routes'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: buildRoutes(pageRoutes, ConsoleLayout),
})

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} · Cike.OS` : 'Cike.OS'
})

export default router
