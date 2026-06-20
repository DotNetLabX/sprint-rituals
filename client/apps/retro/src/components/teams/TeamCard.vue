<template>
  <div
    class="relative bg-surface border border-border rounded-lg shadow-card hover:shadow-card-hover hover:bg-surface-raised transition-all cursor-pointer p-5"
    @click="$emit('click')"
  >
    <!-- Three-dot menu -->
    <div class="absolute top-3 right-3" @click.stop>
      <div class="relative group">
        <button
          class="p-1 rounded hover:bg-surface-raised text-text-muted hover:text-text-secondary"
          :title="'Manage this team. Options include deleting the team.'"
          @click="menuOpen = !menuOpen"
        >
          <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
            <circle cx="12" cy="5" r="1.5" />
            <circle cx="12" cy="12" r="1.5" />
            <circle cx="12" cy="19" r="1.5" />
          </svg>
        </button>
        <div
          v-if="menuOpen"
          ref="menuRef"
          class="absolute right-0 mt-1 w-36 bg-surface-raised border border-border rounded shadow-lg z-20"
        >
          <button
            class="w-full text-left px-3 py-2 text-sm text-red-600 hover:bg-red-50"
            @click="handleDelete"
          >
            Delete team
          </button>
        </div>
      </div>
    </div>

    <!-- Team name -->
    <h3 class="text-base font-semibold text-text-primary pr-8 mb-3">{{ team.name }}</h3>

    <!-- Retro count -->
    <div class="flex items-center gap-1 text-sm text-text-muted mb-1">
      <span>{{ team.retroSessionCount }} retro{{ team.retroSessionCount !== 1 ? 's' : '' }}</span>
      <div class="relative group/tip">
        <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5 text-text-muted cursor-help" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M12 2a10 10 0 100 20A10 10 0 0012 2z" />
        </svg>
        <div class="absolute left-1/2 -translate-x-1/2 bottom-5 w-64 bg-surface-raised text-text-primary border border-border text-xs rounded px-2 py-1 opacity-0 group-hover/tip:opacity-100 transition-opacity pointer-events-none z-10 whitespace-normal">
          The total number of retros created under this team, including draft, active, and closed.
        </div>
      </div>
    </div>

    <!-- Last retro date -->
    <div class="flex items-center gap-1 text-sm text-text-muted">
      <span v-if="team.lastRetroSessionDate">Last retro {{ formatDate(team.lastRetroSessionDate) }}</span>
      <span v-else>No retros yet</span>
      <div v-if="team.lastRetroSessionDate" class="relative group/tip">
        <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5 text-text-muted cursor-help" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M12 2a10 10 0 100 20A10 10 0 0012 2z" />
        </svg>
        <div class="absolute left-1/2 -translate-x-1/2 bottom-5 w-64 bg-surface-raised text-text-primary border border-border text-xs rounded px-2 py-1 opacity-0 group-hover/tip:opacity-100 transition-opacity pointer-events-none z-10 whitespace-normal">
          The date when the most recent retro was created in this team.
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import type { TeamSummary } from '../../stores/teams';

const props = defineProps<{ team: TeamSummary }>();
const emit = defineEmits<{
  click: [];
  delete: [teamId: number];
}>();

const menuOpen = ref(false);
const menuRef = ref<HTMLElement | null>(null);

function handleDelete() {
  menuOpen.value = false;
  emit('delete', props.team.id);
}

function handleClickOutside(e: MouseEvent) {
  if (menuOpen.value && menuRef.value && !menuRef.value.contains(e.target as Node)) {
    menuOpen.value = false;
  }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

onMounted(() => document.addEventListener('click', handleClickOutside));
onUnmounted(() => document.removeEventListener('click', handleClickOutside));
</script>
