<template>
  <div class="flex flex-col gap-3 bg-surface rounded-[--radius-md] shadow-card px-4 py-3">
    <!-- Row 1: Sprint name | timer + badges + action button -->
    <div class="flex items-center justify-between gap-3">
      <h1 class="text-xl font-bold text-text-primary truncate">{{ sprintName }}</h1>
      <div class="flex items-center gap-2 flex-shrink-0">
        <!-- Timer display (Writing/Voting/Discussing phases only) -->
        <TimerDisplay
          v-if="isTimedPhase"
          :sprint-id="sprintId"
          :is-scrum-master="isScrumMaster"
          :current-phase="currentPhase"
          :writing-timer-default="writingTimerDefault"
          :voting-timer-default="votingTimerDefault"
          :discussing-timer-default="discussingTimerDefault"
        />

        <!-- Readiness badge (Writing/Voting, >1 participant) -->
        <div
          v-if="showReadiness"
          class="relative"
          @mouseenter="showReadinessTooltip = true"
          @mouseleave="showReadinessTooltip = false"
        >
          <BaseBadge :variant="allDone ? 'success' : 'default'" :class="{ 'animate-attention': allDone }">{{ readyCount }}/{{ totalCount }} done</BaseBadge>
          <!-- SM tooltip listing ready/not-ready names -->
          <div
            v-if="isScrumMaster && showReadinessTooltip && totalCount > 0"
            class="absolute right-0 top-full mt-1 z-20 w-48 bg-surface-raised text-text-primary border border-border text-xs rounded-[--radius-sm] p-2 shadow-lg"
          >
            <div v-if="readyParticipants.length > 0" class="mb-1">
              <p class="font-semibold text-badge-success-text">Done ({{ readyParticipants.length }})</p>
              <p v-for="name in readyParticipants" :key="name" class="truncate">{{ name }}</p>
            </div>
            <div v-if="notReadyParticipants.length > 0">
              <p class="font-semibold text-text-secondary">Not done ({{ notReadyParticipants.length }})</p>
              <p v-for="name in notReadyParticipants" :key="name" class="truncate">{{ name }}</p>
            </div>
          </div>
        </div>

        <!-- Votes remaining badge (Voting phase only) -->
        <BaseBadge v-if="showVotesBadge" variant="info">
          Votes: {{ remainingBudget }}/{{ votesPerParticipant }}
        </BaseBadge>

        <!-- SM controls -->
        <template v-if="isScrumMaster && !isClosed">
          <button
            v-if="!isReportingPhase"
            class="px-3 py-1.5 bg-primary-600 hover:bg-primary-700 text-white text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
            @click="$emit('advance-phase')"
          >
            Next: {{ nextPhaseName }}
          </button>
          <button
            v-else
            class="px-3 py-1.5 bg-red-600 hover:bg-red-700 text-white text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
            @click="$emit('close-sprint')"
          >
            Close Sprint
          </button>
        </template>
      </div>
    </div>

    <!-- Row 2: Phase stepper -->
    <PhaseIndicator :current-phase="currentPhase" />

    <!-- Row 3: Participant list -->
    <RetroParticipantList :participants="participants" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import BaseBadge from '../ui/BaseBadge.vue'
import PhaseIndicator from '../PhaseIndicator.vue'
import RetroParticipantList from './RetroParticipantList.vue'
import TimerDisplay from './TimerDisplay.vue'

const props = defineProps<{
  sprintId: number
  sprintName: string
  currentPhase: string
  isScrumMaster: boolean
  nextPhaseName: string
  isReportingPhase: boolean
  isClosed: boolean
  participants: { id: number; displayName: string }[]
  readyCount: number
  totalCount: number
  readyParticipants: string[]
  notReadyParticipants: string[]
  isCurrentParticipantReady: boolean
  showReadiness: boolean
  remainingBudget: number
  votesPerParticipant: number
  showVotesBadge: boolean
  writingTimerDefault: number
  votingTimerDefault: number
  discussingTimerDefault: number
}>()

defineEmits<{
  'advance-phase': []
  'close-sprint': []
  'toggle-ready': []
}>()

const showReadinessTooltip = ref(false)

const isTimedPhase = computed(() =>
  props.currentPhase === 'Writing' ||
  props.currentPhase === 'Voting' ||
  props.currentPhase === 'Discussing',
)

const allDone = computed(() => props.totalCount > 0 && props.readyCount >= props.totalCount)
</script>
