<template>
  <div class="relative rounded-[--radius-md] shadow-card border border-border p-3 flex flex-col gap-1 transition-shadow hover:shadow-card-hover" :class="cardBgClass">
    <!-- Blurred -->
    <div v-if="blurred" class="relative select-none">
      <p class="text-sm text-text-primary whitespace-pre-wrap blur-md" aria-hidden="true">Someone's note</p>
      <div class="absolute inset-0" />
    </div>

    <!-- Display mode -->
    <div v-else-if="!editing">
      <p class="text-sm text-text-primary whitespace-pre-wrap">{{ card.text }}</p>
      <BaseBadge v-if="card.assigneeName" variant="info" class="mt-1">{{ card.assigneeName }}</BaseBadge>
    </div>

    <!-- Edit mode -->
    <div v-else class="flex flex-col gap-1">
      <textarea v-model="editText" rows="2" maxlength="2000" class="border border-border rounded-[--radius-sm] px-2 py-1 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500 resize-none" />
      <select v-if="card.cardType === 'ActionItem'" v-model="editAssignee" class="border border-border rounded-[--radius-sm] px-2 py-1 text-sm text-text-primary bg-surface focus:outline-none focus:ring-2 focus:ring-primary-500">
        <option value="">Assign to...</option>
        <option v-for="p in participants" :key="p.displayName" :value="p.displayName">{{ p.displayName }}</option>
      </select>
      <div class="flex gap-2">
        <BaseButton size="sm" variant="primary" :disabled="!editText.trim() || (card.cardType === 'ActionItem' && !editAssignee.trim())" @click="commitEdit">Save</BaseButton>
        <BaseButton size="sm" variant="secondary" @click="editing = false">Cancel</BaseButton>
      </div>
    </div>

    <!-- Evidence links -->
    <div v-if="!blurred && card.evidenceLinks?.length" class="flex flex-col gap-1 mt-1">
      <a v-for="link in card.evidenceLinks" :key="link.id" :href="link.url" target="_blank" rel="noopener noreferrer" class="flex items-center gap-1.5 group cursor-pointer">
        <template v-if="link.fetchStatus === 'Pending'">
          <span class="text-xs text-text-muted italic truncate max-w-[200px]">{{ link.url }}</span>
          <span class="text-xs text-text-muted animate-pulse">...</span>
        </template>
        <template v-else>
          <img v-if="faviconUrl(link)" :src="faviconUrl(link)" class="w-3 h-3 flex-shrink-0" :class="{ 'opacity-50': link.fetchStatus === 'Failed' }" @error="($event.target as HTMLImageElement).style.display='none'" />
          <span v-if="link.source === 'Jira' && link.externalKey" class="px-1.5 py-0.5 bg-blue-600 text-white text-[10px] rounded font-mono flex-shrink-0">{{ link.externalKey }}</span>
          <span class="text-xs text-text-secondary truncate max-w-[180px] group-hover:text-blue-600">{{ link.title || link.url }}</span>
        </template>
        <button v-if="canAttachLink ?? canEdit" class="ml-auto text-text-muted hover:text-red-500 flex-shrink-0 leading-none cursor-pointer" title="Remove link" @click.prevent="emit('detach-evidence-link', card.id, link.id)"><XIcon :size="14" /></button>
      </a>
    </div>

    <!-- Footer: add evidence link (left) + Edit/Delete (right) on one row -->
    <div v-if="!blurred && !editing && ((canAttachLink ?? canEdit) || canEdit)" class="flex items-center gap-2 mt-1">
      <template v-if="canAttachLink ?? canEdit">
        <button v-if="!addingLink" class="text-text-muted hover:text-blue-500 cursor-pointer" title="Add evidence link" @click="addingLink = true"><LinkIcon :size="14" /></button>
        <div v-else class="flex flex-1 gap-1 items-center">
          <input v-model="linkUrl" type="text" placeholder="Paste URL..." class="flex-1 border border-border rounded-[--radius-sm] px-2 py-0.5 text-xs text-text-primary bg-surface focus:outline-none focus:ring-1 focus:ring-primary-500" @keydown.enter="submitLink" @keydown.escape="cancelLink" />
          <BaseButton size="sm" variant="primary" :disabled="!linkUrl.trim()" @click="submitLink">Add</BaseButton>
          <button class="text-text-muted hover:text-text-secondary cursor-pointer" @click="cancelLink"><XIcon :size="14" /></button>
        </div>
      </template>
      <div v-if="canEdit && !addingLink" class="flex gap-2 ml-auto">
        <BaseButton size="sm" variant="ghost" @click="startEdit">Edit</BaseButton>
        <BaseButton size="sm" variant="ghost" @click="emit('delete', card)">Delete</BaseButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { Link as LinkIcon, X as XIcon } from 'lucide-vue-next'
import type { Card, EvidenceLink } from '../../stores/card'
import BaseButton from '../ui/BaseButton.vue'
import BaseBadge from '../ui/BaseBadge.vue'

const props = defineProps<{
  card: Card
  canEdit: boolean
  blurred?: boolean
  canAttachLink?: boolean
  columnColor?: string
  participants?: { displayName: string }[]
}>()

const emit = defineEmits<{
  edit: [card: Card, text: string, assigneeName: string | null]
  delete: [card: Card]
  'attach-evidence-link': [cardId: number, url: string]
  'detach-evidence-link': [cardId: number, evidenceLinkId: number]
}>()

const editing = ref(false)
const editText = ref('')
const editAssignee = ref('')
const addingLink = ref(false)
const linkUrl = ref('')

const cardBgClass = computed(() => {
  if (!props.columnColor) return 'bg-surface'
  // action-items uses a fixed semantic token; palette colors use col-{color}-light
  if (props.columnColor === 'action-items') return 'bg-action-items-light/30'
  return `bg-col-${props.columnColor}-light/30`
})

function startEdit() {
  editText.value = props.card.text
  editAssignee.value = props.card.assigneeName ?? ''
  editing.value = true
}

function commitEdit() {
  if (!editText.value.trim()) return
  const assigneeName = props.card.cardType === 'ActionItem' ? editAssignee.value.trim() || null : null
  emit('edit', props.card, editText.value.trim(), assigneeName)
  editing.value = false
}

function submitLink() {
  if (!linkUrl.value.trim()) return
  emit('attach-evidence-link', props.card.id, linkUrl.value.trim())
  linkUrl.value = ''
  addingLink.value = false
}

function cancelLink() {
  linkUrl.value = ''
  addingLink.value = false
}

function faviconUrl(link: EvidenceLink): string | undefined {
  if (link.iconUrl) { try { return new URL(link.iconUrl, link.url).href } catch { return link.iconUrl } }
  try { return `${new URL(link.url).origin}/favicon.ico` } catch { return undefined }
}
</script>
