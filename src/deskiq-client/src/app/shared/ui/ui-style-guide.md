# UI Style Guide (DeskIQ Client)

This guide centralizes page-level styles for consistency across all views.

## Source of truth

- Global tokens and UI classes: `src/styles.css`
- Reusable class namespace: `ui-*`

## Core principles

1. Always use SAP tokens via CSS variables, never hardcoded colors.
2. Use shared structural classes (`ui-page`, `ui-card`, `ui-table`, `ui-input`) instead of repeating utility bundles.
3. Keep feature templates focused on layout/behavior; styling comes from shared UI classes.
4. Prefer Fundamental components for interactive controls (`fd-button`, form directives) and combine with shared layout classes.

## Recommended structure for new pages

```html
<section class="ui-page">
  <div class="ui-page__header">
    <div>
      <h1 class="ui-page__title">Page title</h1>
      <p class="ui-page__subtitle">Short contextual description.</p>
    </div>
    <div class="ui-actions">
      <button fd-button [fdType]="'emphasized'">Primary action</button>
    </div>
  </div>

  <article class="ui-card ui-card--padded">
    ...
  </article>
</section>
```

## Shared classes

- Layout: `ui-page`, `ui-page__header`, `ui-actions`
- Headings: `ui-page__title`, `ui-page__subtitle`
- Cards: `ui-card`, `ui-card--padded`, `ui-card--soft`, `ui-card__header`, `ui-card__title`
- KPI blocks: `ui-kpi-grid`, `ui-kpi-card`, `ui-kpi-label`, `ui-kpi-value`
- Tables: `ui-table-wrap`, `ui-table`
- Forms: `ui-form-label`, `ui-input`
- Feedback: `ui-feedback`, `ui-feedback--error`, `ui-feedback--success`, `ui-empty-state`
- Badges: `ui-badge`

## Migration note

When touching older views, migrate repeated Tailwind class bundles into shared `ui-*` classes first, then tune specifics.
