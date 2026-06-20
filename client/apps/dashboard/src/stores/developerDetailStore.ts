import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { DeveloperDetailResponse } from '../types'
import { getDeveloperDetail } from '../api/analytics'

export const useDeveloperDetailStore = defineStore('developerDetail', () => {
  const data = ref<DeveloperDetailResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const selectedLast = ref(10)

  async function fetchDetail(accountId: string) {
    loading.value = true
    error.value = null
    try {
      data.value = await getDeveloperDetail(accountId, selectedLast.value)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load developer detail'
    } finally {
      loading.value = false
    }
  }

  async function setSprintRange(accountId: string, last: number) {
    selectedLast.value = last
    await fetchDetail(accountId)
  }

  function reset() {
    data.value = null
    loading.value = false
    error.value = null
    selectedLast.value = 10
  }

  return {
    data,
    loading,
    error,
    selectedLast,
    fetchDetail,
    setSprintRange,
    reset,
  }
})
