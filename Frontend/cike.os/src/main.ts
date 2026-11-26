import { createApp } from 'vue'
import 'vue-sonner/style.css'
import './style.css'
import App from './App.vue'
import router from './router'
import { client } from '@/api/client.gen'
import { initTheme } from '@/composables/useTheme'

initTheme()

// dev 环境 VITE_API_BASE_URL 留空走 Vite proxy；生产由环境变量提供基地址
client.setConfig({ baseUrl: import.meta.env.VITE_API_BASE_URL ?? '' })

createApp(App).use(router).mount('#app')
