# Patient List Spacing Fix — Bugfix Design

## Overview

The patient list items on the Patients page suffer from inconsistent and compressed vertical spacing. The root cause is a combination of small margin values (`margin-bottom: 4px` on `.list-title`, `margin-top: 2px` on `.list-sub`) and no explicit `line-height` on any of the list-item text classes. The fix adjusts CSS margin and line-height values in the scoped `<style>` block of `Patients.razor` to produce balanced, readable spacing without altering fonts, colors, layout structure, or hover behavior.

## Glossary

- **Bug_Condition (C)**: The set of CSS property values on `.list-title`, `.list-sub`, and `.list-hint` that produce cramped or disproportionate vertical spacing inside a patient list item.
- **Property (P)**: The desired spacing behavior — sufficient, proportional gaps between name, data fields, and the action link, with explicit line-height for readability.
- **Preservation**: All visual properties unrelated to vertical spacing — font sizes, font weights, colors, hover background, card border/radius/padding — must remain identical after the fix.
- **`.list-title`**: The CSS class in `Patients.razor` that styles the patient full name (bold, 15px).
- **`.list-sub`**: The CSS class that styles the phone and email lines (gray, 13px).
- **`.list-hint`**: The CSS class that styles the "View details" action link (blue, 12px, 600 weight).
- **`.list-item-btn`**: The clickable button wrapping all text inside a list item (padding 14px 16px).

## Bug Details

### Bug Condition

The bug manifests when a patient list item is rendered with the current CSS values. The vertical gaps between the name, phone, email, and "View details" elements are too small and disproportionate, making individual fields hard to distinguish.

**Formal Specification:**
```
FUNCTION isBugCondition(styles)
  INPUT: styles — the computed CSS property map for .list-title, .list-sub, .list-hint
  OUTPUT: boolean

  titleMarginBottom   := styles[".list-title"]["margin-bottom"]
  subMarginTop        := styles[".list-sub"]["margin-top"]
  hintMarginTop       := styles[".list-hint"]["margin-top"]
  titleLineHeight     := styles[".list-title"]["line-height"]
  subLineHeight       := styles[".list-sub"]["line-height"]
  hintLineHeight      := styles[".list-hint"]["line-height"]

  gapTitleToSub := titleMarginBottom + subMarginTop          -- currently 4+2 = 6px
  gapSubToSub   := subMarginTop                              -- currently 2px
  gapSubToHint  := hintMarginTop                             -- currently 10px
  hasExplicitLH := titleLineHeight != "normal"
                   AND subLineHeight != "normal"
                   AND hintLineHeight != "normal"

  RETURN gapTitleToSub < 6
         OR gapSubToSub < 4
         OR gapSubToHint > 2 * gapSubToSub                   -- disproportionate jump
         OR NOT hasExplicitLH
END FUNCTION
```

### Examples

- **Name → Phone gap**: `.list-title` has `margin-bottom: 4px`, `.list-sub` has `margin-top: 2px` → 6px total. Expected: ≥ 6px (satisfied numerically but feels cramped without line-height).
- **Phone → Email gap**: Two consecutive `.list-sub` elements separated by only `margin-top: 2px` → 2px total. Expected: ≥ 4px.
- **Email → "View details" gap**: `.list-hint` has `margin-top: 10px` while `.list-sub` gaps are 2px → 5× ratio. Expected: proportional (roughly 2–3× at most).
- **Line-height**: All three classes inherit the browser default (~1.2). Expected: explicit 1.4–1.5 for comfortable reading.

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- `.list-title` must remain `font-weight: 700; font-size: 15px;`
- `.list-sub` must remain `color: #6b7280; font-size: 13px;`
- `.list-hint` must remain `font-size: 12px; font-weight: 600; color: #2563eb;`
- `.list-item-btn:hover` must remain `background: #eff6ff;`
- `.list-item` must retain `border: 1px solid #e5e7eb; border-radius: 10px; background: #fafafa;`
- `.list-item-btn` must retain `padding: 14px 16px;`

