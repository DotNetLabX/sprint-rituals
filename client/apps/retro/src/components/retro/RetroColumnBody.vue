<template>
  <RetroColumnPanel
    :title="title"
    :column-color="columnColor"
    :badge-variant="badgeVariant"
    :count="count"
    :visible="visible"
    :extra-class="extraClass"
    :is-scrum-master="isScrumMaster"
    @drop="emit('drop', $event)"
    @update-config="(name, color) => emit('update-config', name, color)"
  >
    <template #empty>
      <slot name="empty" />
    </template>

    <slot />

    <template #input>
      <slot name="input" />
    </template>
  </RetroColumnPanel>
</template>

<script setup lang="ts">
import RetroColumnPanel from './RetroColumnPanel.vue'

defineProps<{
  title: string
  columnColor: string
  badgeVariant: 'success' | 'danger' | 'info' | 'primary'
  count: number
  visible: boolean
  extraClass?: string
  isScrumMaster: boolean
}>()

const emit = defineEmits<{
  drop: [event: DragEvent]
  'update-config': [name: string, color: string]
}>()
</script>
