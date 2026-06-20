<script setup lang="ts">
import { inject, ref } from 'vue'
import { testXrayConnection, syncXrayData } from '../../api/settings'
import type { TestConnectionResponse, XraySyncResponse } from '../../types'
import {
  settingsFormKey,
  isReadOnlyKey,
  settingsStoreKey,
} from './injectionKeys'

const form = inject(settingsFormKey)!
const isReadOnly = inject(isReadOnlyKey)!
const store = inject(settingsStoreKey)!

// Xray save state
const xraySaving = ref(false)
const xraySaved = ref(false)
const xrayError = ref('')

// Test connection state
const testingConnection = ref(false)
const connectionResult = ref<TestConnectionResponse | null>(null)
const connectionError = ref('')

// Xray sync state
const xraySyncing = ref(false)
const xraySyncResult = ref<XraySyncResponse | null>(null)
const xraySyncError = ref('')
const selectedXraySprints = ref<number[]>([])

async function saveXrayPanel() {
  xraySaved.value = false
  xrayError.value = ''
  xraySaving.value = true
  try {
    await store.saveXraySettingsAction(form.xrayEnabled, form.xrayClientId ?? '', form.xrayClientSecret || null)
    if (!store.error) {
      xraySaved.value = true
      setTimeout(() => xraySaved.value = false, 3000)
    } else {
      xrayError.value = store.error
    }
  } catch (e: any) {
    xrayError.value = e.message || 'Failed to save Xray settings.'
  } finally {
    xraySaving.value = false
  }
}

async function runTestConnection() {
  connectionResult.value = null
  connectionError.value = ''
  testingConnection.value = true
  try {
    connectionResult.value = await testXrayConnection()
  } catch (e: any) {
    connectionError.value = e.message || 'Connection test failed.'
  } finally {
    testingConnection.value = false
  }
}

async function runXraySync() {
  xraySyncResult.value = null
  xraySyncError.value = ''
  xraySyncing.value = true
  try {
    xraySyncResult.value = await syncXrayData(selectedXraySprints.value)
  } catch (e: any) {
    xraySyncError.value = e.message || 'Xray sync failed.'
  } finally {
    xraySyncing.value = false
  }
}
</script>

