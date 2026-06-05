<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'

export interface SelectOption {
  label: string
  value: string
  disabled?: boolean
  separator?: boolean
}

const props = withDefaults(defineProps<{
  options: SelectOption[]
  modelValue: string
  placeholder?: string
}>(), {
  placeholder: 'Select...'
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const open = ref(false)
const root = ref<HTMLElement>()

const selectedLabel = computed(() => {
  const opt = props.options.find(o => o.value === props.modelValue)
  return opt?.label ?? props.placeholder
})

function toggle() {
  open.value = !open.value
}

function select(option: SelectOption) {
  if (option.disabled || option.separator) return
  emit('update:modelValue', option.value)
  open.value = false
}

function onClickOutside(e: MouseEvent) {
  if (root.value && !root.value.contains(e.target as Node)) {
    open.value = false
  }
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') open.value = false
}

onMounted(() => {
  document.addEventListener('click', onClickOutside)
  document.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <div ref="root" class="relative">
    <button
      type="button"
      class="flex items-center gap-2 h-9 px-3 rounded-md border border-border-default bg-surface-card text-text-secondary text-sm min-w-44 cursor-pointer"
      @click="toggle"
    >
      <slot name="icon" />
      <span class="flex-1 text-left truncate">{{ selectedLabel }}</span>
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0 text-text-muted">
        <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
      </svg>
    </button>

    <div
      v-if="open"
      class="absolute left-0 top-full mt-1 z-50 min-w-full max-h-64 overflow-y-auto rounded-md border border-border-default bg-surface-elevated shadow-lg"
    >
      <template v-for="option in options" :key="option.value">
        <div
          v-if="option.separator"
          class="border-t border-border-default my-1"
        />
        <button
          v-else
          type="button"
          class="w-full text-left px-3 py-1.5 text-sm truncate"
          :class="[
            option.disabled
              ? 'text-text-muted cursor-default'
              : option.value === modelValue
                ? 'bg-accent-default text-white'
                : 'text-text-secondary hover:bg-surface-card cursor-pointer'
          ]"
          :disabled="option.disabled"
          @click="select(option)"
        >
          {{ option.label }}
        </button>
      </template>
    </div>
  </div>
</template>
