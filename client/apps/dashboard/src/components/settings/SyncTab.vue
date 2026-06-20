<script setup lang="ts">
import { inject, ref } from 'vue'
import { getJiraSprints, syncSprints, syncBacklog } from '../../api/sync'
import type { SyncSprintsResponse, SyncBacklogResponse } from '../../types'
import {
  settingsFormKey,
  isReadOnlyKey,
  settingsStoreKey,
} from './injectionKeys'

const form = inject(settingsFormKey)!
const isReadOnly = inject(isReadOnlyKey)!
const store = inject(settingsStoreKey)!

// Sync config save state
const syncConfigSaving = ref(false)
const syncConfigSaved = ref(false)
const syncConfigError = ref('')

// Analytics targets save state
const analyticsTargetsSaving = ref(false)
const analyticsTargetsSaved = ref(false)
const analyticsTargetsError = ref('')

// Sync all state
const syncing = ref(false)
const syncResult = ref<{ sprints: SyncSprintsResponse; backlog: SyncBacklogResponse } | null>(null)
const syncError = ref('')

// Sync current sprint state
const syncingCurrent = ref(false)
const syncCurrentResult = ref<SyncSprintsResponse | null>(null)
const syncCurrentError = ref('')

// Sprint range sync state
const sprintsForRange = ref<{ id: number; name: string; startDate: string | null; state: string }[]>([])
const sprintsForRangeLoading = ref(false)
const sprintsForRangeError = ref('')
const fromSprintId = ref<number | null>(null)
const toSprintId = ref<number | null>(null)
const syncingRange = ref(false)
const syncRangeResult = ref<SyncSprintsResponse | null>(null)
const syncRangeError = ref('')

async function saveAnalyticsTargetsPanel() {
  analyticsTargetsSaved.value = false
  analyticsTargetsError.value = ''
  analyticsTargetsSaving.value = true
  try {
    await store.saveAnalyticsTargetsAction(form.bugRatioTarget)
    if (!store.error) {
      analyticsTargetsSaved.value = true
      setTimeout(() => analyticsTargetsSaved.value = false, 3000)
    } else {
      analyticsTargetsError.value = store.error
    }
  } catch (e: any) {
    analyticsTargetsError.value = e.message || 'Failed to save analytics targets.'
  } finally {
    analyticsTargetsSaving.value = false
  }
}

async function saveSyncConfigPanel() {
  syncConfigSaved.value = false
  syncConfigError.value = ''
  syncConfigSaving.value = true
  try {
    await store.saveSyncConfigAction(form.syncBackSprintCount, form.planningWindowDays)
    if (!store.error) {
      syncConfigSaved.value = true
      setTimeout(() => syncConfigSaved.value = false, 3000)
    } else {
      syncConfigError.value = store.error
    }
  } catch (e: any) {
    syncConfigError.value = e.message || 'Failed to save sync config.'
  } finally {
    syncConfigSaving.value = false
  }
}

async function syncAll() {
  if (!form.boardId) {
    syncError.value = 'Select a board and save settings first.'
    return
  }

  syncing.value = true
  syncError.value = ''
  syncResult.value = null

  try {
    await store.saveSyncConfigAction(form.syncBackSprintCount, form.planningWindowDays)
    await store.saveBoardAction(form.boardId)
    if (store.error) {
      syncError.value = 'Failed to save settings before sync.'
      syncing.value = false
      return
    }

    const { sprints: jiraSprints } = await getJiraSprints(form.boardId)
    if (jiraSprints.length === 0) {
      syncError.value = 'No sprints found on this board.'
      syncing.value = false
      return
    }

    const lastN = jiraSprints.slice(-form.syncBackSprintCount)
    const first = lastN[0]
    const last = lastN[lastN.length - 1]

    const sprintResult = await syncSprints(first.id, last.id)
    const backlogResult = await syncBacklog()

    syncResult.value = { sprints: sprintResult, backlog: backlogResult }
  } catch (e: any) {
    syncError.value = e.message || 'Sync failed.'
  } finally {
    syncing.value = false
  }
}

async function syncCurrentSprint() {
  if (!form.boardId) {
    syncCurrentError.value = 'Select a board and save settings first.'
    return
  }
  syncingCurrent.value = true
  syncCurrentError.value = ''
  syncCurrentResult.value = null
  try {
    const { sprints } = await getJiraSprints(form.boardId)
    const active = sprints.find(s => s.state === 'active')
    if (!active) {
      syncCurrentError.value = 'No active sprint found on this board.'
      return
    }
    syncCurrentResult.value = await syncSprints(active.id, active.id)
  } catch (e: any) {
    syncCurrentError.value = e.message || 'Sync failed.'
  } finally {
    syncingCurrent.value = false
  }
}

