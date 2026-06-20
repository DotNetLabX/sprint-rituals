<template>
  <div class="flex flex-col gap-5 flex-1 min-h-0">
    <!-- "I'm done" readiness toggle -->
    <div v-if="showReadinessButton" class="flex justify-end">
      <button
        :class="[
          'px-3 py-1.5 text-sm font-medium rounded-[--radius-sm] border transition-colors cursor-pointer',
          isReady
            ? 'bg-emerald-600 border-emerald-600 text-white hover:bg-emerald-700'
            : isDark
              ? 'bg-emerald-600/80 border-transparent text-white hover:bg-emerald-700'
              : 'bg-surface border-border text-text-secondary hover:bg-surface-raised',
        ]"
        @click="$emit('toggle-ready')"
      >
        {{ isReady ? "Done!" : "I'm done" }}
      </button>
    </div>

    <!-- Previous actions review panel (Writing phase only) -->
    <PreviousActionsPanel
      v-if="isWritingPhase && previousActionsStore.items.length > 0"
      :sprint-id="sprintId"
      :team-id="sprintStore.currentSummary?.teamId ?? 0"
      :is-scrum-master="isScrumMaster"
    />

    <RetroMobileTabBar
      v-model="activeTab"
      :show-action-items="showActionItems"
      :column1-name="column1Name"
      :column1-color="column1Color"
      :column2-name="column2Name"
      :column2-color="column2Color"
      :column3-name="column3Name"
      :column3-color="column3Color"
    />

    <div :class="boardGridClasses">
      <!-- Column 1 -->
      <RetroColumnBody
        :title="column1Name"
        :column-color="column1Color"
        badge-variant="success"
        :count="showGroups ? column1Groups.length : column1Cards.length"
        :visible="isColumnVisible('Column1')"
        :is-scrum-master="isScrumMaster"
        @drop="onColumnDrop($event, 'Column1')"
        @update-config="(name, color) => onUpdateColumnConfig(1, name, color)"
      >
        <template #empty>
          <div v-if="showGroups && column1Groups.length === 0" class="text-text-muted text-sm text-center py-4">No groups yet</div>
          <div v-else-if="!showGroups && column1Cards.length === 0" class="text-text-muted text-sm text-center py-4">No cards yet</div>
        </template>
        <template v-if="showGroups">
          <RetroGroupItem v-for="group in column1Groups" :key="'g' + group.id" v-bind="groupItemProps(group, column1Color)"
            @vote="castGroupVote" @retract-vote="retractGroupVote" @dissolve="dissolveGroup"
            @update-title="updateGroupTitle" @merge="mergeGroups" @remove-card="removeCardFromGroup"
            @focus="focusGroup" @toggle-discussed="toggleDiscussed" />
        </template>
        <template v-else>
          <RetroCardItem v-for="card in column1Cards" :key="card.id" v-bind="cardItemProps(card, column1Color)"
            @edit="saveEdit" @delete="requestDelete" @vote="castVote" @retract-vote="retractVote"
            @attach-evidence-link="attachEvidenceLink" @detach-evidence-link="detachEvidenceLink" />
        </template>
        <template #input>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateNote')" class="flex gap-2">
            <BaseInput v-model="newCardText.Column1" :maxlength="2000" placeholder="Add a note..." class="flex-1" @keydown.enter="onAddNote('Column1')" />
            <BaseButton size="sm" variant="primary" :disabled="!newCardText.Column1.trim()" @click="onAddNote('Column1')">Add</BaseButton>
          </div>
        </template>
      </RetroColumnBody>

      <!-- Column 2 -->
      <RetroColumnBody
        :title="column2Name"
        :column-color="column2Color"
        badge-variant="danger"
        :count="showGroups ? column2Groups.length : column2Cards.length"
        :visible="isColumnVisible('Column2')"
        :is-scrum-master="isScrumMaster"
        @drop="onColumnDrop($event, 'Column2')"
        @update-config="(name, color) => onUpdateColumnConfig(2, name, color)"
      >
        <template #empty>
          <div v-if="showGroups && column2Groups.length === 0" class="text-text-muted text-sm text-center py-4">No groups yet</div>
          <div v-else-if="!showGroups && column2Cards.length === 0" class="text-text-muted text-sm text-center py-4">No cards yet</div>
        </template>
        <template v-if="showGroups">
          <RetroGroupItem v-for="group in column2Groups" :key="'g' + group.id" v-bind="groupItemProps(group, column2Color)"
            @vote="castGroupVote" @retract-vote="retractGroupVote" @dissolve="dissolveGroup"
            @update-title="updateGroupTitle" @merge="mergeGroups" @remove-card="removeCardFromGroup"
            @focus="focusGroup" @toggle-discussed="toggleDiscussed" />
        </template>
        <template v-else>
          <RetroCardItem v-for="card in column2Cards" :key="card.id" v-bind="cardItemProps(card, column2Color)"
            @edit="saveEdit" @delete="requestDelete" @vote="castVote" @retract-vote="retractVote"
            @attach-evidence-link="attachEvidenceLink" @detach-evidence-link="detachEvidenceLink" />
        </template>
        <template #input>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateNote')" class="flex gap-2">
            <BaseInput v-model="newCardText.Column2" :maxlength="2000" placeholder="Add a note..." class="flex-1" @keydown.enter="onAddNote('Column2')" />
            <BaseButton size="sm" variant="primary" :disabled="!newCardText.Column2.trim()" @click="onAddNote('Column2')">Add</BaseButton>
          </div>
        </template>
      </RetroColumnBody>

      <!-- Column 3 -->
      <RetroColumnBody
        :title="column3Name"
        :column-color="column3Color"
        badge-variant="primary"
        :count="showGroups ? column3Groups.length : column3Cards.length"
        :visible="isColumnVisible('Column3')"
        :is-scrum-master="isScrumMaster"
        @drop="onColumnDrop($event, 'Column3')"
        @update-config="(name, color) => onUpdateColumnConfig(3, name, color)"
      >
        <template #empty>
          <div v-if="showGroups && column3Groups.length === 0" class="text-text-muted text-sm text-center py-4">No groups yet</div>
          <div v-else-if="!showGroups && column3Cards.length === 0" class="text-text-muted text-sm text-center py-4">No cards yet</div>
        </template>
        <template v-if="showGroups">
          <RetroGroupItem v-for="group in column3Groups" :key="'g' + group.id" v-bind="groupItemProps(group, column3Color)"
            @vote="castGroupVote" @retract-vote="retractGroupVote" @dissolve="dissolveGroup"
            @update-title="updateGroupTitle" @merge="mergeGroups" @remove-card="removeCardFromGroup"
            @focus="focusGroup" @toggle-discussed="toggleDiscussed" />
        </template>
        <template v-else>
          <RetroCardItem v-for="card in column3Cards" :key="card.id" v-bind="cardItemProps(card, column3Color)"
            @edit="saveEdit" @delete="requestDelete" @vote="castVote" @retract-vote="retractVote"
            @attach-evidence-link="attachEvidenceLink" @detach-evidence-link="detachEvidenceLink" />
        </template>
        <template #input>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateNote')" class="flex gap-2">
            <BaseInput v-model="newCardText.Column3" :maxlength="2000" placeholder="Add a note..." class="flex-1" @keydown.enter="onAddNote('Column3')" />
            <BaseButton size="sm" variant="primary" :disabled="!newCardText.Column3.trim()" @click="onAddNote('Column3')">Add</BaseButton>
          </div>
        </template>
      </RetroColumnBody>

      <!-- Action Items column -->
      <RetroColumnBody
        v-if="showActionItems"
        title="Action Items"
        column-color="blue"
        badge-variant="info"
        :count="actionItems.length"
        :visible="isColumnVisible('ActionItems')"
        :is-scrum-master="false"
        @drop="() => {}"
      >
        <template #empty>
          <div v-if="actionItems.length === 0" class="text-text-muted text-sm text-center py-4">No action items yet</div>
        </template>
        <RetroCardItem v-for="card in actionItems" :key="card.id"
          :card="card" :can-edit="canMutateCard(card)"
          :blurred="isWritingPhase && card.participantId !== currentParticipantId"
          :can-attach-link="policyStore.isAllowed(currentPhase, 'AttachEvidenceLink')"
          :participants="participants" column-color="blue"
          @edit="saveEdit" @delete="requestDelete"
          @attach-evidence-link="attachEvidenceLink" @detach-evidence-link="detachEvidenceLink" />
        <template #input>
          <div v-if="policyStore.isAllowed(currentPhase, 'CreateActionItem')" class="flex flex-col gap-2">
            <BaseInput v-model="newActionText" :maxlength="2000" placeholder="Action item..." @keydown.enter="onAddActionItem" />
            <select v-model="newActionAssignee" class="border border-border rounded-[--radius-md] px-3 py-2 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500">
              <option value="">Assign to...</option>
              <option v-for="p in participants" :key="p.id" :value="p.displayName">{{ p.displayName }}</option>
            </select>
            <BaseButton size="sm" variant="primary" :disabled="!newActionText.trim() || !newActionAssignee" @click="onAddActionItem">Add</BaseButton>
          </div>
        </template>
      </RetroColumnBody>
    </div>

    <!-- Delete confirmation modal -->
    <BaseModal :open="!!deleteTarget" title="Delete Card" @close="deleteTarget = null">
      <p class="text-sm text-text-secondary">Are you sure you want to delete this card? This action cannot be undone.</p>
      <template #footer>
        <div class="flex justify-end gap-3">
          <BaseButton variant="secondary" @click="deleteTarget = null">Cancel</BaseButton>
          <BaseButton variant="danger" @click="executeDelete">Delete</BaseButton>
        </div>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { type Card } from '../../stores/card'
