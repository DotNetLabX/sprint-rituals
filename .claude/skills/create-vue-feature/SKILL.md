---
name: create-vue-feature
description: Creates a complete frontend feature slice — types, API module, store, components, view, route. Use when adding a new frontend feature.
---

# Create Vue Feature

Creates a complete frontend vertical slice. Each step produces one file or modifies one file.

## Steps

1. **Define types.** Add TypeScript interfaces for the feature's API responses and domain objects to `client/src/types/index.ts`. Follow existing patterns in the file.

2. **Create API module.** Create `client/src/api/{resource}.ts` using the `apiFetch` wrapper. Follow the pattern in existing API modules (e.g., `client/src/api/settings.ts`).

   ```typescript
   import { apiFetch } from './client'
   import type { MyResource } from '@/types'

   export const myResourceApi = {
     getAll: () => apiFetch<MyResource[]>('/api/my-resource'),
     getById: (id: number) => apiFetch<MyResource>(`/api/my-resource/${id}`),
   }
   ```

3. **Create Pinia store.** Create `client/src/stores/{name}Store.ts` following `pinia-patterns` skill. Include: state refs, loading/error flags, computed getters, async actions.

4. **Create components.** Create feature components in `client/src/components/`. Follow `vue-component-architecture` skill for level assignment:
   - Feature container → L1 (< 200 lines)
   - Data display sections → L3 (props-only)
   - Reusable UI pieces → L4 (< 50 lines)

5. **Create view.** Create `client/src/views/{Name}View.vue` as an L0 page component (< 100 lines). The view:
   - Imports and initializes the store
   - Calls `fetchData()` on mount
   - Passes data to feature components via props
   - Handles local form state (if applicable) using the sync-from-store pattern

6. **Wire route.** Add route entry in `client/src/router.ts` with lazy loading:
   ```typescript
   {
     path: '/my-feature',
     name: 'MyFeature',
     component: () => import('@/views/MyFeatureView.vue'),
   }
   ```

7. **Add navigation.** Add `RouterLink` or nav entry in the app shell (`AppSidebar.vue` or equivalent).

8. **Verify the build.** `npm run build` from `client/` (runs `vue-tsc -b` + Vite).

## Arguments

Pass the feature name as argument: `/create-vue-feature DeveloperThroughput`

The feature name determines: type names, API module file, store name, view name, route path.

## Page Shell Pattern

Every view wraps content in the standard PageLayout + PageToolbar composition:

```vue
<template>
  <PageLayout title="FeatureName">
    <template #toolbar>
      <PageToolbar
        :sprints="store.closedSprints"
        :selected-sprint-id="store.selectedSprintId"
        @update:selected-sprint-id="onSprintChange"
      />
    </template>

    <!-- Loading state -->
    <div v-if="store.initializing" class="flex items-center justify-center h-64">
      <div class="text-text-muted text-sm">Loading...</div>
    </div>

    <!-- Empty state -->
    <EmptyState v-else-if="!store.data" title="No data yet" description="..." />

    <!-- Content -->
    <template v-else>
      <!-- Feature content here -->
    </template>
  </PageLayout>
</template>
```

Views follow the three-state template: initializing → empty → content.

## Feature Folder Structure

```
client/src/
├── types/index.ts                          # Add interfaces here
├── api/{resource}.ts                       # API module
├── stores/{name}Store.ts                   # Pinia store
├── components/{FeatureName}*.vue           # Feature components
├── views/{FeatureName}View.vue             # Page view
└── router.ts                               # Route entry
```

## Checklist

- [ ] Types defined in `types/index.ts`
- [ ] API module uses `apiFetch` wrapper
- [ ] Store follows setup store pattern with loading/error
- [ ] Components assigned correct levels (L0-L4)
- [ ] View is < 100 lines, delegates to components
- [ ] Route uses lazy loading
- [ ] Navigation wired in app shell
- [ ] `npm run build` passes
