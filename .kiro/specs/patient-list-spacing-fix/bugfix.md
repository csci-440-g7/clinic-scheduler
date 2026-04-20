# Bugfix Requirements Document

## Introduction

The patient list items on the Patients page (`/patients`) have inconsistent and overly compressed spacing between the patient name, phone number, email, and "View details" link. The CSS margin and line-height values in the scoped `<style>` block of `Patients.razor` produce only 6px of total gap between the name and the first sub-item, 2px between phone and email lines, and a disproportionate 10px jump before "View details." The lack of explicit `line-height` on any of these classes compounds the cramped appearance. This makes individual data fields hard to distinguish at a glance.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN a patient list item is rendered THEN the system produces only 6px total vertical space between the patient name (`.list-title`, `margin-bottom: 4px`) and the first data field (`.list-sub`, `margin-top: 2px`), making the name and phone number appear visually merged.

1.2 WHEN multiple `.list-sub` elements (phone, email) are rendered consecutively THEN the system separates them by only 2px (`margin-top: 2px`), causing the phone number and email to collapse together with no meaningful visual distinction.

1.3 WHEN the "View details" link (`.list-hint`) is rendered after the data fields THEN the system applies a 10px top margin — disproportionately large compared to the 2px between data fields — creating an unbalanced visual rhythm within the list item.

1.4 WHEN any list item text (`.list-title`, `.list-sub`, `.list-hint`) is rendered THEN the system relies on the browser's default `line-height` (approximately 1.2), which makes the content feel cramped within each line.

### Expected Behavior (Correct)

2.1 WHEN a patient list item is rendered THEN the system SHALL provide sufficient vertical space (at least 6px effective gap) between the patient name and the first data field so they are clearly distinguishable as separate pieces of information.

2.2 WHEN multiple data fields (phone, email) are rendered consecutively THEN the system SHALL separate them with enough vertical space (at least 4px effective gap) so each field is visually distinct.

2.3 WHEN the "View details" link is rendered after the data fields THEN the system SHALL use a top margin that is proportional to the spacing between other elements, avoiding a jarring visual jump while still setting the link apart as an action.

2.4 WHEN any list item text is rendered THEN the system SHALL apply an explicit `line-height` (e.g., 1.4–1.5) to improve readability and reduce the cramped appearance within each line.

### Unchanged Behavior (Regression Prevention)

3.1 WHEN a patient list item is rendered THEN the system SHALL CONTINUE TO display the patient name in bold at 15px font size.

3.2 WHEN a patient list item is rendered THEN the system SHALL CONTINUE TO display phone and email in gray (#6b7280) at 13px font size.

3.3 WHEN a patient list item is rendered THEN the system SHALL CONTINUE TO display "View details" in blue (#2563eb) at 12px with 600 font weight.

3.4 WHEN a user hovers over a patient list item button THEN the system SHALL CONTINUE TO apply the light blue hover background (#eff6ff).

3.5 WHEN the patient list is rendered THEN the system SHALL CONTINUE TO maintain the existing card layout (border, border-radius, background, padding) of each `.list-item` and `.list-item-btn`.
