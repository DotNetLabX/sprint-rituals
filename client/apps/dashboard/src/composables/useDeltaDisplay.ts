export function useDeltaDisplay() {
  function deltaIcon(direction: string | null | undefined): string {
    if (direction === 'up') return '▲'
    if (direction === 'down') return '▼'
    return '—'
  }

  function deltaClass(polarity: string | null | undefined, direction: string | null | undefined): string {
    if (direction === 'flat' || !direction) return 'text-text-secondary'
    if (polarity === 'positive') return 'text-status-success'
    if (polarity === 'negative') return 'text-status-danger'
    return 'text-text-secondary'
  }

  return { deltaIcon, deltaClass }
}
