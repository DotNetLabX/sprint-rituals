<template>
  <div class="min-h-screen bg-background">
    <slot v-if="loading" name="loading">
      <div class="flex items-center justify-center min-h-screen">
        <slot name="loading-content">
          <p class="text-text-muted text-sm">Loading...</p>
        </slot>
      </div>
    </slot>

    <slot v-else-if="empty" name="empty">
      <div class="flex items-center justify-center min-h-screen">
        <slot name="empty-content">
          <p class="text-text-muted text-sm">Nothing here yet.</p>
        </slot>
      </div>
    </slot>

    <div v-else class="w-full max-w-[--page-max-width] mx-auto px-4 sm:px-8 lg:px-12 py-6 flex flex-col gap-5">
      <slot name="header" />
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  loading?: boolean
  empty?: boolean
}>(), {
  loading: false,
  empty: false,
})
</script>