**Scope:**
All CSS properties not related to vertical spacing (margin-top, margin-bottom, line-height) on `.list-title`, `.list-sub`, and `.list-hint` are completely unaffected by this fix. This includes:
- Font family, size, weight, and color declarations
- Hover and focus states
- Card border, border-radius, background, and padding
- The overall list layout (flexbox column with 8px gap)

## Hypothesized Root Cause

Based on the bug description, the issues are:

1. **Insufficient `.list-sub` margin-top**: The current `margin-top: 2px` on `.list-sub` is too small to visually separate consecutive data fields (phone, email). Increasing to `4px` provides a clear gap.

2. **Disproportionate `.list-hint` margin-top**: The current `margin-top: 10px` on `.list-hint` is 5× the sub-to-sub gap, creating a jarring visual jump. Reducing to `8px` keeps the link visually distinct while being proportional (~2× the sub-to-sub gap).

3. **Missing explicit line-height**: None of the three text classes declare `line-height`, so the browser default (~1.2) applies. Adding `line-height: 1.4` to all three classes improves within-line readability and adds a small amount of effective vertical space.

4. **`.list-title` margin-bottom is borderline**: The current `4px` combined with the updated `.list-sub` margin-top of `4px` yields an 8px gap, which is comfortable. Increasing `.list-title` margin-bottom to `6px` provides a stronger visual anchor separating the name from the data fields.

## Correctness Properties

Property 1: Bug Condition — Spacing Values Are Corrected

_For any_ rendering of a patient list item where the bug condition holds (isBugCondition returns true with the old CSS values), the fixed stylesheet SHALL produce: `.list-title` with `margin-bottom ≥ 6px` and `line-height: 1.4`, `.list-sub` with `margin-top ≥ 4px` and `line-height: 1.4`, and `.list-hint` with `margin-top` proportional to the sub-to-sub gap (no more than 2× `margin-top` of `.list-sub`) and `line-height: 1.4`.

**Validates: Requirements 2.1, 2.2, 2.3, 2.4**

Property 2: Preservation — Non-Spacing Visual Properties Unchanged

_For any_ rendering of a patient list item where the bug condition does NOT hold (properties unrelated to vertical spacing), the fixed stylesheet SHALL produce exactly the same computed values as the original stylesheet, preserving font sizes, font weights, colors, hover backgrounds, card borders, border-radius, padding, and overall layout.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

## Fix Implementation

### Changes Required

**File**: `ClinicScheduler/ClinicScheduler.Shared/Pages/Patients.razor`

**Section**: Scoped `<style>` block at the bottom of the file

**Specific Changes**:

1. **`.list-title` — increase `margin-bottom` and add `line-height`**:
   - Change `margin-bottom: 4px;` → `margin-bottom: 6px;`
   - Add `line-height: 1.4;`
   - Full rule becomes: `.list-title { font-weight: 700; font-size: 15px; margin-bottom: 6px; line-height: 1.4; }`

2. **`.list-sub` — increase `margin-top` and add `line-height`**:
   - Change `margin-top: 2px;` → `margin-top: 4px;`
   - Add `line-height: 1.4;`
   - Full rule becomes: `.list-sub { color: #6b7280; font-size: 13px; margin-top: 4px; line-height: 1.4; }`

3. **`.list-hint` — reduce `margin-top` and add `line-height`**:
   - Change `margin-top: 10px;` → `margin-top: 8px;`
   - Add `line-height: 1.4;`
   - Full rule becomes: `.list-hint { margin-top: 8px; font-size: 12px; font-weight: 600; color: #2563eb; line-height: 1.4; }`

4. **No other CSS rules are modified** — all card, button, modal, form, and layout styles remain untouched.

5. **No HTML markup changes** — the Razor template and `@code` block are not modified.

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate the bug on unfixed code, then verify the fix works correctly and preserves existing behavior.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate the spacing defects BEFORE implementing the fix. Confirm or refute the root cause analysis.

