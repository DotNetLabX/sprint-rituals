<script setup lang="ts">
import { onMounted, reactive, ref, computed, provide } from 'vue'
import { useSettingsStore } from '../stores/settingsStore'
import { useAuthStore } from '../stores/authStore'
import { getBoards, getStatuses } from '../api/settings'
import type { AppSettings, BoardOption, StatusOption } from '../types'
import JiraTab from '../components/settings/JiraTab.vue'
import WorkflowTab from '../components/settings/WorkflowTab.vue'
import HealthTab from '../components/settings/HealthTab.vue'
import SyncTab from '../components/settings/SyncTab.vue'
import XrayTab from '../components/settings/XrayTab.vue'
import UsersTab from '../components/settings/UsersTab.vue'
import {
  settingsFormKey, boardsKey, boardsLoadingKey, boardsErrorKey,
  statusesKey, statusesLoadingKey, statusesErrorKey,
  isReadOnlyKey, settingsStoreKey, authStoreKey,
} from '../components/settings/injectionKeys'

const store = useSettingsStore()
const authStore = useAuthStore()
const activeSettingsTab = ref<'jira' | 'workflow' | 'health' | 'sync' | 'xray' | 'users'>('jira')

const boards = ref<BoardOption[]>([])
const boardsLoading = ref(false)
const boardsError = ref(false)
const statuses = ref<StatusOption[]>([])
const statusesLoading = ref(false)
const statusesError = ref(false)
const isReadOnly = computed(() => !authStore.isAdmin)

const form = reactive<AppSettings>({
  boardId: null, doneStatuses: [], workflowStages: [],
  healthThresholds: { completionGreen: 80, completionAmber: 60, disruptionGreen: 10, disruptionAmber: 25, carryOverGreen: 10, carryOverAmber: 25 },
  healthWeights: { completion: 40, disruption: 30, carryOver: 30 },
  bugRatioAlertThreshold: 50, bugRatioConsecutiveSprintCount: 2,
  syncBackSprintCount: 20, planningWindowDays: 2, defaultSpPerBug: 3, bugRatioTarget: 30,
  xrayEnabled: false, xrayClientId: null, xrayClientSecret: null,
  qaHealthThresholds: { coverageGreen: 80, coverageAmber: 50, executionGreen: 80, executionAmber: 50, passRateGreen: 90, passRateAmber: 70 },
  qualityHealthWeight: 20, qualitySubScoreWeights: { coverageWeight: 50, passRateWeight: 50 },
})

function syncFromStore() {
  const s = store.settings
  Object.assign(form, {
    boardId: s.boardId,
    doneStatuses: [...s.doneStatuses],
    workflowStages: [...s.workflowStages],
    healthThresholds: { ...s.healthThresholds },
    healthWeights: { ...s.healthWeights },
    bugRatioAlertThreshold: s.bugRatioAlertThreshold,
    bugRatioConsecutiveSprintCount: s.bugRatioConsecutiveSprintCount,
    syncBackSprintCount: s.syncBackSprintCount,
    planningWindowDays: s.planningWindowDays,
    defaultSpPerBug: s.defaultSpPerBug,
    bugRatioTarget: s.bugRatioTarget,
    xrayEnabled: s.xrayEnabled,
    xrayClientId: s.xrayClientId,
    xrayClientSecret: s.xrayClientSecret,
    qaHealthThresholds: { ...s.qaHealthThresholds },
    qualityHealthWeight: s.qualityHealthWeight,
    qualitySubScoreWeights: { ...s.qualitySubScoreWeights },
  })
}

provide(settingsFormKey, form)
provide(boardsKey, boards)
provide(boardsLoadingKey, boardsLoading)
provide(boardsErrorKey, boardsError)
provide(statusesKey, statuses)
provide(statusesLoadingKey, statusesLoading)
provide(statusesErrorKey, statusesError)
provide(isReadOnlyKey, isReadOnly)
provide(settingsStoreKey, store)
provide(authStoreKey, authStore)

onMounted(async () => {
  boardsLoading.value = true
  statusesLoading.value = true
  const [, boardsResult, statusesResult] = await Promise.allSettled([
    store.fetchSettings().then(() => {
      syncFromStore()
      if (form.workflowStages.length === 0) {
        return store.runDetectWorkflowStages().then(() => {
          if (store.detectionResult?.stages.length) form.workflowStages = [...store.detectionResult.stages]
        })
      }
    }),
    getBoards(),
    getStatuses(),
  ])
  boardsLoading.value = false
  statusesLoading.value = false
  if (boardsResult.status === 'fulfilled') boards.value = boardsResult.value.boards
  else boardsError.value = true
  if (statusesResult.status === 'fulfilled') statuses.value = statusesResult.value.statuses
  else statusesError.value = true
})

const tabs = [
  { key: 'jira', label: 'Jira', adminOnly: false },
  { key: 'workflow', label: 'Workflow', adminOnly: false },
  { key: 'health', label: 'Health', adminOnly: false },
  { key: 'sync', label: 'Sync', adminOnly: false },
  { key: 'xray', label: 'Xray', adminOnly: true },
  { key: 'users', label: 'Users', adminOnly: true },
] as const
</script>

<template>
  <div class="max-w-3xl mx-auto space-y-6">
    <h1 class="text-3xl font-bold text-gray-100">Settings</h1>

    <div v-if="authStore.user?.role === 'Manager'" class="rounded-md bg-amber-500/10 border border-amber-500/30 px-4 py-3 text-sm text-amber-400">
      View only — contact an Admin to make changes.
    </div>

    <div v-if="store.loading" class="text-gray-400">Loading settings...</div>

    <template v-else>
      <div class="flex gap-1 border-b border-border-default mb-6">
        <template v-for="tab in tabs" :key="tab.key">
          <button
            v-if="!tab.adminOnly || authStore.isAdmin"
            :class="['px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors', activeSettingsTab === tab.key ? 'border-accent-default text-accent-default' : 'border-transparent text-text-secondary hover:text-text-primary']"
            @click="activeSettingsTab = tab.key"
          >{{ tab.label }}</button>
        </template>
      </div>

      <JiraTab v-if="activeSettingsTab === 'jira'" />
      <WorkflowTab v-if="activeSettingsTab === 'workflow'" />
      <HealthTab v-if="activeSettingsTab === 'health'" />
      <SyncTab v-if="activeSettingsTab === 'sync'" />
      <XrayTab v-if="activeSettingsTab === 'xray' && authStore.isAdmin" />
      <UsersTab v-if="activeSettingsTab === 'users' && authStore.isAdmin" />
    </template>
  </div>
</template>
