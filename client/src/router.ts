import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from './stores/authStore'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('./views/LoginView.vue'),
      meta: { layout: 'blank', public: true }
    },
    {
      path: '/invite/:token',
      name: 'invite',
      component: () => import('./views/InviteView.vue'),
      meta: { layout: 'blank', public: true }
    },
    {
      path: '/invite/error',
      name: 'invite-error',
      component: () => import('./views/InviteErrorView.vue'),
      meta: { layout: 'blank', public: true }
    },
    {
      path: '/',
      name: 'dashboard',
      component: () => import('./views/DashboardView.vue')
    },
    {
      path: '/developers',
      name: 'developers',
      component: () => import('./views/DevelopersView.vue')
    },
    {
      path: '/developers/:accountId',
      name: 'developer-detail',
      component: () => import('./views/DeveloperDetailView.vue')
    },
    {
      path: '/sprints',
      name: 'sprints',
      component: () => import('./views/SprintsView.vue')
    },
    {
      path: '/epics',
      name: 'epics',
      component: () => import('./views/EpicsView.vue')
    },
    {
      path: '/cycle-time',
      name: 'cycle-time',
      component: () => import('./views/CycleTimeView.vue')
    },
    {
      path: '/team',
      name: 'team',
      component: () => import('./views/TeamView.vue')
    },
    {
      path: '/qa',
      name: 'qa',
      component: () => import('./views/QaTrendsView.vue')
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('./views/SettingsView.vue')
    }
  ]
})

router.beforeEach(async (to) => {
  if (to.meta.public) return true

  const authStore = useAuthStore()
  if (!authStore.initialized) {
    await authStore.fetchMe()
  }

  if (!authStore.isAuthenticated) {
    return { name: 'login' }
  }

  return true
})

export default router
