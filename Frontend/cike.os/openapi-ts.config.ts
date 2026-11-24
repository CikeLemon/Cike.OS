import { defineConfig } from '@hey-api/openapi-ts'
import { loadEnv } from 'vite'

// swagger 来源按环境区分（只有 development / production 两套 .env）：
//   OPENAPI_MODE   读哪份 .env.[mode]，默认 development（本地起后端生成）
//   OPENAPI_INPUT  直接指定 swagger.json 完整地址，优先级最高
// 否则用该环境的 VITE_API_PROXY_TARGET（开发）或 VITE_API_BASE_URL（生产）拼 /swagger/v1/swagger.json
const mode = process.env.OPENAPI_MODE ?? 'development'
const env = loadEnv(mode, process.cwd(), '')
const apiRoot = env.VITE_API_PROXY_TARGET || env.VITE_API_BASE_URL
const input = process.env.OPENAPI_INPUT ?? (apiRoot ? `${apiRoot.replace(/\/$/, '')}/swagger/v1/swagger.json` : '')

if (!input) {
  throw new Error(
    `openapi-ts: 无法确定 swagger 地址。请设置 OPENAPI_INPUT，或在 .env.${mode} 中配置 VITE_API_PROXY_TARGET / VITE_API_BASE_URL。`,
  )
}

export default defineConfig({
  input,
  output: 'src/api',
  plugins: ['@hey-api/typescript', '@hey-api/sdk', '@hey-api/client-fetch'],
})
