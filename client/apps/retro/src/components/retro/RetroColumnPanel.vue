<template>
  <div
    v-show="visible"
    class="bg-surface border border-border rounded-[--radius-md] shadow-card flex flex-col min-h-0"
    :class="extraClass"
  >
    <!-- Column header -->
    <div
      class="px-4 py-3 border-b rounded-t-[--radius-md] flex items-center gap-2"
      :class="`bg-col-${columnColor}-light`"
    >
      <!-- Inline edit mode (SM only) -->
      <template v-if="editing">
        <input
          ref="nameInput"
          v-model="editName"
          type="text"
          maxlength="30"
          class="flex-1 text-sm font-semibold bg-surface border border-border rounded px-2 py-0.5 focus:outline-none focus:ring-2 focus:ring-primary-500"
          :class="`text-col-${columnColor}`"
          @keydown.enter="confirmEdit"
          @blur="confirmEdit"
        />
        <div class="flex gap-1">
          <button
            v-for="color in paletteColors"
            :key="color"
            type="button"
            :title="color"
            class="w-4 h-4 rounded-full border-2 transition-transform cursor-pointer"
            :class="[
              `bg-col-${color}`,
              editColor === color ? 'border-text-primary scale-110' : 'border-transparent',
            ]"
            @mousedown.prevent="selectColor(color)"
          />
        </div>
      </template>

      <!-- Display mode -->
      <template v-else>
        <h2
          class="font-semibold text-base flex-1"
          :class="[`text-col-${columnColor}`, isScrumMaster ? 'cursor-pointer hover:underline' : '']"
          @click="isScrumMaster ? startEdit() : undefined"
        >{{ title }}</h2>
      </template>

      <BaseBadge :variant="badgeVariant">{{ count }}</BaseBadge>
    </div>

    <!-- Card/group list -->
    <TransitionGroup
      name="card-list"
      tag="div"
      class="flex-1 flex flex-col gap-2 p-3 overflow-y-auto min-h-0 relative"
      :class="{ 'bg-primary-500/10': dragOver }"
      @dragover="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <slot name="empty" />
      <slot />
    </TransitionGroup>

    <!-- Add input -->
    <div v-if="$slots.input" class="p-3 border-t">
      <slot name="input" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, nextTick } from 'vue'
import BaseBadge from '../ui/BaseBadge.vue'

const PALETTE_COLORS = ['green', 'red', 'amber', 'blue', 'purple', 'teal', 'pink', 'slate'] as const
const paletteColors = PALETTE_COLORS

const props = defineProps<{
  title: string
  columnColor: string
  badgeVariant: 'success' | 'danger' | 'info' | 'primary'
  count: number
  visible: boolean
  extraClass?: string
  isScrumMaster: boolean
}>()

const emit = defineEmits<{
  drop: [e: DragEvent]
  'update-config': [name: string, color: string]
}>()

const dragOver = ref(false)
const editing = ref(false)
const editName = ref('')
const editColor = ref('')
const nameInput = ref<HTMLInputElement | null>(null)

function startEdit() {
  editName.value = props.title
  editColor.value = props.columnColor
  editing.value = true
  nextTick(() => nameInput.value?.focus())
}

function confirmEdit() {
  if (!editing.value) return
  editing.value = false
  const name = editName.value.trim()
  if (!name || (name === props.title && editColor.value === props.columnColor)) return
  emit('update-config', name || props.title, editColor.value)
}

function selectColor(color: string) {
  editColor.value = color
}

function onDragOver(e: DragEvent) {
  try {
    const types = e.dataTransfer?.types ?? []
    if (!types.includes('application/json')) return
  } catch {
    // Firefox: allow drop
  }
  e.preventDefault()
  e.dataTransfer!.dropEffect = 'move'
  dragOver.value = true
}

function onDragLeave(e: DragEvent) {
  // dragleave also fires when moving onto a child element; only clear when the pointer exits the column.
  if ((e.currentTarget as Node).contains(e.relatedTarget as Node | null)) return
  dragOver.value = false
}

function onDrop(e: DragEvent) {
  e.preventDefault()
  dragOver.value = false
  emit('drop', e)
}
</script>

<style scoped>
.card-list-enter-active,
.card-list-leave-active {
  transition: all 0.2s ease;
}
.card-list-enter-from,
.card-list-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
