<template>
  <div class="flex flex-col items-center p-8">
    <RetroSessionList
      :team-id="teamId"
      :team-name="teamName"
      :retro-sessions="retroSessionStore.retroSessions"
      @select="(id) => router.push({ name: 'retro-session', params: { teamId, retroSessionId: id } })"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useTeamsStore } from '../stores/teams';
import { useRetroSessionStore } from '../stores/retroSession';
import RetroSessionList from '../components/retro-sessions/RetroSessionList.vue';

const router = useRouter();
const route = useRoute();
const teamsStore = useTeamsStore();
const retroSessionStore = useRetroSessionStore();

const teamId = computed(() => Number(route.params.teamId));
const teamName = computed(() => teamsStore.teamById(teamId.value)?.name ?? '');

watch(
  teamId,
  (id) => {
    if (id) retroSessionStore.fetchTeamRetroSessions(id);
  },
  { immediate: true },
);

</script>
