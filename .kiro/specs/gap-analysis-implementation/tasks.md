# Implementation Plan: Gap Analysis Implementation

## Overview

This plan implements 10 requirements from the gap analysis in incremental steps, starting with domain model changes, then infrastructure, service logic, UI, documentation, and finally the EF Core migration. Each task builds on the previous, and property-based tests are placed close to the code they validate. The implementation uses C# / .NET 10 with the existing xUnit + FsCheck + FluentAssertions test stack.

## Tasks

- [x] 1. Add new entities and enumerations
  - [x] 1.1 Create the `TreatmentPlanStatus` enum in `ClinicScheduler.Core/Entities/TreatmentPlanStatus.cs`
    - Define values: `Active`, `Suspended`, `Ended`
    - _Requirements: 4.1_

  - [x] 1.2 Create the `TimeSlot` entity in `ClinicScheduler.Core/Entities/TimeSlot.cs`
    - Add properties: `Id`, `StartTime` (TimeOnly), `EndTime` (TimeOnly), `DayOfWeek`, `LocationId`, `Location` navigation, `CreatedAt`, `UpdatedAt`
    - Add private constructor for EF Core
    - Add public constructor that validates `startTime < endTime` and `DayOfWeek` range (0–6)
    - _Requirements: 1.1, 1.2, 1.3_

  - [ ]* 1.3 Write property test for TimeSlot construction (Property 1)
    - **Property 1: TimeSlot construction validates and preserves inputs**
    - Create `ClinicScheduler.Core.Tests/Entities/TimeSlotPropertyTests.cs`
    - Use FsCheck to generate arbitrary TimeOnly pairs and DayOfWeek values
    - Assert construction succeeds iff startTime < endTime, and all fields are preserved on success
    - **Validates: Requirements 1.1, 1.2**

  - [x] 1.4 Create the `ConflictType` enum and `ScheduleConflict` entity in `ClinicScheduler.Core/Entities/ScheduleConflict.cs`
    - Define `ConflictType` enum: `DoubleBook`, `OutsideHours`, `Capacity`
    - Add properties: `Id`, `AppointmentId`, `Appointment` navigation, `DetectedAt`, `ConflictType`, `Resolved`, `ResolvedAt`
    - Add private constructor for EF Core
    - Add public constructor that sets `DetectedAt = DateTime.UtcNow`, `Resolved = false`, `ResolvedAt = null`
    - Add `Resolve()` method that sets `Resolved = true` and `ResolvedAt = DateTime.UtcNow`; throw if already resolved
    - _Requirements: 2.1, 2.2, 2.4_

  - [ ]* 1.5 Write property tests for ScheduleConflict (Properties 3 and 4)
    - **Property 3: ScheduleConflict construction preserves all fields**
    - **Property 4: ScheduleConflict resolution sets flag and timestamp**
    - Create `ClinicScheduler.Core.Tests/Entities/ScheduleConflictPropertyTests.cs`
    - **Validates: Requirements 2.1, 2.4**

