## 技术栈

| 维度 | 选型 |
|---|---|
| 框架 | Vue 3.5（`<script setup lang="ts">` 单文件组件） |
| 语言 | TypeScript ~6.0（严格，见下） |
| 构建 | Vite 8 |
| 样式 | Tailwind CSS v4（`@tailwindcss/vite`，**CSS-first，无 `tailwind.config.js`**） + `tw-animate-css` |
| 组件库 | shadcn-vue（style `new-york`，baseColor `zinc`，font `geist-sans`，CSS 变量），生成到 `src/components/ui/` |
| 图标 | lucide（`@lucide/vue`） |
| 包管理器 | **pnpm**（非 npm/yarn） |
| 类型检查 | `vue-tsc`（随 `pnpm build` 运行） |

## 常用命令

均在本目录（`Frontend/cike.os`）执行：

```bash
pnpm install          # 安装依赖
pnpm dev              # 开发服务器（Vite，默认 http://localhost:5173）
pnpm build            # 类型检查 + 生产构建（vue-tsc -b && vite build）
pnpm preview          # 预览构建产物
pnpm exec vue-tsc -b  # 只跑类型检查（不构建）
pnpm gen:api          # 从后端 swagger.json 重新生成 API 层（需后端 localhost:5209 在跑）
```

新增依赖用 `pnpm add` / `pnpm add -D`；新增 shadcn-vue 组件见下方专节。**目前没有 `lint` 和 `test` 脚本。**

## 目录结构

```
src/
├─ main.ts              # 应用入口（createApp + mount；含生成 client 的 baseUrl 配置）
├─ App.vue              # 根组件
├─ style.css            # Tailwind v4 入口 + 主题变量（@theme / @layer base）
├─ api/                 # API 层生成物（@hey-api/openapi-ts，pnpm gen:api 重新生成）——勿手改：
│                       #   sdk.gen.ts（每个端点一个类型化函数）/ types.gen.ts（DTO 类型）/ client.gen.ts（HTTP client）等
├─ components/          # 业务组件（自己写）
│  ├─ HelloWorld.vue    # 脚手架遗留示例
│  └─ ui/               # shadcn-vue 生成物——勿手改，见专节（当前尚未 add 任何组件）
├─ lib/
│  ├─ errors.ts         # getErrorMessage()：从 400 响应体提取中文错误文案
│  └─ utils.ts          # cn()：clsx + tailwind-merge 合并类名
├─ assets/              # 图片等静态资源
└─ composables/         # 组合式函数（约定目录，按需惰性创建）
```

### API 调用约定

组件**禁止手写 URL 直调 HTTP 客户端**，一律从 `@/api` 导入生成函数：

```ts
import { getApiV1AppSystemsPagedList } from '@/api'
import type { AppSystemDto } from '@/api'
import { getErrorMessage } from '@/lib/errors'

const { data, error } = await getApiV1AppSystemsPagedList({
  query: { keyword, page, pageSize },
})
if (error) toast.error(getErrorMessage(error))
```

- 后端契约变更后跑 `pnpm gen:api` 重新生成，不手改 `src/api/` 生成物。
- 函数名由后端路由拼接而来（后端 swagger 无 operationId），如 `putApiV1PermissionsById`。
- dev 环境 `VITE_API_BASE_URL` 留空走 Vite proxy，生产由环境变量提供基地址（`main.ts` 里 `client.setConfig`）。

`components.json` 里的路径别名：`@/components`、`@/components/ui`、`@/lib`、`@/lib/utils`、`@/composables`。

## 编码约定

以下为当前配置确立的硬性约定，新代码必须遵循：

- **导入用 `@` 别名**：`@` → `src`（`vite.config.ts` + `tsconfig`）。统一 `@/lib/utils`、`@/components/...`，不要写 `../../` 多级相对路径。
- **`<script setup lang="ts">`**：所有组件用组合式 API + `<script setup>`，不写 Options API。
- **TypeScript 严格**（`tsconfig.app.json` + 基座 `@vue/tsconfig/tsconfig.dom.json`）：
  - `noUnusedLocals` / `noUnusedParameters`：不留未使用的变量、参数。
  - `erasableSyntaxOnly`：**禁用需要运行时代码的 TS 语法**——不能用 `enum`、`namespace`、构造函数参数属性（`constructor(private x)`）。枚举语义改用 `const` 对象 + 字面量联合类型（`type X = 'a' | 'b'`）表达。
  - `verbatimModuleSyntax`：仅类型导入必须写 `import type { ... }`。
  - `noFallthroughCasesInSwitch`：`switch` 每个 case 必须 break/return。
  - 避免 `any` 和无谓的 `as` 断言，优先补全真实类型。
