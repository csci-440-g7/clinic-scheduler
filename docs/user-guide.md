# User Guide

## Roles and Permissions

ClinicScheduler uses role-based access control. Each user is assigned one role:

| Role | Capabilities |
|------|-------------|
| **Admin** | Full access: manage users, locations, rooms, therapists, patients, appointments, treatment plans, therapy types |
| **Clinic Manager** | Manage locations, rooms, therapists, therapy types, appointments, treatment plans |
| **Staff** | Manage patients, appointments, treatment plans, cancel requests |
| **Therapist** | View appointments, patients, treatment plans; manage own schedule |
| **Patient** | View own appointments and submit cancellation requests |

## User Management

Only administrators can create and manage user accounts.

### Creating a User

1. Navigate to the **Users** section from the sidebar.
2. Click **Add User**.
3. Fill in the user's email, name, and assign a role.
4. Set an initial password (must meet the password policy).
5. Click **Save**.

### Password Policy

| Environment | Requirements |
|-------------|-------------|
| Development | 8+ characters, one uppercase, one digit, one special character |
| Production | 10+ characters, one uppercase, one digit, one special character |

### Default Admin Account

On first run, the system seeds an admin account using the `SeedAdmin:Password` environment variable. Use this account to create additional users.

## Appointment Scheduling

### Booking an Appointment (Staff/Manager)

1. Go to **Appointments** from the sidebar.
2. Click **Add Appointment**.
3. Select the **Patient**, **Therapist**, and **Room**.
4. Choose the **Start Time** and **End Time**.
5. Optionally link a **Treatment Plan** and add **Notes**.
6. Click **Save**.

The system validates:
- The time slot falls within the location's configured operating hours (or the default 8:00 AM–5:00 PM weekday schedule).
- The therapist and room are not double-booked.
- The location's daily patient capacity is not exceeded.

If a conflict is detected, the system records a `ScheduleConflict` and flags the appointment.

### Rescheduling an Appointment

1. Open the appointment from the list.
2. Change the **Start Time**.
3. Click **Save**.

The appointment status changes to **Rescheduled** and the original time is preserved in the audit log.

### Canceling an Appointment

**Staff/Manager:** Edit the appointment and set the status to **Canceled**.

**Patient:** Submit a cancellation request:
1. Go to **My Appointments**.
2. Click **Request Cancellation** on the appointment.
3. Provide a reason.
4. Staff will review and process the request.

### Marking an Appointment as Missed

1. Open the appointment.
2. Click **Mark as Missed**.
3. The system automatically:
   - Sets the appointment status to **Missed**.
   - Creates a new appointment in the next available slot for the same patient, therapist, and room.
   - Extends the treatment plan end date by 7 days (if linked).

### Appointment Statuses

| Status | Description |
|--------|-------------|
| Scheduled | Newly created appointment |
| Rescheduled | Time was changed after initial booking |
| Completed | Session was attended |
| Canceled | Appointment was canceled |
| Missed | Patient did not attend |

## Treatment Plan Management

Treatment plans define a patient's therapy schedule over a period of time.

### Creating a Treatment Plan

1. Go to **Treatment Plans** from the sidebar.
2. Click **Add Treatment Plan**.
3. Select the **Patient** and **Therapist**.
4. Set **Frequency** (sessions per week) and **Duration** (total days).
5. Choose a **Start Date**.
6. Select one or more **Therapy Types**.
7. Click **Save**.

### Treatment Plan Lifecycle

Each treatment plan has a status that controls its lifecycle:

| Status | Description | Allowed Transitions |
|--------|-------------|-------------------|
| **Active** | Currently in progress | Suspend, End |
| **Suspended** | Temporarily paused | Reactivate, End |
| **Ended** | Permanently closed | None (terminal state) |

To change a plan's status, use the action buttons in the treatment plan list:
- **Suspend** — pauses the plan (e.g., patient on vacation).
- **Reactivate** — resumes a suspended plan.
- **End** — permanently closes the plan.

An ended plan cannot be reactivated or suspended.

### Editing a Treatment Plan

1. Click the **Edit** button on the treatment plan.
2. Update the schedule, therapist, or therapy types.
3. Click **Save**.

## Managing Locations

Locations represent physical clinic sites.

1. Go to **Locations** from the sidebar.
2. Click **Add Location** to create a new site.
3. Fill in the name, address, city, state, zip code, and time zone.
4. Set the **Daily Capacity** (maximum patients per day, default: 12).
5. Click **Save**.

The daily capacity is enforced during appointment scheduling. If a location reaches its capacity for a given day, new appointments are rejected.

## Managing Rooms

Rooms belong to a location and are used for scheduling appointments.

1. Go to **Rooms** from the sidebar.
2. Click **Add Room**.
3. Select the **Location**, enter a **Name**, **Capacity**, and optional **Description**.
4. Click **Save**.

## Managing Therapists

1. Go to **Therapists** from the sidebar.
2. Click **Add Therapist**.
3. Enter the therapist's name, email, phone, and specialty.
4. Optionally enter their **NPI Number** (must be exactly 10 digits).
5. Click **Save**.

## Managing Patients

1. Go to **Patients** from the sidebar.
2. Click **Add Patient**.
3. Enter the patient's name, email, phone, and date of birth.
4. Click **Save**.

## Managing Therapy Types

Therapy types categorize the services offered by the clinic.

1. Go to **Therapy Types** from the sidebar.
2. Click **Add Therapy Type**.
3. Enter a name, description, specialty, and color code.
4. Click **Save**.

Therapy types are linked to treatment plans and help organize the clinic's service offerings.

## Reports and Dashboard

The **Home** dashboard displays summary statistics:
- Total patients, therapists, and appointments
- Upcoming appointments
- Recent activity

Use the data grids on each management page to filter, sort, and review records. Data can be sorted by clicking column headers.
