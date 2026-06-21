<template>
  <!-- Shows who has voted (pre-reveal: checkmarks only) and vote values (post-reveal) -->
  <div class="flex flex-col gap-3">
    <h3 class="text-xs font-semibold text-text-muted uppercase tracking-wide">Votes</h3>
    <div class="flex flex-wrap gap-2">
      <div
        v-for="p in participants"
        :key="p.id"
        :class="[
          'flex flex-col items-center gap-1 px-3 py-2 rounded-[--radius-md] border min-w-[60px]',
          hasVoted(p.id) ? 'bg-surface-raised border-primary-500/30' : 'bg-surface border-border',
        ]"
      >
        <span
          :class="[
            'text-sm font-bold',
            revealed ? 'text-text-primary' : hasVoted(p.id) ? 'text-success' : 'text-text-muted',
          ]"
        >
          {{ revealed ? (voteValue(p.id) ?? '—') : (hasVoted(p.id) ? '✓' : '…') }}
        </span>
        <span class="text-xs text-text-muted truncate max-w-[72px]">{{ p.displayName }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { ParticipantResponse, VoteResponse } from '../../types'

const props = defineProps<{
  participants: ParticipantResponse[]
  votes: VoteResponse[]
  revealed: boolean
}>()

function hasVoted(participantId: string): boolean {
  return props.votes.some((v) => v.participantId === participantId)
}

function voteValue(participantId: string): string | null {
  return props.votes.find((v) => v.participantId === participantId)?.value ?? null
}
</script>
