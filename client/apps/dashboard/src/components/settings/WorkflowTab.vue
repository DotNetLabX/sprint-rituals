<script setup lang="ts">
import { inject, ref, computed, onMounted } from 'vue'
import { getCycleTimeBoundaries, saveCycleTimeBoundaries } from '../../api/settings'
import {
  settingsFormKey,
  isReadOnlyKey,
  settingsStoreKey,
} from './injectionKeys'

const form = inject(settingsFormKey)!
const isReadOnly = inject(isReadOnlyKey)!
const store = inject(settingsStoreKey)!

// Workflow stages save state
const workflowStagesSaving = ref(false)
const workflowStagesSaved = ref(false)
const workflowStagesError = ref('')

// Stage add input
const newStage = ref('')

// Cycle time boundary state
const boundaryStartStage = ref('')
const boundaryEndStage = ref('')
const boundaryAvailableStages = ref<string[]>([])
const boundariesLoading = ref(false)
const boundariesSaving = ref(false)
const boundariesSaved = ref(false)
const boundariesError = ref('')

const reDetectDisabled = computed(() => {
  if (store.detecting) return true
  const r = store.detectionResult
  if (r !== null && r.confidence.transitionCount === 0 && r.confidence.ticketCount === 0 && r.confidence.sprintCount === 0) return true
  return false
})

onMounted(async () => {
  boundariesLoading.value = true
  try {
    const result = await getCycleTimeBoundaries()
    boundaryStartStage.value = result.startStage
    boundaryEndStage.value = result.endStage
    boundaryAvailableStages.value = result.availableStages
  } finally {
    boundariesLoading.value = false
  }
})

async function saveWorkflowStagesPanel() {
  workflowStagesSaved.value = false
  workflowStagesError.value = ''
  workflowStagesSaving.value = true
  try {
    await store.saveWorkflowStagesAction([...form.workflowStages])
    if (!store.error) {
      store.clearDetection()
      workflowStagesSaved.value = true
      setTimeout(() => workflowStagesSaved.value = false, 3000)
    } else {
      workflowStagesError.value = store.error
    }
  } catch (e: any) {
    workflowStagesError.value = e.message || 'Failed to save workflow stages.'
  } finally {
    workflowStagesSaving.value = false
  }
}

async function saveBoundaries() {
  boundariesSaved.value = false
  boundariesError.value = ''
  boundariesSaving.value = true
  try {
    const result = await saveCycleTimeBoundaries(boundaryStartStage.value, boundaryEndStage.value)
    boundaryStartStage.value = result.startStage
    boundaryEndStage.value = result.endStage
    boundariesSaved.value = true
    setTimeout(() => boundariesSaved.value = false, 3000)
  } catch (e: any) {
    boundariesError.value = e.message || 'Failed to save boundaries.'
  } finally {
    boundariesSaving.value = false
  }
}

async function reDetect() {
  await store.runDetectWorkflowStages()
  if (store.detectionResult) {
    form.workflowStages = [...store.detectionResult.stages]
  }
}

function moveSidelined(status: string) {
  store.moveSidelinedToStages(status)
  if (!form.workflowStages.includes(status)) {
    form.workflowStages.push(status)
  }
}

function addStage() {
  const val = newStage.value.trim()
  if (val && !form.workflowStages.includes(val)) {
    form.workflowStages.push(val)
  }
  newStage.value = ''
}

function removeStage(index: number) {
  form.workflowStages.splice(index, 1)
}

function moveStage(index: number, direction: -1 | 1) {
  const target = index + direction
  if (target < 0 || target >= form.workflowStages.length) return
  const temp = form.workflowStages[index]
  form.workflowStages[index] = form.workflowStages[target]
  form.workflowStages[target] = temp
}
</script>

