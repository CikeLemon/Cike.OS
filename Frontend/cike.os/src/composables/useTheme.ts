import { ref, watch } from 'vue'

const themes = { light: 'light', dark: 'dark' } as const
type Theme = (typeof themes)[keyof typeof themes]

const THEME_STORAGE_KEY = 'cike-os-theme' // 与 index.html 预挂载脚本共用，两处须同步修改

function readInitialTheme(): Theme {
  if (typeof window === 'undefined') {
    return themes.light
  }
  const stored = window.localStorage.getItem(THEME_STORAGE_KEY)
  if (stored === themes.light || stored === themes.dark) {
    return stored
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches
    ? themes.dark
    : themes.light
}

function applyTheme(value: Theme): void {
  if (typeof document === 'undefined') {
    return
  }
  document.documentElement.classList.toggle('dark', value === themes.dark)
}

const theme = ref<Theme>(readInitialTheme())

watch(theme, (value) => {
  applyTheme(value)
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(THEME_STORAGE_KEY, value)
  }
})

/** 应用初始主题（应用挂载前调用一次） */
function initTheme(): void {
  applyTheme(theme.value)
}

/** 两态主题：初始跟随系统，手动切换后本地持久化 */
function useTheme() {
  function toggleTheme(): void {
    theme.value = theme.value === themes.dark ? themes.light : themes.dark
  }

  return { theme, toggleTheme }
}

export { initTheme, themes, useTheme }
export type { Theme }