async function loadSprintsForRange() {
  if (!form.boardId) {
    sprintsForRangeError.value = 'Select a board first.'
    return
  }
  sprintsForRangeLoading.value = true
  sprintsForRangeError.value = ''
  try {
    const { sprints } = await getJiraSprints(form.boardId)
    sprintsForRange.value = [...sprints].sort((a, b) => {
      if (!a.startDate) return 1
      if (!b.startDate) return -1
      return a.startDate.localeCompare(b.startDate)
    })
    if (sprintsForRange.value.length > 0) {
      const last = sprintsForRange.value[sprintsForRange.value.length - 1].id
      fromSprintId.value = last
      toSprintId.value = last
    }
  } catch (e: any) {
    sprintsForRangeError.value = e.message || 'Failed to load sprints.'
  } finally {
    sprintsForRangeLoading.value = false
  }
}

async function syncRange() {
  if (!fromSprintId.value || !toSprintId.value) {
    syncRangeError.value = 'Select from and to sprints.'
    return
  }
  syncingRange.value = true
  syncRangeError.value = ''
  syncRangeResult.value = null
  try {
    syncRangeResult.value = await syncSprints(fromSprintId.value, toSprintId.value)
  } catch (e: any) {
    syncRangeError.value = e.message || 'Sync range failed.'
  } finally {
    syncingRange.value = false
  }
}
</script>

