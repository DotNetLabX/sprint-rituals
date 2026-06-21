<template>
  <AppHeader />
  <RouterView />
  <ToastContainer />
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import AppHeader from './components/AppHeader.vue'
import ToastContainer from './components/ui/ToastContainer.vue'
import { useAuthStore } from './stores/auth'

const authStore = useAuthStore()

onMounted(() => {
  authStore.fetchMe()
})
</script>
