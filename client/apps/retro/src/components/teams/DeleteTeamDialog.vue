<template>
  <div
    v-if="open"
    class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
    @click.self="$emit('close')"
  >
    <div class="bg-surface rounded-lg shadow-modal p-6 w-full max-w-md mx-4">
      <!-- Active sprint blocked state -->
      <template v-if="team.hasActiveSprint">
        <h2 class="text-lg font-semibold text-text-primary mb-3">Cannot Delete Team</h2>
        <p class="text-sm text-text-secondary mb-4">
          This team has an active sprint. Close or delete it before deleting the team.
        </p>
        <button
          class="w-full px-4 py-2 bg-surface-raised text-text-secondary rounded hover:bg-border text-sm"
          @click="$emit('close')"
        >
          Close
        </button>
      </template>

      <!-- Confirmation state -->
      <template v-else>
        <h2 class="text-lg font-semibold text-text-primary mb-2">Delete Team</h2>
        <p class="text-sm text-text-secondary mb-4">
          Delete team <strong>"{{ team.name }}"</strong> and all its sprints? This cannot be undone.
        </p>

        <div class="mb-4">
          <label class="block text-sm font-medium text-text-secondary mb-1">
            Type the team name to confirm
          </label>
          <div class="relative group">
            <input
              v-model="confirmationInput"
              type="text"
              :placeholder="team.name"
              class="w-full border border-border rounded px-3 py-2 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-red-400"
            />
            <div class="absolute left-0 bottom-10 w-64 bg-surface-raised text-text-primary border border-border text-xs rounded px-2 py-1 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-10 whitespace-normal">
              Type the team name exactly as shown above to confirm deletion.
            </div>
          </div>
        </div>

        <div class="flex gap-2 justify-end">
          <button
            class="px-4 py-2 text-sm text-text-secondary hover:bg-surface-raised rounded"
            @click="$emit('close')"
          >
            Cancel
          </button>
          <button
            class="px-4 py-2 text-sm bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="confirmationInput !== team.name || busy"
            @click="handleConfirm"
          >
            {{ busy ? 'Deleting…' : 'Delete Team' }}
          </button>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import type { TeamSummary } from '../../stores/teams';

const props = defineProps<{
  team: TeamSummary;
  open: boolean;
}>();

const emit = defineEmits<{
  confirm: [teamId: number, confirmationName: string];
  close: [];
}>();

const confirmationInput = ref('');
const busy = ref(false);

watch(() => props.open, (open) => {
  if (!open) {
    confirmationInput.value = '';
    busy.value = false;
  }
});

watch(() => props.team.id, () => {
  confirmationInput.value = '';
  busy.value = false;
});

async function handleConfirm() {
  if (confirmationInput.value !== props.team.name) return;
  busy.value = true;
  try {
    emit('confirm', props.team.id, confirmationInput.value);
  } finally {
    busy.value = false;
  }
}
</script>
