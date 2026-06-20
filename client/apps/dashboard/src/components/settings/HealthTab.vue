<script setup lang="ts">
import { inject, ref } from 'vue'
import InfoTooltip from '../InfoTooltip.vue'
import {
  settingsFormKey,
  isReadOnlyKey,
  settingsStoreKey,
} from './injectionKeys'

const form = inject(settingsFormKey)!
const isReadOnly = inject(isReadOnlyKey)!
const store = inject(settingsStoreKey)!

// Health config save state
const healthConfigSaving = ref(false)
const healthConfigSaved = ref(false)
const healthConfigError = ref('')

// Bug ratio alerts save state
const bugRatioAlertsSaving = ref(false)
const bugRatioAlertsSaved = ref(false)
const bugRatioAlertsError = ref('')

const weightsSum = () => form.healthWeights.completion + form.healthWeights.disruption + form.healthWeights.carryOver
const qualityWeightsSum = () => form.qualitySubScoreWeights.coverageWeight + form.qualitySubScoreWeights.passRateWeight

async function saveHealthConfigPanel() {
  healthConfigSaved.value = false
  healthConfigError.value = ''
  healthConfigSaving.value = true
  try {
    await store.saveHealthConfigAction(
      { ...form.healthThresholds },
      { ...form.healthWeights },
      { ...form.qaHealthThresholds },
      form.qualityHealthWeight,
      { ...form.qualitySubScoreWeights }
    )
    if (!store.error) {
      healthConfigSaved.value = true
      setTimeout(() => healthConfigSaved.value = false, 3000)
    } else {
      healthConfigError.value = store.error
    }
  } catch (e: any) {
    healthConfigError.value = e.message || 'Failed to save health config.'
  } finally {
    healthConfigSaving.value = false
  }
}

async function saveBugRatioAlertsPanel() {
  bugRatioAlertsSaved.value = false
  bugRatioAlertsError.value = ''
  bugRatioAlertsSaving.value = true
  try {
    await store.saveBugRatioAlertsAction(form.bugRatioAlertThreshold, form.bugRatioConsecutiveSprintCount, form.defaultSpPerBug)
    if (!store.error) {
      bugRatioAlertsSaved.value = true
      setTimeout(() => bugRatioAlertsSaved.value = false, 3000)
    } else {
      bugRatioAlertsError.value = store.error
    }
  } catch (e: any) {
    bugRatioAlertsError.value = e.message || 'Failed to save bug ratio alerts.'
  } finally {
    bugRatioAlertsSaving.value = false
  }
}
</script>

