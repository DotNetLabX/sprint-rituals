import { createRouter, createWebHistory } from 'vue-router'
import PlanningRoomView from '../views/PlanningRoomView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      // /poker/:id  — the planning room; route parameter matches SessionId (Guid)
      path: '/:id',
      name: 'planning-room',
      component: PlanningRoomView,
      meta: { title: 'Planning Poker' },
    },
  ],
})

router.afterEach((to) => {
  document.title = (to.meta.title as string | undefined) ?? 'Planning Poker'
})

export default router
