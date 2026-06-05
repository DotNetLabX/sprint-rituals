<script setup lang="ts">
defineProps<{
  title: string
}>()
</script>

<template>
  <div class="flex flex-col gap-4 h-full">
    <!-- Title area -->
    <div class="flex items-center justify-between">
      <h1 class="text-2xl font-bold text-text-primary">{{ title }}</h1>
    </div>

    <!-- Toolbar area (optional) -->
    <div v-if="$slots.toolbar">
      <slot name="toolbar" />
    </div>

    <!-- Content area -->
    <div class="flex-1">
      <slot />
    </div>
  </div>
</template>
