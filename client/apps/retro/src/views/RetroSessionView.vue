<template>
  <div class="flex items-center justify-center min-h-screen bg-background">
    <div v-if="!retroSession" class="text-text-muted">Loading retro…</div>
    <RetroSessionDetailPanel
      v-else
      :retro-session="retroSession"
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
import { useRetroSessionStore } from '../stores/retroSession'
import { useParticipantStore } from '../stores/participant'
import RetroSessionDetailPanel from '../components/retro-sessions/RetroSessionDetailPanel.vue'

const route = useRoute()
const retroSessionStore = useRetroSessionStore()
const participantStore = useParticipantStore()

const teamId = Number(route.params.teamId)
const retroSessionId = Number(route.params.retroSessionId)

const busy = ref(false)
const errorMessage = ref('')
const copied = ref(false)
let copiedTimer: ReturnType<typeof setTimeout> | null = null

const retroSession = computed(() => retroSessionStore.currentRetroSession)
const participants = computed(() => participantStore.participants)

onMounted(async () => {
  await retroSessionStore.fetchRetroSession(teamId, retroSessionId)
  if (retroSessionStore.currentRetroSession?.state === 'Active') {
    await participantStore.fetchParticipants(retroSessionId)
  }
})

async function handleStart() {
  errorMessage.value = ''
  busy.value = true
  try { await retroSessionStore.startRetroSession(teamId, retroSessionId) }
  catch { errorMessage.value = 'Failed to start retro.' }
  finally { busy.value = false }
}

async function confirmKick(p: { id: number; displayName: string }) {
  if (!confirm(`Kick ${p.displayName}? They'll need a new invite to rejoin.`)) return
  errorMessage.value = ''
  busy.value = true
  try { await participantStore.kickParticipant(retroSessionId, p.id) }
  catch { errorMessage.value = 'Failed to kick participant.' }
  finally { busy.value = false }
}

async function copyInviteLink() {
  const url = `${window.location.origin}/retro/${retroSessionId}`
  try { await navigator.clipboard.writeText(url) }
  catch { errorMessage.value = 'Failed to copy link. Copy manually: ' + url; return }
  copied.value = true
  if (copiedTimer) clearTimeout(copiedTimer)
  copiedTimer = setTimeout(() => (copied.value = false), 2000)
}
</script>
