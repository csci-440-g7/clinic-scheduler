using ClinicScheduler.Web.Contracts.Appointments;
using ClinicScheduler.Web.Contracts.Locations;
using ClinicScheduler.Web.Contracts.Patients;
using ClinicScheduler.Web.Contracts.Rooms;
using ClinicScheduler.Web.Contracts.Therapists;
using ClinicScheduler.Web.Contracts.TherapyTypes;
using ClinicScheduler.Web.Contracts.TreatmentPlans;

namespace ClinicScheduler.Web.Services;

public interface IClinicApiService
{
    // Patients
    Task<List<PatientDto>> GetPatientsAsync();
    Task<PatientDto?> CreatePatientAsync(CreatePatientRequest request);
    Task<bool> UpdatePatientAsync(int id, UpdatePatientRequest request);
    Task<bool> DeletePatientAsync(int id);

    // Therapists
    Task<List<TherapistDto>> GetTherapistsAsync();
    Task<TherapistDto?> CreateTherapistAsync(CreateTherapistRequest request);
    Task<bool> UpdateTherapistAsync(int id, UpdateTherapistRequest request);
    Task<bool> DeleteTherapistAsync(int id);

    // Locations
    Task<List<LocationDto>> GetLocationsAsync();
    Task<LocationDto?> CreateLocationAsync(CreateLocationRequest request);
    Task<bool> UpdateLocationAsync(int id, UpdateLocationRequest request);
    Task<bool> DeleteLocationAsync(int id);

    // Rooms
    Task<List<RoomDto>> GetRoomsAsync();
    Task<RoomDto?> CreateRoomAsync(CreateRoomRequest request);
    Task<bool> UpdateRoomAsync(int id, UpdateRoomRequest request);
    Task<bool> DeleteRoomAsync(int id);

    // Therapy Types
    Task<List<TherapyTypeDto>> GetTherapyTypesAsync();
    Task<TherapyTypeDto?> CreateTherapyTypeAsync(CreateTherapyTypeRequest request);
    Task<bool> UpdateTherapyTypeAsync(int id, UpdateTherapyTypeRequest request);
    Task<bool> DeleteTherapyTypeAsync(int id);

    // Appointments
    Task<List<AppointmentDto>> GetAppointmentsAsync();
    Task<(AppointmentDto? Appointment, string? ErrorMessage)> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<(bool Success, string? ErrorMessage)> UpdateAppointmentAsync(int id, UpdateAppointmentRequest request);
    Task<bool> DeleteAppointmentAsync(int id);

    // Treatment Plans
    Task<List<TreatmentPlanDto>> GetTreatmentPlansAsync();
    Task<TreatmentPlanDto?> CreateTreatmentPlanAsync(CreateTreatmentPlanRequest request);
    Task<bool> UpdateTreatmentPlanAsync(int id, UpdateTreatmentPlanRequest request);
    Task<bool> DeleteTreatmentPlanAsync(int id);
}
