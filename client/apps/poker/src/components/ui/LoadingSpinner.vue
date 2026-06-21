<template>
  <div class="flex flex-col items-center justify-center gap-2">
    <LoaderCircle class="animate-spin text-primary-500" :size="spinnerSize" />
    <span v-if="$slots.default" class="text-sm text-text-muted">
      <slot />
    </span>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { LoaderCircle } from 'lucide-vue-next'

const props = withDefaults(defineProps<{
  size?: 'sm' | 'md' | 'lg'
}>(), { size: 'md' })

const spinnerSize = computed(() => ({ sm: 16, md: 24, lg: 40 })[props.size])
</script>
