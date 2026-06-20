<template>
  <div class="flex-1 min-h-0 overflow-y-auto">
    <div v-if="reportStore.loading" class="text-text-muted text-sm text-center py-8">Loading report...</div>
    <div v-else-if="reportStore.error" class="text-red-600 text-sm text-center py-8">{{ reportStore.error }}</div>
    <div v-else-if="reportStore.report" class="flex flex-col gap-4 max-w-4xl mx-auto">
      <!-- Report header -->
      <div class="bg-surface rounded-[--radius-md] shadow-card p-4 flex items-start justify-between">
        <div>
          <h2 class="text-xl font-bold text-text-primary">{{ reportStore.report.retroSession.name }} — Retro Report</h2>
          <p class="text-sm text-text-muted mt-1">
            <span class="font-medium text-text-secondary">{{ reportStore.report.team.name }}</span>
            &middot; {{ formatDate(reportStore.report.retroSession.createdAt) }}
          </p>
          <div class="flex flex-wrap gap-2 mt-2">
            <BaseBadge v-for="p in reportStore.report.participants" :key="p.displayName" variant="primary">
              {{ p.displayName }}
            </BaseBadge>
          </div>
        </div>
        <BaseButton size="sm" variant="secondary" @click="$emit('copy-markdown')">Copy as markdown</BaseButton>
      </div>

      <!-- Dynamic column sections -->
      <div v-for="col in reportStore.report.columns" :key="col.columnName" class="bg-surface rounded-[--radius-md] shadow-card">
        <div class="px-4 py-3 border-b rounded-t-[--radius-md]" :class="`bg-col-${col.columnColor}-light`">
          <h3 class="font-semibold text-sm" :class="`text-col-${col.columnColor}`">{{ col.columnName }}</h3>
        </div>
        <div class="p-4">
          <div v-if="col.groups.length === 0" class="text-text-muted text-sm text-center py-4">No items</div>
          <div v-else class="flex flex-col gap-4">
            <div v-for="(group, i) in col.groups" :key="i">
              <div class="flex items-center gap-2 mb-1">
                <p class="text-sm font-medium text-text-primary">{{ group.groupTitle }}</p>
                <span class="text-xs text-text-muted">({{ group.voteCount }} votes)</span>
                <BaseBadge v-if="group.isDiscussed" variant="success">discussed</BaseBadge>
              </div>
              <ul class="flex flex-col gap-1 pl-3 border-l-2" :class="`border-col-${col.columnColor}-light`">
                <li v-for="(card, j) in group.cards" :key="j">
                  <p class="text-sm text-text-secondary">{{ card.text }}</p>
                  <div v-if="card.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
                    <a v-for="(link, k) in card.evidenceLinks" :key="k" :href="link.url" target="_blank" rel="noopener"
                      class="inline-block px-2 py-0.5 rounded text-xs hover:opacity-80"
                      :class="`bg-col-${col.columnColor}-light text-col-${col.columnColor}`">
                      {{ link.title || truncateUrl(link.url) }}
                    </a>
                  </div>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      <!-- Action Items -->
      <div class="bg-surface rounded-[--radius-md] shadow-card">
        <div class="px-4 py-3 border-b bg-action-items-light rounded-t-[--radius-md]">
          <h3 class="font-semibold text-action-items text-sm">Action Items</h3>
        </div>
        <div class="p-4">
          <div v-if="reportStore.report.actionItems.length === 0" class="text-text-muted text-sm text-center py-4">No action items</div>
          <ul v-else class="flex flex-col gap-3">
            <li v-for="(item, i) in reportStore.report.actionItems" :key="i">
              <p class="text-sm text-text-primary">
                {{ item.text }}
                <span v-if="item.assigneeName" class="text-primary-600 font-medium ml-1">@{{ item.assigneeName }}</span>
              </p>
              <div v-if="item.evidenceLinks.length > 0" class="flex flex-wrap gap-1 mt-1">
                <a v-for="(link, j) in item.evidenceLinks" :key="j" :href="link.url" target="_blank" rel="noopener" class="inline-block px-2 py-0.5 bg-col-blue-light text-col-blue rounded text-xs hover:opacity-80">
                  {{ link.title || truncateUrl(link.url) }}
                </a>
              </div>
            </li>
          </ul>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useReportStore } from '../../stores/report'
import BaseBadge from '../ui/BaseBadge.vue'
import BaseButton from '../ui/BaseButton.vue'

defineEmits<{
  'copy-markdown': []
}>()

const reportStore = useReportStore()

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString()
}

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
