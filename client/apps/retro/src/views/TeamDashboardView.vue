<template>
  <div class="flex flex-col items-center min-h-[calc(100vh-3.5rem)] p-8">
    <!-- Not signed in -->
    <div v-if="authStore.loaded && !authStore.isSignedIn" class="flex flex-col items-center gap-4 mt-16">
      <h1 class="text-2xl font-semibold text-text-primary">Welcome to Reflekt</h1>
      <button
        class="px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700"
        @click="authStore.signIn()"
      >
        Sign in with Google
      </button>
    </div>

    <!-- Signed in -->
    <template v-else-if="authStore.isSignedIn">
      <!-- Empty state: no teams -->
      <div v-if="teamsStore.loaded && !teamsStore.hasTeams" class="mt-16">
        <CreateTeamForm @created="handleTeamCreated" />
      </div>

      <!-- Teams card grid -->
      <div v-else class="w-full max-w-4xl">
        <div class="flex items-center justify-between mb-6">
          <h1 class="text-2xl font-semibold text-text-primary">Your Teams</h1>
          <div class="relative group">
            <button
              class="px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700 text-sm disabled:opacity-50 disabled:cursor-not-allowed"
              :disabled="teamsStore.isAtLimit"
              @click="showCreateForm = true"
            >
              Create Team
            </button>
            <div
              v-if="teamsStore.isAtLimit"
              class="absolute right-0 mt-1 w-64 bg-surface-raised text-text-primary border border-border text-xs rounded px-2 py-1 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-10"
            >
              You've reached the maximum of 10 teams. Delete a team to create a new one.
            </div>
            <div
              v-else
              class="absolute right-0 mt-1 w-52 bg-surface-raised text-text-primary border border-border text-xs rounded px-2 py-1 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-10"
            >
              Create a new team. You can manage up to 10 teams.
            </div>
          </div>
        </div>

        <div v-if="showCreateForm" class="mb-6">
          <CreateTeamForm @created="handleTeamCreated" @cancel="showCreateForm = false" />
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <TeamCard
            v-for="team in teamsStore.teams"
            :key="team.id"
            :team="team"
            @click="navigateToTeam(team.id)"
            @delete="handleDeleteRequest(team.id)"
          />
        </div>
      </div>
    </template>

    <DeleteTeamDialog
      v-if="pendingDeleteTeam"
      :team="pendingDeleteTeam"
      :open="!!pendingDeleteTeam"
      @confirm="handleDeleteConfirm"
      @close="pendingDeleteTeam = null"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useTeamsStore, type TeamSummary } from '../stores/teams';
import CreateTeamForm from '../components/teams/CreateTeamForm.vue';
import TeamCard from '../components/teams/TeamCard.vue';
import DeleteTeamDialog from '../components/teams/DeleteTeamDialog.vue';

const router = useRouter();
const authStore = useAuthStore();
const teamsStore = useTeamsStore();

const showCreateForm = ref(false);
const pendingDeleteTeam = ref<TeamSummary | null>(null);

watch(
  () => authStore.loaded,
  (loaded) => {
    if (loaded && authStore.isSignedIn) teamsStore.fetchMyTeams();
  },
  { immediate: true },
);

function navigateToTeam(teamId: number) {
  router.push({ name: 'team-retro-sessions', params: { teamId } });
}

function handleTeamCreated(team: TeamSummary) {
  showCreateForm.value = false;
  router.push({ name: 'team-retro-sessions', params: { teamId: team.id } });
}

function handleDeleteRequest(teamId: number) {
  const team = teamsStore.teamById(teamId);
  if (team) pendingDeleteTeam.value = team;
}

async function handleDeleteConfirm(teamId: number, confirmationName: string) {
  await teamsStore.deleteTeam(teamId, confirmationName);
  pendingDeleteTeam.value = null;
}
</script>
