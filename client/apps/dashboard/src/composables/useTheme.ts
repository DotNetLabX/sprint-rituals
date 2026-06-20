import { ref } from 'vue'

const STORAGE_KEY = 'fokus-theme'
const isDark = ref(true)

function applyTheme(dark: boolean) {
  const html = document.documentElement
  if (dark) {
    html.classList.add('dark')
    html.classList.remove('light')
  } else {
    html.classList.add('light')
    html.classList.remove('dark')
  }
}

export function useTheme() {
  function initTheme() {
    const stored = localStorage.getItem(STORAGE_KEY)
    const dark = stored !== null ? stored === 'dark' : true
    isDark.value = dark
    applyTheme(dark)
  }

  function toggleTheme() {
    isDark.value = !isDark.value
    applyTheme(isDark.value)
    localStorage.setItem(STORAGE_KEY, isDark.value ? 'dark' : 'light')
  }

  return { isDark, initTheme, toggleTheme }
}
