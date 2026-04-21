# Browser Compatibility and Responsive Design

## Supported Browsers

The application targets the latest two major versions of each browser:

| Browser | Versions | Platform |
|---------|----------|----------|
| Google Chrome | Latest 2 | Windows, macOS, Linux, Android |
| Mozilla Firefox | Latest 2 | Windows, macOS, Linux |
| Microsoft Edge | Latest 2 | Windows, macOS |
| Safari | Latest 2 | macOS, iOS |

### Blazor WebAssembly Requirements

Blazor WebAssembly requires browsers with WebAssembly support. All browsers listed above support WebAssembly in their specified versions. Internet Explorer is not supported.

### Known Limitations

- **Safari on iOS:** Blazor Server connections may drop on iOS when the app is backgrounded for extended periods. The app reconnects automatically when brought back to the foreground.
- **Firefox Private Browsing:** Some local storage features may behave differently in private browsing mode.

## Responsive Design Breakpoints

The application uses MudBlazor's responsive breakpoint system, which maps to the following screen widths:

| Breakpoint | Min Width | Typical Devices |
|------------|-----------|-----------------|
| `xs` | 0px | Small phones (portrait) |
| `sm` | 600px | Large phones, small tablets |
| `md` | 960px | Tablets (portrait), small laptops |
| `lg` | 1280px | Laptops, desktops |
| `xl` | 1920px | Large desktops, wide monitors |

### Layout Behavior by Breakpoint

| Breakpoint | Sidebar | Data Tables | Forms | Dashboard Cards |
|------------|---------|-------------|-------|-----------------|
| `xs` – `sm` | Collapsed (hamburger menu) | Horizontal scroll | Single column, full width | Stacked vertically |
| `md` | Collapsible drawer | Full width with scroll | Two-column where appropriate | 2-column grid |
| `lg` – `xl` | Persistent sidebar | Full display | Multi-column | 3–4 column grid |

## Manual Testing Checklist

Use this checklist when verifying responsive behavior across viewports.

### Mobile (320px – 599px)

- [ ] Sidebar collapses to a hamburger menu
- [ ] Navigation drawer opens/closes on tap
- [ ] Data tables scroll horizontally without breaking layout
- [ ] Form fields stack vertically and fill available width
- [ ] Dialog modals are full-width or near full-width
- [ ] Buttons and touch targets are at least 44×44px
- [ ] Text is readable without horizontal scrolling
- [ ] Dashboard stat cards stack in a single column

### Tablet (600px – 959px)

- [ ] Sidebar is collapsible via toggle
- [ ] Data tables display key columns; secondary columns may be hidden
- [ ] Forms use a reasonable column layout (1–2 columns)
- [ ] Dialogs are appropriately sized (not full-screen, not too narrow)
- [ ] Dashboard cards display in a 2-column grid

### Desktop (960px+)

- [ ] Sidebar is persistent and visible
- [ ] Data tables display all columns without horizontal scroll
- [ ] Forms use multi-column layout where appropriate
- [ ] Dialogs are centered with reasonable max-width
- [ ] Dashboard cards display in 3–4 column grid
- [ ] No content is clipped or overflowing

### Cross-Browser Checks (All Viewports)

- [ ] Page loads without JavaScript errors in the browser console
- [ ] Authentication flow (login/logout) works correctly
- [ ] Data grids sort and filter correctly
- [ ] Form validation messages display properly
- [ ] MudBlazor components render consistently (buttons, chips, dialogs, snackbars)
- [ ] Date/time pickers function correctly
- [ ] Navigation between pages works without full page reloads (Blazor routing)
- [ ] Skip navigation link is functional (Tab → Enter jumps to main content)
- [ ] ARIA landmarks are present (`role="navigation"`, `role="main"`)

### Testing Tools

| Tool | Purpose |
|------|---------|
| Chrome DevTools Device Mode | Simulate mobile/tablet viewports |
| Firefox Responsive Design Mode | Cross-check responsive behavior |
| Safari Web Inspector | Test Safari-specific rendering |
| BrowserStack / LambdaTest | Real device testing across browser/OS combinations |
| Lighthouse (Chrome) | Automated accessibility and performance audits |
