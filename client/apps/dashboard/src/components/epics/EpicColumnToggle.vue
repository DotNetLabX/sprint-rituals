<script setup lang="ts">
import { ref } from 'vue'

export interface ColumnDef {
  id: string
  label: string
}

const props = defineProps<{
  columns: ColumnDef[]
  hiddenColumns: Set<string>
  hasQaData: boolean
}>()

const emit = defineEmits<{
  toggle: [columnId: string]
}>()

const QA_COLUMN_IDS = new Set(['coverageRate', 'passRate', 'bugsFound'])

const open = ref(false)

function toggleDropdown() {
  open.value = !open.value
}

function closeDropdown() {
  open.value = false
}

function visibleColumns(): ColumnDef[] {
  if (props.hasQaData) return props.columns
  return props.columns.filter(c => !QA_COLUMN_IDS.has(c.id))
}
</script>

<template>
  <div class="relative">
    <button
      type="button"
      class="flex items-center gap-1.5 px-2.5 py-1.5 text-sm text-text-secondary hover:text-text-primary border border-border-default rounded hover:bg-surface-elevated transition-colors"
      :title="'Toggle column visibility'"
      @click="toggleDropdown"
    >
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
        <path fill-rule="evenodd" d="M7.84 1.804A1 1 0 018.82 1h2.36a1 1 0 01.98.804l.331 1.652a6.993 6.993 0 011.929 1.115l1.598-.54a1 1 0 011.186.447l1.18 2.044a1 1 0 01-.205 1.251l-1.267 1.113a7.047 7.047 0 010 2.228l1.267 1.113a1 1 0 01.206 1.25l-1.18 2.045a1 1 0 01-1.187.447l-1.598-.54a6.993 6.993 0 01-1.929 1.115l-.33 1.652a1 1 0 01-.98.804H8.82a1 1 0 01-.98-.804l-.331-1.652a6.993 6.993 0 01-1.929-1.115l-1.598.54a1 1 0 01-1.186-.447l-1.18-2.044a1 1 0 01.205-1.251l1.267-1.113a7.047 7.047 0 010-2.228L1.821 7.773a1 1 0 01-.206-1.25l1.18-2.045a1 1 0 011.187-.447l1.598.54A6.993 6.993 0 017.51 3.456l.33-1.652zM10 13a3 3 0 100-6 3 3 0 000 6z" clip-rule="evenodd" />
      </svg>
      <span class="hidden sm:inline">Columns</span>
    </button>

    <!-- Dropdown -->
    <div
      v-if="open"
      class="absolute right-0 mt-1 w-48 bg-surface-elevated border border-border-default rounded shadow-lg z-20"
    >
      <div class="py-1">
        <label
          v-for="col in visibleColumns()"
          :key="col.id"
          class="flex items-center gap-2 px-3 py-1.5 text-sm text-text-primary hover:bg-surface-elevated/60 cursor-pointer select-none"
        >
          <input
            type="checkbox"
            class="accent-accent-default"
            :checked="!hiddenColumns.has(col.id)"
            @change="emit('toggle', col.id)"
          />
          {{ col.label }}
        </label>
      </div>
    </div>

    <!-- Click-outside overlay -->
    <div
      v-if="open"
      class="fixed inset-0 z-10"
      @click="closeDropdown"
    />
  </div>
</template>
