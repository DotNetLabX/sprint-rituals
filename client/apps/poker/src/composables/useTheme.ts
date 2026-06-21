import { ref, computed } from 'vue'

const STORAGE_KEY = 'poker:theme'

// Module-level refs so all consumers share the same reactive state
const theme = ref<'dark' | 'light'>(
  (localStorage.getItem(STORAGE_KEY) as 'dark' | 'light') ?? 'dark',
)

export function useTheme() {
  const isDark = computed(() => theme.value === 'dark')

  function toggleTheme() {
    theme.value = theme.value === 'dark' ? 'light' : 'dark'
    document.documentElement.className = theme.value === 'light' ? 'light' : ''
    localStorage.setItem(STORAGE_KEY, theme.value)
  }

  return { theme, isDark, toggleTheme }
}
