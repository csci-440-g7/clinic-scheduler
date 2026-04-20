using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;

namespace ClinicScheduler.Core.Services;

public class AppointmentSchedulingService
{
    public const int SlotDurationMinutes = 30;
    private const int MaxConcurrentPatients = 12;
    public static readonly TimeSpan SlotDuration = TimeSpan.FromMinutes(SlotDurationMinutes);
    private static readonly TimeOnly ClinicOpen  = new(8, 0);
    private static readonly TimeOnly ClinicClose = new(17, 0);

    private readonly IRepository<Appointment> _appointmentRepository;
    private readonly IRepository<Patient> _patientRepository;
    private readonly IRepository<Therapist> _therapistRepository;
    private readonly IRepository<Room> _roomRepository;

    public AppointmentSchedulingService(
        IRepository<Appointment> appointmentRepository,
        IRepository<Patient> patientRepository,
        IRepository<Therapist> therapistRepository,
        IRepository<Room> roomRepository)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _therapistRepository = therapistRepository;
        _roomRepository = roomRepository;
    }

    /// <summary>
    /// Creates a new appointment after validating slot rules and conflicts.
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
        ValidateSlot(startTime);

        var patient = await _patientRepository.GetByIdAsync(patientId, ct)
            ?? throw new ArgumentException("Patient not found.", nameof(patientId));

        var therapist = await _therapistRepository.GetByIdAsync(therapistId, ct)
            ?? throw new ArgumentException("Therapist not found.", nameof(therapistId));

        var room = await _roomRepository.GetByIdAsync(roomId, ct)
            ?? throw new ArgumentException("Room not found.", nameof(roomId));

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

        var concurrentPatientCount = overlappingList.Select(a => a.PatientId).Distinct().Count();
        if (concurrentPatientCount >= MaxConcurrentPatients)
            throw new InvalidOperationException($"Clinic capacity reached: cannot schedule more than {MaxConcurrentPatients} concurrent patients.");

        var newAppointment = new Appointment(patient, therapist, room, startTime, duration, therapyType);
        return await _appointmentRepository.AddAsync(newAppointment, ct);
    }

    /// <summary>
    /// Finds the next available 30-min slot for the same therapist/room as the missed appointment
    /// and creates a replacement appointment. Searches forward up to 30 calendar days.
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

        var missedTime = TimeOnly.FromDateTime(missed.StartTime);
        var searchStart = missed.StartTime.Date.AddDays(1);
        var searchEnd = searchStart.AddDays(30);

        // Build candidate slots: try same time-of-day first across 30 days, then all other slots
        var sameTimeSlots = GenerateWeekdaySlots(searchStart, searchEnd)
            .Where(s => TimeOnly.FromDateTime(s) == missedTime);

        var otherSlots = GenerateWeekdaySlots(searchStart, searchEnd)
            .Where(s => TimeOnly.FromDateTime(s) != missedTime);

        foreach (var slot in sameTimeSlots.Concat(otherSlots))
        {
            var slotEnd = slot.Add(SlotDuration);

            var overlapping = await _appointmentRepository.FindAsync(a =>
                a.Status != AppointmentStatus.Canceled && a.Status != AppointmentStatus.Missed &&
                a.StartTime < slotEnd && a.EndTime > slot, ct);

            var overlappingList = overlapping.ToList();

            if (overlappingList.Any(a => a.TherapistId == missed.TherapistId)) continue;
            if (overlappingList.Any(a => a.RoomId == missed.RoomId)) continue;
            if (overlappingList.Any(a => a.PatientId == missed.PatientId)) continue;

            var concurrentCount = overlappingList.Select(a => a.PatientId).Distinct().Count();
            if (concurrentCount >= MaxConcurrentPatients) continue;

            var rescheduled = new Appointment(patient, therapist, room, slot, SlotDuration, missed.TherapyType);
            rescheduled.TreatmentPlanId = missed.TreatmentPlanId;
            return await _appointmentRepository.AddAsync(rescheduled, ct);
        }

        throw new InvalidOperationException(
            "No available time slot found within the next 30 days for this therapist and room.");
    }

    /// <summary>
    /// Validates that a slot falls on a weekday, on a 30-minute boundary, within clinic hours.
    /// </summary>
    public static void ValidateSlot(DateTime startTime)
    {
        if (startTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new ArgumentException("Appointments can only be scheduled on weekdays (Mon–Fri).");

        var time = TimeOnly.FromDateTime(startTime);
        if (time < ClinicOpen)
            throw new ArgumentException($"Appointments must be scheduled between {ClinicOpen:h:mm tt} and {ClinicClose:h:mm tt}.");

        if (time.AddMinutes(30) > ClinicClose)
            throw new ArgumentException($"Appointments must end by {ClinicClose:h:mm tt}.");

        if (startTime.Minute != 0 && startTime.Minute != 30)
            throw new ArgumentException("Appointments must start on a 30-minute boundary (:00 or :30).");
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
