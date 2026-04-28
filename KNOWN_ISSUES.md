# ClinicScheduler — Known Issues & Next Steps

Last updated: April 28, 2026

---

## Resolved (this sprint)

| # | Issue | Fix |
|---|---|---|
| 1 | **Treatment plan creation fails** — "An error occurred while saving the entity changes" on every create attempt | Root cause: Patient and Therapist entities were loaded from a scoped `DbContext` (via `IRepository`) but saved through a different `DbContext` (via `IDbContextFactory`). EF Core rejects entities tracked by a different context. Fixed by loading all entities from the same `DbContext` instance used for the save. |
| 2 | **Patient search is case-sensitive** — searching "alice" doesn't find "Alice" in both the Patients list and Doctor Dashboard | Switched from `string.Contains()` (case-sensitive on PostgreSQL) to `EF.Functions.ILike()` for case-insensitive matching. |
| 3 | **Inconsistent patient detail views** — clicking a patient in the Patients list opens a basic modal, but clicking a patient name on the Schedule calendar navigates to the full profile page (`/patients/{id}/profile`) | Changed the Patients list to navigate to the same profile page instead of opening the modal. |
| 4 | **Sidebar email overflows** — long email addresses break the sidebar layout | Added text truncation (`overflow: hidden; text-overflow: ellipsis`) with a hover tooltip showing the full email. |
| 5 | **Two calendars are confusing** — "Calendar" and "Schedule" nav items both look like calendars with no clear distinction | Renamed the nav link to "My Calendar" to clarify it's the personal weekly view, while "Schedule" remains the staff management grid. |

---

## Known Issues (open)

### MAUI Mobile App

- **Status:** Under active testing — behavior is inconsistent across devices.
- **Details:** The MAUI Blazor Hybrid shell wraps the same shared Razor components used by the web app, but platform-specific rendering differences (WebView2 on Windows, WKWebView on iOS, Android WebView) cause layout and navigation issues that don't appear in the browser.
- **Recommendation:** Document as a known limitation for the capstone presentation. The web app is the primary supported platform; the MAUI shell is a proof-of-concept demonstrating cross-platform potential.

---

## Future Features / Next Steps

### Short-term (presentation-ready improvements)

- **Move user name/email to the top bar** — instead of the sidebar bottom, display the logged-in user's name in the top navigation bar for better visibility and to avoid overflow issues entirely.
- **Consolidate calendar views** — merge the "My Calendar" (patient/therapist weekly view) and "Schedule" (staff grid) into a single page with role-based view switching, reducing navigation confusion.
- **Patient dashboard upcoming view** — currently scoped correctly per-patient, but could benefit from showing appointment type and room info inline.

### Medium-term (post-capstone)

- **HTTPS / domain setup** — wire up Nginx reverse proxy + Let's Encrypt on EC2 (currently HTTP only on port 8081).
- **CI/CD pipeline** — GitHub Actions workflow to auto-deploy on push to `main` (see Phase 3 in [DEPLOYMENT_NOTES.md](DEPLOYMENT_NOTES.md)).
- **Notification delivery** — email/SMS notifications in addition to in-app (currently in-app only).
- **Reporting dashboard** — therapist utilization, appointment completion rates, no-show trends.
- **MAUI mobile polish** — resolve platform-specific rendering issues, test on physical iOS/Android devices, add push notifications.

### Long-term (production readiness)

- **Multi-location support** — location-aware scheduling and room assignment.
- **Insurance / billing integration** — track insurance info per patient, generate billing codes.
- **Telehealth** — video appointment support with calendar integration.
- **Accessibility audit** — full WCAG 2.1 AA compliance review with assistive technology testing.