- [x] 2. Modify existing entities with new properties
  - [x] 2.1 Add `Status` property and lifecycle methods to `TreatmentPlan`
    - Add `public TreatmentPlanStatus Status { get; private set; } = TreatmentPlanStatus.Active;`
    - Add `Suspend()` method: reject if Ended, set Status to Suspended, update timestamp
    - Add `End()` method: set Status to Ended, update timestamp
    - Add `Reactivate()` method: reject if Ended, set Status to Active, update timestamp
    - Set initial Status to Active in the existing constructor
    - _Requirements: 4.2, 4.3, 4.4, 4.5, 4.6_

  - [ ]* 2.2 Write property tests for TreatmentPlan status lifecycle (Properties 7, 8, 9)
    - **Property 7: New TreatmentPlan starts with Active status**
    - **Property 8: TreatmentPlan valid status transitions update state and timestamp**
    - **Property 9: Ended TreatmentPlan rejects status changes**
    - Create `ClinicScheduler.Core.Tests/Entities/TreatmentPlanStatusPropertyTests.cs`
    - **Validates: Requirements 4.3, 4.4, 4.5, 4.6**

  - [x] 2.3 Add `NpiNumber` property and validation to `Therapist`
    - Add `public string? NpiNumber { get; private set; }`
    - Make `Therapist` class `partial` to support `[GeneratedRegex]`
    - Add `SetNpiNumber(string?)` method with regex validation (`^\d{10}$`)
    - Add optional `npiNumber` parameter to the existing constructor
    - _Requirements: 5.1, 5.2_

  - [ ]* 2.4 Write property test for NPI number validation (Property 10)
    - **Property 10: NPI number validation accepts exactly 10-digit strings**
    - Create `ClinicScheduler.Core.Tests/Entities/TherapistNpiPropertyTests.cs`
    - **Validates: Requirements 5.2**

  - [x] 2.5 Add `DailyCapacity` property and validation to `Location`
    - Add `public int DailyCapacity { get; private set; } = 12;`
    - Add `SetDailyCapacity(int)` method that validates value > 0
    - Add `ICollection<TimeSlot> TimeSlots { get; set; } = [];` navigation property
    - _Requirements: 8.1, 8.5_

  - [ ]* 2.6 Write property test for Location DailyCapacity (Property 11)
    - **Property 11: Location DailyCapacity validation and defaults**
    - Create `ClinicScheduler.Core.Tests/Entities/LocationCapacityPropertyTests.cs`
    - **Validates: Requirements 8.1, 8.5**

  - [x] 2.7 Add `ScheduleConflicts` navigation property to `Appointment`
    - Add `public ICollection<ScheduleConflict> ScheduleConflicts { get; set; } = [];`
    - _Requirements: 2.6_

- [x] 3. Checkpoint — Verify entity changes compile and tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Update DbContext and infrastructure
  - [x] 4.1 Add new DbSets and model configuration to `ClinicDbContext`
    - Add `DbSet<TimeSlot> TimeSlots`
    - Add `DbSet<ScheduleConflict> ScheduleConflicts`
    - Configure `Location` → `TimeSlot` one-to-many relationship in `OnModelCreating`
    - Configure `Appointment` → `ScheduleConflict` one-to-many relationship in `OnModelCreating`
    - Configure unique filtered index on `Therapist.NpiNumber` (exclude nulls)
    - _Requirements: 1.4, 2.5, 5.4_

  - [x] 4.2 Implement automatic audit logging in `SaveChangesAsync`
    - Extend the existing `SaveChangesAsync` override to iterate `ChangeTracker.Entries()` for Added, Modified, and Deleted states
    - Create `AuditLog` entries with entity name, entity ID, action, and change summary
    - For Modified entities, capture original and current values of changed properties and format as ChangeSummary
    - For Added entities, include key property values in ChangeSummary
    - For Deleted entities, include key property values in ChangeSummary
    - Exclude `AuditLog` entities from being audited to prevent recursion
    - Wrap audit logic in try-catch so failures don't block the primary save
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [ ]* 4.3 Write property tests for AuditLog (Properties 5 and 6)
    - **Property 5: AuditLog construction preserves all fields**
    - **Property 6: Change summary formatting contains all property changes**
    - Create `ClinicScheduler.Core.Tests/Entities/AuditLogPropertyTests.cs`
    - **Validates: Requirements 3.2, 3.3**

