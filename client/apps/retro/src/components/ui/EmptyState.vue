<template>
  <div class="flex flex-col items-center justify-center py-8 gap-3">
    <component v-if="icon" :is="icon" class="w-12 h-12 text-text-muted" />
    <div class="text-center">
      <p class="text-sm font-medium text-text-secondary">{{ title }}</p>
      <p v-if="description" class="mt-1 text-xs text-text-muted">{{ description }}</p>
    </div>
    <div v-if="$slots.default" class="mt-2">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Component } from 'vue';

defineProps<{
  title: string;
  description?: string;
  icon?: Component;
}>();
</script>
