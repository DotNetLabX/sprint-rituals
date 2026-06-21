<template>
  <!-- Sidebar list of items — SM can select next item to estimate -->
  <div class="flex flex-col gap-1 overflow-y-auto">
    <h3 class="text-xs font-semibold text-text-muted uppercase tracking-wide mb-1">Items</h3>
    <button
      v-for="item in items"
      :key="item.id"
      :class="[
        'w-full text-left px-3 py-2 rounded-[--radius-sm] text-sm transition-colors',
        item.id === activeItemId
          ? 'bg-primary-600/20 border border-primary-500/40 text-text-primary'
          : 'text-text-secondary hover:bg-surface-raised border border-transparent',
      ]"
      :disabled="!isScrumMaster || item.status === 'Decided' || item.status === 'Skipped'"
      @click="isScrumMaster && emit('select', item.id)"
    >
      <div class="flex items-center justify-between gap-2">
        <span class="truncate">{{ item.ticketKey ? `${item.ticketKey} — ` : '' }}{{ item.label }}</span>
        <span
          :class="[
            'shrink-0 text-xs px-1.5 py-0.5 rounded-[--radius-sm]',
            statusBadge(item.status),
          ]"
        >{{ item.status }}</span>
      </div>
    </button>

    <div v-if="items.length === 0" class="text-sm text-text-muted px-3 py-2">
      No items yet.
    </div>
  </div>
</template>

<script setup lang="ts">
import type { EstimationItemResponse, ItemStatus } from '../../types'

defineProps<{
  items: EstimationItemResponse[]
  activeItemId: string | null
  isScrumMaster: boolean
}>()

const emit = defineEmits<{ select: [itemId: string] }>()

function statusBadge(status: ItemStatus): string {
  const map: Record<ItemStatus, string> = {
    Voting: 'bg-badge-info-bg text-badge-info-text',
    Revealed: 'bg-badge-warning-bg text-badge-warning-text',
    Decided: 'bg-badge-success-bg text-badge-success-text',
    Skipped: 'bg-badge-default-bg text-badge-default-text',
  }
  return map[status]
}
</script>
