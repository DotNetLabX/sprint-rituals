<template>
  <div :class="['bg-background flex', joined && !kicked && !loading ? 'h-[calc(100vh-3.5rem)] overflow-hidden flex-col' : 'min-h-screen items-center justify-center']">
    <div v-if="kicked" class="w-full max-w-sm p-8 bg-surface rounded-[--radius-md] shadow-card flex flex-col gap-4 items-center">
      <h1 class="text-2xl font-bold text-danger">You were removed</h1>
      <p class="text-sm text-text-secondary text-center">The Scrum Master removed you from this retro. You'll need a new invite to rejoin.</p>
    </div>

    <div v-else-if="loading" class="flex flex-col items-center gap-4">
      <LoadingSpinner size="lg">Joining retro...</LoadingSpinner>
    </div>

    <RetroJoinForm
      v-else-if="!joined"
      :retro-session-name="retroSessionName || `Retro #${retroSessionId}`"
      :retro-session-id="retroSessionId"
      :participants="summaryParticipants"
      :error-message="errorMessage"
      :busy="busy"
      :show-google-sign-in="showGoogleOption"
      @join="handleJoin"
    />

    <div v-else class="w-full flex-1 min-h-0 px-4 sm:px-8 lg:px-12 py-6 flex flex-col gap-5">
      <RetroPhaseHeader
        :retro-session-id="retroSessionId"
        :retro-session-name="retroSessionName || `Retro #${retroSessionId}`"
        :current-phase="currentPhase"
        :is-scrum-master="isScrumMaster"
        :next-phase-name="nextPhaseName"
        :is-reporting-phase="isReportingPhase"
        :is-closed="isClosed"
        :participants="participants"
        :ready-count="readyCount"
        :total-count="totalCount"
        :ready-participants="readyParticipants"
        :not-ready-participants="notReadyParticipants"
        :is-current-participant-ready="isCurrentParticipantReady"
        :show-readiness="showReadiness"
        :remaining-budget="voteStore.remainingBudget"
        :votes-per-participant="voteStore.votesPerParticipant"
        :show-votes-badge="showVotesBadge"
        :writing-timer-default="retroSessionStore.currentSummary?.writingTimerDefault ?? 5"
        :voting-timer-default="retroSessionStore.currentSummary?.votingTimerDefault ?? 3"
        :discussing-timer-default="retroSessionStore.currentSummary?.discussingTimerDefault ?? 10"
        :sprint-context-open="sprintContextOpen"
        @advance-phase="onAdvanceClick"
        @close-retro-session="onCloseClick"
        @toggle-ready="onToggleReady"
        @toggle-sprint-context="sprintContextOpen = !sprintContextOpen"
      />

      <div v-if="isClosed" class="bg-badge-warning-bg border border-badge-warning-text rounded-[--radius-sm] px-4 py-2 text-sm text-badge-warning-text">
        This retro has been closed. The board is now read-only.
      </div>

      <div v-if="isReportingPhase" class="flex items-center gap-2">
        <button class="px-4 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer" :class="reportingView === 'report' ? 'bg-primary-600 text-white' : 'bg-surface-raised text-text-secondary hover:bg-border'" @click="reportingView = 'report'">Report</button>
        <button class="px-4 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer" :class="reportingView === 'board' ? 'bg-primary-600 text-white' : 'bg-surface-raised text-text-secondary hover:bg-border'" @click="reportingView = 'board'">Board</button>
      </div>

      <RetroReportPanel v-if="isReportingPhase && reportingView === 'report'" @copy-markdown="handleCopyMarkdown" />
      <RetroBoardPanel
        v-if="!isReportingPhase || reportingView === 'board'"
        :retro-session-id="retroSessionId"
        :is-ready="isCurrentParticipantReady"
        :show-readiness-button="showReadiness"
        @toggle-ready="onToggleReady"
      />
    </div>

    <!-- Sprint context panel (overlay, sibling to main content — always accessible when joined) -->
    <SprintContextPanel
      v-if="joined"
      :open="sprintContextOpen"
      @close="sprintContextOpen = false"
    />

    <!-- Advance phase confirmation dialog -->
    <BaseModal :open="showAdvanceDialog" :title="advanceDialogTitle" @close="showAdvanceDialog = false">
      <p v-if="advanceError" class="text-sm text-danger">{{ advanceError }}</p>
      <p
        v-else
        class="text-sm text-text-secondary"
        :title="advanceIsWritingWarning ? advanceWarningTooltip : undefined"
      >{{ advanceDialogMessage }}</p>
      <template #footer>
        <div class="flex justify-end gap-3">
          <BaseButton variant="secondary" @click="showAdvanceDialog = false">Cancel</BaseButton>
          <BaseButton v-if="!advanceError" variant="primary" @click="confirmAdvance">Advance</BaseButton>
        </div>
      </template>
    </BaseModal>

    <!-- Close retro confirmation dialog -->
    <BaseModal :open="showCloseDialog" title="Close Retro" @close="showCloseDialog = false">
      <p class="text-sm text-text-secondary">Close this retro? No more changes will be allowed.</p>
      <template #footer>
        <div class="flex justify-end gap-3">
          <BaseButton variant="secondary" @click="showCloseDialog = false">Cancel</BaseButton>
          <BaseButton variant="danger" @click="confirmClose">Close Retro</BaseButton>
        </div>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useParticipantStore } from '../stores/participant'
