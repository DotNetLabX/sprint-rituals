<script setup lang="ts">
defineProps<{
  title: string
  description: string
  actionText?: string
  actionRoute?: string
}>()
</script>

<template>
  <div class="flex flex-col items-center justify-center h-full min-h-64 gap-4 text-center px-6 py-12">
    <!-- Icon slot -->
    <div class="text-text-muted w-12 h-12 flex items-center justify-center">
      <slot name="icon" />
    </div>

    <div class="flex flex-col gap-2 max-w-sm">
      <h2 class="text-lg font-semibold text-text-primary">{{ title }}</h2>
      <p class="text-sm text-text-secondary leading-relaxed">{{ description }}</p>
    </div>

    <RouterLink
      v-if="actionText && actionRoute"
      :to="actionRoute"
      class="mt-2 inline-flex items-center px-4 py-2 rounded-md text-sm font-medium bg-accent-default hover:bg-accent-hover text-white transition-colors"
    >
      {{ actionText }}
    </RouterLink>
  </div>
</template>
