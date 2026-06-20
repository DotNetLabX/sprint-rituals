import { defineStore } from 'pinia'
import { useGuestStore } from './guest'

export interface PreviousActionItem {
  cardId: number
  text: string
  assigneeName: string | null
  reviewStatus: string
  previousRetroSessionId: number
  previousRetroSessionName: string
}

export const usePreviousActionsStore = defineStore('previousActions', {
  state: () => ({
    items: [] as PreviousActionItem[],
    loading: false,
    previousRetroSessionName: null as string | null,
    previousRetroSessionId: null as number | null,
  }),
  getters: {
    totalCount: (state): number => state.items.length,
    reviewedCount: (state): number =>
      state.items.filter((i) => i.reviewStatus !== 'None').length,
    unreviewedCount: (state): number =>
      state.items.filter((i) => i.reviewStatus === 'None').length,
    allReviewed: (state): boolean =>
      state.items.length > 0 && state.items.every((i) => i.reviewStatus !== 'None'),
    summaryCounts: (state) => ({
      done: state.items.filter((i) => i.reviewStatus === 'Done').length,
      carriedOver: state.items.filter((i) => i.reviewStatus === 'CarriedOver').length,
      dropped: state.items.filter((i) => i.reviewStatus === 'Dropped').length,
    }),
  },
  actions: {
    async fetchPreviousActions(retroSessionId: number): Promise<void> {
      this.loading = true
      try {
        const guestStore = useGuestStore()
        const headers: Record<string, string> = {}
        if (guestStore.token) headers['X-Guest-Token'] = guestStore.token
        const res = await fetch(`/api/retro-sessions/${retroSessionId}/previous-actions`, {
          headers,
          credentials: 'include',
        })
        if (!res.ok) {
          this.items = []
          return
        }
        const data = (await res.json()) as PreviousActionItem[]
        this.items = data
        const first = data[0]
        if (data.length > 0 && first !== undefined) {
          this.previousRetroSessionId = first.previousRetroSessionId
          this.previousRetroSessionName = first.previousRetroSessionName
        } else {
          this.previousRetroSessionId = null
          this.previousRetroSessionName = null
        }
      } finally {
        this.loading = false
      }
    },

    applyReviewStatusChanged(payload: {
      cardId: number
      reviewStatus: string
      // reviewedCount and totalCount are provided by the server but intentionally unused here:
      // allReviewed, reviewedCount, and totalCount are derived from local `items` state via
      // computed getters, so they stay in sync automatically without needing the server values.
      reviewedCount: number
      totalCount: number
    }): void {
      const item = this.items.find((i) => i.cardId === payload.cardId)
      if (item) {
        item.reviewStatus = payload.reviewStatus
      }
    },
  },
})
