import { defineStore, storeToRefs } from 'pinia'
import { ref, computed } from 'vue'
import type { EpicProgressResponse, EpicProgressEntry } from '../types'
import { getEpicProgress, getSubTeams } from '../api/analytics'
import { useSettingsStore } from './settingsStore'

const SORT_STORAGE_KEY = 'fokus-epics-sort'
const COLUMNS_STORAGE_KEY = 'fokus-epics-columns'
const DEFAULT_SORT_COLUMN = 'lastWorkDate'
const DEFAULT_SORT_DIRECTION: 'asc' | 'desc' = 'desc'

// All sortable column ids. spDoneTotal is intentionally absent — it has no sort handler.
const SORTABLE_COLUMNS = new Set([
  'epicName', 'progress', 'tickets', 'velocity', 'projected',
  'startedDate', 'lastWorkDate', 'coverageRate', 'passRate', 'bugsFound',
])
// QA-gated sortable columns — only valid when hasQaData is true
const QA_SORTABLE_COLUMNS = new Set(['coverageRate', 'passRate', 'bugsFound'])

function loadSortFromStorage(): { column: string; direction: 'asc' | 'desc' } {
  try {
    const raw = localStorage.getItem(SORT_STORAGE_KEY)
    if (raw) {
      const parsed = JSON.parse(raw)
      if (
        parsed &&
        typeof parsed.column === 'string' &&
        (parsed.direction === 'asc' || parsed.direction === 'desc') &&
        SORTABLE_COLUMNS.has(parsed.column)   // LOW: reject unknown column ids
      ) {
        return parsed
      }
    }
  } catch {
    // ignore parse errors
  }
  return { column: DEFAULT_SORT_COLUMN, direction: DEFAULT_SORT_DIRECTION }
}

// Returns true when columnId can be sorted given current visibility and QA data state.
function isSortableColumn(columnId: string, hiddenCols: Set<string>, hasQaData: boolean): boolean {
  if (!SORTABLE_COLUMNS.has(columnId)) return false
  if (hiddenCols.has(columnId)) return false
  if (QA_SORTABLE_COLUMNS.has(columnId) && !hasQaData) return false
  return true
}

function loadHiddenColumnsFromStorage(): Set<string> {
  try {
    const raw = localStorage.getItem(COLUMNS_STORAGE_KEY)
    if (raw) {
      const parsed = JSON.parse(raw)
      if (Array.isArray(parsed)) {
        return new Set<string>(parsed)
      }
    }
  } catch {
    // ignore parse errors
  }
  return new Set<string>()
}

function getEpicSortValue(epic: EpicProgressEntry, column: string): number | string | null {
  switch (column) {
    case 'epicName': return epic.epicName
    case 'progress':
      return epic.spCompletionPercentage !== null
        ? epic.spCompletionPercentage
        : epic.ticketCompletionPercentage
    case 'tickets': return epic.doneTickets
    case 'velocity': return epic.velocity
    case 'projected': return epic.projectedSprintsRemaining
    // Date columns return ISO 8601 strings (YYYY-MM-DD) — lexicographic order equals chronological order,
    // so localeCompare in the sortedEpics comparator produces correct date sorting.
    case 'startedDate': return epic.startedDate
    case 'lastWorkDate': return epic.lastWorkDate
    case 'coverageRate': return epic.coverageRate
    case 'passRate': return epic.passRate
    case 'bugsFound': return epic.bugsFound
    default: return null
  }
}

