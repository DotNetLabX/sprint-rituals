<template>
  <div class="flex sm:hidden gap-1">
    <button
      class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
      :class="modelValue === 'Column1' ? `bg-col-${column1Color} text-white` : `bg-col-${column1Color}-light text-col-${column1Color}`"
      @click="$emit('update:modelValue', 'Column1')"
    >
      {{ column1Name }}
    </button>
    <button
      class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
      :class="modelValue === 'Column2' ? `bg-col-${column2Color} text-white` : `bg-col-${column2Color}-light text-col-${column2Color}`"
      @click="$emit('update:modelValue', 'Column2')"
    >
      {{ column2Name }}
    </button>
    <button
      class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
      :class="modelValue === 'Column3' ? `bg-col-${column3Color} text-white` : `bg-col-${column3Color}-light text-col-${column3Color}`"
      @click="$emit('update:modelValue', 'Column3')"
    >
      {{ column3Name }}
    </button>
    <button
      v-if="showActionItems"
      class="flex-1 px-3 py-2 text-sm font-medium rounded-[--radius-sm] transition-colors cursor-pointer"
      :class="modelValue === 'ActionItems' ? 'bg-action-items text-white' : 'bg-action-items-light text-action-items'"
      @click="$emit('update:modelValue', 'ActionItems')"
    >
      Actions
    </button>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  modelValue: 'Column1' | 'Column2' | 'Column3' | 'ActionItems'
  showActionItems: boolean
  column1Name: string
  column1Color: string
  column2Name: string
  column2Color: string
  column3Name: string
  column3Color: string
}>()

defineEmits<{
  'update:modelValue': [value: 'Column1' | 'Column2' | 'Column3' | 'ActionItems']
}>()
</script>
