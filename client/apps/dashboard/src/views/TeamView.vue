<script setup lang="ts">
import { onMounted } from 'vue'
import { useTeamStore } from '../stores/teamStore'
import PageLayout from '../components/PageLayout.vue'
import TeamTable from '../components/team/TeamTable.vue'
import type { UpdateTeamConfigRequest } from '../types'

const store = useTeamStore()

onMounted(async () => {
  await store.fetchTeam()
})

async function onUpdate(accountId: string, config: UpdateTeamConfigRequest) {
  try {
    await store.updateConfig(accountId, config)
  } catch {
    // Error is set on the store; row already reverted by store
  }
}
</script>

<template>
  <PageLayout title="Team">
    <!-- Loading -->
    <div v-if="store.loading" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- Error -->
    <div v-else-if="store.error" class="text-sm text-status-danger">
      {{ store.error }}
    </div>

    <!-- Empty state -->
    <div v-else-if="store.developers.length === 0" class="flex flex-col items-center justify-center h-64 gap-3 text-text-muted">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-10 h-10 opacity-40">
        <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v1h8v-1zM6 8a2 2 0 11-4 0 2 2 0 014 0zM16 18v-1a5.972 5.972 0 00-.75-2.906A3.005 3.005 0 0119 15v1h-3zM4.75 12.094A5.973 5.973 0 004 15v1H1v-1a3 3 0 013.75-2.906z" />
      </svg>
      <p class="text-sm">No developers found. Sync a sprint to discover team members.</p>
    </div>

    <!-- Data -->
    <TeamTable
      v-else
      :grouped-by-sub-team="store.groupedBySubTeam"
      :sub-teams="store.subTeams"
      @update="onUpdate"
    />
  </PageLayout>
</template>
