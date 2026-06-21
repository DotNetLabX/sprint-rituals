<template>
  <Teleport to="body">
    <div class="fixed top-4 right-4 z-50 flex flex-col gap-2 pointer-events-none">
      <TransitionGroup name="toast">
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="relative overflow-hidden pointer-events-auto flex items-start gap-3 min-w-[300px] max-w-sm bg-surface border border-border rounded-[--radius-md] shadow-card-hover px-4 py-3"
        >
          <component :is="iconMap[toast.variant]" :size="18" :class="iconColorMap[toast.variant]" class="shrink-0 mt-0.5" />
          <p class="flex-1 text-sm text-text-primary">{{ toast.message }}</p>
          <button
            class="shrink-0 text-text-muted hover:text-text-secondary transition-colors cursor-pointer"
            @click="removeToast(toast.id)"
          >
            <X :size="16" />
          </button>
          <div
            class="absolute bottom-0 left-0 h-0.5 rounded-b-[--radius-md]"
            :class="barColorMap[toast.variant]"
            :style="{ animation: `toast-progress ${toast.duration}ms linear forwards` }"
          />
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { CheckCircle, XCircle, Info, X } from 'lucide-vue-next'
import { useToast } from '../../composables/useToast'

const { toasts, removeToast } = useToast()

const iconMap = { success: CheckCircle, error: XCircle, info: Info }
const iconColorMap = { success: 'text-success', error: 'text-danger', info: 'text-info' }
const barColorMap = { success: 'bg-success', error: 'bg-danger', info: 'bg-info' }
</script>

<style scoped>
.toast-enter-active { transition: all 0.3s ease; }
.toast-leave-active { transition: all 0.2s ease; }
.toast-enter-from { opacity: 0; transform: translateX(40px); }
.toast-leave-to { opacity: 0; transform: translateX(40px); }
.toast-move { transition: transform 0.3s ease; }

@keyframes toast-progress {
  from { width: 100%; }
  to { width: 0%; }
}
</style>
