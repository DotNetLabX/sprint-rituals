import { defineStore } from 'pinia'
import { ref } from 'vue'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import type { HubConnection } from '@microsoft/signalr'
import type { DeveloperProgressResponse } from '../types'
import { getDeveloperProgress } from '../api/analytics'
import { useDevelopersStore } from './developersStore'

export const useDailyProgressStore = defineStore('dailyProgress', () => {
  const data = ref<DeveloperProgressResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const signalRConnected = ref(false)

  let connection: HubConnection | null = null

  async function fetchProgress(subTeam?: string | null) {
    loading.value = true
    error.value = null
    try {
      data.value = await getDeveloperProgress(subTeam ?? undefined)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load daily progress'
    } finally {
      loading.value = false
    }
  }

  async function startSignalR() {
    if (connection && connection.state !== HubConnectionState.Disconnected) return

    connection = new HubConnectionBuilder()
      .withUrl('/hubs/sprint')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('SprintSynced', async () => {
      const developersStore = useDevelopersStore()
      await fetchProgress(developersStore.selectedSubTeam)
    })

    connection.onclose(() => {
      signalRConnected.value = false
    })

    connection.onreconnected(() => {
      signalRConnected.value = true
    })

    try {
      await connection.start()
      signalRConnected.value = true
    } catch (e) {
      signalRConnected.value = false
      // SignalR connection failure is non-fatal — tab still shows data from the last fetch
    }
  }

  async function stopSignalR() {
    if (connection) {
      try {
        await connection.stop()
      } catch {
        // Ignore stop errors
      }
      connection = null
    }
    signalRConnected.value = false
  }

  return {
    data,
    loading,
    error,
    signalRConnected,
    fetchProgress,
    startSignalR,
    stopSignalR,
  }
})