export const useEpicsStore = defineStore('epics', () => {
  const settingsStore = useSettingsStore()
  const { settings } = storeToRefs(settingsStore)

  const subTeams = ref<string[]>([])
  const selectedSubTeam = ref<string | null>(null)
  const activeFilter = ref<'active' | 'completed'>('active')
  const epicProgress = ref<EpicProgressResponse | null>(null)
  const expandedEpicKeys = ref<Set<string>>(new Set())
  const loading = ref(false)
  const initializing = ref(false)
  const error = ref<string | null>(null)

  // F35: search, sort, and column visibility state — hydrated from localStorage on store init
  const searchQuery = ref<string>('')

  const savedSort = loadSortFromStorage()
  const savedHiddenColumns = loadHiddenColumnsFromStorage()

  // BR11: if stored sort column is hidden or not in the renderable sortable set, reset to default.
  // At hydration time hasQaData is unknown — treat QA columns as invalid to avoid sorting by a
  // column that may be gated off; they will become available once the API response arrives.
  const initialSortValid = isSortableColumn(savedSort.column, savedHiddenColumns, /* hasQaData */ false)
  const initialSortColumn = initialSortValid ? savedSort.column : DEFAULT_SORT_COLUMN
  const initialSortDirection = initialSortValid ? savedSort.direction : DEFAULT_SORT_DIRECTION

  const sortColumn = ref<string>(initialSortColumn)
  const sortDirection = ref<'asc' | 'desc'>(initialSortDirection)
  const hiddenColumns = ref<Set<string>>(savedHiddenColumns)

  // Active/completed filter applied to the raw API response
  const filteredEpics = computed<EpicProgressEntry[]>(() => {
    if (!epicProgress.value) return []
    return activeFilter.value === 'active'
      ? epicProgress.value.epics.filter(e => !e.isCompleted)
      : epicProgress.value.epics.filter(e => e.isCompleted)
  })

  // F35: search filter applied on top of active/completed filter
  const searchFilteredEpics = computed<EpicProgressEntry[]>(() => {
    const query = searchQuery.value.trim().toLowerCase()
    if (!query) return filteredEpics.value
    return filteredEpics.value.filter(e =>
      e.epicName.toLowerCase().includes(query) ||
      e.epicKey.toLowerCase().includes(query)
    )
  })

  // F35: sorted view — null values always sort last (BR8)
  const sortedEpics = computed<EpicProgressEntry[]>(() => {
    const epics = [...searchFilteredEpics.value]
    const col = sortColumn.value
    const dir = sortDirection.value

    epics.sort((a, b) => {
      const aVal = getEpicSortValue(a, col)
      const bVal = getEpicSortValue(b, col)

      // Nulls always last regardless of direction
      if (aVal === null && bVal === null) return 0
      if (aVal === null) return 1
      if (bVal === null) return -1

      let cmp: number
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        cmp = aVal.localeCompare(bVal)
      } else {
        cmp = (aVal as number) < (bVal as number) ? -1 : (aVal as number) > (bVal as number) ? 1 : 0
      }

      return dir === 'asc' ? cmp : -cmp
    })

    return epics
  })

  // F35: summary metrics recalculated from search-filtered active epics (BR5)
  const searchSummaryMetrics = computed(() => {
    const searchActive = searchFilteredEpics.value.filter(e => !e.isCompleted)
    const searchCompleted = searchFilteredEpics.value.filter(e => e.isCompleted)

    const activeEpicCount = activeFilter.value === 'active' ? searchActive.length : searchCompleted.length

    // Average completion: weighted by adjustedTotalSp, fallback to ticket completion average
    let averageCompletion = 0
    const epicsForCompletion = activeFilter.value === 'completed' ? searchCompleted : searchActive
    if (activeFilter.value === 'completed') {
      averageCompletion = 100
    } else {
      const totalAdjustedSp = epicsForCompletion
        .filter(e => e.adjustedTotalSp !== null)
        .reduce((sum, e) => sum + e.adjustedTotalSp!, 0)

      if (totalAdjustedSp > 0) {
        const weightedSum = epicsForCompletion
          .filter(e => e.adjustedTotalSp !== null && e.spCompletionPercentage !== null)
          .reduce((sum, e) => sum + e.spCompletionPercentage! * e.adjustedTotalSp!, 0)
        averageCompletion = totalAdjustedSp > 0 ? weightedSum / totalAdjustedSp : 0
      } else if (epicsForCompletion.length > 0) {
        averageCompletion = epicsForCompletion.reduce((sum, e) => sum + e.ticketCompletionPercentage, 0) / epicsForCompletion.length
      }
    }

    // Average test coverage: arithmetic mean of non-null coverageRate (BR5 — from search-filtered set)
    const coverageRates = searchFilteredEpics.value
      .filter(e => e.coverageRate !== null)
      .map(e => e.coverageRate!)
    const averageTestCoverage = coverageRates.length > 0
      ? coverageRates.reduce((sum, r) => sum + r, 0) / coverageRates.length
      : null

    return {
      activeEpicCount,
      averageCompletion,
      averageTestCoverage
    }
  })

  const hasEpics = computed<boolean>(() =>
    (epicProgress.value?.epics.length ?? 0) > 0
  )

  const isXrayEnabled = computed<boolean>(() =>
    settings.value.xrayEnabled
  )

  async function initialize() {
    initializing.value = true
    error.value = null
    try {
      const [teams] = await Promise.all([
        getSubTeams(),
        settingsStore.fetchSettings(),
        fetchEpicProgress()
      ])
      subTeams.value = teams
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to initialize epics'
    } finally {
      initializing.value = false
    }
  }

  async function fetchEpicProgress() {
    loading.value = true
    error.value = null
    try {
      epicProgress.value = await getEpicProgress(selectedSubTeam.value ?? undefined)
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load epic progress'
    } finally {
      loading.value = false
    }
  }

  async function selectSubTeam(subTeam: string | null) {
    selectedSubTeam.value = subTeam
    await fetchEpicProgress()
  }

  function setActiveFilter(filter: 'active' | 'completed') {
    activeFilter.value = filter
  }

  function toggleEpicExpanded(epicKey: string) {
    if (expandedEpicKeys.value.has(epicKey)) {
      expandedEpicKeys.value.delete(epicKey)
    } else {
      expandedEpicKeys.value.add(epicKey)
    }
  }

  function isEpicExpanded(epicKey: string): boolean {
    return expandedEpicKeys.value.has(epicKey)
  }

  // F35 actions

  function setSearchQuery(query: string) {
    searchQuery.value = query
  }

  function toggleSort(column: string) {
    if (sortColumn.value === column) {
      sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
    } else {
      sortColumn.value = column
      sortDirection.value = 'asc'
    }
    localStorage.setItem(SORT_STORAGE_KEY, JSON.stringify({ column: sortColumn.value, direction: sortDirection.value }))
  }

  function toggleColumnVisibility(columnId: string) {
    // Reassign .value to trigger Vue reactivity — ref<Set> tracks .value reassignment, not Set mutations
    const next = new Set(hiddenColumns.value)
    if (next.has(columnId)) {
      next.delete(columnId)
    } else {
      next.add(columnId)
    }
    hiddenColumns.value = next

    // BR11: after visibility change, validate current sort column against the full renderable set
    // (respects both hidden state and hasQaData gate). Reset to default if no longer sortable.
    const hasQaData = epicProgress.value?.hasQaData ?? false
    if (!isSortableColumn(sortColumn.value, next, hasQaData)) {
      sortColumn.value = DEFAULT_SORT_COLUMN
      sortDirection.value = DEFAULT_SORT_DIRECTION
      localStorage.setItem(SORT_STORAGE_KEY, JSON.stringify({ column: DEFAULT_SORT_COLUMN, direction: DEFAULT_SORT_DIRECTION }))
    }

    localStorage.setItem(COLUMNS_STORAGE_KEY, JSON.stringify(Array.from(next)))
  }

  function isColumnVisible(columnId: string): boolean {
    return !hiddenColumns.value.has(columnId)
  }

  return {
    subTeams,
    selectedSubTeam,
    activeFilter,
    epicProgress,
    expandedEpicKeys,
    loading,
    initializing,
    error,
    filteredEpics,
    searchQuery,
    sortColumn,
    sortDirection,
    hiddenColumns,
    sortedEpics,
    searchSummaryMetrics,
    hasEpics,
    isXrayEnabled,
    initialize,
    fetchEpicProgress,
    selectSubTeam,
    setActiveFilter,
    toggleEpicExpanded,
    isEpicExpanded,
    setSearchQuery,
    toggleSort,
    toggleColumnVisibility,
    isColumnVisible
  }
})