- [x] 5. Update AppointmentSchedulingService for location-aware scheduling
  - [x] 5.1 Add `IRepository<TimeSlot>` and `IRepository<Location>` dependencies to the service constructor
    - Add new repository fields and constructor parameters
    - Mark existing `ValidateSlot(DateTime)` static method as `[Obsolete]`
    - Mark `MaxConcurrentPatients` constant as `[Obsolete]`
    - _Requirements: 1.5, 8.2_

  - [x] 5.2 Implement location-aware time slot validation
    - Add a new `ValidateSlotForLocation(DateTime startTime, int locationId)` async method
    - Query `TimeSlot` records for the given location and day of week
    - If TimeSlot records exist, verify the appointment start time falls within one of them
    - If no TimeSlot records exist, fall back to default 8:00 AM–5:00 PM weekday schedule
    - _Requirements: 1.5, 1.6_

  - [ ]* 5.3 Write property test for location-aware scheduling validation (Property 2)
    - **Property 2: Scheduling service validates appointment times against location TimeSlots**
    - Create `ClinicScheduler.Core.Tests/Entities/AppointmentSchedulingPropertyTests.cs`
    - Use Moq to mock `IRepository<TimeSlot>` with generated TimeSlot sets
    - **Validates: Requirements 1.5, 1.6**

  - [x] 5.4 Update `CreateAppointmentAsync` for location daily capacity and conflict recording
    - Accept `locationId` parameter (or derive from room's location)
    - Replace `MaxConcurrentPatients` check with `Location.DailyCapacity` check: count distinct patients with active appointments at the location on the appointment date
    - Create `ScheduleConflict` records with appropriate `ConflictType` when conflicts are detected
    - Set `Appointment.HasConflict = true` when any ScheduleConflict is created
    - _Requirements: 2.3, 2.6, 8.2, 8.3_

  - [ ]* 5.5 Write unit tests for updated CreateAppointmentAsync
    - Test location daily capacity enforcement with mock repositories
    - Test ScheduleConflict record creation on conflict detection
    - Test HasConflict sync with ScheduleConflicts
    - _Requirements: 2.3, 2.6, 8.2_

- [x] 6. Checkpoint — Verify service changes compile and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. UI changes — WCAG 2.1 accessibility enhancements
  - [x] 7.1 Update `MainLayout.razor` with skip navigation and ARIA landmarks
    - Add `<a href="#main-content" class="skip-nav">Skip to main content</a>` as the first focusable element
    - Add `role="navigation"` to the sidebar/drawer element
    - Add `role="main"` and `id="main-content"` to the main content area
    - Add CSS for `.skip-nav` (visually hidden until focused)
    - _Requirements: 6.1, 6.2_

  - [x] 7.2 Add ARIA attributes to Home.razor stat cards and interactive elements
    - Add `role="region"` and `aria-label` to stat card containers
    - Add `aria-label` attributes to all icon buttons lacking visible text
    - Ensure data tables have appropriate header associations
    - _Requirements: 6.3, 6.4, 6.5_

- [x] 8. UI changes — Entity management pages
  - [x] 8.1 Update `TreatmentPlans.razor` with Status column and lifecycle controls
    - Add a Status column to the data grid displaying color-coded MudChip
    - Add Suspend, End, and Reactivate action buttons in the Actions column
    - Wire buttons to call `TreatmentPlan.Suspend()`, `.End()`, `.Reactivate()` with error handling via MudSnackbar
    - Add Status display in the create/edit dialog
    - _Requirements: 4.7_

  - [x] 8.2 Update `Therapists.razor` with NPI Number field
    - Add NPI Number text field to the create/edit dialog with 10-digit validation
    - Add NPI Number column to the therapist data grid
    - Wire to `Therapist.SetNpiNumber()` with error handling
    - _Requirements: 5.3_

  - [x] 8.3 Update Location management UI with DailyCapacity field
    - Add DailyCapacity numeric field to the location create/edit form
    - Display DailyCapacity in the locations list/data grid
    - Wire to `Location.SetDailyCapacity()` with validation and error handling
    - _Requirements: 8.4_

- [x] 9. Checkpoint — Verify UI changes compile and all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Create documentation files
  - [x] 10.1 Create `docs/architecture.md`
    - Describe the layered project structure (Core, Infrastructure, Shared, Web)
    - Document key design patterns (Repository, Domain Entities)
    - List the technology stack (.NET 10, Blazor, EF Core, PostgreSQL, MudBlazor)
    - _Requirements: 7.1_

  - [x] 10.2 Create `docs/setup-guide.md`
    - List prerequisites (.NET 10 SDK, PostgreSQL, Node.js if needed)
    - Document database configuration steps and connection string setup
    - Describe environment variables (SeedAdmin:Password, ConnectionStrings:DefaultConnection)
    - Include commands to build and run the application
    - _Requirements: 7.2_

  - [x] 10.3 Create `docs/api-reference.md`
    - List all REST API endpoints from the Controllers
    - Document HTTP methods, request parameters, and response formats
    - _Requirements: 7.3_

  - [x] 10.4 Create `docs/user-guide.md`
    - Cover user management workflows
    - Document appointment scheduling workflows
    - Document treatment plan management
    - Document report generation
    - _Requirements: 7.4_

  - [x] 10.5 Create `docs/browser-compatibility.md`
    - List supported browsers and minimum versions (Chrome, Firefox, Edge latest 2; Safari latest 2 on macOS/iOS)
    - Describe responsive design breakpoints and expected layout behavior
    - Include manual testing checklist for mobile, tablet, and desktop viewports
    - _Requirements: 9.1, 9.2, 9.3_

  - [x] 10.6 Create `docs/deployment-https.md`
    - Explain HTTPS termination at the load balancer layer
    - Document why `UseHttpsRedirection()` is skipped in production
    - Include request flow description: client → load balancer (TLS termination) → application container (HTTP)
    - List required load balancer configuration for TLS certificates and health check endpoints
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

- [x] 11. Generate EF Core migration and final verification
  - [x] 11.1 Generate the EF Core migration
    - Run `dotnet ef migrations add GapAnalysisImplementation` to create the migration
    - The migration should create `TimeSlots` and `ScheduleConflicts` tables
    - The migration should add `Status` column to `TreatmentPlans` (default 0 = Active)
    - The migration should add `NpiNumber` column to `Therapists` (nullable, unique filtered index)
    - The migration should add `DailyCapacity` column to `Locations` (default 12)
    - _Requirements: 1.4, 2.5, 4.2, 5.4, 8.1_

  - [ ]* 11.2 Write integration tests for DbContext configuration
    - Verify new DbSets are queryable
    - Verify Location → TimeSlot and Appointment → ScheduleConflict relationships
    - Verify unique filtered index on Therapist.NpiNumber
    - Verify audit logging creates AuditLog entries for Added, Modified, Deleted entities
    - _Requirements: 1.4, 2.5, 3.1, 5.4_

- [x] 12. Final checkpoint — Ensure all tests pass and solution builds cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [x] 13. Dead code analysis and cleanup
  - [x] 13.1 Scan for unreferenced entities, services, and interfaces
    - Search for classes, interfaces, and enums that are defined but never referenced outside their own file
    - Check for unused `using` directives across all `.cs` and `.razor` files
    - Identify any orphaned test helper classes or test fixtures no longer exercised
  - [x] 13.2 Scan for unreachable methods and unused properties
    - Identify public/internal methods on entities and services that are never called from any other file
    - Check for private methods within classes that are never invoked
    - Look for properties with only a getter that are never read, or only a setter that is never written
  - [x] 13.3 Identify obsolete code paths after gap implementation
    - Review the `[Obsolete]` markers added in task 5.1 (`ValidateSlot`, `MaxConcurrentPatients`) and determine if any callers still reference them; if not, remove them
    - Check if the old hardcoded scheduling constants are still used anywhere after the location-aware refactor
    - Look for any UI code or service registrations that reference removed or replaced functionality
  - [x] 13.4 Remove confirmed dead code and verify build
    - Delete confirmed dead code files, methods, and properties
    - Remove unused `using` directives
    - Run `dotnet build` on the full solution to confirm no regressions
    - Run all tests to confirm nothing breaks

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirement clauses for traceability
- Checkpoints at tasks 3, 6, 9, and 12 ensure incremental validation
- Property tests validate the 11 correctness properties from the design document using FsCheck
- Unit and integration tests validate specific scenarios and edge cases using xUnit + FluentAssertions
- The EF Core migration (task 11.1) should be generated after all entity and DbContext changes are complete
- All existing data is preserved — new columns have sensible defaults and new tables start empty
