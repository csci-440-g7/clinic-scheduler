using ClinicScheduler.Core.Entities;
using ClinicScheduler.Core.Interfaces;

namespace ClinicScheduler.Core.Services;

/// <summary>
/// Handles the business logic for creating and validating appointments.
/// </summary>
public class AppointmentSchedulingService
{
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
    /// Creates a new appointment after validating for scheduling conflicts.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if a scheduling conflict is found.</exception>
    /// <exception cref="ArgumentException">Thrown if dependent entities (Patient, Therapist, Room) are not found.</exception>
    public async Task<Appointment> CreateAppointmentAsync(
        int patientId, 
        int therapistId, 
        int roomId, 
        DateTime startTime, 
        TimeSpan duration, 
        CancellationToken ct = default)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId, ct) 
            ?? throw new ArgumentException("Patient not found.", nameof(patientId));
        
        var therapist = await _therapistRepository.GetByIdAsync(therapistId, ct)
            ?? throw new ArgumentException("Therapist not found.", nameof(therapistId));

        var room = await _roomRepository.GetByIdAsync(roomId, ct)
            ?? throw new ArgumentException("Room not found.", nameof(roomId));

        var endTime = startTime.Add(duration);

        // Business rule: appointments must be exactly 30 minutes
        if (duration != TimeSpan.FromMinutes(30))
            throw new ArgumentException("Appointments must be exactly 30 minutes.", nameof(duration));

        // Business rule: appointments must be on weekdays only
        if (startTime.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            throw new ArgumentException("Appointments can only be scheduled on weekdays (Monday–Friday).", nameof(startTime));

        // Business rule: appointments must fall within 8:00 am – 5:00 pm
        var clinicOpen = TimeSpan.FromHours(8);
        var clinicClose = TimeSpan.FromHours(17);
        if (startTime.TimeOfDay < clinicOpen || endTime.TimeOfDay > clinicClose)
            throw new ArgumentException("Appointments must be scheduled between 8:00 AM and 5:00 PM.", nameof(startTime));

        // Business rule: max 12 concurrent patients at any one time
        var concurrentAppointments = await _appointmentRepository.FindAsync(a =>
            a.Status != AppointmentStatus.Canceled && a.Status != AppointmentStatus.Missed &&
            a.StartTime < endTime && a.EndTime > startTime, ct);
        if (concurrentAppointments.Count >= 12)
            throw new InvalidOperationException("The clinic has reached its maximum capacity of 12 concurrent patients for this time slot.");

        var conflictingAppointments = await _appointmentRepository.FindAsync(a =>
            (a.TherapistId == therapistId || a.RoomId == roomId || a.PatientId == patientId) &&
            a.Status != AppointmentStatus.Canceled && a.Status != AppointmentStatus.Missed &&
            a.StartTime < endTime && a.EndTime > startTime, ct);

        if (conflictingAppointments.Any())
        {
            if (conflictingAppointments.Any(a => a.TherapistId == therapistId))
                throw new InvalidOperationException("The selected therapist is unavailable at the requested time.");
            
            if (conflictingAppointments.Any(a => a.RoomId == roomId))
                throw new InvalidOperationException("The selected room is unavailable at the requested time.");

            if (conflictingAppointments.Any(a => a.PatientId == patientId))
                throw new InvalidOperationException("The patient is already scheduled for another appointment at the requested time.");
        }

        var newAppointment = new Appointment(patient, therapist, room, startTime, duration);
        return await _appointmentRepository.AddAsync(newAppointment, ct);
    }
}