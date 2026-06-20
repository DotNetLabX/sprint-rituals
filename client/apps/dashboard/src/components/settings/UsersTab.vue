<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getUsers, getInvitations, createInvitation, updateUserRole, updateUserStatus, revokeInvitation } from '../../api/auth'
import type { UserEntry, InvitationEntry, CreateInvitationResponse } from '../../types'

// User management state
const users = ref<UserEntry[]>([])
const invitations = ref<InvitationEntry[]>([])
const usersLoading = ref(false)
const inviteEmail = ref('')
const inviteRole = ref('Manager')
const inviting = ref(false)
const inviteResult = ref<CreateInvitationResponse | null>(null)
const inviteError = ref('')
const userMgmtError = ref('')

onMounted(async () => {
  await loadUserManagement()
})

async function loadUserManagement() {
  usersLoading.value = true
  try {
    const [usersResult, invitationsResult] = await Promise.allSettled([getUsers(), getInvitations()])
    if (usersResult.status === 'fulfilled') users.value = usersResult.value
    if (invitationsResult.status === 'fulfilled') invitations.value = invitationsResult.value
  } finally {
    usersLoading.value = false
  }
}

async function sendInvite() {
  inviteError.value = ''
  inviteResult.value = null
  inviting.value = true
  try {
    inviteResult.value = await createInvitation({ email: inviteEmail.value.trim(), role: inviteRole.value })
    inviteEmail.value = ''
    await loadUserManagement()
  } catch (e: any) {
    inviteError.value = e.message || 'Failed to send invitation.'
  } finally {
    inviting.value = false
  }
}

async function changeUserRole(userId: number, role: string) {
  userMgmtError.value = ''
  try {
    const updated = await updateUserRole(userId, role)
    const idx = users.value.findIndex(u => u.id === userId)
    if (idx !== -1) users.value[idx] = updated
  } catch (e: any) {
    userMgmtError.value = e.message || 'Failed to update role.'
  }
}

async function toggleUserStatus(userId: number, isActive: boolean) {
  userMgmtError.value = ''
  try {
    const updated = await updateUserStatus(userId, isActive)
    const idx = users.value.findIndex(u => u.id === userId)
    if (idx !== -1) users.value[idx] = updated
  } catch (e: any) {
    userMgmtError.value = e.message || 'Failed to update status.'
  }
}

async function revokeInvite(inviteId: number) {
  userMgmtError.value = ''
  try {
    await revokeInvitation(inviteId)
    await loadUserManagement()
  } catch (e: any) {
    userMgmtError.value = e.message || 'Failed to revoke invitation.'
  }
}

function copyInviteLink(link: string) {
  navigator.clipboard.writeText(link)
}
</script>

<template>
  <!-- User Management (Admin only) -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-6">
    <h2 class="text-lg font-semibold text-gray-200">User Management</h2>

    <div v-if="userMgmtError" class="text-red-400 text-sm">{{ userMgmtError }}</div>

    <!-- Invite form -->
    <div class="space-y-3">
      <h3 class="text-sm font-medium text-gray-300">Invite a new user</h3>
      <div class="flex gap-2">
        <input
          v-model="inviteEmail"
          type="email"
          placeholder="user@company.com"
          class="flex-1 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 text-sm"
        />
        <select
          v-model="inviteRole"
          class="bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 text-sm"
        >
          <option value="Manager">Manager</option>
          <option value="Admin">Admin</option>
        </select>
        <button
          @click="sendInvite"
          :disabled="inviting || !inviteEmail"
          class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded text-sm font-medium"
        >
          {{ inviting ? 'Inviting...' : 'Send Invite' }}
        </button>
      </div>
      <div v-if="inviteError" class="text-red-400 text-sm">{{ inviteError }}</div>
      <div v-if="inviteResult" class="space-y-1 bg-gray-800 rounded p-3 text-sm">
        <p class="text-green-400">Invitation created! Share this link:</p>
        <div class="flex items-center gap-2">
          <code class="flex-1 text-gray-300 text-xs break-all">{{ inviteResult.inviteLink }}</code>
          <button
            @click="copyInviteLink(inviteResult!.inviteLink)"
            class="border border-gray-600 hover:border-gray-400 text-gray-300 hover:text-gray-100 px-2 py-1 rounded text-xs"
          >Copy</button>
        </div>
        <p class="text-gray-500 text-xs">Expires {{ new Date(inviteResult.expiresAt).toLocaleDateString() }}</p>
      </div>
    </div>

    <!-- Pending invitations -->
    <div v-if="invitations.filter(i => i.status === 'Pending').length > 0" class="space-y-2">
      <h3 class="text-sm font-medium text-gray-300">Pending Invitations</h3>
      <table class="w-full text-sm text-gray-300">
        <thead>
          <tr class="text-gray-500 text-xs border-b border-gray-800">
            <th class="text-left py-1 font-normal">Email</th>
            <th class="text-left py-1 font-normal">Role</th>
            <th class="text-left py-1 font-normal">Expires</th>
            <th class="text-right py-1 font-normal"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="inv in invitations.filter(i => i.status === 'Pending')" :key="inv.id" class="border-b border-gray-800/50">
            <td class="py-2">{{ inv.email }}</td>
            <td class="py-2">{{ inv.role }}</td>
            <td class="py-2 text-xs text-gray-500">{{ new Date(inv.expiresAt).toLocaleDateString() }}</td>
            <td class="py-2 text-right">
              <button
                @click="revokeInvite(inv.id)"
                class="text-xs text-red-400 hover:text-red-300"
              >Revoke</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Users table -->
    <div class="space-y-2">
      <h3 class="text-sm font-medium text-gray-300">Users</h3>
      <div v-if="usersLoading" class="text-gray-500 text-sm">Loading users...</div>
      <table v-else class="w-full text-sm text-gray-300">
        <thead>
          <tr class="text-gray-500 text-xs border-b border-gray-800">
            <th class="text-left py-1 font-normal">Name</th>
            <th class="text-left py-1 font-normal">Email</th>
            <th class="text-left py-1 font-normal">Role</th>
            <th class="text-left py-1 font-normal">Last login</th>
            <th class="text-left py-1 font-normal">Active</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in users" :key="u.id" class="border-b border-gray-800/50">
            <td class="py-2">
              <div class="flex items-center gap-2">
                <img v-if="u.avatarUrl" :src="u.avatarUrl" class="w-6 h-6 rounded-full" />
                <span>{{ u.displayName }}</span>
              </div>
            </td>
            <td class="py-2 text-gray-400 text-xs">{{ u.email }}</td>
            <td class="py-2">
              <select
                :value="u.role"
                @change="changeUserRole(u.id, ($event.target as HTMLSelectElement).value)"
                class="bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-xs focus:outline-none focus:border-blue-500"
              >
                <option value="Admin">Admin</option>
                <option value="Manager">Manager</option>
              </select>
            </td>
            <td class="py-2 text-xs text-gray-500">
              {{ u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : 'Never' }}
            </td>
            <td class="py-2">
              <button
                @click="toggleUserStatus(u.id, !u.isActive)"
                :class="u.isActive ? 'bg-green-600 hover:bg-green-700' : 'bg-gray-700 hover:bg-gray-600'"
                class="px-2 py-1 rounded text-xs text-white transition-colors"
              >
                {{ u.isActive ? 'Active' : 'Inactive' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