import { useVoteStore } from '../stores/vote'
import { useRetroSessionStore } from '../stores/retroSession'
import { useToast } from '../composables/useToast'
import { useRetroRoom } from '../composables/useRetroRoom'
import { useReadiness } from '../composables/useReadiness'
import LoadingSpinner from '../components/ui/LoadingSpinner.vue'
import BaseButton from '../components/ui/BaseButton.vue'
import BaseModal from '../components/ui/BaseModal.vue'
import RetroJoinForm from '../components/retro/RetroJoinForm.vue'
import RetroPhaseHeader from '../components/retro/RetroPhaseHeader.vue'
import RetroReportPanel from '../components/retro/RetroReportPanel.vue'
import RetroBoardPanel from '../components/retro/RetroBoardPanel.vue'
import SprintContextPanel from '../components/retro/SprintContextPanel.vue'

const route = useRoute()
const participantStore = useParticipantStore()
const voteStore = useVoteStore()
const retroSessionStore = useRetroSessionStore()
const { addToast } = useToast()

const retroSessionId = Number(route.params.retroSessionId)
const reportingView = ref<'report' | 'board'>('report')
const sprintContextOpen = ref(false)

const {
  kicked, busy, errorMessage, joined, loading,
  retroSessionName, currentPhase, summaryParticipants, isReportingPhase,
  handleJoin, reportStore, showGoogleOption,
  isClosed, teamId, isScrumMaster,
} = useRetroRoom(retroSessionId)

const {
  toggleReady,
  isCurrentParticipantReady,
  readyCount,
  totalCount,
  readyParticipants,
  notReadyParticipants,
} = useReadiness()

const participants = computed(() => participantStore.participants)

// Phase name map
const phaseNextMap: Record<string, string> = {
  Writing: 'Grouping',
  Grouping: 'Voting',
  Voting: 'Discussing',
  Discussing: 'Reporting',
}

const nextPhaseName = computed(() => phaseNextMap[currentPhase.value] ?? '')

// Readiness is shown during Writing and Voting when participant count > 1
const showReadiness = computed(() =>
  (currentPhase.value === 'Writing' || currentPhase.value === 'Voting') &&
  participants.value.length > 1,
)

// Votes badge only during Voting
const showVotesBadge = computed(() => currentPhase.value === 'Voting')

