<template>
  <!-- Read-only summary displayed after session is closed -->
  <div class="flex flex-col gap-6">
    <div class="grid grid-cols-3 gap-4">
      <div class="bg-surface rounded-[--radius-md] border border-border p-4 text-center">
        <p class="text-2xl font-bold text-text-primary">{{ summary.itemsEstimated }}</p>
        <p class="text-xs text-text-muted mt-1">Estimated</p>
      </div>
      <div class="bg-surface rounded-[--radius-md] border border-border p-4 text-center">
        <p class="text-2xl font-bold text-text-primary">{{ summary.itemsSkipped }}</p>
        <p class="text-xs text-text-muted mt-1">Skipped</p>
      </div>
      <div class="bg-surface rounded-[--radius-md] border border-border p-4 text-center">
        <p class="text-2xl font-bold text-primary-400">{{ summary.totalStoryPoints }}</p>
        <p class="text-xs text-text-muted mt-1">Story Points</p>
      </div>
    </div>

    <div class="flex flex-col gap-2">
      <h3 class="text-sm font-semibold text-text-secondary">Items</h3>
      <div
        v-for="item in summary.items"
        :key="item.id"
        class="flex items-center justify-between px-3 py-2 bg-surface rounded-[--radius-sm] border border-border"
      >
        <span class="text-sm text-text-primary">
          {{ item.ticketKey ? `${item.ticketKey} — ` : '' }}{{ item.label }}
        </span>
        <span
          :class="[
            'text-xs font-semibold px-2 py-0.5 rounded-[--radius-sm]',
            item.status === 'Decided' ? 'bg-badge-success-bg text-badge-success-text' : 'bg-badge-default-bg text-badge-default-text',
          ]"
        >
          {{ item.status === 'Decided' ? item.finalEstimate : item.status }}
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { SessionSummaryResponse } from '../../types'

defineProps<{ summary: SessionSummaryResponse }>()
</script>
