# Font Style Guide - DeskIQ Client

## Overview
This document defines the standardized font system used throughout the DeskIQ application. All font usage should follow these guidelines to ensure consistency and maintainability.

**Note**: The application has been migrated from the SAP/Fundamental-NGX ecosystem to pure Tailwind CSS. All SAP theming dependencies have been removed and replaced with corporate CSS variables.

## Font Family

### Primary Font
- **Font**: Inter (Google Fonts)
- **Definition**: `font-family: 'Inter', sans-serif;`
- **Location**: `src/styles.css` (line 38)
- **CDN**: Loaded from Google Fonts in `src/index.html`

### Font Weights (Tailwind Classes)

| Class | Weight | CSS Value | Usage Examples |
|-------|--------|-----------|----------------|
| `font-medium` | 500 | 500 | Form labels, secondary text |
| `font-semibold` | 600 | 600 | Navigation items, user names, button text |
| `font-bold` | 700 | 700 | Section headings, important data points, table headers |
| `font-extrabold` | 800 | 800 | Logo/brand text ("DeskIQ Service Hub") |
| `font-black` | 900 | 900 | Main page titles (H1) |

### Font Sizes (Tailwind Classes)

| Class | Size (px) | Usage Examples |
|-------|-----------|----------------|
| `text-xs` | 12px | Labels, metadata, tags, timestamps, ticket IDs, small UI elements |
| `text-sm` | 14px | Body text, descriptions, form inputs, table data |
| `text-base` | 16px | Standard paragraph text (not currently used) |
| `text-lg` | 18px | Large subtitles, logo text |
| `text-xl` | 20px | Section headings (H2) |
| `text-2xl` | 24px | Dashboard metrics, large numbers |
| `text-3xl` | 30px | Page titles (H1) |
| `text-4xl` | 36px | Large page titles (e.g., individual ticket detail) |

## Usage Guidelines

### DO:
- ✅ Use standard Tailwind font classes (`text-xs`, `text-sm`, etc.)
- ✅ Use standard Tailwind weight classes (`font-bold`, `font-semibold`, etc.)
- ✅ Use `text-xs` for labels, metadata, and small UI elements
- ✅ Use `text-sm` for body text and descriptions
- ✅ Use `font-black` for main page titles
- ✅ Use `font-semibold` for navigation and interactive elements

### DON'T:
- ❌ Use arbitrary font sizes (e.g., `text-[10px]`, `text-[11px]`, `text-[12px]`)
- ❌ Use arbitrary font weights
- ❌ Hardcode font-family values in inline styles
- ❌ Mix multiple font families

## Common Patterns

### Page Titles
```html
<h1 class="text-3xl font-black tracking-tight text-[var(--color-text)]">Page Title</h1>
```

### Section Headings
```html
<h2 class="text-xl font-bold text-[var(--color-text)]">Section Title</h2>
```

### Labels
```html
<p class="text-xs font-bold uppercase tracking-[0.16em] text-[var(--color-muted)]">Label Text</p>
```

### Body Text
```html
<p class="text-sm text-[var(--color-muted)]">Description text goes here</p>
```

### Metadata/Small Text
```html
<p class="text-xs text-[var(--color-muted)]">Metadata or timestamp</p>
```

### Ticket IDs
```html
<td class="text-xs text-[var(--color-muted)]">TICKET-001</td>
```

### Navigation Items
```html
<span class="text-xs font-semibold uppercase tracking-[0.08em]">Menu Item</span>
```

## Implementation Details

### Font Loading
The Inter font is loaded from Google Fonts in `src/index.html`:
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800;900&display=swap" rel="stylesheet">
```

### Base Styles
Defined in `src/styles.css`:
```css
html,
body {
	margin: 0;
	min-height: 100%;
	background: var(--color-bg);
	color: var(--color-text);
	font-family: 'Inter', sans-serif;
}

/* Force Inter font on all elements */
*,
*::before,
*::after {
	font-family: 'Inter', sans-serif !important;
}
```

## Maintenance

When adding new UI components:
1. Reference this guide for appropriate font classes
2. Use standard Tailwind classes only
3. Test font rendering across different browsers
4. Ensure accessibility (minimum 12px for readability)

## Troubleshooting

### Multiple Fonts Appearing in the Application

**Problem**: The application previously used the SAP/Fundamental-NGX ecosystem which had external library font definitions.

**Solution**: The SAP ecosystem has been completely removed. The application now uses:
- Pure Tailwind CSS for styling
- Corporate CSS variables defined in `src/styles.css`
- Inter font loaded from Google Fonts
- Universal selectors with `!important` to enforce Inter font

**Note**: If you see inconsistent fonts, check if:
1. The Google Fonts CDN is loading correctly in `index.html`
2. The universal font-family rule is present in `src/styles.css`
3. Any new CSS is not overriding the universal selector
4. No SAP/Fundamental-NGX dependencies remain in `package.json`

## Changes Log

### April 10, 2026
- Added Google Fonts CDN for Inter font
- Standardized all arbitrary font sizes to Tailwind classes
- Replaced `text-[10px]`, `text-[11px]`, `text-[12px]` with `text-xs`
- Removed monospace font usage from the application (no code display)
- **Removed entire SAP/Fundamental-NGX ecosystem**:
  - Removed SAP/Fundamental imports from `styles.css`
  - Removed SAP theming imports from `angular.json`
  - Removed SAP dependencies from `package.json` (@fundamental-ngx/*, @sap-theming/theming-base-content, fundamental-styles)
  - Replaced all fd-button components with Tailwind-styled buttons
  - Replaced fd-form-* components with standard HTML/Tailwind forms
  - Defined corporate CSS variables to replace SAP theming variables
- Application now uses pure Tailwind CSS with corporate styling
- Created this style guide document
