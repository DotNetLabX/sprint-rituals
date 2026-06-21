<template>
  <div :class="['bg-background', joined && !loading ? 'h-[calc(100vh-3.5rem)] overflow-hidden flex flex-col' : 'min-h-screen flex items-center justify-center']">
    <!-- Loading -->
    <div v-if="loading" class="flex flex-col items-center gap-4">
      <LoadingSpinner size="lg">Loading session...</LoadingSpinner>
    </div>

    <!-- Join gate -->
    <PokerJoinForm
      v-else-if="!joined"
      :session-title="sessionStore.session?.title ?? sessionStore.session?.sprintName ?? `Session ${sessionId.slice(0, 8)}`"
      :participants="sessionStore.participants"
      :error-message="joinError"
      :busy="joinBusy"
      :is-signed-in="authStore.isSignedIn"
      :show-google-sign-in="!authStore.isSignedIn"
      @join="handleJoin"
      @sign-in="authStore.signIn()"
    />

    <!-- Room -->
    <template v-else-if="sessionStore.session">
      <!-- Summary view (after closed) -->
      <div v-if="summaryMode" class="w-full max-w-2xl mx-auto px-4 py-8 flex flex-col gap-6">
        <h1 class="text-xl font-bold text-text-primary">Session Summary</h1>
        <SessionSummaryPanel v-if="summary" :summary="summary" />
        <LoadingSpinner v-else size="sm">Loading summary...</LoadingSpinner>
      </div>

      <!-- Active estimation room -->
      <div v-else class="flex-1 min-h-0 flex overflow-hidden">
        <!-- Left sidebar: item queue -->
        <aside class="hidden md:flex flex-col w-56 lg:w-64 shrink-0 border-r border-border px-4 py-6 overflow-y-auto bg-surface">
          <ItemQueue
            :items="sessionStore.session.items"
            :active-item-id="sessionStore.activeItemId"
            :is-scrum-master="isScrumMaster"
            @select="onSelectItem"
          />

          <!-- Free mode: add item form -->
          <div v-if="!sessionStore.isSprintBound && isScrumMaster" class="mt-4 flex flex-col gap-2 border-t border-border pt-4">
            <input
              v-model="newItemLabel"
              type="text"
              placeholder="Add item label…"
              class="px-3 py-1.5 text-sm bg-surface-raised border border-border rounded-[--radius-sm] text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-primary-500"
              @keydown.enter="onAddItem"
            />
            <BaseButton size="sm" variant="secondary" :disabled="!newItemLabel.trim()" @click="onAddItem">
              Add Item
            </BaseButton>
          </div>
        </aside>

        <!-- Main area -->
        <main class="flex-1 min-h-0 overflow-y-auto px-4 sm:px-8 py-6 flex flex-col gap-6">
          <!-- Active item header -->
          <div v-if="sessionStore.activeItem" class="flex flex-col gap-1">
            <div class="flex items-center gap-2 text-xs text-text-muted">
              <span v-if="sessionStore.activeItem.ticketKey" class="font-mono">{{ sessionStore.activeItem.ticketKey }}</span>
              <span class="uppercase font-semibold">{{ sessionStore.activeItem.status }}</span>
            </div>
            <h2 class="text-lg font-semibold text-text-primary">{{ sessionStore.activeItem.label }}</h2>
          </div>
          <div v-else class="text-sm text-text-muted">Waiting for the Scrum Master to select an item…</div>

          <!-- Vote board -->
          <VoteBoard
            v-if="sessionStore.activeItem"
            :participants="sessionStore.participants"
            :votes="sessionStore.activeItem.votes"
            :revealed="sessionStore.activeItem.status === 'Revealed' || sessionStore.activeItem.status === 'Decided'"
          />

          <!-- Card fan (voter) -->
          <FibonacciCardFan
            v-if="sessionStore.activeItem?.status === 'Voting'"
            :selected-value="myVote"
            :can-vote="canVote"
            @vote="onVote"
          />

          <!-- Decided result -->
          <div v-if="sessionStore.activeItem?.status === 'Decided'" class="flex items-center gap-3 px-4 py-3 bg-badge-success-bg border border-badge-success-text/30 rounded-[--radius-md]">
            <span class="text-badge-success-text font-semibold text-sm">Estimate: {{ sessionStore.activeItem.finalEstimate }} SP</span>
          </div>
        </main>

        <!-- Right sidebar: SM controls + participants -->
        <aside v-if="isScrumMaster" class="hidden lg:flex flex-col w-56 xl:w-64 shrink-0 border-l border-border px-4 py-6 overflow-y-auto bg-surface">
          <SmControls
            :item-status="sessionStore.activeItem?.status ?? null"
            :session-closed="sessionStore.isClosed"
            :write-back-error="sessionStore.writeBackError"
            @reveal="onReveal"
            @decide="onDecide"
            @skip="onSkip"
            @close-session="onCloseSession"
            @clear-write-back-error="sessionStore.clearWriteBackError()"
          />
        </aside>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { storeToRefs } from 'pinia'
