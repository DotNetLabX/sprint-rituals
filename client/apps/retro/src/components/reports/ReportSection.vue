<template>
  <div class="bg-surface border border-border rounded shadow-card">
    <div class="px-4 py-3 border-b border-border rounded-t" :class="`bg-col-${columnColor}-light`">
      <h2 class="font-semibold text-sm" :class="`text-col-${columnColor}`">{{ columnName }}</h2>
    </div>
    <div class="p-4">
      <div v-if="groups.length === 0" class="text-text-muted text-sm text-center py-4">No items</div>
      <div v-else class="flex flex-col gap-4">
        <div v-for="(group, i) in groups" :key="i">
          <div class="flex items-center gap-2 mb-1">
            <p class="text-sm font-medium text-text-primary">{{ group.groupTitle }}</p>
            <span class="text-xs text-text-muted">({{ group.voteCount }} votes)</span>
          </div>
          <ul v-if="group.cards.length > 1" class="flex flex-col gap-1 pl-3 border-l-2" :class="`border-col-${columnColor}`">
            <li v-for="(card, j) in group.cards" :key="j">
              <p class="text-sm text-text-secondary">{{ card.text }}</p>
              <div v-if="card.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
                <a v-for="(link, k) in card.evidenceLinks" :key="k" :href="link.url" target="_blank" rel="noopener"
                  class="inline-block px-2 py-0.5 rounded text-xs hover:opacity-80"
                  :class="`bg-col-${columnColor}-light text-col-${columnColor}`">
                  {{ link.title || truncateUrl(link.url) }}
                </a>
              </div>
            </li>
          </ul>
          <div v-else-if="(group.cards[0]?.evidenceLinks?.length ?? 0) > 0" class="flex flex-wrap gap-1 mt-1">
            <a v-for="(link, k) in group.cards[0]?.evidenceLinks" :key="k" :href="link.url" target="_blank" rel="noopener"
              class="inline-block px-2 py-0.5 rounded text-xs hover:opacity-80"
              :class="`bg-col-${columnColor}-light text-col-${columnColor}`">
              {{ link.title || truncateUrl(link.url) }}
            </a>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { ReportGroupEntry } from '../../stores/report'

defineProps<{
  columnName: string
  columnColor: string
  groups: ReportGroupEntry[]
}>()

function truncateUrl(url: string): string {
  try {
    const u = new URL(url)
    const path = u.pathname.length > 20 ? u.pathname.slice(0, 20) + '...' : u.pathname
    return u.hostname + path
  } catch {
    return url.length > 40 ? url.slice(0, 40) + '...' : url
  }
}
</script>
