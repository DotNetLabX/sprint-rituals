---
name: tailwind-theme
description: Tailwind CSS v4 theme patterns — design tokens, @theme directive, dark mode, responsive design, component styling. Loaded when working with styles or theme configuration.
user-invocable: true
---

# Tailwind Theme Patterns

Tailwind CSS v4 with CSS-first configuration. No `tailwind.config.js`.

## Tailwind v4 Setup

This project uses Tailwind v4 which is fundamentally different from v3:

```css
/* client/src/assets/main.css */
@import "tailwindcss";

/* Design tokens — two-layer pattern */
@theme {
  --color-surface-page: var(--surface-page);
  --color-surface-card: var(--surface-card);
  --color-surface-elevated: var(--surface-elevated);
  --color-text-primary: var(--text-primary);
  --color-text-secondary: var(--text-secondary);
  --color-border-default: var(--border-default);
  --color-accent: var(--accent);
}
```

**Two-layer pattern:**
1. `@theme` block declares aliases mapping Tailwind utility names to CSS custom properties
2. `:root` / `.dark` / `.light` blocks define the actual token values

```css
:root {
  --surface-page: #0a0a0f;
  --surface-card: #12121a;
  --text-primary: #e2e8f0;
}

.dark {
  --surface-page: #0a0a0f;
  --surface-card: #12121a;
  --text-primary: #e2e8f0;
}

.light {
  --surface-page: #ffffff;
  --surface-card: #f8fafc;
  --text-primary: #1e293b;
}
```

## Dark Mode

Theme switching via class on `<html>`:

```typescript
// composables/useTheme.ts
const theme = ref<'dark' | 'light'>(
  localStorage.getItem('theme') as 'dark' | 'light' ?? 'dark'
)

function toggleTheme() {
  theme.value = theme.value === 'dark' ? 'light' : 'dark'
  document.documentElement.className = theme.value
  localStorage.setItem('theme', theme.value)
}
```

Use token-based classes, not `dark:` prefix:
```vue
<!-- Use tokens (correct for this project) -->
<div class="bg-surface-page text-text-primary">

<!-- NOT dark: prefix (v3 pattern, not used here) -->
<div class="bg-white dark:bg-gray-900">
```

## Component Styling

Use computed class maps for variants:

```typescript
const sizeClasses = {
  sm: 'px-3 py-1.5 text-sm',
  md: 'px-4 py-2 text-base',
  lg: 'px-6 py-3 text-lg',
} as const

const variantClasses = {
  primary: 'bg-accent text-white hover:opacity-90',
  secondary: 'bg-surface-card text-text-primary hover:bg-surface-elevated',
  danger: 'bg-red-600 text-white hover:bg-red-700',
} as const

const buttonClasses = computed(() => [
  'inline-flex items-center justify-center font-medium transition-colors rounded-lg',
  sizeClasses[props.size],
  variantClasses[props.variant],
])
```

## Responsive Design

Mobile-first. Layer breakpoints:

```vue
<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
  <div v-for="item in items" :key="item.id"
       class="rounded-lg bg-surface-card p-4 shadow">
    {{ item.name }}
  </div>
</div>
```

## Form Elements

```vue
<select class="appearance-none bg-transparent text-text-primary border border-border-default
               rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-1 focus:ring-accent">
  <option v-for="opt in options" :key="opt.value" :value="opt.value">
    {{ opt.label }}
  </option>
</select>
```

**Note:** `<select>` elements need `appearance-none bg-transparent` to override native browser styling.

## White Flash Prevention

In `index.html`, use inline `style` with literal hex on `<body>` to prevent flash before CSS variables resolve:

```html
<body style="background-color: #0a0a0f;">
```

## Animations

Prefer Tailwind utilities:
```vue
<div class="transform hover:scale-105 transition-transform duration-200">
  Hover effect
</div>

<div class="animate-spin h-5 w-5 border-2 border-accent border-t-transparent rounded-full">
  <!-- Loading spinner -->
</div>
```

## Chart Libraries and Design Tokens

JS chart libraries (ApexCharts, Chart.js) accept hex color strings, not CSS utility classes. Read CSS custom properties at runtime:

```typescript
function getToken(name: string): string {
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim()
}

const chartOptions = computed(() => ({
  colors: [getToken('--accent-default')],
  xaxis: {
    labels: { style: { colors: getToken('--text-muted') } }
  },
  yaxis: {
    labels: { style: { colors: getToken('--text-muted') } },
    title: { style: { color: getToken('--text-muted') } }
  },
  grid: { borderColor: getToken('--border-default') },
  legend: { labels: { colors: getToken('--text-secondary') } },
  tooltip: { theme: isDark.value ? 'dark' : 'light' }
}))
```

**Rules:**
- Never hardcode hex values in chart configs — read from CSS custom properties.
- Re-compute chart options on theme change (wrap in `computed` or `watch` on `isDark`).
- Use the same token names as the Tailwind `@theme` block.

## What NOT to Do

- No `tailwind.config.js` — v4 uses CSS-first configuration
- No `@tailwind base/components/utilities` directives — use `@import "tailwindcss"`
- No hardcoded color values in components — use design tokens
- No `dark:` prefix for theme switching — use CSS custom properties
- No `@apply` in components — use utility classes directly
