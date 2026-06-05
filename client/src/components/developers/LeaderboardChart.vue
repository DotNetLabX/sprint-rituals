<script setup lang="ts">
defineProps<{
  developers: Array<{ displayName: string; featureSp: number; bugSp: number; totalSp: number }>
}>()
</script>

<template>
  <div
    class="flex flex-col gap-2"
    title="Blue segments are feature SP; red segments are bug SP. Sorted by total SP descending."
  >
    <div
      v-for="dev in developers"
      :key="dev.displayName"
      class="flex items-center gap-3 text-sm"
    >
      <!-- Developer name -->
      <span class="w-32 text-text-primary truncate shrink-0 text-xs">{{ dev.displayName }}</span>
      <!-- Stacked bar -->
      <div class="flex-1 h-5 bg-surface-elevated rounded overflow-hidden flex">
        <div
          v-if="dev.totalSp > 0"
          class="h-full bg-blue-500"
          :style="{ width: `${(dev.featureSp / dev.totalSp) * 100}%` }"
        />
        <div
          v-if="dev.totalSp > 0"
          class="h-full bg-red-500"
          :style="{ width: `${(dev.bugSp / dev.totalSp) * 100}%` }"
        />
      </div>
      <!-- Total SP label -->
      <span class="w-14 text-right text-text-secondary tabular-nums shrink-0 text-xs">{{ dev.totalSp.toFixed(1) }} SP</span>
    </div>
    <!-- Legend -->
    <div class="flex items-center gap-4 mt-1 text-xs text-text-muted">
      <span class="flex items-center gap-1">
        <span class="inline-block w-3 h-3 rounded-sm bg-blue-500"></span>
        Feature SP
      </span>
      <span class="flex items-center gap-1">
        <span class="inline-block w-3 h-3 rounded-sm bg-red-500"></span>
        Bug SP
      </span>
    </div>
  </div>
</template>
