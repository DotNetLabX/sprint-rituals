<template>
  <PageLayout :loading="reportStore.loading" :empty="!reportStore.loading && !report">
    <template #loading-content>
      <p class="text-text-muted text-sm">Loading report...</p>
    </template>

    <template #empty-content>
      <div class="bg-surface border border-border rounded shadow-card p-8 text-center">
        <p class="text-danger text-sm">{{ reportStore.error ?? 'Report not found.' }}</p>
      </div>
    </template>

    <SprintReportPanel v-if="report" :report="report" @copy-markdown="handleCopy" />
  </PageLayout>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useGuestStore } from '../stores/guest'
import { useReportStore } from '../stores/report'
import { useToast } from '../composables/useToast'
import PageLayout from '../components/layout/PageLayout.vue'
import SprintReportPanel from '../components/reports/SprintReportPanel.vue'

const route = useRoute()
const guestStore = useGuestStore()
const reportStore = useReportStore()
const { addToast } = useToast()

const teamId = Number(route.params.teamId)
const sprintId = Number(route.params.sprintId)

const report = computed(() => reportStore.report)

async function handleCopy() {
  const ok = await reportStore.copyAsMarkdown()
  if (ok) addToast('Copied to clipboard', 'success')
}

onMounted(() => {
  guestStore.loadToken(sprintId)
  reportStore.fetchReport(teamId, sprintId)
})
</script>
