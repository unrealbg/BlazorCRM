# UI Design System

This document describes the base UI tokens and components used across the CRM UI. The goal is consistent spacing, typography, and interaction states while staying compatible with Tailwind and MudBlazor.

## Design tokens

Tokens live in [src/Presentation/Crm.Web/wwwroot/css/app.css](../src/Presentation/Crm.Web/wwwroot/css/app.css).

- Spacing: `--space-1` to `--space-6`
- Radius: `--radius-sm`, `--radius-md`, `--radius-lg`
- Shadows: `--shadow-sm`, `--shadow-md`, `--shadow-lg`
- Colors: `--color-surface`, `--color-text`, `--color-brand`, `--color-accent`, `--color-success`, `--color-warning`, `--color-danger`

## Base components

Components live in [src/Presentation/Crm.UI/Components/Base](../src/Presentation/Crm.UI/Components/Base).

- `UiCard`: Surface container.
- `UiButton`: Primary/muted button styling.
- `UiInput`: Text input with focus ring.
- `UiSelect`: Select input styling.
- `UiBadge`: Small status badge.
- `UiTable`: Table wrapper with standardized headers.
- `UiEmptyState`: Empty state block with title/description/actions.
- `UiSkeleton`: Loading placeholder.
- `UiToast`: Inline toast card with tone.
- `UiModal`: Base modal with header/body/footer.
- `UiDrawer`: Base drawer with header/body/footer.

## Usage patterns

- Prefer `UiCard` for page sections and collections.
- Use `UiButton` with `Variant="primary"` or `Variant="muted"`.
- Use `UiEmptyState` when data lists are empty.
- Use `UiSkeleton` during async loading.

## Notes

- Tailwind preflight is disabled to avoid conflicts with MudBlazor.
- Base components rely on classes defined in app.css and Tailwind utilities.