import { usePlanningSessionStore } from '../stores/planningSession'
import { useGuestStore } from '../stores/guest'
import { useAuthStore } from '../stores/auth'
import { useToast } from '../composables/useToast'
import { usePlanningHub } from '../composables/usePlanningHub'
import { sessionsApi } from '../api/sessions'
import LoadingSpinner from '../components/ui/LoadingSpinner.vue'
import BaseButton from '../components/ui/BaseButton.vue'
import PokerJoinForm from '../components/poker/PokerJoinForm.vue'
import FibonacciCardFan from '../components/poker/FibonacciCardFan.vue'
import VoteBoard from '../components/poker/VoteBoard.vue'
import ItemQueue from '../components/poker/ItemQueue.vue'
import SmControls from '../components/poker/SmControls.vue'
import SessionSummaryPanel from '../components/poker/SessionSummaryPanel.vue'
import type { FibonacciValue, SessionSummaryResponse } from '../types'

const route = useRoute()
const sessionId = route.params['id'] as string

const sessionStore = usePlanningSessionStore()
const guestStore = useGuestStore()
const authStore = useAuthStore()
const { addToast } = useToast()

const { writeBackError } = storeToRefs(sessionStore)

const loading = ref(true)
const joined = ref(false)
const joinBusy = ref(false)
const joinError = ref<string | null>(null)
const summaryMode = ref(false)
const summary = ref<SessionSummaryResponse | null>(null)
const myVote = ref<string | null>(null)
const newItemLabel = ref('')

const hub = usePlanningHub((payload) => {
  addToast(`Session closed — ${payload.itemsEstimated} items estimated, ${payload.totalStoryPoints} SP total`, 'info')
  summaryMode.value = true
  loadSummary()
})

// SM check: signed in AND the session's creator matches the signed-in user's Person id.
// `authStore.user.id` is the Identity Person id from /api/auth/me.
// `createdByPersonId` is a non-nullable int, so `!== undefined` was always true — comparing
// against the actual person id is the correct discriminator.
const isScrumMaster = computed(
  () =>
    authStore.isSignedIn &&
    authStore.user !== null &&
    sessionStore.session?.createdByPersonId === authStore.user.id,
)

const canVote = computed(
  () => joined.value && !sessionStore.isClosed && sessionStore.activeItem?.status === 'Voting',
)

onMounted(async () => {
  guestStore.loadToken(sessionId)
  await sessionStore.fetchSession(sessionId)
  loading.value = false

  // If we already have a guest token from a previous visit, re-join directly
  if (guestStore.token) {
    await connectToHub()
    joined.value = true
  } else if (authStore.isSignedIn) {
    // Authenticated users can join directly without a display name prompt
    await handleJoin('')
  }

  if (sessionStore.isClosed) {
    summaryMode.value = true
    await loadSummary()
  }
})

onUnmounted(async () => {
  await hub.disconnect()
})

async function handleJoin(displayName: string) {
  joinBusy.value = true
  joinError.value = null
  try {
    const result = await sessionsApi.join(sessionId, { displayName: displayName || (authStore.user?.displayName ?? 'Unknown') })
    guestStore.saveToken(sessionId, result.guestToken, result.displayName, result.participantId)
    await connectToHub()
    joined.value = true
  } catch (e: unknown) {
    const status = e instanceof Object && 'status' in e ? (e as { status: number }).status : 0
    joinError.value = status === 409 ? 'This session is closed.' : 'Failed to join. Please try again.'
  } finally {
    joinBusy.value = false
  }
}

async function connectToHub() {
  try {
    await hub.connect(sessionId)
  } catch {
    addToast('Real-time connection failed. Some updates may be delayed.', 'error')
  }
}

async function loadSummary() {
  try {
    summary.value = await sessionsApi.getSummary(sessionId)
  } catch {
    addToast('Could not load session summary.', 'error')
  }
}

async function onSelectItem(itemId: string) {
  try {
    await hub.selectItem(sessionId, itemId)
    myVote.value = null
  } catch {
    addToast('Failed to select item.', 'error')
  }
}

async function onVote(value: FibonacciValue) {
  if (!sessionStore.activeItemId) return
  try {
    if (myVote.value === null) {
      await hub.submitVote(sessionId, sessionStore.activeItemId, value)
    } else {
      await hub.changeVote(sessionId, sessionStore.activeItemId, value)
    }
    myVote.value = value
  } catch {
    addToast('Failed to cast vote.', 'error')
  }
}

async function onReveal() {
  if (!sessionStore.activeItemId) return
  try {
    await hub.revealVotes(sessionId, sessionStore.activeItemId)
  } catch {
    addToast('Failed to reveal votes.', 'error')
  }
}

async function onDecide(value: FibonacciValue) {
  if (!sessionStore.activeItemId) return
  try {
    await hub.decideEstimate(sessionId, sessionStore.activeItemId, value)
  } catch {
    addToast('Failed to set estimate.', 'error')
  }
}

async function onSkip() {
  if (!sessionStore.activeItemId) return
  try {
    await hub.skipItem(sessionId, sessionStore.activeItemId)
  } catch {
    addToast('Failed to skip item.', 'error')
  }
}

async function onCloseSession() {
  try {
    await hub.closeSession(sessionId)
  } catch {
    addToast('Failed to close session.', 'error')
  }
}

async function onAddItem() {
  const label = newItemLabel.value.trim()
  if (!label) return
  try {
    await sessionsApi.addItem(sessionId, { label })
    newItemLabel.value = ''
    // Optimistic: item arrives via SignalR broadcast, no re-fetch needed
  } catch {
    addToast('Failed to add item.', 'error')
  }
}

// Show write-back error as toast so SM notices it even if controls are off screen
watch(writeBackError, (err) => {
  if (err) addToast(`Jira write-back failed: ${err.reason}`, 'error')
})
</script>