import { useParticipantStore } from '../../stores/participant'
import { usePolicyStore } from '../../stores/policy'
import { useSprintStore } from '../../stores/sprint'
import { useToast } from '../../composables/useToast'
import { useTheme } from '../../composables/useTheme'
import { useRetroBoardActions } from '../../composables/useRetroBoardActions'
import RetroCardItem from './RetroCardItem.vue'
import RetroGroupItem from './RetroGroupItem.vue'
import RetroColumnBody from './RetroColumnBody.vue'
import RetroMobileTabBar from './RetroMobileTabBar.vue'
import PreviousActionsPanel from './PreviousActionsPanel.vue'
import BaseInput from '../ui/BaseInput.vue'
import BaseButton from '../ui/BaseButton.vue'
import BaseModal from '../ui/BaseModal.vue'
import { usePreviousActionsStore } from '../../stores/previousActions'

const props = defineProps<{
  sprintId: number
  isReady?: boolean
  showReadinessButton?: boolean
}>()

defineEmits<{
  'toggle-ready': []
}>()

const participantStore = useParticipantStore()
const policyStore = usePolicyStore()
const sprintStore = useSprintStore()
const previousActionsStore = usePreviousActionsStore()
const { addToast } = useToast()
const { isDark } = useTheme()

const newCardText = ref({ Column1: '', Column2: '', Column3: '' })
const newActionText = ref('')
const newActionAssignee = ref('')
const deleteTarget = ref<Card | null>(null)
const activeTab = ref<'Column1' | 'Column2' | 'Column3' | 'ActionItems'>('Column1')
const isMobile = ref(typeof window !== 'undefined' ? window.innerWidth < 640 : false)

