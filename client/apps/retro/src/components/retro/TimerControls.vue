<template>
  <div
    class="absolute right-0 top-full mt-1 z-30 bg-surface-raised border border-border rounded-[--radius-sm] shadow-lg p-2 flex flex-col gap-1.5 min-w-[140px]"
    @click.stop
  >
    <!-- Running: Pause, Reset, +time -->
    <template v-if="timerStatus === 'running'">
      <button class="timer-btn timer-btn-secondary" @click="invoke('PauseTimer')">Pause</button>
      <button class="timer-btn timer-btn-secondary" @click="invoke('ResetTimer')">Reset</button>
      <div class="flex gap-1 mt-0.5">
        <button class="timer-btn-add" @click="invokeAddTime(1)">+1m</button>
        <button class="timer-btn-add" @click="invokeAddTime(2)">+2m</button>
        <button class="timer-btn-add" @click="invokeAddTime(5)">+5m</button>
      </div>
    </template>

    <!-- Paused: Resume, Reset, +time -->
    <template v-else-if="timerStatus === 'paused'">
      <button class="timer-btn timer-btn-primary" @click="invoke('ResumeTimer')">Resume</button>
      <button class="timer-btn timer-btn-secondary" @click="invoke('ResetTimer')">Reset</button>
      <div class="flex gap-1 mt-0.5">
        <button class="timer-btn-add" @click="invokeAddTime(1)">+1m</button>
        <button class="timer-btn-add" @click="invokeAddTime(2)">+2m</button>
        <button class="timer-btn-add" @click="invokeAddTime(5)">+5m</button>
      </div>
    </template>

    <!-- Expired: Reset, +time -->
    <template v-else-if="timerStatus === 'expired'">
      <button class="timer-btn timer-btn-secondary" @click="invoke('ResetTimer')">Reset</button>
      <div class="flex gap-1 mt-0.5">
        <button class="timer-btn-add" @click="invokeAddTime(1)">+1m</button>
        <button class="timer-btn-add" @click="invokeAddTime(2)">+2m</button>
        <button class="timer-btn-add" @click="invokeAddTime(5)">+5m</button>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { getHubConnection } from '../../composables/useSprintHub'
import { useToast } from '../../composables/useToast'
import type { TimerStatus } from '../../composables/useTimer'

const props = defineProps<{
  sprintId: number
  timerStatus: TimerStatus
}>()

const emit = defineEmits<{
  close: []
}>()

const { addToast } = useToast()

async function invoke(method: string) {
  const conn = getHubConnection()
  if (!conn) { addToast('Not connected to server.', 'error'); return }
  try {
    await conn.invoke(method, props.sprintId)
  } catch (e: any) {
    addToast(e?.message ?? 'Timer action failed.', 'error')
  }
  emit('close')
}

async function invokeAddTime(minutes: number) {
  const conn = getHubConnection()
  if (!conn) { addToast('Not connected to server.', 'error'); return }
  try {
    await conn.invoke('AddTime', props.sprintId, minutes)
  } catch (e: any) {
    addToast(e?.message ?? 'Failed to add time.', 'error')
  }
  emit('close')
}
</script>

<style scoped>
@reference "../../assets/main.css";
.timer-btn {
  @apply w-full px-3 py-1 text-xs font-medium rounded-[--radius-sm] transition-colors cursor-pointer text-center;
}
.timer-btn-primary {
  @apply bg-primary-600 hover:bg-primary-700 text-white;
}
.timer-btn-secondary {
  @apply bg-surface-raised hover:bg-border text-text-secondary;
}
.timer-btn-add {
  @apply flex-1 px-1 py-1 text-xs font-medium rounded-[--radius-sm] bg-surface-raised hover:bg-border text-text-secondary transition-colors cursor-pointer text-center;
}
</style>
