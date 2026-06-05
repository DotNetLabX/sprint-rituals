<script setup lang="ts">
import type { TeamDeveloperDto, UpdateTeamConfigRequest } from '../../types'
import TeamRow from './TeamRow.vue'

const props = defineProps<{
  groupedBySubTeam: Map<string | null, TeamDeveloperDto[]>
  subTeams: string[]
}>()

const emit = defineEmits<{
  update: [accountId: string, config: UpdateTeamConfigRequest]
}>()

function sortedGroups(): Array<{ label: string; developers: TeamDeveloperDto[] }> {
  const result: Array<{ label: string; developers: TeamDeveloperDto[] }> = []
  const unassigned: TeamDeveloperDto[] = []

  for (const [key, devs] of props.groupedBySubTeam) {
    const sorted = [...devs].sort((a, b) => {
      if (a.isActive !== b.isActive) return a.isActive ? -1 : 1
      return a.displayName.localeCompare(b.displayName)
    })
    if (key === null) {
      unassigned.push(...sorted)
    } else {
      result.push({ label: key, developers: sorted })
    }
  }

  result.sort((a, b) => a.label.localeCompare(b.label))

  if (unassigned.length > 0) {
    result.push({ label: 'Unassigned', developers: unassigned })
  }

  return result
}
</script>

<template>
  <div class="flex flex-col gap-6">
    <div
      v-for="group in sortedGroups()"
      :key="group.label"
    >
      <div class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-2 px-1">
        {{ group.label }}
      </div>
      <div class="overflow-x-auto rounded-md border border-border-default">
        <table class="w-auto text-sm">
          <thead>
            <tr class="text-text-muted text-left border-b border-border-default bg-surface-elevated">
              <th class="pb-2 pt-2 px-4 font-medium">Developer</th>
              <th class="pb-2 pt-2 pr-4 font-medium">Role</th>
              <th class="pb-2 pt-2 pr-4 font-medium" title="Default capacity used when no sprint-level override is set.">Default Capacity</th>
              <th class="pb-2 pt-2 pr-4 font-medium">Sub-Team</th>
              <th class="pb-2 pt-2 pr-4 font-medium text-center" title="Inactive developers are excluded from analytics views.">Active</th>
            </tr>
          </thead>
          <tbody>
            <TeamRow
              v-for="dev in group.developers"
              :key="dev.accountId"
              :developer="dev"
              :sub-teams="subTeams"
              @update="(accountId, config) => emit('update', accountId, config)"
            />
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
