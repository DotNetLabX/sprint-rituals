<template>
  <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
    <div class="flex items-center gap-3 min-w-0">
      <slot name="title" />
    </div>
    <div v-if="$slots.filters" class="flex items-center gap-2 flex-wrap">
      <slot name="filters" />
    </div>
    <div v-if="$slots.actions" class="flex items-center gap-3 shrink-0">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup lang="ts">
// No props — PageToolbar is purely slot-based for maximum composability.
// Use the title, filters, and actions slots to fill each region.
</script>