<template>
  <!-- Analytics Targets -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <h2 class="text-lg font-semibold text-gray-200">Analytics Targets</h2>
    <p class="text-sm text-gray-400">Configure targets used in developer detail analytics charts.</p>

    <div class="flex items-center gap-3">
      <label
        class="text-sm text-gray-300 whitespace-nowrap cursor-help"
        title="Target bug SP percentage for the work allocation chart. Developers spending more than this on bugs will be flagged."
      >Bug ratio target (%)</label>
      <input
        v-model.number="form.bugRatioTarget"
        type="number"
        min="0"
        max="100"
        :disabled="isReadOnly"
        class="w-24 bg-gray-800 border border-gray-700 rounded px-3 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
      />
      <span class="text-xs text-gray-500">(0–100%)</span>
    </div>

    <div class="flex items-center gap-4">
      <button
        @click="saveAnalyticsTargetsPanel"
        :disabled="analyticsTargetsSaving || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ analyticsTargetsSaving ? 'Saving...' : 'Save Analytics Targets' }}
      </button>
      <span v-if="analyticsTargetsSaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="analyticsTargetsError" class="text-red-400 text-sm">{{ analyticsTargetsError }}</span>
    </div>
  </section>

  <!-- Sync -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <div class="flex items-center gap-1">
      <h2 class="text-lg font-semibold text-gray-200">Sync from Jira</h2>
      <span
        class="text-gray-500 cursor-help"
        title="Pull sprint, ticket, developer, and transition data from Jira for the selected range."
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
        </svg>
      </span>
    </div>
    <p class="text-sm text-gray-400" title="Re-syncing a sprint overwrites its data with fresh values from Jira. No duplicates created.">Pull all sprints, tickets, developers, and epics from the configured board. Closed sprints are idempotent — safe to re-run.</p>

    <!-- Default sprint count -->
    <div class="flex items-center gap-3">
      <label class="text-sm text-gray-300 whitespace-nowrap" title="Number of most-recent sprints Sync All will pull. Saved with Save Settings.">Default sprint count</label>
      <input
        v-model.number="form.syncBackSprintCount"
        type="number"
        min="1"
        max="50"
        :disabled="isReadOnly"
        class="w-24 bg-gray-800 border border-gray-700 rounded px-3 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
      />
      <span class="text-xs text-gray-500">(1–50 sprints, save with Save Sync Config)</span>
    </div>

    <!-- Planning window -->
    <div class="flex items-center gap-3">
      <label class="text-sm text-gray-300 whitespace-nowrap" title="Tickets added within this many days after sprint start are treated as committed (part of planning), not mid-sprint additions. Affects disruption rate and classification. Re-sync after changing.">Planning window (days)</label>
      <input
        v-model.number="form.planningWindowDays"
        type="number"
        min="0"
        max="7"
        :disabled="isReadOnly"
        class="w-24 bg-gray-800 border border-gray-700 rounded px-3 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
      />
      <span class="text-xs text-gray-500">(0–7 days, re-sync to apply)</span>
    </div>

    <div class="flex items-center gap-4">
      <button
        @click="saveSyncConfigPanel"
        :disabled="syncConfigSaving || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ syncConfigSaving ? 'Saving...' : 'Save Sync Config' }}
      </button>
      <span v-if="syncConfigSaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="syncConfigError" class="text-red-400 text-sm">{{ syncConfigError }}</span>
    </div>

    <div class="flex items-center gap-3 flex-wrap">
      <button
        @click="syncAll"
        :disabled="syncing || !form.boardId || isReadOnly"
        class="bg-green-600 hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-6 py-2 rounded font-medium"
      >
        {{ syncing ? 'Syncing...' : 'Sync All' }}
      </button>
      <button
        @click="syncCurrentSprint"
        :disabled="syncingCurrent || !form.boardId || isReadOnly"
        class="border border-green-600 hover:bg-green-600/10 disabled:opacity-50 disabled:cursor-not-allowed text-green-400 px-4 py-2 rounded font-medium text-sm"
      >
        {{ syncingCurrent ? 'Syncing...' : 'Sync Current Sprint' }}
      </button>
    </div>
    <p class="text-xs text-gray-500">Sync All also syncs future sprints and epics.</p>

    <div v-if="syncError" class="text-red-400 text-sm">{{ syncError }}</div>
    <div v-if="syncCurrentError" class="text-red-400 text-sm">{{ syncCurrentError }}</div>
    <div v-if="syncCurrentResult" class="space-y-1 text-sm">
      <p class="text-green-400 font-medium">Current sprint synced!</p>
      <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-300">
        <span>Sprints synced:</span><span class="text-gray-100">{{ syncCurrentResult.sprintsSynced }}</span>
        <span>Tickets upserted:</span><span class="text-gray-100">{{ syncCurrentResult.ticketsUpserted }}</span>
        <span>Developers discovered:</span><span class="text-gray-100">{{ syncCurrentResult.developersDiscovered }}</span>
      </div>
    </div>

    <div v-if="syncResult" class="space-y-2 text-sm">
      <p class="text-green-400 font-medium" title="Post-sync report showing sprints synced, tickets upserted, developers found, and any failures.">Sync complete!</p>
      <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-300">
        <span>Sprints synced:</span><span class="text-gray-100">{{ syncResult.sprints.sprintsSynced }}</span>
        <span>Tickets upserted:</span><span class="text-gray-100">{{ syncResult.sprints.ticketsUpserted }}</span>
        <span>Developers discovered:</span><span class="text-gray-100">{{ syncResult.sprints.developersDiscovered }}</span>
        <span>Epic tickets discovered:</span><span class="text-gray-100">{{ syncResult.backlog.epicTicketsDiscovered }}</span>
      </div>
      <div v-if="syncResult.sprints.failures.length > 0" class="text-amber-400">
        {{ syncResult.sprints.failures.length }} sprint(s) failed to sync.
      </div>
      <template v-if="syncResult.sprints.xray">
        <div class="border-t border-gray-800 pt-2 space-y-1">
          <p class="text-gray-400 font-medium text-xs">Xray QA data</p>
          <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-300">
            <span>Test executions synced:</span><span class="text-gray-100">{{ syncResult.sprints.xray.testExecutionsSynced }}</span>
            <span>Test runs synced:</span><span class="text-gray-100">{{ syncResult.sprints.xray.testRunsSynced }}</span>
            <span>Test sets synced:</span><span class="text-gray-100">{{ syncResult.sprints.xray.testSetsSynced }}</span>
          </div>
          <div v-if="syncResult.sprints.xray.warnings.length > 0" class="space-y-1">
            <p v-for="(w, i) in syncResult.sprints.xray.warnings" :key="i" class="text-amber-400 text-xs">{{ w }}</p>
          </div>
        </div>
      </template>
    </div>

    <!-- Sync Range -->
    <div class="border-t border-gray-800 pt-4 space-y-3">
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-medium text-gray-300">Sync Custom Range</h3>
        <button
          @click="loadSprintsForRange"
          :disabled="sprintsForRangeLoading || !form.boardId || isReadOnly"
          class="border border-gray-600 hover:border-gray-400 text-gray-300 hover:text-gray-100 disabled:opacity-40 disabled:cursor-not-allowed px-3 py-1 rounded text-sm"
        >
          {{ sprintsForRangeLoading ? 'Loading...' : 'Load Sprints' }}
        </button>
      </div>
      <p v-if="sprintsForRangeError" class="text-red-400 text-xs">{{ sprintsForRangeError }}</p>
      <template v-if="sprintsForRange.length > 0">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs text-gray-400 mb-1">From sprint</label>
            <select
              v-model.number="fromSprintId"
              :disabled="isReadOnly"
              class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <option v-for="s in sprintsForRange" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs text-gray-400 mb-1">To sprint</label>
            <select
              v-model.number="toSprintId"
              :disabled="isReadOnly"
              class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <option v-for="s in sprintsForRange" :key="s.id" :value="s.id">{{ s.name }}</option>
            </select>
          </div>
        </div>
        <button
          @click="syncRange"
          :disabled="syncingRange || !fromSprintId || !toSprintId || isReadOnly"
          class="bg-green-600 hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded text-sm font-medium"
        >
          {{ syncingRange ? 'Syncing...' : 'Sync Range' }}
        </button>
        <div v-if="syncRangeError" class="text-red-400 text-sm">{{ syncRangeError }}</div>
        <div v-if="syncRangeResult" class="space-y-1 text-sm">
          <p class="text-green-400 font-medium">Range sync complete!</p>
          <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-300">
            <span>Sprints synced:</span><span class="text-gray-100">{{ syncRangeResult.sprintsSynced }}</span>
            <span>Tickets upserted:</span><span class="text-gray-100">{{ syncRangeResult.ticketsUpserted }}</span>
            <span>Developers discovered:</span><span class="text-gray-100">{{ syncRangeResult.developersDiscovered }}</span>
          </div>
          <div v-if="syncRangeResult.failures.length > 0" class="text-amber-400">
            {{ syncRangeResult.failures.length }} sprint(s) failed to sync.
          </div>
        </div>
      </template>
    </div>
  </section>
</template>
