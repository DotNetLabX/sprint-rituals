<template>
  <span :class="badgeClasses">
    <slot />
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(defineProps<{
  variant?: 'default' | 'success' | 'danger' | 'warning' | 'info' | 'primary';
  size?: 'sm' | 'md';
}>(), {
  variant: 'default',
  size: 'sm',
});

const badgeClasses = computed(() => {
  const base = 'inline-flex items-center font-medium rounded-[--radius-full]';

  const variants = {
    default: 'bg-badge-default-bg text-badge-default-text',
    success: 'bg-badge-success-bg text-badge-success-text',
    danger: 'bg-badge-danger-bg text-badge-danger-text',
    warning: 'bg-badge-warning-bg text-badge-warning-text',
    info: 'bg-badge-info-bg text-badge-info-text',
    primary: 'bg-badge-primary-bg text-badge-primary-text',
  };

  const sizes = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-2.5 py-1 text-sm',
  };

  return [base, variants[props.variant], sizes[props.size]];
});
</script>
