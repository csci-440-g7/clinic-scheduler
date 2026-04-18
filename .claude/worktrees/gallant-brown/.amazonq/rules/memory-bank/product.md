# Product Overview

## Purpose
Pain Management Clinic Scheduler — a capstone project for East Texas A&M CSCI-440 Group 7. A web-based scheduling system for managing appointments at a pain management clinic.

## Key Features
- Schedule, update, cancel, and complete appointments
- Enforce business rules: weekdays only, 8am–5pm window, 30-minute slots, max 12 concurrent patients
- Conflict detection: therapist, room, and patient double-booking prevention
- Auto-reschedule missed appointments to the next available slot
- Manage patients, therapists, rooms, locations, therapy types, and treatment plans
- REST API with OpenAPI/Swagger documentation
- Blazor interactive UI (Server + WebAssembly hybrid)

## Target Users
- Clinic staff scheduling patient appointments
- Administrators managing clinic resources (rooms, therapists, locations)

## Domain Entities
- **Patient** — clinic patient with contact info and date of birth
- **Therapist** — care provider with specialty
- **Room** — treatment room within a location, with capacity
- **Location** — physical clinic site
- **Appointment** — links patient + therapist + room at a time slot; statuses: Scheduled, Completed, Missed, Canceled
- **TreatmentPlan** — a patient's care plan composed of therapy types
- **TherapyType** — a named therapy with specialty and color code
