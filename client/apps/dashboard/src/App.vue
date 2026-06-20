<script setup lang="ts">
import { useRoute } from 'vue-router'
import AppShell from './components/AppShell.vue'

const route = useRoute()
</script>

<template>
  <template v-if="route.meta.layout === 'blank'">
    <router-view />
  </template>
  <template v-else>
    <AppShell>
      <router-view />
    </AppShell>
  </template>
</template>
