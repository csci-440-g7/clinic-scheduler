# Implementation Plan

- [x] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - Patient List Spacing Values Are Cramped
  - **CRITICAL**: This test MUST FAIL on unfixed code - failure confirms the bug exists
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior - it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate the spacing bug exists in the scoped `<style>` block of `Patients.razor`
  - **Scoped PBT Approach**: Parse the CSS declarations for `.list-title`, `.list-sub`, and `.list-hint` from the `<style>` block and assert spacing thresholds
  - Test that `.list-title` has `margin-bottom >= 6px` (Bug Condition: current value is 4px, will fail)
  - Test that `.list-sub` has `margin-top >= 4px` (Bug Condition: current value is 2px, will fail)
  - Test that `.list-hint` `margin-top` is proportional to `.list-sub` `margin-top` — no more than 2× (Bug Condition: current 10px vs 2×2px=4px, will fail)
  - Test that all three classes have explicit `line-height` set (Bug Condition: none currently have it, will fail)
  - The test assertions match the Expected Behavior Properties from design: `margin-bottom >= 6px`, `margin-top >= 4px`, proportional hint margin, and `line-height: 1.4`
  - Run test on UNFIXED code
  - **EXPECTED OUTCOME**: Test FAILS (this is correct - it proves the bug exists)
  - Document counterexamples found: `.list-title` margin-bottom is 4px (< 6px), `.list-sub` margin-top is 2px (< 4px), `.list-hint` margin-top 10px exceeds 2× sub gap, no explicit line-height
  - Mark task complete when test is written, run, and failure is documented
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Non-Spacing Visual Properties Unchanged
  - **IMPORTANT**: Follow observation-first methodology
  - Observe on UNFIXED code: `.list-title` has `font-weight: 700; font-size: 15px;`
  - Observe on UNFIXED code: `.list-sub` has `color: #6b7280; font-size: 13px;`
  - Observe on UNFIXED code: `.list-hint` has `font-size: 12px; font-weight: 600; color: #2563eb;`
  - Observe on UNFIXED code: `.list-item-btn:hover` has `background: #eff6ff;`
  - Observe on UNFIXED code: `.list-item` has `border: 1px solid #e5e7eb; border-radius: 10px; background: #fafafa;`
  - Observe on UNFIXED code: `.list-item-btn` has `padding: 14px 16px;`
  - Write property-based test: parse the `<style>` block and for all non-spacing CSS properties on `.list-title`, `.list-sub`, `.list-hint`, `.list-item`, `.list-item-btn`, and `.list-item-btn:hover`, assert they match the observed baseline values
  - Verify test passes on UNFIXED code
  - **EXPECTED OUTCOME**: Tests PASS (this confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 3. Fix patient list spacing in Patients.razor

  - [x] 3.1 Implement the CSS spacing fix
    - In `ClinicScheduler/ClinicScheduler.Shared/Pages/Patients.razor`, update the scoped `<style>` block:
    - Change `.list-title` `margin-bottom: 4px` → `margin-bottom: 6px` and add `line-height: 1.4;`
    - Change `.list-sub` `margin-top: 2px` → `margin-top: 4px` and add `line-height: 1.4;`
    - Change `.list-hint` `margin-top: 10px` → `margin-top: 8px` and add `line-height: 1.4;`
    - Do NOT modify any other CSS rules, HTML markup, or `@code` block
    - _Bug_Condition: isBugCondition(styles) where `.list-title` margin-bottom < 6px OR `.list-sub` margin-top < 4px OR `.list-hint` margin-top > 2× `.list-sub` margin-top OR no explicit line-height_
    - _Expected_Behavior: `.list-title` margin-bottom: 6px, line-height: 1.4; `.list-sub` margin-top: 4px, line-height: 1.4; `.list-hint` margin-top: 8px, line-height: 1.4_
    - _Preservation: font sizes, font weights, colors, hover background, card border/radius/padding, button padding all unchanged_
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 3.2 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Patient List Spacing Values Are Corrected
    - **IMPORTANT**: Re-run the SAME test from task 1 - do NOT write a new test
    - The test from task 1 encodes the expected behavior: margin-bottom >= 6px, margin-top >= 4px, proportional hint margin, explicit line-height
    - When this test passes, it confirms the expected behavior is satisfied
    - Run bug condition exploration test from step 1
    - **EXPECTED OUTCOME**: Test PASSES (confirms bug is fixed)
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 3.3 Verify preservation tests still pass
    - **Property 2: Preservation** - Non-Spacing Visual Properties Unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 - do NOT write new tests
    - Run preservation property tests from step 2
    - **EXPECTED OUTCOME**: Tests PASS (confirms no regressions)
    - Confirm all non-spacing CSS properties (font sizes, weights, colors, hover, card layout, padding) are identical after fix
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 4. Checkpoint - Ensure all tests pass
  - Run the full test suite to confirm both bug condition and preservation tests pass
  - Verify no other tests were broken by the CSS changes
  - Ask the user if questions arise
