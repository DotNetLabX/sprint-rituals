<template>
  <Teleport to="body">
    <Transition name="modal">
      <div v-if="open" class="fixed inset-0 z-50 flex items-center justify-center" @keydown.escape="$emit('close')">
        <div class="fixed inset-0 bg-black/50" @click="$emit('close')" />
        <div
          class="relative z-10 w-full max-w-md bg-surface rounded-[--radius-lg] shadow-modal"
          role="dialog"
          aria-modal="true"
        >
          <div class="flex items-center justify-between px-5 py-4 border-b border-border">
            <h2 class="text-lg font-semibold text-text-primary">{{ title }}</h2>
            <button
              class="text-text-muted hover:text-text-secondary transition-colors cursor-pointer"
              @click="$emit('close')"
            >
              <X :size="20" />
            </button>
          </div>
          <div class="px-5 py-4">
            <slot />
          </div>
          <div v-if="$slots.footer" class="px-5 py-4 border-t border-border">
            <slot name="footer" />
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { watch, onUnmounted } from 'vue';
import { X } from 'lucide-vue-next';

const props = defineProps<{
  open: boolean;
  title: string;
}>();

defineEmits<{
  close: [];
}>();

watch(() => props.open, (isOpen) => {
  if (isOpen) {
    document.body.style.overflow = 'hidden';
  } else {
    document.body.style.overflow = '';
  }
});

onUnmounted(() => {
  document.body.style.overflow = '';
});
</script>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}
.modal-enter-active > .relative,
.modal-leave-active > .relative {
  transition: opacity 0.2s ease, transform 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
.modal-enter-from > .relative {
  opacity: 0;
  transform: scale(0.95);
}
.modal-leave-to > .relative {
  opacity: 0;
  transform: scale(0.95);
}
</style>
