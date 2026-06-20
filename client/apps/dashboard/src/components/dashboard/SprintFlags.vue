<script setup lang="ts">
import type { FlagsResult } from '../../types'
import { useSettingsStore } from '../../stores/settingsStore'
import InfoTooltip from '../InfoTooltip.vue'

const settingsStore = useSettingsStore()

defineProps<{
  flags: FlagsResult
}>()
</script>

<template>
  <div class="flex flex-col gap-4">
    <div v-if="!flags.hasAnyFlags" class="flex items-center gap-2 text-status-success text-sm">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5 shrink-0">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
      </svg>
      <span>No flags this sprint</span>
      <InfoTooltip text="All sprint health indicators passed with no issues detected." />
    </div>

    <!-- Zombie tickets -->
    <div v-if="flags.zombieTickets.length > 0">
      <div class="text-sm font-medium text-status-warning mb-2 flex items-center gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
          <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
        </svg>
        Zombie Tickets ({{ flags.zombieTickets.length }})
        <InfoTooltip text="Tickets that have appeared in 3 or more sprints without being completed." />
      </div>
      <div class="flex flex-col gap-1">
        <div
          v-for="zombie in flags.zombieTickets"
          :key="zombie.ticketKey"
          class="flex items-center justify-between text-sm bg-surface-elevated rounded px-3 py-2"
        >
          <div class="flex items-center gap-2 min-w-0">
            <span class="text-text-muted font-mono text-xs shrink-0">{{ zombie.ticketKey }}</span>
            <span class="text-text-secondary truncate">{{ zombie.summary }}</span>
            <span v-if="zombie.assigneeName" class="text-xs text-text-muted shrink-0 truncate max-w-[8rem]">— {{ zombie.assigneeName }}</span>
          </div>
          <span class="text-status-warning text-xs shrink-0 ml-2">{{ zombie.sprintCount }} sprints</span>
        </div>
      </div>
    </div>

    <!-- Mid-sprint disruption -->
    <div v-if="flags.midSprintDisruption">
      <div class="text-sm font-medium text-status-warning mb-2 flex items-center gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
          <path d="M11.983 1.907a.75.75 0 00-1.292-.657l-8.5 9.5A.75.75 0 002.75 12h6.572l-1.305 6.093a.75.75 0 001.292.657l8.5-9.5A.75.75 0 0017.25 8h-6.572l1.305-6.093z" />
        </svg>
        Mid-Sprint Disruption
        <InfoTooltip :text="`Story points and ticket count added to the sprint after the first ${settingsStore.settings.planningWindowDays} days.`" />
      </div>
      <div class="text-sm text-text-secondary bg-surface-elevated rounded px-3 py-2">
        {{ flags.midSprintDisruption.ticketCount }} ticket{{ flags.midSprintDisruption.ticketCount !== 1 ? 's' : '' }}
        added after the 2-day grace period
        <span v-if="flags.midSprintDisruption.totalSp > 0">
          ({{ flags.midSprintDisruption.totalSp }} SP)
        </span>
      </div>
    </div>

    <!-- Zero-SP developers -->
    <div v-if="flags.zeroSpDevelopers.length > 0">
      <div class="text-sm font-medium text-status-warning mb-2 flex items-center gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
          <path d="M10 9a3 3 0 100-6 3 3 0 000 6zM6 8a2 2 0 11-4 0 2 2 0 014 0zM1.49 15.326a.78.78 0 01-.358-.442 3 3 0 014.308-3.516 6.484 6.484 0 00-1.905 3.959c-.023.222-.014.442.025.654a4.97 4.97 0 01-2.07-.655zM16.44 15.98a4.97 4.97 0 002.07-.654.78.78 0 00.357-.442 3 3 0 00-4.308-3.517 6.484 6.484 0 011.907 3.96 2.32 2.32 0 01-.026.654zM18 8a2 2 0 11-4 0 2 2 0 014 0zM5.304 16.19a.844.844 0 01-.277-.71 5 5 0 019.947 0 .843.843 0 01-.277.71A6.975 6.975 0 0110 18a6.974 6.974 0 01-4.696-1.81z" />
        </svg>
        Zero-SP Developers ({{ flags.zeroSpDevelopers.length }})
        <InfoTooltip text="Active developers who had assigned tickets but completed zero story points." />
      </div>
      <div class="flex flex-wrap gap-2">
        <span
          v-for="name in flags.zeroSpDevelopers"
          :key="name"
          class="text-xs bg-surface-elevated text-text-secondary rounded px-2 py-1"
        >
          {{ name }}
        </span>
      </div>
    </div>

    <!-- Testing crunch -->
    <div v-if="flags.testingCrunch">
      <div class="text-sm font-medium text-status-warning mb-2 flex items-center gap-2">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
          <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
        </svg>
        Testing Crunch
        <InfoTooltip text="Fires when >50% of test runs completed in the last 2 sprint days. Shows percentage and affected ticket count." />
      </div>
      <div class="text-sm text-text-secondary bg-surface-elevated rounded px-3 py-2">
        {{ flags.testingCrunch.crunchPercentage.toFixed(1) }}% of test runs completed in the last 2 days
        ({{ flags.testingCrunch.crunchRunCount }} of {{ flags.testingCrunch.totalRunCount }} runs)
      </div>
    </div>
  </div>
</template>
