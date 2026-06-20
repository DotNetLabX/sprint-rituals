<template>
  <div class="flex items-center justify-center min-h-screen bg-background">
    <div v-if="!sprint" class="text-text-muted">Loading sprint…</div>
    <SprintDetailPanel
      v-else
      :sprint="sprint"
      :team-id="teamId"
      :participants="participants"
      :busy="busy"
      :copied="copied"
      :error-message="errorMessage"
      @start="handleStart"
      @copy-invite="copyInviteLink"
      @kick="confirmKick"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useSprintStore } from '../stores/sprint'
import { useParticipantStore } from '../stores/participant'
import SprintDetailPanel from '../components/sprints/SprintDetailPanel.vue'

const route = useRoute()
const sprintStore = useSprintStore()
const participantStore = useParticipantStore()

const teamId = Number(route.params.teamId)
const sprintId = Number(route.params.sprintId)

const busy = ref(false)
const errorMessage = ref('')
const copied = ref(false)
let copiedTimer: ReturnType<typeof setTimeout> | null = null

const sprint = computed(() => sprintStore.currentSprint)
const participants = computed(() => participantStore.participants)

onMounted(async () => {
  await sprintStore.fetchSprint(teamId, sprintId)
  if (sprintStore.currentSprint?.state === 'Active') {
    await participantStore.fetchParticipants(sprintId)
  }
})

async function handleStart() {
  errorMessage.value = ''
  busy.value = true
  try { await sprintStore.startSprint(teamId, sprintId) }
  catch { errorMessage.value = 'Failed to start sprint.' }
  finally { busy.value = false }
}

async function confirmKick(p: { id: number; displayName: string }) {
  if (!confirm(`Kick ${p.displayName}? They'll need a new invite to rejoin.`)) return
  errorMessage.value = ''
  busy.value = true
  try { await participantStore.kickParticipant(sprintId, p.id) }
  catch { errorMessage.value = 'Failed to kick participant.' }
  finally { busy.value = false }
}

async function copyInviteLink() {
  const url = `${window.location.origin}/sprint/${sprintId}`
  try { await navigator.clipboard.writeText(url) }
  catch { errorMessage.value = 'Failed to copy link. Copy manually: ' + url; return }
  copied.value = true
  if (copiedTimer) clearTimeout(copiedTimer)
  copiedTimer = setTimeout(() => (copied.value = false), 2000)
}
</script>
