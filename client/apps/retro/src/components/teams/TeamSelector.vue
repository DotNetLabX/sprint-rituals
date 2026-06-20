<template>
  <div class="flex items-center justify-center min-h-screen">
    <!-- Not signed in -->
    <div v-if="!authStore.isSignedIn" class="flex flex-col items-center gap-4">
      <h1 class="text-2xl font-semibold text-text-primary">Welcome to Reflekt</h1>
      <button
        class="px-4 py-2 bg-primary-600 text-white rounded hover:bg-primary-700"
        @click="authStore.signIn()"
      >
        Sign in with Google
      </button>
    </div>

    <!-- Signed in, has team -->
    <RetroSessionList
      v-else-if="teamsStore.hasTeams"
      :team-name="teamsStore.teams[0]!.name"
      :retro-sessions="retroSessionStore.retroSessions"
      @select="(id) => router.push({ name: 'retro-session', params: { teamId: teamsStore.teams[0]!.id, retroSessionId: id } })"
      @create-retro-session="handleCreateRetroSession"
    />

    <!-- Signed in, no team -->
    <CreateTeamForm v-else @create="handleCreateTeam" />
  </div>
</template>

<script setup lang="ts">
import { watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { useTeamsStore } from '../../stores/teams'
import { useRetroSessionStore } from '../../stores/retroSession'
import RetroSessionList from '../retro-sessions/RetroSessionList.vue'
import CreateTeamForm from './CreateTeamForm.vue'

const router = useRouter()
const authStore = useAuthStore()
const teamsStore = useTeamsStore()
const retroSessionStore = useRetroSessionStore()

watch(
  () => authStore.loaded,
  (loaded) => { if (loaded && authStore.isSignedIn) teamsStore.fetchMyTeams() },
  { immediate: true },
)

watch(
  () => teamsStore.teams[0],
  (team) => { if (team) retroSessionStore.fetchTeamRetroSessions(team.id) },
  { immediate: true },
)

async function handleCreateTeam(name: string) {
  await teamsStore.createTeam(name)
}

async function handleCreateRetroSession(payload: { name: string; votesPerParticipant: number | undefined }) {
  await retroSessionStore.createRetroSession(teamsStore.teams[0]!.id, payload)
}
</script>
