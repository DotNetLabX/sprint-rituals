<template>
  <div class="flex flex-col items-center p-8">
    <SprintList
      :team-name="teamName"
      :sprints="sprintStore.sprints"
      @select="(id) => router.push({ name: 'sprint', params: { teamId, sprintId: id } })"
      @create-sprint="handleCreateSprint"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useTeamsStore } from '../stores/teams';
import { useSprintStore } from '../stores/sprint';
import SprintList from '../components/sprints/SprintList.vue';

const router = useRouter();
const route = useRoute();
const teamsStore = useTeamsStore();
const sprintStore = useSprintStore();

const teamId = computed(() => Number(route.params.teamId));
const teamName = computed(() => teamsStore.teamById(teamId.value)?.name ?? '');

watch(
  teamId,
  (id) => {
    if (id) sprintStore.fetchTeamSprints(id);
  },
  { immediate: true },
);

async function handleCreateSprint(payload: {
  name: string
  votesPerParticipant: number | undefined
  column1Name: string
  column1Color: string
  column2Name: string
  column2Color: string
  column3Name: string
  column3Color: string
}) {
  await sprintStore.createSprint(teamId.value, payload);
}
</script>