**Test Plan**: Inspect the computed CSS values of `.list-title`, `.list-sub`, and `.list-hint` in the unfixed code and assert they meet the spacing thresholds from the requirements. These assertions will fail on the unfixed code, confirming the bug.

**Test Cases**:
1. **Title-to-Sub Gap Test**: Assert `margin-bottom` of `.list-title` ≥ 6px (will fail — current value is 4px)
2. **Sub-to-Sub Gap Test**: Assert `margin-top` of `.list-sub` ≥ 4px (will fail — current value is 2px)
3. **Hint Proportionality Test**: Assert `margin-top` of `.list-hint` ≤ 2× `margin-top` of `.list-sub` (will fail — 10px vs 2×2=4px)
4. **Line-Height Test**: Assert `line-height` is explicitly set on all three classes (will fail — none have it)

**Expected Counterexamples**:
- `.list-title` margin-bottom is 4px, below the 6px threshold
- `.list-sub` margin-top is 2px, below the 4px threshold
- `.list-hint` margin-top (10px) is 5× the sub-to-sub gap (2px), exceeding the 2× proportionality limit
- No explicit line-height on any class

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed stylesheet produces the expected spacing values.

**Pseudocode:**
```
FOR ALL element WHERE isBugCondition(element.styles) DO
  fixedStyles := applyFixedStylesheet(element)
  ASSERT fixedStyles[".list-title"]["margin-bottom"] >= 6px
  ASSERT fixedStyles[".list-title"]["line-height"] == 1.4
  ASSERT fixedStyles[".list-sub"]["margin-top"] >= 4px
  ASSERT fixedStyles[".list-sub"]["line-height"] == 1.4
  ASSERT fixedStyles[".list-hint"]["margin-top"] <= 2 * fixedStyles[".list-sub"]["margin-top"]
  ASSERT fixedStyles[".list-hint"]["line-height"] == 1.4
END FOR
```

### Preservation Checking

**Goal**: Verify that for all CSS properties where the bug condition does NOT hold, the fixed stylesheet produces the same computed values as the original.

**Pseudocode:**
```
FOR ALL property WHERE NOT isBugCondition(property) DO
  ASSERT originalStylesheet(property) = fixedStylesheet(property)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It can generate many combinations of list items (varying name lengths, with/without phone, with/without email) and verify non-spacing properties remain identical
- It catches edge cases where margin collapse or inheritance might unexpectedly alter preserved properties
- It provides strong guarantees that the fix is purely additive to spacing

**Test Plan**: Observe the computed values of all non-spacing CSS properties on unfixed code, then write tests asserting those values are identical after the fix.

**Test Cases**:
1. **Font Preservation**: Verify `.list-title` retains `font-weight: 700; font-size: 15px;` after fix
2. **Color Preservation**: Verify `.list-sub` retains `color: #6b7280; font-size: 13px;` and `.list-hint` retains `color: #2563eb; font-size: 12px; font-weight: 600;` after fix
3. **Hover Preservation**: Verify `.list-item-btn:hover` retains `background: #eff6ff;` after fix
4. **Card Layout Preservation**: Verify `.list-item` retains `border: 1px solid #e5e7eb; border-radius: 10px; background: #fafafa;` and `.list-item-btn` retains `padding: 14px 16px;` after fix

### Unit Tests

- Parse the scoped `<style>` block and assert the three changed CSS values match the fix specification
- Assert no other CSS rules in the style block were modified (diff check)
- Assert the Razor markup is unchanged (no HTML modifications)

### Property-Based Tests

- Generate random patient data (varying name lengths, optional phone/email) and verify spacing properties meet thresholds across all rendered items
- Generate random viewport widths and verify spacing values remain consistent (no responsive breakpoint regressions)
- Verify that for any patient list item, non-spacing CSS properties are identical between original and fixed stylesheets

### Integration Tests

- Render the Patients page with multiple patients and visually verify spacing between name, phone, email, and "View details"
- Render the Patients page and verify hover behavior is unchanged
- Render the patient detail modal and verify it is unaffected by the spacing fix