<template>
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <div class="flex items-center gap-1">
      <h2 class="text-lg font-semibold text-gray-200">Xray Integration</h2>
      <span
        class="text-gray-500 cursor-help"
        title="Enable to connect Fokus with Xray Cloud for test coverage and execution data."
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
        </svg>
      </span>
    </div>

    <!-- Toggle -->
    <div class="flex items-center gap-3">
      <label class="relative inline-flex items-center cursor-pointer">
        <input
          type="checkbox"
          v-model="form.xrayEnabled"
          :disabled="isReadOnly"
          class="sr-only peer"
        />
        <div class="w-11 h-6 bg-gray-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed"></div>
      </label>
      <span class="text-sm text-gray-300" title="Enable to connect Fokus with Xray Cloud for test coverage and execution data.">
        {{ form.xrayEnabled ? 'Enabled' : 'Disabled' }}
      </span>
    </div>

    <!-- Credentials (shown when enabled) -->
    <template v-if="form.xrayEnabled">
      <div class="space-y-3">
        <div>
          <label
            class="block text-sm text-gray-300 mb-1 cursor-help"
            title="Your Xray API Client ID from Xray Global Settings > API Keys."
          >Client ID</label>
          <input
            v-model="form.xrayClientId"
            type="text"
            placeholder="Enter Xray Client ID"
            :disabled="isReadOnly"
            class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
          />
        </div>
        <div>
          <label
            class="block text-sm text-gray-300 mb-1 cursor-help"
            title="Your Xray API Client Secret from Xray Global Settings > API Keys."
          >Client Secret</label>
          <input
            v-model="form.xrayClientSecret"
            type="password"
            placeholder="Enter Xray Client Secret (leave blank to keep existing)"
            :disabled="isReadOnly"
            class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 placeholder-gray-500 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
          />
          <p class="text-xs text-gray-500 mt-1">Leave blank to keep the existing secret.</p>
        </div>
      </div>
    </template>

    <!-- Save button -->
    <div class="flex items-center gap-4">
      <button
        @click="saveXrayPanel"
        :disabled="xraySaving || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ xraySaving ? 'Saving...' : 'Save Xray Settings' }}
      </button>
      <span v-if="xraySaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="xrayError" class="text-red-400 text-sm">{{ xrayError }}</span>
    </div>

    <!-- Test Connection (shown when enabled and credentials saved) -->
    <template v-if="form.xrayEnabled && store.settings.xrayClientId">
      <div class="border-t border-gray-800 pt-4 space-y-3">
        <div class="flex items-center gap-1">
          <h3 class="text-sm font-medium text-gray-300">Test Connection</h3>
          <span
            class="text-gray-500 cursor-help"
            title="Verifies your Xray credentials and confirms Fokus can access test data."
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
            </svg>
          </span>
        </div>
        <div class="flex items-center gap-4">
          <button
            @click="runTestConnection"
            :disabled="testingConnection || isReadOnly"
            class="border border-gray-600 hover:border-gray-400 text-gray-300 hover:text-gray-100 disabled:opacity-40 disabled:cursor-not-allowed px-4 py-2 rounded text-sm font-medium"
          >
            {{ testingConnection ? 'Testing...' : 'Test Connection' }}
          </button>
          <span v-if="connectionResult?.success" class="text-green-400 text-sm">{{ connectionResult.message }}</span>
          <span v-else-if="connectionResult && !connectionResult.success" class="text-red-400 text-sm">{{ connectionResult.message }}</span>
          <span v-if="connectionError" class="text-red-400 text-sm">{{ connectionError }}</span>
        </div>
      </div>
    </template>

    <!-- Sync QA Data (shown when enabled and credentials saved) -->
    <template v-if="form.xrayEnabled && store.settings.xrayClientId">
      <div class="border-t border-gray-800 pt-4 space-y-3">
        <div class="flex items-center gap-1">
          <h3 class="text-sm font-medium text-gray-300">Sync QA Data</h3>
          <span
            class="text-gray-500 cursor-help"
            title="Fetches latest test execution results from Xray for selected sprints."
          >
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3 h-3">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
            </svg>
          </span>
        </div>
        <p class="text-sm text-gray-400">Fetches test execution results from Xray for the synced sprints.</p>
        <div class="flex items-center gap-4">
          <button
            @click="runXraySync"
            :disabled="xraySyncing || isReadOnly"
            class="bg-green-600 hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded text-sm font-medium"
          >
            {{ xraySyncing ? 'Syncing...' : 'Sync QA Data' }}
          </button>
        </div>
        <div v-if="xraySyncError" class="text-red-400 text-sm">{{ xraySyncError }}</div>
        <div v-if="xraySyncResult" class="space-y-1 text-sm">
          <p class="text-green-400 font-medium">Xray sync complete!</p>
          <div class="grid grid-cols-2 gap-x-4 gap-y-1 text-gray-300">
            <span>Test executions synced:</span><span class="text-gray-100">{{ xraySyncResult.testExecutionsSynced }}</span>
            <span>Test runs synced:</span><span class="text-gray-100">{{ xraySyncResult.testRunsSynced }}</span>
            <span>Test sets synced:</span><span class="text-gray-100">{{ xraySyncResult.testSetsSynced }}</span>
          </div>
          <div v-if="xraySyncResult.warnings.length > 0" class="space-y-1">
            <p v-for="(w, i) in xraySyncResult.warnings" :key="i" class="text-amber-400 text-xs">{{ w }}</p>
          </div>
        </div>
      </div>
    </template>
  </section>
</template>
