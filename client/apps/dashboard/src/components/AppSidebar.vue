<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/authStore'
import { useSettingsStore } from '../stores/settingsStore'

const authStore = useAuthStore()
const settingsStore = useSettingsStore()

const route = useRoute()
const collapsed = ref(false)

function checkWidth() {
  collapsed.value = window.innerWidth < 1024
}

onMounted(() => {
  checkWidth()
  window.addEventListener('resize', checkWidth)
  settingsStore.fetchSettings()
})

onUnmounted(() => {
  window.removeEventListener('resize', checkWidth)
})

const baseNavItems = [
  {
    path: '/',
    label: 'Dashboard',
    exact: true,
    icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
      <path d="M2 3a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H3a1 1 0 01-1-1V3zm0 8a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H3a1 1 0 01-1-1v-6zm8-8a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1h-6a1 1 0 01-1-1V3zm0 8a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1h-6a1 1 0 01-1-1v-6z" />
    </svg>`
  },
  {
    path: '/developers',
    label: 'Developers',
    exact: false,
    icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
      <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z" />
    </svg>`
  },
  {
    path: '/team',
    label: 'Team',
    exact: false,
    icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
      <path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v1h8v-1zM6 8a2 2 0 11-4 0 2 2 0 014 0zM16 18v-1a5.972 5.972 0 00-.75-2.906A3.005 3.005 0 0119 15v1h-3zM4.75 12.094A5.973 5.973 0 004 15v1H1v-1a3 3 0 013.75-2.906z" />
    </svg>`
  },
  {
    path: '/sprints',
    label: 'Sprints',
    exact: false,
    icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
      <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
    </svg>`
  },
  {
    path: '/epics',
    label: 'Epics',
    exact: false,
    icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
      <path d="M7 3a1 1 0 000 2h6a1 1 0 100-2H7zM4 7a1 1 0 011-1h10a1 1 0 110 2H5a1 1 0 01-1-1zM2 11a2 2 0 012-2h12a2 2 0 012 2v4a2 2 0 01-2 2H4a2 2 0 01-2-2v-4z" />
    </svg>`
  }
]

const qaNavItem = {
  path: '/qa',
  label: 'QA',
  exact: false,
  icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
    <path fill-rule="evenodd" d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V7.414A2 2 0 0015.414 6L12 2.586A2 2 0 0010.586 2H6zm2 10a1 1 0 10-2 0v3a1 1 0 102 0v-3zm2-3a1 1 0 011 1v5a1 1 0 11-2 0v-5a1 1 0 011-1zm4-1a1 1 0 10-2 0v6a1 1 0 102 0V8z" clip-rule="evenodd" />
  </svg>`
}

const tailNavItems = [
  {
    path: '/cycle-time',
    label: 'Cycle Time',
    exact: false,
    icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
      <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd" />
    </svg>`
  },
  {
    path: '/settings',
    label: 'Settings',
    exact: false,
    icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
      <path fill-rule="evenodd" d="M11.49 3.17c-.38-1.56-2.6-1.56-2.98 0a1.532 1.532 0 01-2.286.948c-1.372-.836-2.942.734-2.106 2.106.54.886.061 2.042-.947 2.287-1.561.379-1.561 2.6 0 2.978a1.532 1.532 0 01.947 2.287c-.836 1.372.734 2.942 2.106 2.106a1.532 1.532 0 012.287.947c.379 1.561 2.6 1.561 2.978 0a1.533 1.533 0 012.287-.947c1.372.836 2.942-.734 2.106-2.106a1.533 1.533 0 01.947-2.287c1.561-.379 1.561-2.6 0-2.978a1.532 1.532 0 01-.947-2.287c.836-1.372-.734-2.942-2.106-2.106a1.532 1.532 0 01-2.287-.947zM10 13a3 3 0 100-6 3 3 0 000 6z" clip-rule="evenodd" />
    </svg>`
  }
]

const navItems = computed(() => {
  const items = [...baseNavItems]
  if (settingsStore.settings.xrayEnabled) {
    items.push(qaNavItem)
  }
  items.push(...tailNavItems)
  return items
})

function isActive(item: { path: string; exact: boolean }) {
  if (item.exact) {
    return route.path === item.path
  }
  return route.path.startsWith(item.path)
}
</script>

<template>
  <nav
    class="flex flex-col shrink-0 bg-sidebar-bg border-r border-border-default transition-all duration-200"
    :class="collapsed ? 'w-14' : 'w-56'"
  >
    <!-- Logo area -->
    <div
      class="flex items-center h-14 px-4 border-b border-border-default"
      :class="collapsed ? 'justify-center' : 'gap-2'"
    >
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5 text-accent-default shrink-0">
        <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
        <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
      </svg>
      <span v-if="!collapsed" class="text-sm font-bold text-text-primary tracking-wide">Fokus</span>
    </div>

    <!-- Nav items -->
    <ul class="flex flex-col gap-1 p-2 flex-1">
      <li v-for="item in navItems" :key="item.path">
        <RouterLink
          :to="item.path"
          class="flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors"
          :class="[
            collapsed ? 'justify-center' : '',
            isActive(item)
              ? 'bg-sidebar-item-active text-accent-default'
              : 'text-text-secondary hover:bg-sidebar-item-hover hover:text-text-primary'
          ]"
          :title="collapsed ? item.label : undefined"
        >
          <span class="shrink-0" v-html="item.icon" />
          <span v-if="!collapsed" class="truncate">{{ item.label }}</span>
        </RouterLink>
      </li>
    </ul>

    <!-- User profile + logout -->
    <div v-if="authStore.user" class="p-2 border-t border-border-default">
      <div
        class="flex items-center gap-2 px-3 py-2 rounded-md"
        :class="collapsed ? 'justify-center' : ''"
      >
        <img
          v-if="authStore.user.avatarUrl"
          :src="authStore.user.avatarUrl"
          :alt="authStore.user.displayName"
          class="w-7 h-7 rounded-full shrink-0"
          :title="collapsed ? authStore.user.displayName : undefined"
        />
        <div
          v-else
          class="w-7 h-7 rounded-full bg-accent-default flex items-center justify-center text-white text-xs font-bold shrink-0"
          :title="collapsed ? authStore.user.displayName : undefined"
        >
          {{ authStore.user.displayName.charAt(0).toUpperCase() }}
        </div>
        <div v-if="!collapsed" class="flex-1 min-w-0">
          <p class="text-xs font-medium text-text-primary truncate">{{ authStore.user.displayName }}</p>
          <p class="text-xs text-text-secondary truncate">{{ authStore.user.role }}</p>
        </div>
        <button
          v-if="!collapsed"
          @click="authStore.logout()"
          class="shrink-0 text-text-secondary hover:text-text-primary transition-colors"
          title="Sign out"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4">
            <path fill-rule="evenodd" d="M3 4.25A2.25 2.25 0 015.25 2h5.5A2.25 2.25 0 0113 4.25v2a.75.75 0 01-1.5 0v-2a.75.75 0 00-.75-.75h-5.5a.75.75 0 00-.75.75v11.5c0 .414.336.75.75.75h5.5a.75.75 0 00.75-.75v-2a.75.75 0 011.5 0v2A2.25 2.25 0 0110.75 18h-5.5A2.25 2.25 0 013 15.75V4.25z" clip-rule="evenodd" />
            <path fill-rule="evenodd" d="M19 10a.75.75 0 00-.75-.75H8.704l1.048-.943a.75.75 0 10-1.004-1.114l-2.5 2.25a.75.75 0 000 1.114l2.5 2.25a.75.75 0 101.004-1.114l-1.048-.943h9.546A.75.75 0 0019 10z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
    </div>
  </nav>
</template>
