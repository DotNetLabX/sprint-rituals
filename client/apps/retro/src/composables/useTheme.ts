import { ref, computed } from 'vue'

const STORAGE_KEY = 'reflekt:theme'

// Module-level refs so all consumers share the same reactive state
const theme = ref<'dark' | 'light'>(
  (localStorage.getItem(STORAGE_KEY) as 'dark' | 'light') ?? 'dark',
)

export function useTheme() {
  const isDark = computed(() => theme.value === 'dark')

  function toggleTheme() {
    theme.value = theme.value === 'dark' ? 'light' : 'dark'
    // dark: no class on <html> (matches :root default)
    // light: add 'light' class (matches .light override block)
    document.documentElement.className = theme.value === 'light' ? 'light' : ''
    localStorage.setItem(STORAGE_KEY, theme.value)
  }

  return { theme, isDark, toggleTheme }
}