<template>
  <!-- Health Thresholds -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <h2 class="text-lg font-semibold text-gray-200">Health Thresholds</h2>
    <p class="text-sm text-gray-400">Green/amber boundaries for health score. Values above green are green, between amber and green are amber, below amber are red.</p>

    <div class="grid grid-cols-3 gap-4">
      <div>
        <h3 class="text-sm font-medium text-gray-300 mb-2">Completion Rate (%)</h3>
        <div class="space-y-2">
          <div>
            <label class="text-xs text-green-400">Green &ge;</label>
            <input v-model.number="form.healthThresholds.completionGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
          </div>
          <div>
            <label class="text-xs text-amber-400">Amber &ge;</label>
            <input v-model.number="form.healthThresholds.completionAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
          </div>
        </div>
      </div>

      <div>
        <h3 class="text-sm font-medium text-gray-300 mb-2">Disruption Rate (%)</h3>
        <div class="space-y-2">
          <div>
            <label class="text-xs text-green-400">Green &le;</label>
            <input v-model.number="form.healthThresholds.disruptionGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
          </div>
          <div>
            <label class="text-xs text-amber-400">Amber &le;</label>
            <input v-model.number="form.healthThresholds.disruptionAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
          </div>
        </div>
      </div>

      <div>
        <h3 class="text-sm font-medium text-gray-300 mb-2">Carry-Over Rate (%)</h3>
        <div class="space-y-2">
          <div>
            <label class="text-xs text-green-400">Green &le;</label>
            <input v-model.number="form.healthThresholds.carryOverGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
          </div>
          <div>
            <label class="text-xs text-amber-400">Amber &le;</label>
            <input v-model.number="form.healthThresholds.carryOverAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
          </div>
        </div>
      </div>
    </div>
  </section>

  <!-- Health Weights -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <h2 class="text-lg font-semibold text-gray-200">Health Weights</h2>
    <p class="text-sm text-gray-400">Relative weights for composite health score. Must sum to 100.</p>
    <div class="grid grid-cols-3 gap-4">
      <div>
        <label class="block text-sm text-gray-300 mb-1">Completion</label>
        <input v-model.number="form.healthWeights.completion" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
      </div>
      <div>
        <label class="block text-sm text-gray-300 mb-1">Disruption</label>
        <input v-model.number="form.healthWeights.disruption" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
      </div>
      <div>
        <label class="block text-sm text-gray-300 mb-1">Carry-Over</label>
        <input v-model.number="form.healthWeights.carryOver" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
      </div>
    </div>
    <div class="text-sm" :class="weightsSum() === 100 ? 'text-green-400' : 'text-red-400'">
      Sum: {{ weightsSum() }} / 100
    </div>
  </section>

  <!-- Quality sub-group (Xray only) -->
  <section v-if="form.xrayEnabled" class="bg-gray-900 rounded-lg p-6 space-y-4">
    <h2 class="text-lg font-semibold text-gray-200">Quality Settings</h2>
    <p class="text-sm text-gray-400">Configure how test quality contributes to the health score. Quality weight is additive to delivery weights.</p>

    <!-- Quality Health Weight -->
    <div>
      <label class="flex items-center gap-1 text-sm text-gray-300 mb-1">
        Quality Health Weight
        <InfoTooltip text="How much Quality contributes to the health score. Additive to delivery weights. Set 0 to observe without affecting score." />
      </label>
      <input
        v-model.number="form.qualityHealthWeight"
        type="number"
        min="0"
        max="100"
        :disabled="isReadOnly"
        class="w-32 bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
      />
    </div>

    <!-- QA Thresholds -->
    <div>
      <h3 class="text-sm font-medium text-gray-300 mb-2">
        Coverage Rate Thresholds
        <InfoTooltip text="Green/amber thresholds for Coverage Rate card coloring. Default: green ≥ 80%, amber ≥ 50%." />
      </h3>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="text-xs text-green-400">Green &ge;</label>
          <input v-model.number="form.qaHealthThresholds.coverageGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
        </div>
        <div>
          <label class="text-xs text-amber-400">Amber &ge;</label>
          <input v-model.number="form.qaHealthThresholds.coverageAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
        </div>
      </div>
    </div>

    <div>
      <h3 class="text-sm font-medium text-gray-300 mb-2">
        Execution Rate Thresholds
        <InfoTooltip text="Green/amber thresholds for Execution Rate card coloring. Default: green ≥ 80%, amber ≥ 50%." />
      </h3>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="text-xs text-green-400">Green &ge;</label>
          <input v-model.number="form.qaHealthThresholds.executionGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
        </div>
        <div>
          <label class="text-xs text-amber-400">Amber &ge;</label>
          <input v-model.number="form.qaHealthThresholds.executionAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
        </div>
      </div>
    </div>

    <div>
      <h3 class="text-sm font-medium text-gray-300 mb-2">
        Pass Rate Thresholds
        <InfoTooltip text="Green/amber thresholds for Pass Rate card coloring. Default: green ≥ 90%, amber ≥ 70%." />
      </h3>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="text-xs text-green-400">Green &ge;</label>
          <input v-model.number="form.qaHealthThresholds.passRateGreen" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
        </div>
        <div>
          <label class="text-xs text-amber-400">Amber &ge;</label>
          <input v-model.number="form.qaHealthThresholds.passRateAmber" type="number" min="0" max="100" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-2 py-1 text-gray-100 text-sm focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
        </div>
      </div>
    </div>

    <!-- Coverage / Pass Rate Weights -->
    <div>
      <h3 class="flex items-center gap-1 text-sm font-medium text-gray-300 mb-2">
        Coverage / Pass Rate Weights
        <InfoTooltip text="How Coverage Rate and Pass Rate combine within the Quality sub-score. Default: 50/50. Minimum 1 each." />
      </h3>
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm text-gray-300 mb-1">Coverage Rate</label>
          <input v-model.number="form.qualitySubScoreWeights.coverageWeight" type="number" min="1" max="99" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
        </div>
        <div>
          <label class="block text-sm text-gray-300 mb-1">Pass Rate</label>
          <input v-model.number="form.qualitySubScoreWeights.passRateWeight" type="number" min="1" max="99" :disabled="isReadOnly" class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed" />
        </div>
      </div>
      <div class="text-sm mt-2" :class="qualityWeightsSum() === 100 ? 'text-green-400' : 'text-red-400'">
        Sum: {{ qualityWeightsSum() }} / 100
      </div>
    </div>
  </section>

  <!-- Save button shared for all health/quality config -->
  <section class="bg-gray-900 rounded-lg p-6">
    <div class="flex items-center gap-4">
      <button
        @click="saveHealthConfigPanel"
        :disabled="healthConfigSaving || weightsSum() !== 100 || (form.xrayEnabled && qualityWeightsSum() !== 100) || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ healthConfigSaving ? 'Saving...' : 'Save Health Config' }}
      </button>
      <span v-if="healthConfigSaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="healthConfigError" class="text-red-400 text-sm">{{ healthConfigError }}</span>
    </div>
  </section>

  <!-- Bug Ratio Alerts -->
  <section class="bg-gray-900 rounded-lg p-6 space-y-4">
    <div class="flex items-center gap-1">
      <h2 class="text-lg font-semibold text-gray-200">Bug Ratio Alerts</h2>
      <span
        class="text-gray-500 cursor-help"
        title="Configure when the bug ratio alert triggers: threshold percentage and consecutive sprint count."
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-3.5 h-3.5">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a.75.75 0 000 1.5h.253a.25.25 0 01.244.304l-.459 2.066A1.75 1.75 0 0010.747 15H11a.75.75 0 000-1.5h-.253a.25.25 0 01-.244-.304l.459-2.066A1.75 1.75 0 009.253 9H9z" clip-rule="evenodd" />
        </svg>
      </span>
    </div>
    <p class="text-sm text-gray-400">Alert thresholds for flagging developers with consistently high bug ratios.</p>
    <div class="grid grid-cols-2 gap-4">
      <div>
        <label class="block text-sm text-gray-300 mb-1">Alert Threshold (%)</label>
        <input
          v-model.number="form.bugRatioAlertThreshold"
          type="number"
          min="0"
          max="100"
          :disabled="isReadOnly"
          class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        />
        <p class="text-xs text-gray-500 mt-1">Bug ratio percentage above which a sprint counts toward the alert (0–100).</p>
      </div>
      <div>
        <label class="block text-sm text-gray-300 mb-1">Consecutive Sprint Count</label>
        <input
          v-model.number="form.bugRatioConsecutiveSprintCount"
          type="number"
          min="1"
          max="10"
          :disabled="isReadOnly"
          class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        />
        <p class="text-xs text-gray-500 mt-1">Number of consecutive sprints above threshold before alert fires (1–10).</p>
      </div>
    </div>
    <div class="grid grid-cols-2 gap-4">
      <div>
        <label class="block text-sm text-gray-300 mb-1 cursor-help" title="Default story points applied to bug tickets with no estimate. Used across all SP calculations. Set to 0 to disable the fallback.">Default SP per Bug</label>
        <input
          v-model.number="form.defaultSpPerBug"
          type="number"
          min="0"
          max="13"
          :disabled="isReadOnly"
          class="w-full bg-gray-800 border border-gray-700 rounded px-3 py-2 text-gray-100 focus:outline-none focus:border-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        />
        <p class="text-xs text-gray-500 mt-1">SP applied to unestimated bug tickets across all metrics (0–13). Default: 3.</p>
      </div>
    </div>
    <div class="flex items-center gap-4">
      <button
        @click="saveBugRatioAlertsPanel"
        :disabled="bugRatioAlertsSaving || isReadOnly"
        class="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-white px-4 py-2 rounded font-medium text-sm"
      >
        {{ bugRatioAlertsSaving ? 'Saving...' : 'Save Bug Ratio Alerts' }}
      </button>
      <span v-if="bugRatioAlertsSaved" class="text-green-400 text-sm">Saved.</span>
      <span v-if="bugRatioAlertsError" class="text-red-400 text-sm">{{ bugRatioAlertsError }}</span>
    </div>
  </section>
</template>
