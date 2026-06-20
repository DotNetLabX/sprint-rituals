<template>
  <div class="flex items-center gap-2 mt-1">
    <CheckCircle v-if="group.isDiscussed" :size="14" class="text-green-600" />
    <span v-if="group.isDiscussed" class="text-xs text-green-600 font-medium">Discussed</span>
    <button
      v-if="isScrumMaster && !group.isFocused"
      class="text-xs text-primary-600 hover:text-primary-800 cursor-pointer flex items-center gap-1"
      @click="$emit('focus')"
    >
      <MessageCircle :size="12" />
      Discuss
    </button>
    <button
      v-if="isScrumMaster"
      class="text-xs text-text-muted hover:text-text-secondary cursor-pointer"
      @click="$emit('toggle-discussed')"
    >
      {{ group.isDiscussed ? 'Unmark' : 'Mark discussed' }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { CheckCircle, MessageCircle } from 'lucide-vue-next'
import type { CardGroup } from '../../stores/cardGroup'

defineProps<{
  group: CardGroup
  isScrumMaster: boolean
}>()

defineEmits<{
  focus: []
  'toggle-discussed': []
}>()
</script>
