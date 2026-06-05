import type { AppSettings, BoardOption, DetectionResult, StatusOption, CycleTimeBoundariesResponse, SaveCycleTimeBoundariesResponse, HealthThresholdConfig, HealthWeightConfig, TestConnectionResponse, XraySyncResponse, QaHealthThresholdConfig, QualitySubScoreWeightConfig } from '../types'
import { apiFetch } from './client'

export function getSettings(): Promise<AppSettings> {
  return apiFetch<AppSettings>('/settings')
}

export function saveBoard(boardId: number | null): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>('/settings/board', {
    method: 'PUT',
    body: JSON.stringify({ boardId })
  })
}

export function saveDoneStatuses(statuses: string[]): Promise<string[]> {
  return apiFetch<string[]>('/settings/done-statuses', {
    method: 'PUT',
    body: JSON.stringify({ statuses })
  })
}

export function saveWorkflowStages(stages: string[]): Promise<string[]> {
  return apiFetch<string[]>('/settings/workflow-stages', {
    method: 'PUT',
    body: JSON.stringify({ stages })
  })
}

export function saveHealthConfig(
  healthThresholds: HealthThresholdConfig,
  healthWeights: HealthWeightConfig,
  qaHealthThresholds: QaHealthThresholdConfig,
  qualityHealthWeight: number,
  qualitySubScoreWeights: QualitySubScoreWeightConfig
): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>('/settings/health-config', {
    method: 'PUT',
    body: JSON.stringify({ healthThresholds, healthWeights, qaHealthThresholds, qualityHealthWeight, qualitySubScoreWeights })
  })
}

export function saveBugRatioAlerts(alertThreshold: number, consecutiveSprintCount: number, defaultSpPerBug: number): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>('/settings/bug-ratio-alerts', {
    method: 'PUT',
    body: JSON.stringify({ alertThreshold, consecutiveSprintCount, defaultSpPerBug })
  })
}

export function saveAnalyticsTargets(bugRatioTarget: number): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>('/settings/analytics-targets', {
    method: 'PUT',
    body: JSON.stringify({ bugRatioTarget })
  })
}

export function saveSyncConfig(syncBackSprintCount: number, planningWindowDays: number): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>('/settings/sync-config', {
    method: 'PUT',
    body: JSON.stringify({ syncBackSprintCount, planningWindowDays })
  })
}

export function detectWorkflowStages(): Promise<DetectionResult> {
  return apiFetch<DetectionResult>('/settings/workflow-stages/detect')
}

export function getBoards(): Promise<{ boards: BoardOption[] }> {
  return apiFetch<{ boards: BoardOption[] }>('/boards')
}

export function getStatuses(): Promise<{ statuses: StatusOption[] }> {
  return apiFetch<{ statuses: StatusOption[] }>('/jira/statuses')
}

export function getExcludedStatuses(): Promise<string[]> {
  return apiFetch<string[]>('/settings/excluded-statuses')
}

export function saveExcludedStatuses(statuses: string[]): Promise<string[]> {
  return apiFetch<string[]>('/settings/excluded-statuses', {
    method: 'PUT',
    body: JSON.stringify({ statuses })
  })
}

export function getCycleTimeBoundaries(): Promise<CycleTimeBoundariesResponse> {
  return apiFetch<CycleTimeBoundariesResponse>('/settings/cycle-time-boundaries')
}

export function saveCycleTimeBoundaries(startStage: string, endStage: string): Promise<SaveCycleTimeBoundariesResponse> {
  return apiFetch<SaveCycleTimeBoundariesResponse>('/settings/cycle-time-boundaries', {
    method: 'PUT',
    body: JSON.stringify({ startStage, endStage })
  })
}

// xrayClientSecret: pass the new secret string to update it, or null to leave the existing secret unchanged
export function saveXraySettings(xrayEnabled: boolean, xrayClientId: string, xrayClientSecret: string | null): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>('/settings/xray', {
    method: 'PUT',
    body: JSON.stringify({ xrayEnabled, xrayClientId, xrayClientSecret })
  })
}

export function testXrayConnection(): Promise<TestConnectionResponse> {
  return apiFetch<TestConnectionResponse>('/xray/test-connection', {
    method: 'POST'
  })
}

export function syncXrayData(sprintIds: number[]): Promise<XraySyncResponse> {
  return apiFetch<XraySyncResponse>('/xray/sync', {
    method: 'POST',
    body: JSON.stringify({ sprintIds })
  })
}
