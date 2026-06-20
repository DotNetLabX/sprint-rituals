<script setup lang="ts">
import type { TeamDeveloperDto, UpdateTeamConfigRequest } from '../../types'

const props = defineProps<{
  developer: TeamDeveloperDto
  subTeams: string[]
}>()

const emit = defineEmits<{
  update: [accountId: string, config: UpdateTeamConfigRequest]
}>()

const ROLES = ['Developer', 'Tech Lead', 'Tester', 'PO']

function onRoleChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value
  emit('update', props.developer.accountId, { role: value })
}

function onCapacityChange(event: Event) {
  const value = parseInt((event.target as HTMLInputElement).value, 10)
  if (!isNaN(value) && value >= 0 && value <= 100) {
    emit('update', props.developer.accountId, { defaultCapacityPercent: value })
  }
}

function onSubTeamChange(event: Event) {
  const value = (event.target as HTMLSelectElement).value
  emit('update', props.developer.accountId, { subTeam: value === '' ? null : value, subTeamProvided: true })
}

function onNewSubTeam(event: Event) {
  const value = (event.target as HTMLInputElement).value.trim()
  if (value) {
    emit('update', props.developer.accountId, { subTeam: value, subTeamProvided: true })
  }
}

function onActiveChange(event: Event) {
  const checked = (event.target as HTMLInputElement).checked
  emit('update', props.developer.accountId, { isActive: checked })
}
</script>

<template>
  <tr
    class="border-b border-border-default last:border-0 transition-opacity"
    :class="developer.isActive ? '' : 'opacity-50'"
  >
    <!-- Developer -->
    <td class="py-3 pr-4">
      <div class="flex items-center gap-2">
        <img
          v-if="developer.avatarUrl"
          :src="developer.avatarUrl"
          :alt="developer.displayName"
          class="w-7 h-7 rounded-full shrink-0"
        />
        <div
          v-else
          class="w-7 h-7 rounded-full bg-surface-elevated shrink-0 flex items-center justify-center text-xs text-text-muted"
        >
          {{ developer.displayName.charAt(0).toUpperCase() }}
        </div>
        <span class="text-sm text-text-primary truncate">{{ developer.displayName }}</span>
      </div>
    </td>

    <!-- Role -->
    <td class="py-3 pr-4">
      <select
        :value="developer.role"
        class="bg-surface-elevated border border-border-default rounded px-2 py-1 text-sm text-text-primary outline-none focus:border-accent-default w-full"
        @change="onRoleChange"
      >
        <option v-for="r in ROLES" :key="r" :value="r">{{ r }}</option>
      </select>
    </td>

    <!-- Default Capacity -->
    <td class="py-3 pr-4">
      <div class="flex items-center gap-1">
        <input
          type="number"
          :value="developer.defaultCapacityPercent"
          min="0"
          max="100"
          class="w-16 text-right bg-surface-elevated border border-border-default rounded px-1 py-1 text-sm outline-none focus:border-accent-default tabular-nums"
          :class="developer.defaultCapacityPercent !== 100 ? 'text-status-warning' : 'text-text-primary'"
          @change="onCapacityChange"
        />
        <span class="text-sm text-text-muted">%</span>
      </div>
    </td>

    <!-- Sub-Team -->
    <td class="py-3 pr-4">
      <div class="flex items-center gap-1">
        <select
          :value="developer.subTeam ?? ''"
          class="bg-surface-elevated border border-border-default rounded px-2 py-1 text-sm text-text-primary outline-none focus:border-accent-default"
          @change="onSubTeamChange"
        >
          <option value="">Unassigned</option>
          <option v-for="st in subTeams" :key="st" :value="st">{{ st }}</option>
        </select>
        <input
          type="text"
          placeholder="New..."
          class="w-20 bg-surface-elevated border border-border-default rounded px-2 py-1 text-sm text-text-primary outline-none focus:border-accent-default placeholder-text-muted"
          @change="onNewSubTeam"
        />
      </div>
    </td>

    <!-- Active -->
    <td class="py-3 text-center">
      <input
        type="checkbox"
        :checked="developer.isActive"
        class="w-4 h-4 accent-accent-default cursor-pointer"
        @change="onActiveChange"
      />
    </td>
  </tr>
</template>
