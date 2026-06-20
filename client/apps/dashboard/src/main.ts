import { createApp } from 'vue'
import { createPinia } from 'pinia'
import VueApexCharts from 'vue3-apexcharts'
import App from './App.vue'
import router from './router'
import './assets/main.css'
import { useTheme } from './composables/useTheme'

// Apply theme before first paint to avoid flash of wrong theme
const { initTheme } = useTheme()
initTheme()

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(VueApexCharts)
await router.isReady()
app.mount('#app')