function onResize() { isMobile.value = window.innerWidth < 640 }
onMounted(() => window.addEventListener('resize', onResize))
onUnmounted(() => window.removeEventListener('resize', onResize))

const {
  participants, column1Cards, column2Cards, column3Cards, actionItems,
  currentPhase, isWritingPhase, showGroups,
  column1Groups, column2Groups, column3Groups, showActionItems, boardGridClasses,
  isScrumMaster, cardItemProps, groupItemProps, canMutateCard,
  addNote, addActionItem, saveEdit, deleteCard, castVote, retractVote, castGroupVote, retractGroupVote,
  attachEvidenceLink, detachEvidenceLink,
  focusGroup, toggleDiscussed, dissolveGroup, updateGroupTitle, mergeGroups, removeCardFromGroup,
  updateColumnConfig,
} = useRetroBoardActions(props.sprintId, (msg) => addToast(msg, 'error'))

const currentParticipantId = computed(() => participantStore.currentParticipant?.id ?? 0)

// Column config from sprint summary (reactive)
const column1Name = computed(() => sprintStore.currentSummary?.column1Name ?? 'Went Well')
const column1Color = computed(() => sprintStore.currentSummary?.column1Color ?? 'green')
const column2Name = computed(() => sprintStore.currentSummary?.column2Name ?? "Didn't Go Well")
const column2Color = computed(() => sprintStore.currentSummary?.column2Color ?? 'red')
const column3Name = computed(() => sprintStore.currentSummary?.column3Name ?? 'Start Doing')
const column3Color = computed(() => sprintStore.currentSummary?.column3Color ?? 'amber')

watch(showActionItems, (visible) => { if (!visible && activeTab.value === 'ActionItems') activeTab.value = 'Column1' })

function isColumnVisible(column: string): boolean { return !isMobile.value || activeTab.value === column }

async function onAddNote(column: 'Column1' | 'Column2' | 'Column3') {
  const text = newCardText.value[column].trim()
  if (!text) return
  newCardText.value[column] = ''
  await addNote(column, text)
}

async function onAddActionItem() {
  const text = newActionText.value.trim()
  const assigneeName = newActionAssignee.value
  if (!text || !assigneeName) return
  newActionText.value = ''; newActionAssignee.value = ''
  await addActionItem(text, assigneeName)
}

function requestDelete(card: Card) { deleteTarget.value = card }

async function executeDelete() {
  const card = deleteTarget.value
  if (!card) return
  deleteTarget.value = null
  await deleteCard(card)
}

async function onUpdateColumnConfig(colIndex: 1 | 2 | 3, name: string, color: string) {
  const c1Name = colIndex === 1 ? name : column1Name.value
  const c1Color = colIndex === 1 ? color : column1Color.value
  const c2Name = colIndex === 2 ? name : column2Name.value
  const c2Color = colIndex === 2 ? color : column2Color.value
  const c3Name = colIndex === 3 ? name : column3Name.value
  const c3Color = colIndex === 3 ? color : column3Color.value
  await updateColumnConfig(c1Name, c1Color, c2Name, c2Color, c3Name, c3Color)
}

function onColumnDrop(e: DragEvent, column: string) {
  e.preventDefault()
  const raw = e.dataTransfer?.getData('application/json')
  if (!raw) return
  let payload: { type: string; columnType: string; sourceGroupId?: number; cardId?: number }
  try { payload = JSON.parse(raw) } catch { return }
  if (payload.type !== 'card' || payload.columnType !== column) return
  if (!payload.sourceGroupId || !payload.cardId) return
  removeCardFromGroup(payload.sourceGroupId, payload.cardId)
}
</script>
