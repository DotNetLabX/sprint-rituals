# Recommended Packages

Approved ecosystem for this project. All are tree-shakeable and actively maintained.

## Core Stack (already installed)

| Package | Purpose |
|---------|---------|
| `vue` 3.5+ | SPA framework |
| `pinia` 3 | State management |
| `vue-router` | Client-side routing |
| `vue3-apexcharts` | Charts and sparklines |
| `tailwindcss` 4 | Utility-first CSS |
| `vite` 8 | Build tool |
| `typescript` 6 | Type safety |

## Recommended Additions

| Package | Purpose | Install |
|---------|---------|---------|
| `@vueuse/core` | 200+ composables (storage, media, fetch, sensors) | `npm i @vueuse/core` |
| `@tanstack/vue-query` | Server-state cache, background refetch, stale-while-revalidate | `npm i @tanstack/vue-query` |
| `@tanstack/vue-table` | Headless table logic (sort, filter, pagination) | `npm i @tanstack/vue-table` |
| `vee-validate` + `zod` | Schema-based form validation, composable API | `npm i vee-validate @vee-validate/zod zod` |
| `date-fns` | Tree-shakeable date functions, built-in TS types | `npm i date-fns` |
| `shadcn-vue` + `reka-ui` | Accessible component primitives, Tailwind v4 native | `npx shadcn-vue@latest init` |
| `@heroicons/vue` | 300+ icons by Tailwind team | `npm i @heroicons/vue` |

## Install Priority

1. `@vueuse/core` + `date-fns` — pure utility, zero UI risk
2. `@tanstack/vue-query` — simplifies async data in stores
3. `@tanstack/vue-table` — headless, pairs with Tailwind
4. `vee-validate` + `zod` — when form complexity grows
5. `shadcn-vue` + `reka-ui` — modifies CSS entry point, do last

## Notes

- **shadcn-vue v1** uses Reka UI (not Radix Vue). Tailwind v4 supported natively.
- **VueUse v14** requires Vue 3.5+ (project qualifies at 3.5.32).
- **TanStack Query v5** complements Pinia (server state vs client state).
- **Keep `vue3-apexcharts`** — only add `vue-echarts` if 100k+ data points needed.
- **date-fns v4** coexists with native Temporal API (Chrome 144+).

## Do NOT Add

- Full UI kits (Vuetify, PrimeVue, Element Plus) — project constraint
- Moment.js or Luxon — use date-fns instead
- Axios — use the project's `apiFetch` wrapper
- A second chart library unless ApexCharts cannot cover the use case
