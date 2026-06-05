<script setup lang="ts">
import { reactive, ref, computed, watch } from 'vue'
import type { SprintItem } from '../../types'
import { updateSprint } from '../../api/analytics'
import type { UpdateSprintResponse } from '../../api/analytics'

const props = defineProps<{
  sprint: SprintItem
  open: boolean
}>()

const emit = defineEmits<{
  close: []
  saved: [sprint: UpdateSprintResponse]
}>()

const form = reactive({
  name: '',
  startDate: '',
  endDate: '',
  goal: ''
})

const saving = ref(false)
const error = ref<string | null>(null)
const isClosed = computed(() => props.sprint.state === 'Closed')

function toDateInput(isoString: string): string {
  return isoString.slice(0, 10)
}

watch(
  () => props.open,
  (open) => {
    if (open) {
      form.name = props.sprint.name
      form.startDate = toDateInput(props.sprint.startDate)
      form.endDate = toDateInput(props.sprint.endDate)
      form.goal = props.sprint.goal ?? ''
      error.value = null
    }
  },
  { immediate: true }
)

function onBackdropClick() {
  emit('close')
}

function onDialogClick(e: MouseEvent) {
  e.stopPropagation()
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    emit('close')
  }
}

async function onSubmit() {
  saving.value = true
  error.value = null
  try {
    const result = await updateSprint(props.sprint.id, {
      name: form.name,
      startDate: new Date(form.startDate).toISOString(),
      endDate: new Date(form.endDate).toISOString(),
      goal: form.goal || undefined
    })
    emit('saved', result)
  } catch (e: any) {
    error.value = e.message || 'Failed to update sprint.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
      @click="onBackdropClick"
      @keydown="onKeydown"
    >
      <div
        class="bg-surface-card border border-border-default rounded-lg shadow-xl w-full max-w-md mx-4 p-6 space-y-5"
        @click="onDialogClick"
      >
        <h2 class="text-lg font-semibold text-text-primary">Edit sprint: {{ sprint.name }}</h2>

        <div class="space-y-4">
          <!-- Sprint name -->
          <div>
            <label class="block text-sm text-text-secondary mb-1">
              Sprint name <span class="text-red-400">*</span>
            </label>
            <input
              v-model="form.name"
              type="text"
              class="w-full bg-surface-elevated border border-border-default rounded px-3 py-2 text-text-primary focus:outline-none focus:border-accent-default"
            />
          </div>

          <!-- Start date (not editable on closed sprints) -->
          <div v-if="!isClosed">
            <label class="block text-sm text-text-secondary mb-1">
              Start date <span class="text-red-400">*</span>
            </label>
            <input
              v-model="form.startDate"
              type="date"
              class="w-full bg-surface-elevated border border-border-default rounded px-3 py-2 text-text-primary focus:outline-none focus:border-accent-default"
            />
          </div>

          <!-- End date (not editable on closed sprints) -->
          <div v-if="!isClosed">
            <label class="block text-sm text-text-secondary mb-1">
              End date <span class="text-red-400">*</span>
            </label>
            <input
              v-model="form.endDate"
              type="date"
              class="w-full bg-surface-elevated border border-border-default rounded px-3 py-2 text-text-primary focus:outline-none focus:border-accent-default"
            />
          </div>

          <!-- Sprint goal -->
          <div>
            <label class="block text-sm text-text-secondary mb-1">Sprint goal</label>
            <textarea
              v-model="form.goal"
              rows="3"
              class="w-full bg-surface-elevated border border-border-default rounded px-3 py-2 text-text-primary focus:outline-none focus:border-accent-default resize-none"
            />
          </div>
        </div>

        <!-- Buttons + error -->
        <div class="flex items-center gap-3 pt-1">
          <button
            @click="emit('close')"
            class="border border-border-default text-text-secondary hover:text-text-primary px-4 py-2 rounded text-sm"
          >
            Cancel
          </button>
          <button
            @click="onSubmit"
            :disabled="saving"
            class="bg-accent-default hover:bg-accent-hover disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded text-sm font-medium"
          >
            {{ saving ? 'Updating...' : 'Update' }}
          </button>
          <span v-if="error" class="text-red-400 text-sm">{{ error }}</span>
        </div>
      </div>
    </div>
  </teleport>
</template>
