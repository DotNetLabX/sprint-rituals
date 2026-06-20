<template>
  <BaseCard padding="sm">
    <div class="flex flex-col gap-2">
      <h2 class="text-xs font-semibold text-text-muted uppercase tracking-wide">Participants</h2>
      <div class="flex flex-wrap gap-2">
        <BaseBadge
          v-for="p in participants"
          :key="p.id"
          variant="primary"
        >
          {{ p.displayName }}
        </BaseBadge>
      </div>
    </div>
  </BaseCard>
</template>

<script setup lang="ts">
import BaseCard from '../ui/BaseCard.vue'
import BaseBadge from '../ui/BaseBadge.vue'

defineProps<{
  participants: { id: number; displayName: string }[]
}>()
</script>
