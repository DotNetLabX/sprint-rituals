import { computed } from 'vue'
import { useCardStore, type Card, type EvidenceLink } from '../stores/card'
import { useParticipantStore } from '../stores/participant'
import { useAuthStore } from '../stores/auth'
import { useVoteStore } from '../stores/vote'
import { useRetroSessionStore } from '../stores/retroSession'
import { usePolicyStore } from '../stores/policy'
import { useCardGroupStore, type CardGroup } from '../stores/cardGroup'

export function useRetroBoardActions(retroSessionId: number, onError: (msg: string) => void) {
  const cardStore = useCardStore()
  const participantStore = useParticipantStore()
  const authStore = useAuthStore()
  const voteStore = useVoteStore()
  const retroSessionStore = useRetroSessionStore()
  const policyStore = usePolicyStore()
  const cardGroupStore = useCardGroupStore()

  // --- Board state ---

  const participants = computed(() => participantStore.participants)
  const column1Cards = computed(() => cardStore.column1Cards)
  const column2Cards = computed(() => cardStore.column2Cards)
  const column3Cards = computed(() => cardStore.column3Cards)
  const actionItems = computed(() => cardStore.actionItemCards)
  const currentPhase = computed(() => retroSessionStore.currentSummary?.currentPhase ?? '')
  const currentParticipantId = computed(() => participantStore.currentParticipant?.id ?? 0)
  const isWritingPhase = computed(() => currentPhase.value === 'Writing')
  const isDiscussingPhase = computed(() => currentPhase.value === 'Discussing')
  const showGroups = computed(() => ['Grouping', 'Voting', 'Discussing', 'Reporting'].includes(currentPhase.value))
  const isScrumMaster = computed(() => retroSessionStore.currentSummary?.isScrumMaster ?? false)
  const canGroup = computed(() => policyStore.isAllowed(currentPhase.value, 'GroupCards'))
  const canUngroup = computed(() => policyStore.isAllowed(currentPhase.value, 'UngroupCards'))
  const column1Groups = computed(() => [...cardGroupStore.column1Groups].sort((a, b) => voteStore.voteCountForGroup(b.id) - voteStore.voteCountForGroup(a.id)))
  const column2Groups = computed(() => [...cardGroupStore.column2Groups].sort((a, b) => voteStore.voteCountForGroup(b.id) - voteStore.voteCountForGroup(a.id)))
  const column3Groups = computed(() => [...cardGroupStore.column3Groups].sort((a, b) => voteStore.voteCountForGroup(b.id) - voteStore.voteCountForGroup(a.id)))
  const showActionItems = computed(() => policyStore.isAllowed(currentPhase.value, 'CreateActionItem') || currentPhase.value === 'Reporting')
  const boardGridClasses = computed(() => showActionItems.value
    ? 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 flex-1 min-h-0 grid-rows-[1fr]'
    : 'grid grid-cols-1 sm:grid-cols-3 gap-4 flex-1 min-h-0 grid-rows-[1fr]')

  // --- Prop builders ---

  function canMutateCard(card: Card): boolean {
    const ownsCard = card.participantId === participantStore.currentParticipant?.id || authStore.user !== null
    if (!ownsCard) return false
    const editAction = card.cardType === 'ActionItem' ? 'EditActionItem' : 'EditNote'
    return policyStore.isAllowed(currentPhase.value, editAction)
  }

  function cardItemProps(card: Card, color: string) {
    return {
      card,
      canEdit: canMutateCard(card),
      blurred: isWritingPhase.value && card.participantId !== currentParticipantId.value,
      canAttachLink: policyStore.isAllowed(currentPhase.value, 'AttachEvidenceLink'),
      showVoting: false, voteCount: 0, myVoteCount: 0,
      remainingBudget: voteStore.remainingBudget,
      columnColor: color,
    }
  }

  function groupItemProps(group: CardGroup, color: string) {
    return {
      group, columnColor: color,
      showVoting: policyStore.isAllowed(currentPhase.value, 'CastVote'),
      voteCount: voteStore.voteCountForGroup(group.id),
      myVoteCount: voteStore.myVotesOnGroup(group.id),
      remainingBudget: voteStore.remainingBudget,
      isScrumMaster: isScrumMaster.value,
      canGroup: canGroup.value, canUngroup: canUngroup.value,
      isDiscussing: isDiscussingPhase.value,
    }
  }

  // --- Mutation handlers ---

  async function addNote(column: 'Column1' | 'Column2' | 'Column3', text: string) {
    try { await cardStore.createCard(retroSessionId, { text, columnType: column, cardType: 'Note' }) }
    catch { onError('Failed to add card.') }
  }

  async function addActionItem(text: string, assigneeName: string) {
    try { await cardStore.createCard(retroSessionId, { text, columnType: 'ActionItems', cardType: 'ActionItem', assigneeName }) }
    catch { onError('Failed to add action item.') }
  }

  async function saveEdit(card: Card, text: string, assigneeName: string | null) {
    const prev = { ...card }
    const idx = cardStore.cards.findIndex((c) => c.id === card.id)
    if (idx !== -1) cardStore.cards[idx] = { ...card, text, assigneeName, updatedAt: new Date().toISOString() }
    try { await cardStore.editCard(retroSessionId, card.id, { text, assigneeName }) }
    catch { if (idx !== -1) cardStore.cards[idx] = prev; onError('Failed to update card.') }
  }

  async function deleteCard(card: Card) {
    const snapshot = [...cardStore.cards]
    cardStore.cards = cardStore.cards.filter((c) => c.id !== card.id)
    try { await cardStore.deleteCard(retroSessionId, card.id) }
    catch { cardStore.cards = snapshot; onError('Failed to delete card.') }
  }

  async function castVote(card: Card) {
    if (!card.groupId) return
    try { await voteStore.castVote(retroSessionId, card.groupId) }
    catch { onError('Failed to cast vote.') }
  }

  async function retractVote(card: Card) {
    if (!card.groupId) return
    try { await voteStore.retractVote(retroSessionId, card.groupId) }
    catch { onError('Failed to retract vote.') }
  }

  async function castGroupVote(group: CardGroup) {
    try { await voteStore.castVote(retroSessionId, group.id) }
    catch { onError('Failed to cast vote.') }
  }

  async function retractGroupVote(group: CardGroup) {
    try { await voteStore.retractVote(retroSessionId, group.id) }
    catch { onError('Failed to retract vote.') }
  }

  async function attachEvidenceLink(cardId: number, url: string) {
    const card = cardStore.cards.find((c) => c.id === cardId)
    if (!card) return
    const optimistic: EvidenceLink = {
      id: -Date.now(), cardId, url, source: 'Generic', title: null, description: null,
      iconUrl: null, imageUrl: null, externalKey: null, fetchedAt: null, fetchStatus: 'Pending',
    }
    card.evidenceLinks.push(optimistic)
    try {
      const real = await cardStore.attachEvidenceLink(retroSessionId, cardId, url)
      const idx = card.evidenceLinks.findIndex((e) => e.id === optimistic.id)
      if (idx !== -1) card.evidenceLinks[idx] = real
    } catch { card.evidenceLinks = card.evidenceLinks.filter((e) => e.id !== optimistic.id); onError('Failed to attach link.') }
  }

  async function detachEvidenceLink(cardId: number, evidenceLinkId: number) {
    const card = cardStore.cards.find((c) => c.id === cardId)
    if (!card) return
    const snapshot = [...card.evidenceLinks]
    card.evidenceLinks = card.evidenceLinks.filter((e) => e.id !== evidenceLinkId)
    try { await cardStore.detachEvidenceLink(retroSessionId, cardId, evidenceLinkId) }
    catch { card.evidenceLinks = snapshot; onError('Failed to remove link.') }
  }

  async function focusGroup(group: CardGroup) {
    try { await cardGroupStore.focusGroup(retroSessionId, group.id) }
    catch { onError('Failed to focus group.') }
  }

  async function toggleDiscussed(group: CardGroup) {
    try { await cardGroupStore.toggleDiscussed(retroSessionId, group.id) }
    catch { onError('Failed to toggle discussed state.') }
  }

  async function dissolveGroup(group: CardGroup) {
    try { await cardGroupStore.dissolveGroup(retroSessionId, group.id) }
    catch { onError('Failed to dissolve group.') }
  }

  async function updateGroupTitle(group: CardGroup, title: string | null) {
    try { await cardGroupStore.updateGroupTitle(retroSessionId, group.id, title) }
    catch { onError('Failed to update group title.') }
  }

  async function mergeGroups(source: CardGroup, target: CardGroup) {
    try {
      const sourceIsSolo = source.cards.length === 1
      const targetIsSolo = target.cards.length === 1
      if (sourceIsSolo && !targetIsSolo) await cardGroupStore.addCardToGroup(retroSessionId, target.id, source.cards[0]!.id)
      else if (!sourceIsSolo && targetIsSolo) await cardGroupStore.addCardToGroup(retroSessionId, source.id, target.cards[0]!.id)
      else await cardGroupStore.createGroup(retroSessionId, [...source.cards.map((c) => c.id), ...target.cards.map((c) => c.id)])
    } catch { onError('Failed to merge groups.') }
  }

  async function removeCardFromGroup(groupId: number, cardId: number) {
    try { await cardGroupStore.removeCardFromGroup(retroSessionId, groupId, cardId) }
    catch { onError('Failed to remove card from group.') }
  }

  async function updateColumnConfig(
    column1Name: string, column1Color: string,
    column2Name: string, column2Color: string,
    column3Name: string, column3Color: string,
  ) {
    try {
      await retroSessionStore.updateColumns(retroSessionId, { column1Name, column1Color, column2Name, column2Color, column3Name, column3Color })
    } catch { onError('Failed to update column configuration.') }
  }

  return {
    // state
    participants, column1Cards, column2Cards, column3Cards, actionItems,
    currentPhase, isWritingPhase, showGroups, isScrumMaster,
    column1Groups, column2Groups, column3Groups, showActionItems, boardGridClasses,
    // prop builders
    cardItemProps, groupItemProps, canMutateCard,
    // handlers
    addNote, addActionItem, saveEdit, deleteCard,
    castVote, retractVote, castGroupVote, retractGroupVote,
    attachEvidenceLink, detachEvidenceLink,
    focusGroup, toggleDiscussed, dissolveGroup, updateGroupTitle, mergeGroups, removeCardFromGroup,
    updateColumnConfig,
  }
}
