/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** 浏览器直连的 API 基地址；留空表示同源（开发态由 Vite 代理 /api） */
  readonly VITE_API_BASE_URL?: string
  /** 仅构建期：Vite dev 代理 /api 的目标，不暴露到客户端 */
  readonly VITE_API_PROXY_TARGET?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