// Dialog state
const showAdvanceDialog = ref(false)
const showCloseDialog = ref(false)
const advanceDialogTitle = ref('Advance Phase')
const advanceDialogMessage = ref('')
const advanceError = ref('')
const advanceIsWritingWarning = ref(false)

const advanceWarningTooltip = "Some action items from the last retro haven't been reviewed yet. You can still advance if needed."

async function onAdvanceClick() {
  advanceError.value = ''
  advanceDialogTitle.value = `Advance to ${nextPhaseName.value}`

  if (!teamId.value) return

  try {
    const check = await retroSessionStore.advancePhaseCheck(teamId.value, retroSessionId)

    if (check.currentPhase === 'Writing') {
      const unreviewed = check.unreviewedActionCount ?? 0
      const total = check.totalPreviousActionCount ?? 0
      if (unreviewed > 0 && total > 0) {
        advanceDialogMessage.value = `${unreviewed} of ${total} previous action item${total === 1 ? '' : 's'} haven't been reviewed yet. Advance anyway?`
        advanceIsWritingWarning.value = true
      } else {
        advanceDialogMessage.value = `Advance to ${nextPhaseName.value}? This cannot be undone.`
        advanceIsWritingWarning.value = false
      }
      advanceError.value = ''
      showAdvanceDialog.value = true
    } else if (check.currentPhase === 'Voting') {
      const n = check.nonVotingParticipantCount ?? 0
      const notDone = totalCount.value - readyCount.value
      if (n > 0) {
        advanceDialogMessage.value = `${n} participant${n === 1 ? '' : 's'} haven't voted yet. Advance to Discussing anyway?`
      } else if (notDone > 0) {
        advanceDialogMessage.value = `All participants have voted, but ${notDone} of ${totalCount.value} haven't marked as done. Advance to Discussing anyway?`
      } else {
        advanceDialogMessage.value = `All participants have voted and are done. Advance to Discussing?`
      }
      advanceError.value = ''
      showAdvanceDialog.value = true
    } else if (check.currentPhase === 'Discussing') {
      const discussed = check.discussedGroupCount ?? 0
      if (discussed < 2) {
        advanceDialogTitle.value = 'Cannot Advance'
        advanceError.value = `At least 2 groups must be discussed before advancing to Reporting. Currently discussed: ${discussed}.`
        advanceDialogMessage.value = ''
        showAdvanceDialog.value = true
      } else {
        advanceDialogMessage.value = `Advance to Reporting? ${discussed} group${discussed === 1 ? '' : 's'} have been discussed.`
        advanceError.value = ''
        showAdvanceDialog.value = true
      }
    } else {
      advanceDialogMessage.value = `Advance to ${nextPhaseName.value}? This cannot be undone.`
      advanceError.value = ''
      showAdvanceDialog.value = true
    }
  } catch {
    addToast('Failed to check phase status.', 'error')
  }
}

function onCloseClick() {
  showCloseDialog.value = true
}

async function confirmAdvance() {
  showAdvanceDialog.value = false
  if (!teamId.value) return
  try {
    await retroSessionStore.advancePhase(teamId.value, retroSessionId)
  } catch {
    addToast('Failed to advance phase.', 'error')
  }
}

async function confirmClose() {
  showCloseDialog.value = false
  if (!teamId.value) return
  try {
    await retroSessionStore.closeRetroSession(teamId.value, retroSessionId)
  } catch {
    addToast('Failed to close retro.', 'error')
  }
}

async function onToggleReady() {
  await toggleReady(retroSessionId)
}

watch(isReportingPhase, (reporting) => {
  if (reporting && joined.value) { reportingView.value = 'report'; reportStore.fetchParticipantReport(retroSessionId) }
})

watch(isClosed, (closed) => {
  if (closed && joined.value) {
    addToast('This retro has been closed by the Scrum Master.', 'info')
  }
})

async function handleCopyMarkdown() {
  const ok = await reportStore.copyAsMarkdown()
  if (ok) addToast('Copied to clipboard', 'success')
}
</script>
