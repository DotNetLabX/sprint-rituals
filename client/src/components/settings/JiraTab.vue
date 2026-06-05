<script setup lang="ts">
import { inject, ref, computed, onMounted } from 'vue'
import { getExcludedStatuses, saveExcludedStatuses } from '../../api/settings'
import {
  settingsFormKey,
  boardsKey,
  boardsLoadingKey,
  boardsErrorKey,
  statusesKey,
  statusesLoadingKey,
  statusesErrorKey,
  isReadOnlyKey,
  settingsStoreKey,
} from './injectionKeys'

const form = inject(settingsFormKey)!
const boards = inject(boardsKey)!
const boardsLoading = inject(boardsLoadingKey)!
const boardsError = inject(boardsErrorKey)!
const statuses = inject(statusesKey)!
const statusesLoading = inject(statusesLoadingKey)!
const statusesError = inject(statusesErrorKey)!
const isReadOnly = inject(isReadOnlyKey)!
const store = inject(settingsStoreKey)!

// Board panel save state
const boardSaving = ref(false)
const boardSaved = ref(false)
const boardError = ref('')

// Done statuses panel save state
const doneStatusesSaving = ref(false)
const doneStatusesSaved = ref(false)
const doneStatusesError = ref('')

// Done statuses dropdown state
const newStatus = ref('')
const doneStatusOpen = ref(false)
const selectedStatus = ref<string[]>([])

// Excluded statuses state
const excludedStatuses = ref<string[]>([])
const selectedExcludedStatus = ref<string[]>([])
const excludedStatusOpen = ref(false)
const excludedSaving = ref(false)
const excludedSaved = ref(false)
const excludedError = ref('')

const availableStatuses = computed(() => {
  const notAdded = statuses.value.filter(s => !form.doneStatuses.includes(s.name))
  const done = notAdded.filter(s => s.categoryKey === 'done')
  const rest = notAdded.filter(s => s.categoryKey !== 'done')
  return [...done, ...rest]
})

const availableExcludedStatuses = computed(() => {
  return statuses.value.filter(s => !excludedStatuses.value.includes(s.name))
})

onMounted(async () => {
  try {
    excludedStatuses.value = await getExcludedStatuses()
  } catch {
    // silently ignore — statuses will be empty
  }
})

async function saveBoardPanel() {
  boardSaved.value = false
  boardError.value = ''
  boardSaving.value = true
  try {
    await store.saveBoardAction(form.boardId)
    if (!store.error) {
      boardSaved.value = true
      setTimeout(() => boardSaved.value = false, 3000)
    } else {
      boardError.value = store.error
    }
  } catch (e: any) {
    boardError.value = e.message || 'Failed to save board.'
  } finally {
    boardSaving.value = false
  }
}

async function saveDoneStatusesPanel() {
  doneStatusesSaved.value = false
  doneStatusesError.value = ''
  doneStatusesSaving.value = true
  try {
    await store.saveDoneStatusesAction([...form.doneStatuses])
    if (!store.error) {
      doneStatusesSaved.value = true
      setTimeout(() => doneStatusesSaved.value = false, 3000)
    } else {
      doneStatusesError.value = store.error
    }
  } catch (e: any) {
    doneStatusesError.value = e.message || 'Failed to save done statuses.'
  } finally {
    doneStatusesSaving.value = false
  }
}

function addStatus() {
  if (statuses.value.length > 0 && !statusesError.value) {
    for (const val of selectedStatus.value) {
      if (val && !form.doneStatuses.includes(val)) {
        form.doneStatuses.push(val)
      }
    }
    selectedStatus.value = []
    doneStatusOpen.value = false
  } else {
    const val = newStatus.value.trim()
    if (val && !form.doneStatuses.includes(val)) {
      form.doneStatuses.push(val)
    }
    newStatus.value = ''
  }
}

function removeStatus(index: number) {
  form.doneStatuses.splice(index, 1)
}

function addExcludedStatus() {
  if (statuses.value.length > 0 && !statusesError.value) {
    for (const val of selectedExcludedStatus.value) {
      if (val && !excludedStatuses.value.includes(val)) {
        excludedStatuses.value.push(val)
      }
    }
    selectedExcludedStatus.value = []
    excludedStatusOpen.value = false
  }
}

function removeExcludedStatus(index: number) {
  excludedStatuses.value.splice(index, 1)
}

async function saveExcluded() {
  excludedSaved.value = false
  excludedError.value = ''
  excludedSaving.value = true
  try {
    const result = await saveExcludedStatuses(excludedStatuses.value)
    excludedStatuses.value = result
    excludedSaved.value = true
    setTimeout(() => excludedSaved.value = false, 3000)
  } catch (e: any) {
    excludedError.value = e.message || 'Failed to save excluded statuses.'
  } finally {
    excludedSaving.value = false
  }
}
</script>

