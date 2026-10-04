<template>
  <!-- Solo group: plain card style -->
  <div
    v-if="isSolo"
    class="rounded-[--radius-md] shadow-card p-3 flex flex-col gap-1 transition-all hover:shadow-card-hover"
    :class="[groupBgClass, dragClasses]"
    :draggable="canGroup"
    @dragstart="onDragStart"
    @dragend="dragging = false"
    @dragover="onDragOver"
    @dragleave="onDragLeave"
    @drop="onDrop"
  >
    <div class="flex items-start gap-1">
      <GripVertical v-if="canGroup" :size="14" class="text-text-muted flex-shrink-0 mt-0.5" />
      <p class="text-sm text-text-primary whitespace-pre-wrap flex-1">{{ group.cards[0]?.text }}</p>
      <div v-if="showVoting" class="flex items-center gap-1 flex-shrink-0">
        <button class="flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border transition-all" :class="voteBtnClass" :disabled="remainingBudget <= 0 && myVoteCount === 0" @click="myVoteCount > 0 ? emit('retract-vote', group) : emit('vote', group)">
          <Check v-if="myVoteCount > 0" :size="14" /><ChevronUp v-else :size="14" /><span>{{ voteCount }}</span>
        </button>
      </div>
      <span v-else-if="voteCount > 0" class="text-xs font-medium text-text-muted flex-shrink-0">{{ voteCount }} votes</span>
    </div>
    <div v-if="group.cards[0]?.evidenceLinks?.length" class="flex flex-col gap-1 mt-1">
      <a v-for="link in group.cards[0]!.evidenceLinks" :key="link.id" :href="link.url" target="_blank" rel="noopener noreferrer" class="text-xs text-text-secondary truncate max-w-[200px] hover:text-blue-600">{{ link.title || link.url }}</a>
    </div>
    <RetroGroupDiscussion v-if="isDiscussing" :group="group" :is-scrum-master="isScrumMaster" @focus="emit('focus', group)" @toggle-discussed="emit('toggle-discussed', group)" />
    <div v-if="isScrumMaster && canGroup" class="flex items-center gap-2 mt-1 justify-end">
      <button class="text-xs text-text-muted hover:text-text-secondary cursor-pointer" @click="editingTitle = true">Rename</button>
    </div>
    <div v-if="editingTitle" class="flex gap-1">
      <input v-model="titleInput" maxlength="100" placeholder="Group title..." class="flex-1 border border-border rounded-[--radius-sm] px-2 py-1 text-xs text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500" @keydown.enter="saveTitle" @keydown.escape="editingTitle = false" />
      <button class="px-2 py-1 text-xs bg-primary-600 text-white rounded-[--radius-sm] cursor-pointer" @click="saveTitle">Save</button>
    </div>
  </div>

  <!-- Multi-card group -->
  <div
    v-else
    class="rounded-[--radius-md] shadow-card p-3 flex flex-col gap-2 transition-all hover:shadow-card-hover"
    :class="[groupBgClass, dragClasses]"
    :draggable="canGroup"
    @dragstart="onDragStart"
    @dragend="dragging = false"
    @dragover="onDragOver"
    @dragleave="onDragLeave"
    @drop="onDrop"
  >
    <div class="flex items-center gap-2">
      <GripVertical v-if="canGroup" :size="14" class="text-text-muted flex-shrink-0" />
      <span class="text-sm font-medium text-text-primary flex-1 truncate">{{ group.title || group.cards[0]?.text || 'Untitled group' }}</span>
      <div v-if="showVoting" class="flex items-center gap-1">
        <button class="flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border transition-all" :class="voteBtnClass" :disabled="remainingBudget <= 0 && myVoteCount === 0" @click="myVoteCount > 0 ? emit('retract-vote', group) : emit('vote', group)">
          <Check v-if="myVoteCount > 0" :size="14" /><ChevronUp v-else :size="14" /><span>{{ voteCount }}</span>
        </button>
      </div>
      <span v-else-if="voteCount > 0" class="text-xs font-medium text-text-muted">{{ voteCount }} votes</span>
    </div>
    <div v-if="expanded" class="flex flex-col gap-1 pl-2 border-l-2 border-border">
      <div v-for="card in group.cards" :key="card.id" class="text-xs text-text-secondary py-0.5" :class="{ 'cursor-grab': canUngroup }" :draggable="canUngroup" @dragstart.stop="onCardDragStart($event, card)">{{ card.text }}</div>
    </div>
    <div class="flex items-center gap-2">
      <button class="text-xs text-primary-600 hover:text-primary-800 cursor-pointer" @click="expanded = !expanded">{{ expanded ? 'Collapse' : `Show ${group.cards.length} cards` }}</button>
      <template v-if="isScrumMaster">
        <button v-if="canGroup" class="text-xs text-text-muted hover:text-text-secondary cursor-pointer" @click="editingTitle = true">Rename</button>
        <button v-if="canUngroup && group.cards.length > 1" class="text-xs text-danger hover:text-red-700 cursor-pointer" @click="emit('dissolve', group)">Dissolve</button>
      </template>
    </div>
    <RetroGroupDiscussion v-if="isDiscussing" :group="group" :is-scrum-master="isScrumMaster" @focus="emit('focus', group)" @toggle-discussed="emit('toggle-discussed', group)" />
    <div v-if="editingTitle" class="flex gap-1">
      <input v-model="titleInput" maxlength="100" placeholder="Group title..." class="flex-1 border border-border rounded-[--radius-sm] px-2 py-1 text-xs text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500" @keydown.enter="saveTitle" @keydown.escape="editingTitle = false" />
      <button class="px-2 py-1 text-xs bg-primary-600 text-white rounded-[--radius-sm] cursor-pointer" @click="saveTitle">Save</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { ChevronUp, Check, GripVertical } from 'lucide-vue-next'