<template>
  <!-- Workflow Stages -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-1">
        <h2 class="text-lg font-semibold text-gray-200">Workflow Stages</h2>
        <span
          class="text-gray-500 cursor-help"
          title="The ordered list of status phases your team's tickets move through from start to done."
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
          </svg>
        </span>
      </div>
      <button
        @click="reDetect"
        :disabled="reDetectDisabled || isReadOnly"
        class="border border-gray-600 hover:border-gray-400 text-gray-300 hover:text-gray-100 disabled:opacity-40 disabled:cursor-not-allowed px-3 py-1 rounded text-sm"
        title="Runs a fresh detection using current sync data, replacing the displayed proposal."
      >
        {{ store.detecting ? 'Detecting...' : 'Re-detect' }}
      </button>
    </div>
    <p class="text-sm text-gray-400">Ordered workflow stages for cycle time computation. Will be auto-detected on first sync if left empty.</p>

    <!-- Confidence summary -->
    <p
      v-if="store.detectionResult !== null"
      class="text-sm text-gray-400"
      title="Shows how much data backs the detection: transitions analyzed, tickets covered, sprints spanned."
    >
      Based on {{ store.detectionResult.confidence.transitionCount }} transitions from {{ store.detectionResult.confidence.ticketCount }} tickets across {{ store.detectionResult.confidence.sprintCount }} sprints.
    </p>

    <!-- Empty state: no stages and no detection result (no data) -->
    <p
      v-if="form.workflowStages.length === 0 && store.detectionResult === null && !store.detecting"
      class="text-sm text-gray-500 italic"
    >
      Sync sprints to enable workflow detection.
    </p>

    <div class="space-y-2">
      <div
        v-for="(stage, i) in form.workflowStages"
        :key="i"
        class="flex items-center gap-2 bg-gray-800 rounded px-3 py-2"
      >
        <span class="text-gray-400 text-sm w-6">{{ i + 1 }}.</span>
        <span class="flex-1 text-gray-200">{{ stage }}</span>
        <button @click="moveStage(i, -1)" :disabled="i === 0 || isReadOnly" class="text-gray-500 hover:text-gray-300 disabled:opacity-30">&uarr;</button>
        <button @click="moveStage(i, 1)" :disabled="i === form.workflowStages.length - 1 || isReadOnly" class="text-gray-500 hover:text-gray-300 disabled:opacity-30">&darr;</button>
        <button @click="removeStage(i)" :disabled="isReadOnly" class="text-gray-500 hover:text-red-400 disabled:opacity-50 disabled:cursor-not-allowed">&times;</button>
      </div>
    </div>
    <div class="flex gap-2">
      <input
        v-model="newStage"
        @keyup.enter="addStage"
        placeholder="Add stage..."
        :disabled="isReadOnly"
        class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        title="Type workflow stages by hand if you already know your pipeline or prefer not to use detection."
      />
      <button @click="addStage" :disabled="isReadOnly" class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded">Add</button>
    </div>

    <!-- Other statuses (sidelined) -->
    <div
      v-if="store.detectionResult !== null && store.detectionResult.sidelined.length > 0"
      class="space-y-2 pt-2 border-t border-gray-800"
    >
      <p class="text-sm text-gray-400 font-medium cursor-help" title="Statuses found in your data but excluded from the main pipeline — add them manually if needed.">Other statuses</p>
      <div class="flex flex-wrap gap-2">
        <span
          v-for="status in store.detectionResult.sidelined"
          :key="status"
          class="inline-flex items-center gap-1 bg-gray-800 text-gray-300 px-3 py-1 rounded-full text-sm"
        >
          {{ status }}
          <button @click="moveSidelined(status)" :disabled="isReadOnly" class="text-gray-500 hover:text-green-400 ml-1 disabled:opacity-50 disabled:cursor-not-allowed" title="Add to stages">+</button>
        </span>
      </div>
    </div>
    <div class="flex items-center gap-4">
      <button
        @click="saveWorkflowStagesPanel"
        :disabled="workflowStagesSaving || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ workflowStagesSaving ? 'Saving...' : 'Save Workflow Stages' }}
      </button>
      <span v-if="workflowStagesSaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="workflowStagesError" class="text-red-400 text-sm">{{ workflowStagesError }}</span>
    </div>
  </section>

  <!-- Cycle Time Boundaries -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <h2 class="text-lg font-semibold text-gray-200">Cycle Time Boundaries</h2>
    <p class="text-sm text-gray-400">Configure which workflow stages mark the start and end of cycle time measurement.</p>

    <div v-if="boundariesLoading" class="text-gray-400 text-sm">Loading boundaries...</div>

    <template v-else>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm text-gray-300 mb-1 cursor-help" title="Which workflow stage starts the cycle time clock. Default: first active work stage.">
            Cycle Starts At
          </label>
          <select
            v-model="boundaryStartStage"
            class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="boundaryAvailableStages.length === 0 || isReadOnly"
          >
            <option v-if="boundaryAvailableStages.length === 0" value="">No stages configured</option>
            <option
              v-for="stage in boundaryAvailableStages"
              :key="stage"
              :value="stage"
            >{{ stage }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm text-gray-300 mb-1 cursor-help" title="Which workflow stage stops the cycle time clock. Default: first done status.">
            Cycle Ends At
          </label>
          <select
            v-model="boundaryEndStage"
            class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="boundaryAvailableStages.length === 0 || isReadOnly"
          >
            <option v-if="boundaryAvailableStages.length === 0" value="">No stages configured</option>
            <option
              v-for="stage in boundaryAvailableStages"
              :key="stage"
              :value="stage"
            >{{ stage }}</option>
          </select>
        </div>
      </div>

      <div class="flex items-center gap-4">
        <button
          @click="saveBoundaries"
          :disabled="boundariesSaving || !boundaryStartStage || !boundaryEndStage || isReadOnly"
          class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
        >
          {{ boundariesSaving ? 'Saving...' : 'Save Boundaries' }}
        </button>
        <span v-if="boundariesSaved" class="text-green-400 text-sm">Boundaries saved.</span>
        <span v-if="boundariesError" class="text-red-400 text-sm">{{ boundariesError }}</span>
      </div>
    </template>
  </section>
</template>
