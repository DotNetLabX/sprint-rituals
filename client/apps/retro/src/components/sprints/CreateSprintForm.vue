<template>
  <div class="flex flex-col gap-2 pt-2 border-t border-border-subtle">
    <h3 class="text-sm font-semibold text-text-secondary">New Sprint</h3>
    <input
      v-model="sprintName"
      type="text"
      placeholder="Sprint name"
      maxlength="100"
      class="border border-border rounded px-3 py-2 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
    />
    <input
      v-model.number="votesPerParticipant"
      type="number"
      placeholder="Votes per participant (default 3)"
      min="1"
      class="border border-border rounded px-3 py-2 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
    />

    <!-- Column configuration rows -->
    <div class="flex flex-col gap-2 mt-1">
      <p class="text-xs font-medium text-text-muted uppercase tracking-wide">Column Configuration</p>
      <!-- Column 1 -->
      <div class="flex items-center gap-2">
        <input v-model="col1.name" type="text" placeholder="Column 1 name" maxlength="30"
          class="flex-1 border border-border rounded px-3 py-1.5 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500" />
        <div class="flex gap-1">
          <button v-for="color in paletteColors" :key="color" type="button" :title="color"
            class="w-5 h-5 rounded-full border-2 transition-transform cursor-pointer"
            :class="[`bg-col-${color}`, col1.color === color ? 'border-text-primary scale-110' : 'border-transparent']"
            @click="col1.color = color" />
        </div>
      </div>
      <!-- Column 2 -->
      <div class="flex items-center gap-2">
        <input v-model="col2.name" type="text" placeholder="Column 2 name" maxlength="30"
          class="flex-1 border border-border rounded px-3 py-1.5 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500" />
        <div class="flex gap-1">
          <button v-for="color in paletteColors" :key="color" type="button" :title="color"
            class="w-5 h-5 rounded-full border-2 transition-transform cursor-pointer"
            :class="[`bg-col-${color}`, col2.color === color ? 'border-text-primary scale-110' : 'border-transparent']"
            @click="col2.color = color" />
        </div>
      </div>
      <!-- Column 3 -->
      <div class="flex items-center gap-2">
        <input v-model="col3.name" type="text" placeholder="Column 3 name" maxlength="30"
          class="flex-1 border border-border rounded px-3 py-1.5 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500" />
        <div class="flex gap-1">
          <button v-for="color in paletteColors" :key="color" type="button" :title="color"
            class="w-5 h-5 rounded-full border-2 transition-transform cursor-pointer"
            :class="[`bg-col-${color}`, col3.color === color ? 'border-text-primary scale-110' : 'border-transparent']"
            @click="col3.color = color" />
        </div>
      </div>
    </div>

    <!-- Timer defaults -->
    <div class="flex flex-col gap-2 mt-1">
      <p class="text-xs font-medium text-text-muted uppercase tracking-wide">Timer Defaults (minutes)</p>
      <div class="flex gap-2">
        <div class="flex flex-col gap-1 flex-1">
          <label class="text-xs text-text-secondary">Writing</label>
          <input
            v-model.number="writingTimerDefault"
            type="number"
            min="1"
            max="60"
            class="border border-border rounded px-2 py-1.5 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
        </div>
        <div class="flex flex-col gap-1 flex-1">
          <label class="text-xs text-text-secondary">Voting</label>
          <input
            v-model.number="votingTimerDefault"
            type="number"
            min="1"
            max="60"
            class="border border-border rounded px-2 py-1.5 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
        </div>
        <div class="flex flex-col gap-1 flex-1">
          <label class="text-xs text-text-secondary">Discussing</label>
          <input
            v-model.number="discussingTimerDefault"
            type="number"
            min="1"
            max="60"
            class="border border-border rounded px-2 py-1.5 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
        </div>
      </div>
    </div>

    <p v-if="error" class="text-danger text-sm">{{ error }}</p>
    <button
      class="px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700 text-sm disabled:opacity-50"
      :disabled="busy"
      @click="handleCreate"
    >
      {{ busy ? 'Creating…' : 'Create Sprint' }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useSprintStore } from '../../stores/sprint'

const PALETTE_COLORS = ['green', 'red', 'amber', 'blue', 'purple', 'teal', 'pink', 'slate'] as const
type PaletteColor = typeof PALETTE_COLORS[number]

const paletteColors = PALETTE_COLORS

const DEFAULT_COLUMNS = [
  { name: 'Went Well', color: 'green' as PaletteColor },
  { name: "Didn't Go Well", color: 'red' as PaletteColor },
  { name: 'Start Doing', color: 'amber' as PaletteColor },
]

const emit = defineEmits<{
  create: [payload: {
    name: string
    votesPerParticipant: number | undefined
    column1Name: string
    column1Color: string
    column2Name: string
    column2Color: string
    column3Name: string
    column3Color: string
    writingTimerDefault: number
    votingTimerDefault: number
    discussingTimerDefault: number
  }]
}>()

const sprintStore = useSprintStore()
const sprintName = ref('')
const votesPerParticipant = ref<number | undefined>(undefined)
const error = ref('')
const busy = ref(false)
const writingTimerDefault = ref(5)
const votingTimerDefault = ref(3)
const discussingTimerDefault = ref(10)

const col1 = ref({ name: DEFAULT_COLUMNS[0]!.name, color: DEFAULT_COLUMNS[0]!.color as string })
const col2 = ref({ name: DEFAULT_COLUMNS[1]!.name, color: DEFAULT_COLUMNS[1]!.color as string })
const col3 = ref({ name: DEFAULT_COLUMNS[2]!.name, color: DEFAULT_COLUMNS[2]!.color as string })

onMounted(() => {
  // Pre-fill from most recent sprint if available
  const mostRecent = sprintStore.sprints[0]
  if (mostRecent) {
    col1.value.name = mostRecent.column1Name
    col1.value.color = mostRecent.column1Color
    col2.value.name = mostRecent.column2Name
    col2.value.color = mostRecent.column2Color
    col3.value.name = mostRecent.column3Name
    col3.value.color = mostRecent.column3Color
  }
})

function validateTimerDefault(value: number, label: string): boolean {
  if (!Number.isInteger(value) || value < 1 || value > 60) {
    error.value = `${label} timer must be between 1 and 60 minutes.`
    return false
  }
  return true
}

async function handleCreate() {
  error.value = ''
  const name = sprintName.value.trim()
  if (!name) { error.value = 'Sprint name is required.'; return }
  if (!col1.value.name.trim()) { error.value = 'Column 1 name is required.'; return }
  if (!col2.value.name.trim()) { error.value = 'Column 2 name is required.'; return }
  if (!col3.value.name.trim()) { error.value = 'Column 3 name is required.'; return }
  if (!validateTimerDefault(writingTimerDefault.value, 'Writing')) return
  if (!validateTimerDefault(votingTimerDefault.value, 'Voting')) return
  if (!validateTimerDefault(discussingTimerDefault.value, 'Discussing')) return
  busy.value = true
  try {
    emit('create', {
      name,
      votesPerParticipant: votesPerParticipant.value,
      column1Name: col1.value.name.trim(),
      column1Color: col1.value.color,
      column2Name: col2.value.name.trim(),
      column2Color: col2.value.color,
      column3Name: col3.value.name.trim(),
      column3Color: col3.value.color,
      writingTimerDefault: writingTimerDefault.value,
      votingTimerDefault: votingTimerDefault.value,
      discussingTimerDefault: discussingTimerDefault.value,
    })
    sprintName.value = ''
    votesPerParticipant.value = undefined
  } finally {
    busy.value = false
  }
}
</script>