- **样式用 Tailwind v4**：主题令牌（颜色/字体等）在 `style.css` 的 `@theme` 中以 CSS 变量定义，**没有 `tailwind.config.js`**，改主题去改 `style.css`。多来源类名合并统一走 `cn()`。暗色模式基于 `.dark` 类（`@custom-variant dark`）。
- **组件优先 shadcn-vue**：需要通用 UI（按钮/表格/对话框等）先 `add` 官方组件再在业务层组合，不重复造轮子。图标统一用 lucide。

## 组件约定

- **基础组件靠 CLI 生成**：`src/components/ui/` 下的组件一律用 `pnpm dlx shadcn-vue@latest add <component>` 引入（见下节），禁止从零手写基础组件；CLI 的 style / baseColor / 路径别名以 `components.json` 为准。
- **`ui/` 与业务组件分层**：业务组件放 `src/components/`（`ui/` 之外），文件名 PascalCase 且与组件名一致；对 `ui/` 组件的定制若被多处复用，封装成业务包装组件，不在 `ui/` 生成物上反复打补丁。
- **组件命名模式**：表单类独立成 `*Form.vue`，对话框类独立成 `*Dialog.vue` / `*Modal.vue`（确认框用 shadcn-vue 的 `AlertDialog`），不内联在页面或其它组件里。
- **图标只用 `@lucide/vue`**：本项目依赖的是 `@lucide/vue`，禁止引入 `lucide-vue-next` 或其它图标库（两者包名 / API 不同，混用会重复打包）。

## 配色约束（zinc 中性色板）

- **色板基调**：全局用 shadcn-vue `zinc` 中性灰（`style.css` 里的 OKLCH 变量，hue≈285、低 chroma），唯一高饱和色是 `destructive`（红，仅用于危险/删除操作）。不要引入这之外的色相或品牌色。
- **一律用语义 token，禁止硬编码颜色**：组件里用 `bg-background` / `text-foreground` / `bg-primary` / `text-primary-foreground` / `bg-secondary` / `bg-muted` / `text-muted-foreground` / `bg-accent` / `border-border` / `bg-destructive` / `ring-ring` 等语义类；**禁止** `bg-[#1a1a1a]`、`text-zinc-500`、`bg-gray-100` 这类任意值或 Tailwind 原始调色板类——它们不跟随主题、暗色模式下会失真。图表用 `--chart-1..5`，侧栏用 `--sidebar-*` 令牌；只能用 `style.css` 已定义的语义 token，禁止臆造或引用未定义的 token。
- **业务样式只管布局与组合**：业务组件的 class 只负责布局（间距 / 排列 / 尺寸），不改 `ui/` 组件的圆角 / 边框 / 阴影 / 配色等外观底座。
- **改色只改 `style.css`**：调色板/主题色统一在 `style.css` 的 `:root`（亮色）与 `.dark`（暗色）里改 CSS 变量，一处生效全局；不要在组件里散落颜色定义。
- **暗色是一等公民**：新增任何自定义颜色变量，必须同时在 `:root` 和 `.dark` 给出对应值，避免只配亮色。

## 新增 shadcn-vue 组件

```bash
pnpm dlx shadcn-vue@latest add <component>   # 例：add button
```

组件生成到 `src/components/ui/`。这些是生成物：轻量一次性微调可接受，但**被多处复用的定制应封装成业务包装组件**（放 `src/components/`），不要在 `ui/` 上反复打补丁。

## 尚未搭建（不要臆测存在，需引入前先问用户）

- **路由**：未安装 vue-router，目前只有单页 `App.vue`。
- **状态管理**：未安装 Pinia。
- **测试**：无单元测试 / e2e 框架，无 `test` 脚本。
- **Lint / 格式化**：未配置 ESLint / Prettier，无 `lint` 脚本。

引入以上任何一项（即新增任何 pnpm 依赖）**前必须先询问用户**，不要擅自安装或搭建脚手架。将来引入时，目录命名建议对齐社区惯例（`pages/`（或 `views/`）、`layouts/`、`router/`、`stores/`、`composables/`），但这只是约定，不代表当前已存在。

## 禁止事项（前端特有）

通用禁止事项（不擅自引入依赖、不扩大范围、不破坏测试、不隐藏问题）见根 `AGENTS.md`，对全部子项目生效。本节只列前端特有：

- **不在生成物上反复打补丁**：`src/components/ui/` 下的 shadcn-vue 组件为生成代码，复用型定制走业务层包装（见「组件约定」）。
