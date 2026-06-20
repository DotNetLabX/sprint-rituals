import { createRouter, createWebHistory } from 'vue-router'
import WelcomeView from '../views/WelcomeView.vue'
import TeamDashboardView from '../views/TeamDashboardView.vue'
import SprintListView from '../views/SprintListView.vue'
import SprintView from '../views/SprintView.vue'
import RetroRoomView from '../views/RetroRoomView.vue'
import ReportView from '../views/ReportView.vue'
import { useAuthStore } from '../stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string;
    requiresAuth?: boolean;
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'welcome',
      component: WelcomeView,
      meta: { title: 'Reflekt' },
    },
    {
      path: '/teams',
      name: 'teams',
      component: TeamDashboardView,
      meta: { title: 'Teams — Reflekt', requiresAuth: true },
    },
    {
      path: '/teams/:teamId/sprints',
      name: 'team-sprints',
      component: SprintListView,
      meta: { title: 'Sprints — Reflekt', requiresAuth: true },
    },
    {
      path: '/teams/:teamId/sprints/:sprintId',
      name: 'sprint',
      component: SprintView,
      meta: { title: 'Sprint — Reflekt' },
    },
    {
      path: '/sprint/:sprintId',
      name: 'retro-room',
      component: RetroRoomView,
      meta: { title: 'Retro Room — Reflekt' },
    },
    {
      path: '/teams/:teamId/sprints/:sprintId/report',
      name: 'report',
      component: ReportView,
      meta: { title: 'Report — Reflekt' },
    },
  ],
})

router.afterEach((to) => {
  document.title = to.meta.title || 'Reflekt';
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  // On cold load authStore.loaded is false — wait for auth state to resolve
  // before making any redirect decision. This prevents a brief unauthorized
  // render of protected routes before the initial /api/auth/me call completes.
  if (!authStore.loaded) {
    await authStore.fetchMe()
  }

  if (to.name === 'welcome' && authStore.isSignedIn) {
    return { name: 'teams' }
  }
  if (to.meta.requiresAuth && !authStore.isSignedIn) {
    return { name: 'welcome' }
  }
})

export default router
