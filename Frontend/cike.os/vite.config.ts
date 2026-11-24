import path from 'node:path'
import { defineConfig, loadEnv } from 'vite'
import tailwindcss from '@tailwindcss/vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  // 第三个参数 '' 表示不加前缀地加载全部变量；VITE_* 会同时暴露给客户端代码
  const env = loadEnv(mode, __dirname, '')
  const proxyTarget = env.VITE_API_PROXY_TARGET

  return {
    plugins: [vue(), tailwindcss()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      // 只在配置了代理目标时启用；目标来自 .env.[mode] 的 VITE_API_PROXY_TARGET
      proxy: proxyTarget
        ? { '/api': { target: proxyTarget, changeOrigin: true } }
        : undefined,
    },
  }
})
