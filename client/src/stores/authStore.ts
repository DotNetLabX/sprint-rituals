import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { AuthUser } from '../types'
import { getMe, logout as apiLogout } from '../api/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthUser | null>(null)
  const loading = ref(false)
  const initialized = ref(false)

  const isAuthenticated = computed(() => user.value !== null)
  const isAdmin = computed(() => user.value?.role === 'Admin')
  const displayName = computed(() => user.value?.displayName ?? '')

  async function fetchMe() {
    loading.value = true
    try {
      user.value = await getMe()
    } catch {
      user.value = null
    } finally {
      loading.value = false
      initialized.value = true
    }
  }

  async function logout() {
    try {
      await apiLogout()
    } catch {
      // ignore errors on logout
    }
    user.value = null
    window.location.href = '/login'
  }

  return {
    user,
    loading,
    initialized,
    isAuthenticated,
    isAdmin,
    displayName,
    fetchMe,
    logout
  }
})