import type { CardGroup, CardGroupCard } from '../../stores/cardGroup'
import RetroGroupDiscussion from './RetroGroupDiscussion.vue'

export interface DragPayload {
  type: 'group' | 'card'
  groupId: number
  columnType: string
  cardIds: number[]
  cardId?: number
  sourceGroupId?: number
}

const props = defineProps<{
  group: CardGroup
  columnColor: string
  showVoting: boolean
  voteCount: number
  myVoteCount: number
  remainingBudget: number
  isScrumMaster: boolean
  canGroup: boolean
  canUngroup: boolean
  isDiscussing?: boolean
}>()

const emit = defineEmits<{
  vote: [group: CardGroup]
  'retract-vote': [group: CardGroup]
  dissolve: [group: CardGroup]
  'update-title': [group: CardGroup, title: string | null]
  merge: [source: CardGroup, target: CardGroup]
  'remove-card': [groupId: number, cardId: number]
  focus: [group: CardGroup]
  'toggle-discussed': [group: CardGroup]
}>()

const expanded = ref(props.group.cards.length === 1)
const editingTitle = ref(false)
const titleInput = ref(props.group.title ?? '')
const dragging = ref(false)
const dragOver = ref(false)

const isSolo = computed(() => props.group.cards.length === 1)

const groupBgClass = computed(() => {
  if (props.group.isFocused) return `bg-col-${props.columnColor}-light ring-2 ring-primary-500`
  if (props.group.isDiscussed) return `bg-col-${props.columnColor}-light opacity-75`
  return `bg-col-${props.columnColor}-light`
})

const dragClasses = computed(() => ({
  'opacity-50': dragging.value,
  'ring-2 ring-primary-400': dragOver.value && !props.group.isFocused,
  'cursor-grab': props.canGroup && !dragging.value,
  'cursor-grabbing': dragging.value,
}))

const voteBtnClass = computed(() =>
  props.myVoteCount > 0
    ? 'bg-primary-600 text-white border-primary-600 hover:bg-primary-700 cursor-pointer'
    : props.remainingBudget <= 0
      ? 'border-border text-text-muted cursor-not-allowed'
      : 'border-border text-text-muted hover:border-primary-400 hover:text-primary-600 cursor-pointer'
)

function onDragStart(e: DragEvent) {
  if (!props.canGroup) return
  dragging.value = true
  const payload: DragPayload = { type: 'group', groupId: props.group.id, columnType: props.group.columnType, cardIds: props.group.cards.map((c) => c.id) }
  e.dataTransfer!.setData('application/json', JSON.stringify(payload))
  e.dataTransfer!.effectAllowed = 'move'
}

function onDragOver(e: DragEvent) {
  if (!props.canGroup) return
  try { if (!(e.dataTransfer?.types ?? []).includes('application/json')) return } catch { /* Firefox */ }
  e.preventDefault()
  e.dataTransfer!.dropEffect = 'move'
  dragOver.value = true
}

function onDragLeave(e: DragEvent) {
  // Ignore leaves into child elements (text, grip icon) — they made the highlight flicker.
  if ((e.currentTarget as Node).contains(e.relatedTarget as Node | null)) return
  dragOver.value = false
}

function onDrop(e: DragEvent) {
  e.preventDefault()
  dragOver.value = false
  if (!props.canGroup) return
  const raw = e.dataTransfer?.getData('application/json')
  if (!raw) return
  let payload: DragPayload
  try { payload = JSON.parse(raw) } catch { return }
  if (payload.columnType !== props.group.columnType) return
  if (payload.groupId === props.group.id) return
  if (payload.type === 'card') { emit('remove-card', payload.sourceGroupId!, payload.cardId!); return }
  emit('merge', { id: payload.groupId, columnType: payload.columnType, cards: payload.cardIds.map((id) => ({ id })) } as unknown as CardGroup, props.group)
}

function onCardDragStart(e: DragEvent, card: CardGroupCard) {
  if (!props.canUngroup) return
  const payload: DragPayload = { type: 'card', groupId: props.group.id, columnType: props.group.columnType, cardIds: [card.id], cardId: card.id, sourceGroupId: props.group.id }
  e.dataTransfer!.setData('application/json', JSON.stringify(payload))
  e.dataTransfer!.effectAllowed = 'move'
}

function saveTitle() {
  emit('update-title', props.group, titleInput.value.trim() || null)
  editingTitle.value = false
}
</script>
