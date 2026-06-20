import type { DeveloperQualityEntry, SparklinePoint } from '../types'

export function useQualityAverages() {
  function avgCoverage(dev: DeveloperQualityEntry): number {
    const vals = dev.sprintBreakdowns.filter(b => b.stories > 0).map(b => b.coveragePercent)
    if (vals.length === 0) return 0
    return vals.reduce((a, b) => a + b, 0) / vals.length
  }

  function avgPassRate(dev: DeveloperQualityEntry): number {
    const vals = dev.sprintBreakdowns.filter(b => b.stories > 0).map(b => b.passRatePercent)
    if (vals.length === 0) return 0
    return vals.reduce((a, b) => a + b, 0) / vals.length
  }

  function avgStories(dev: DeveloperQualityEntry): number {
    const vals = dev.sprintBreakdowns.map(b => b.stories)
    if (vals.length === 0) return 0
    return vals.reduce((a, b) => a + b, 0) / vals.length
  }

  function avgCovered(dev: DeveloperQualityEntry): number {
    const vals = dev.sprintBreakdowns.map(b => b.covered)
    if (vals.length === 0) return 0
    return vals.reduce((a, b) => a + b, 0) / vals.length
  }

  function avgUntested(dev: DeveloperQualityEntry): number {
    const vals = dev.sprintBreakdowns.map(b => b.untested)
    if (vals.length === 0) return 0
    return vals.reduce((a, b) => a + b, 0) / vals.length
  }

  function avgBugsFound(dev: DeveloperQualityEntry): number {
    const vals = dev.sprintBreakdowns.map(b => b.bugsFound)
    if (vals.length === 0) return 0
    return vals.reduce((a, b) => a + b, 0) / vals.length
  }

  function sparklinePath(points: SparklinePoint[] | null | undefined): string {
    if (!points || points.length < 2) return ''
    const w = 48
    const h = 20
    const values = points.map(p => p.value)
    const min = Math.min(...values)
    const max = Math.max(...values)
    const range = max - min || 1
    const step = w / (points.length - 1)
    return points
      .map((p, i) => {
        const x = i * step
        const y = h - ((p.value - min) / range) * h
        return `${i === 0 ? 'M' : 'L'}${x.toFixed(1)},${y.toFixed(1)}`
      })
      .join(' ')
  }

  return { avgCoverage, avgPassRate, avgStories, avgCovered, avgUntested, avgBugsFound, sparklinePath }
}
