import { computed } from 'vue'
import { useReadinessStore } from '../stores/readiness'
import { useParticipantStore } from '../stores/participant'
import { getHubConnection } from './useRetroHub'

export function useReadiness() {
  const readinessStore = useReadinessStore()
  const participantStore = useParticipantStore()

  async function toggleReady(retroSessionId: number) {
    const connection = getHubConnection()
    if (!connection) return
    await connection.invoke('ToggleReady', retroSessionId)
  }

  const isCurrentParticipantReady = computed(() => {
    const currentId = participantStore.currentParticipant?.id
    if (currentId === undefined) return false
    return readinessStore.isReady(currentId)
  })

  const readyCount = computed(() => readinessStore.readyCount)
  const totalCount = computed(() => readinessStore.totalCount)

  const readyParticipants = computed(() =>
    participantStore.participants
      .filter((p) => readinessStore.readyParticipantIds.includes(p.id))
      .map((p) => p.displayName),
  )

  const notReadyParticipants = computed(() =>
    participantStore.participants
      .filter((p) => !readinessStore.readyParticipantIds.includes(p.id))
      .map((p) => p.displayName),
  )

  return {
    toggleReady,
    isCurrentParticipantReady,
    readyCount,
    totalCount,
    readyParticipants,
    notReadyParticipants,
  }
}
