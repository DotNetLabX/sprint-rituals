<template>
  <div>
    <input
      :type="type"
      :value="modelValue"
      :placeholder="placeholder"
      :maxlength="maxlength"
      :disabled="disabled"
      :class="inputClasses"
      v-bind="$attrs"
      @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
    />
    <p v-if="error" class="mt-1 text-sm text-danger">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(defineProps<{
  modelValue?: string | number;
  type?: string;
  placeholder?: string;
  maxlength?: number;
  disabled?: boolean;
  error?: string;
}>(), {
  type: 'text',
  disabled: false,
});

defineEmits<{
  'update:modelValue': [value: string];
}>();

const inputClasses = computed(() => {
  const base = 'block w-full px-3 py-2 text-sm border rounded-[--radius-md] transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500 ring-offset-focus-offset';

  if (props.disabled) {
    return [base, 'bg-surface-raised text-text-muted cursor-not-allowed border-border'];
  }
  if (props.error) {
    return [base, 'border-danger text-text-primary focus:ring-danger focus:border-danger'];
  }
  return [base, 'border-border text-text-primary bg-surface'];
});
</script>