<template>
  <!-- Jira Board -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <div class="flex items-center gap-1">
      <h2 class="text-lg font-semibold text-gray-200">Jira Board</h2>
      <span
        class="text-gray-500 cursor-help"
        title="Link Fokus to your Jira instance using your email and an API token."
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
        </svg>
      </span>
    </div>
    <div>
      <label class="block text-sm text-gray-400 mb-1 cursor-help" title="Choose which Jira board to sync sprints from. Only one board is supported.">Board</label>
      <!-- Dropdown when boards loaded successfully -->
      <template v-if="!boardsError && !boardsLoading && boards.length > 0">
        <select
          v-model.number="form.boardId"
          :disabled="isReadOnly"
          class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <option :value="null">-- Select a board --</option>
          <option v-for="board in boards" :key="board.id" :value="board.id">
            {{ board.name }} ({{ board.type }})
          </option>
        </select>
      </template>
      <!-- Loading state -->
      <template v-else-if="boardsLoading">
        <input
          v-model.number="form.boardId"
          type="number"
          placeholder="Loading boards..."
          disabled
          class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-500 placeholder-gray-600 focus:outline-none cursor-not-allowed"
        />
      </template>
      <!-- Fallback: numeric input on error or no boards -->
      <template v-else>
        <input
          v-model.number="form.boardId"
          type="number"
          placeholder="Enter Jira board ID"
          :disabled="isReadOnly"
          class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        />
        <p v-if="boardsError" class="text-xs text-amber-400 mt-1">Could not load boards from Jira.</p>
      </template>
    </div>
    <div class="flex items-center gap-4">
      <button
        @click="saveBoardPanel"
        :disabled="boardSaving || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ boardSaving ? 'Saving...' : 'Save Board' }}
      </button>
      <span v-if="boardSaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="boardError" class="text-red-400 text-sm">{{ boardError }}</span>
    </div>
  </section>

  <!-- Done Statuses -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <h2 class="text-lg font-semibold text-gray-200">Done Statuses</h2>
    <p class="text-sm text-gray-400">Status names that count as "done" when computing metrics.</p>
    <div class="flex flex-wrap gap-2">
      <span
        v-for="(status, i) in form.doneStatuses"
        :key="status"
        class="inline-flex items-center gap-1 bg-gray-800 text-gray-200 px-3 py-1 rounded-full text-sm"
      >
        {{ status }}
        <button @click="removeStatus(i)" :disabled="isReadOnly" class="text-gray-500 hover:text-red-400 ml-1 disabled:opacity-50 disabled:cursor-not-allowed">&times;</button>
      </span>
    </div>
    <!-- Dropdown mode -->
    <template v-if="!statusesError && !statusesLoading && statuses.length > 0">
      <div class="flex gap-2 items-center">
        <div class="relative flex-1">
          <div v-if="doneStatusOpen" class="fixed inset-0 z-[9]" @click="doneStatusOpen = false"></div>
          <button
            type="button"
            @click="doneStatusOpen = !doneStatusOpen"
            :disabled="isReadOnly"
            class="w-full flex items-center justify-between bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-left focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed"
            :class="doneStatusOpen ? 'border-blue-500' : ''"
          >
            <span :class="selectedStatus.length ? 'text-gray-100' : 'text-gray-500'">
              {{ selectedStatus.length ? `${selectedStatus.length} status${selectedStatus.length > 1 ? 'es' : ''} selected` : 'Select statuses...' }}
            </span>
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-gray-400 transition-transform" :class="doneStatusOpen ? 'rotate-180' : ''" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
          <div
            v-if="doneStatusOpen"
            class="absolute z-10 w-full bottom-full mb-1 bg-gray-800 border border-gray-700 rounded shadow-lg max-h-48 overflow-y-auto"
          >
            <label
              v-for="s in availableStatuses"
              :key="s.name"
              class="flex items-center gap-2 px-3 py-2 hover:bg-gray-700 cursor-pointer text-sm text-gray-100"
            >
              <input type="checkbox" :value="s.name" v-model="selectedStatus" class="accent-blue-500" />
              <span>{{ s.name }}<template v-if="s.categoryKey === 'done'"> *</template></span>
            </label>
            <p v-if="availableStatuses.length === 0" class="px-3 py-2 text-sm text-gray-500 italic">All statuses added</p>
          </div>
        </div>
        <button
          @click="addStatus"
          :disabled="!selectedStatus.length || isReadOnly"
          class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded"
        >Add</button>
      </div>
      <p class="text-xs text-gray-500">* Statuses marked with * are in the "done" category.</p>
    </template>
    <!-- Loading state -->
    <template v-else-if="statusesLoading">
      <div class="flex gap-2">
        <input
          placeholder="Loading statuses..."
          disabled
          class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-500 placeholder-gray-600 focus:outline-none cursor-not-allowed"
        />
        <button disabled class="bg-blue-600 opacity-50 cursor-not-allowed text-white px-4 py-2 rounded">Add</button>
      </div>
    </template>
    <!-- Fallback: free-text input on error -->
    <template v-else>
      <div class="flex gap-2">
        <input
          v-model="newStatus"
          @keyup.enter="addStatus"
          placeholder="Add status..."
          :disabled="isReadOnly"
          class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        />
        <button @click="addStatus" :disabled="isReadOnly" class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded">Add</button>
      </div>
      <p v-if="statusesError" class="text-xs text-amber-400 mt-1">Could not load statuses from Jira.</p>
    </template>
    <div class="flex items-center gap-4">
      <button
        @click="saveDoneStatusesPanel"
        :disabled="doneStatusesSaving || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ doneStatusesSaving ? 'Saving...' : 'Save Done Statuses' }}
      </button>
      <span v-if="doneStatusesSaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="doneStatusesError" class="text-red-400 text-sm">{{ doneStatusesError }}</span>
    </div>
  </section>

  <!-- Excluded From Scope Statuses -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <h2 class="text-lg font-semibold text-gray-200">Excluded From Scope Statuses</h2>
    <p class="text-sm text-gray-400">Tickets in these statuses are excluded from committed scope when computing metrics. Useful for parked or blocked statuses that shouldn't count as active work.</p>
    <div class="flex flex-wrap gap-2">
      <span
        v-for="(status, i) in excludedStatuses"
        :key="status"
        class="inline-flex items-center gap-1 bg-gray-800 text-gray-200 px-3 py-1 rounded-full text-sm"
      >
        {{ status }}
        <button @click="removeExcludedStatus(i)" :disabled="isReadOnly" class="text-gray-500 hover:text-red-400 ml-1 disabled:opacity-50 disabled:cursor-not-allowed">&times;</button>
      </span>
    </div>
    <!-- Dropdown mode -->
    <template v-if="!statusesError && !statusesLoading && statuses.length > 0">
      <div class="flex gap-2 items-center">
        <div class="relative flex-1">
          <div v-if="excludedStatusOpen" class="fixed inset-0 z-[9]" @click="excludedStatusOpen = false"></div>
          <button
            type="button"
            @click="excludedStatusOpen = !excludedStatusOpen"
            :disabled="isReadOnly"
            class="w-full flex items-center justify-between bg-gray-800 border border-gray-700 rounded px-3 py-2 text-sm text-left focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed"
            :class="excludedStatusOpen ? 'border-blue-500' : ''"
          >
            <span :class="selectedExcludedStatus.length ? 'text-gray-100' : 'text-gray-500'">
              {{ selectedExcludedStatus.length ? `${selectedExcludedStatus.length} status${selectedExcludedStatus.length > 1 ? 'es' : ''} selected` : 'Select statuses...' }}
            </span>
            <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 text-gray-400 transition-transform" :class="excludedStatusOpen ? 'rotate-180' : ''" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </button>
          <div
            v-if="excludedStatusOpen"
            class="absolute z-10 w-full bottom-full mb-1 bg-gray-800 border border-gray-700 rounded shadow-lg max-h-48 overflow-y-auto"
          >
            <label
              v-for="s in availableExcludedStatuses"
              :key="s.name"
              class="flex items-center gap-2 px-3 py-2 hover:bg-gray-700 cursor-pointer text-sm text-gray-100"
            >
              <input type="checkbox" :value="s.name" v-model="selectedExcludedStatus" class="accent-blue-500" />
              <span>{{ s.name }}</span>
            </label>
            <p v-if="availableExcludedStatuses.length === 0" class="px-3 py-2 text-sm text-gray-500 italic">All statuses added</p>
          </div>
        </div>
        <button
          @click="addExcludedStatus"
          :disabled="!selectedExcludedStatus.length || isReadOnly"
          class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded"
        >Add</button>
      </div>
    </template>
    <!-- Loading state -->
    <template v-else-if="statusesLoading">
      <div class="flex gap-2">
        <input
          placeholder="Loading statuses..."
          disabled
          class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-500 placeholder-gray-600 focus:outline-none cursor-not-allowed"
        />
        <button disabled class="bg-blue-600 opacity-50 cursor-not-allowed text-white px-4 py-2 rounded">Add</button>
      </div>
    </template>
    <!-- Fallback: no statuses loaded -->
    <template v-else>
      <p class="text-xs text-gray-500">Load statuses from Jira to add excluded statuses.</p>
    </template>
    <div class="flex items-center gap-4">
      <button
        @click="saveExcluded"
        :disabled="excludedSaving || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ excludedSaving ? 'Saving...' : 'Save Excluded Statuses' }}
      </button>
      <span v-if="excludedSaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="excludedError" class="text-red-400 text-sm">{{ excludedError }}</span>
    </div>
  </section>
</template>
