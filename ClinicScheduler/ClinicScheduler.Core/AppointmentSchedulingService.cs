using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;

namespace ClinicScheduler.Core.Services;

public class AppointmentSchedulingService
{
    public const int SlotDurationMinutes = 30;

    public static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(SlotDurationMinutes);
    private static readonly TimeOnly ClinicOpen  = new(8, 0);
    private static readonly TimeOnly ClinicClose = new(17, 0);

    private readonly IRepository<Appointment> _appointmentRepository;
    private readonly IRepository<Patient> _patientRepository;
    private readonly IRepository<Therapist> _therapistRepository;
    private readonly IRepository<Room> _roomRepository;
    private readonly IRepository<TimeSlot> _timeSlotRepository;
    private readonly IRepository<Location> _locationRepository;
    private readonly IRepository<ScheduleConflict> _scheduleConflictRepository;

    public AppointmentSchedulingService(
        IRepository<Appointment> appointmentRepository,
        IRepository<Patient> patientRepository,
        IRepository<Therapist> therapistRepository,
        IRepository<Room> roomRepository,
        IRepository<TimeSlot> timeSlotRepository,
        IRepository<Location> locationRepository,
        IRepository<ScheduleConflict> scheduleConflictRepository)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _therapistRepository = therapistRepository;
        _roomRepository = roomRepository;
        _timeSlotRepository = timeSlotRepository;
        _locationRepository = locationRepository;
        _scheduleConflictRepository = scheduleConflictRepository;
    }

    /// <summary>
    /// Creates a new appointment after validating slot rules and conflicts.
    /// Uses location-aware validation: derives the location from the room,
    /// checks TimeSlot records for schedule validation, and enforces Location.DailyCapacity.
    /// Creates ScheduleConflict records when conflicts are detected.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown on conflict or capacity exceeded.</exception>
    /// <exception cref="ArgumentException">Thrown if entities not found or slot is invalid.</exception>
    public async Task<Appointment> CreateAppointmentAsync(
        int patientId,
        int therapistId,
        int roomId,
        DateTime startTime,
        TimeSpan duration,
        CancellationToken ct = default,
        TherapyType? therapyType = null)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId, ct)
            ?? throw new ArgumentException("Patient not found.", nameof(patientId));

        var therapist = await _therapistRepository.GetByIdAsync(therapistId, ct)
            ?? throw new ArgumentException("Therapist not found.", nameof(therapistId));

        var room = await _roomRepository.GetByIdAsync(roomId, ct)
            ?? throw new ArgumentException("Room not found.", nameof(roomId));

        // Derive locationId from the room
        var locationId = room.LocationId;

        // Location-aware time slot validation
        await ValidateSlotForLocation(startTime, locationId, ct);

        var endTime = startTime.Add(duration);

        if (duration != SlotDuration)
            throw new ArgumentException($"Appointments must be exactly {SlotDurationMinutes} minutes.", nameof(duration));

        var overlapping = await _appointmentRepository.FindAsync(a =>
            a.Status != AppointmentStatus.Canceled && a.Status != AppointmentStatus.Missed &&
            a.StartTime < endTime && a.EndTime > startTime, ct);

        var overlappingList = overlapping.ToList();

        if (overlappingList.Any(a => a.TherapistId == therapistId))
            throw new InvalidOperationException("The selected therapist is unavailable at the requested time.");

        if (overlappingList.Any(a => a.RoomId == roomId))
            throw new InvalidOperationException("The selected room is unavailable at the requested time.");

        if (overlappingList.Any(a => a.PatientId == patientId))
            throw new InvalidOperationException("The patient is already scheduled for another appointment at the requested time.");

        // Create the appointment first so we can attach conflicts to it
        var newAppointment = new Appointment(patient, therapist, room, startTime, duration, therapyType);

        // Check location daily capacity: count distinct patients with active appointments
        // at the location on the appointment date
        var location = await _locationRepository.GetByIdAsync(locationId, ct)
            ?? throw new ArgumentException("Location not found.");

        var appointmentDate = startTime.Date;
        var nextDate = appointmentDate.AddDays(1);

        // Find all rooms at this location
        var locationRooms = await _roomRepository.FindAsync(r => r.LocationId == locationId, ct);
        var locationRoomIds = locationRooms.Select(r => r.Id).ToHashSet();

        // Count distinct patients with active appointments at this location on the same date
        var dailyAppointments = await _appointmentRepository.FindAsync(a =>
            a.Status != AppointmentStatus.Canceled && a.Status != AppointmentStatus.Missed &&
            a.StartTime >= appointmentDate && a.StartTime < nextDate, ct);

        var locationDailyAppointments = dailyAppointments
            .Where(a => locationRoomIds.Contains(a.RoomId))
            .ToList();

        var distinctPatientCount = locationDailyAppointments
            .Select(a => a.PatientId)
            .Distinct()
            .Count();

        // If the current patient is not already among today's patients, they would be a new addition
        var patientAlreadyScheduledToday = locationDailyAppointments.Any(a => a.PatientId == patientId);
        var effectivePatientCount = patientAlreadyScheduledToday ? distinctPatientCount : distinctPatientCount + 1;

        if (effectivePatientCount > location.DailyCapacity)
        {
            // Record a Capacity conflict
            newAppointment = await _appointmentRepository.AddAsync(newAppointment, ct);
            var capacityConflict = new ScheduleConflict(newAppointment, ConflictType.Capacity);
            await _scheduleConflictRepository.AddAsync(capacityConflict, ct);
            newAppointment.HasConflict = true;
            await _appointmentRepository.UpdateAsync(newAppointment, ct);
            throw new InvalidOperationException(
                $"Location daily capacity reached: cannot schedule more than {location.DailyCapacity} patients on this date.");
        }

        newAppointment = await _appointmentRepository.AddAsync(newAppointment, ct);
        return newAppointment;
    }

    /// <summary>
    /// Validates that the appointment start time falls within one of the location's configured
    /// TimeSlot records for the given day of week. If no TimeSlot records exist for the location,
    /// falls back to the default 8:00 AM–5:00 PM weekday schedule.
    /// </summary>
    /// <param name="startTime">The proposed appointment start time.</param>
    /// <param name="locationId">The location to validate against.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown if the start time is outside configured hours.</exception>
    public async Task ValidateSlotForLocation(DateTime startTime, int locationId, CancellationToken ct = default)
    {
        if (startTime.Minute != 0 && startTime.Minute != 30)
            throw new ArgumentException("Appointments must start on a 30-minute boundary (:00 or :30).");

        var dayOfWeek = startTime.DayOfWeek;

        // Query TimeSlot records for this location and day of week
        var timeSlots = await _timeSlotRepository.FindAsync(
            ts => ts.LocationId == locationId && ts.DayOfWeek == dayOfWeek, ct);

        var timeSlotList = timeSlots.ToList();

        if (timeSlotList.Count > 0)
        {
            // Validate against configured time slots
            var appointmentTime = TimeOnly.FromDateTime(startTime);
            var appointmentEnd = appointmentTime.AddMinutes(SlotDurationMinutes);

            var withinSlot = timeSlotList.Any(ts =>
                appointmentTime >= ts.StartTime && appointmentEnd <= ts.EndTime);

            if (!withinSlot)
                throw new ArgumentException(
                    "Appointment time is outside the configured schedule for this location.");
        }
        else
        {
            // Fall back to default 8:00 AM–5:00 PM weekday schedule
            if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                throw new ArgumentException(
                    "Appointment time is outside the configured schedule for this location.");

            var time = TimeOnly.FromDateTime(startTime);
            if (time < ClinicOpen || time.AddMinutes(SlotDurationMinutes) > ClinicClose)
                throw new ArgumentException(
                    "Appointment time is outside the configured schedule for this location.");
        }
    }

    /// <summary>
    /// Finds the next available 30-min slot for the same therapist/room as the missed appointment
    /// and creates a replacement appointment. Searches forward up to 30 calendar days.
    /// Uses location-aware validation for time slot checking.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no open slot is found within 30 days.</exception>
    public async Task<Appointment> RescheduleAfterMissedAsync(
        Appointment missed,
        CancellationToken ct = default)
    {
        if (missed.Status != AppointmentStatus.Missed)
            throw new ArgumentException("Appointment must be marked as Missed before rescheduling.", nameof(missed));

        var patient = await _patientRepository.GetByIdAsync(missed.PatientId, ct)
            ?? throw new InvalidOperationException("Patient not found for missed appointment.");

        var therapist = await _therapistRepository.GetByIdAsync(missed.TherapistId, ct)
            ?? throw new InvalidOperationException("Therapist not found for missed appointment.");

        var room = await _roomRepository.GetByIdAsync(missed.RoomId, ct)
            ?? throw new InvalidOperationException("Room not found for missed appointment.");

        var locationId = room.LocationId;

        var missedTime = TimeOnly.FromDateTime(missed.StartTime);
        // Always reschedule at least 7 days from now, regardless of when the
        // original appointment was.  This avoids booking into the past when an
        // old appointment is marked missed and gives the patient reasonable notice.
        var searchStart = DateTime.UtcNow.Date.AddDays(7);
        var searchEnd = searchStart.AddDays(30);

        // Build candidate slots: try same time-of-day first across 30 days, then all other slots
        var sameTimeSlots = GenerateWeekdaySlots(searchStart, searchEnd)
            .Where(s => TimeOnly.FromDateTime(s) == missedTime);

        var otherSlots = GenerateWeekdaySlots(searchStart, searchEnd)
            .Where(s => TimeOnly.FromDateTime(s) != missedTime);

        foreach (var slot in sameTimeSlots.Concat(otherSlots))
        {
            // Use location-aware validation; skip slots that don't pass
            try
            {
                await ValidateSlotForLocation(slot, locationId, ct);
            }
            catch (ArgumentException)
            {
                continue;
            }

            var slotEnd = slot.Add(SlotDuration);

            var overlapping = await _appointmentRepository.FindAsync(a =>
                a.Status != AppointmentStatus.Canceled && a.Status != AppointmentStatus.Missed &&
                a.StartTime < slotEnd && a.EndTime > slot, ct);

            var overlappingList = overlapping.ToList();

            if (overlappingList.Any(a => a.TherapistId == missed.TherapistId)) continue;
            if (overlappingList.Any(a => a.RoomId == missed.RoomId)) continue;
            if (overlappingList.Any(a => a.PatientId == missed.PatientId)) continue;

            // Check location daily capacity
            var location = await _locationRepository.GetByIdAsync(locationId, ct);
            if (location != null)
            {
                var appointmentDate = slot.Date;
                var nextDate = appointmentDate.AddDays(1);

                var locationRooms = await _roomRepository.FindAsync(r => r.LocationId == locationId, ct);
                var locationRoomIds = locationRooms.Select(r => r.Id).ToHashSet();

                var dailyAppointments = await _appointmentRepository.FindAsync(a =>
                    a.Status != AppointmentStatus.Canceled && a.Status != AppointmentStatus.Missed &&
                    a.StartTime >= appointmentDate && a.StartTime < nextDate, ct);

                var locationDailyAppointments = dailyAppointments
                    .Where(a => locationRoomIds.Contains(a.RoomId))
                    .ToList();

                var distinctPatientCount = locationDailyAppointments
                    .Select(a => a.PatientId)
                    .Distinct()
                    .Count();

                var patientAlreadyScheduledToday = locationDailyAppointments.Any(a => a.PatientId == missed.PatientId);
                var effectivePatientCount = patientAlreadyScheduledToday ? distinctPatientCount : distinctPatientCount + 1;

                if (effectivePatientCount > location.DailyCapacity) continue;
            }

            var rescheduled = new Appointment(patient, therapist, room, slot, SlotDuration, missed.TherapyType);
            rescheduled.TreatmentPlanId = missed.TreatmentPlanId;
            return await _appointmentRepository.AddAsync(rescheduled, ct);
        }

        throw new InvalidOperationException(
            "No available time slot found within the next 30 days for this therapist and room.");
    }

    private static IEnumerable<DateTime> GenerateWeekdaySlots(DateTime from, DateTime to)
    {
        var current = from.Date.AddHours(ClinicOpen.Hour);
        while (current < to)
        {
            if (current.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                var slotTime = TimeOnly.FromDateTime(current);
                while (slotTime < ClinicClose)
                {
                    yield return current.Date.AddHours(slotTime.Hour).AddMinutes(slotTime.Minute);
                    slotTime = slotTime.AddMinutes(30);
                }
            }
            current = current.AddDays(1);
        }
    }
}
