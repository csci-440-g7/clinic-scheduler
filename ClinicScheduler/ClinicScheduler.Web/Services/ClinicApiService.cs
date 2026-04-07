using System.Net.Http.Json;
using ClinicScheduler.Web.Contracts.Appointments;
using ClinicScheduler.Web.Contracts.Locations;
using ClinicScheduler.Web.Contracts.Patients;
using ClinicScheduler.Web.Contracts.Rooms;
using ClinicScheduler.Web.Contracts.Therapists;
using ClinicScheduler.Web.Contracts.TherapyTypes;
using ClinicScheduler.Web.Contracts.TreatmentPlans;

namespace ClinicScheduler.Web.Services;

public class ClinicApiService(HttpClient http) : IClinicApiService
{
    // Patients
    public async Task<List<PatientDto>> GetPatientsAsync() =>
        await http.GetFromJsonAsync<List<PatientDto>>("/api/patients") ?? [];

    public async Task<PatientDto?> CreatePatientAsync(CreatePatientRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/patients", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PatientDto>()
            : null;
    }

    public async Task<bool> UpdatePatientAsync(int id, UpdatePatientRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/patients/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeletePatientAsync(int id)
    {
        var response = await http.DeleteAsync($"/api/patients/{id}");
        return response.IsSuccessStatusCode;
    }

    // Therapists
    public async Task<List<TherapistDto>> GetTherapistsAsync() =>
        await http.GetFromJsonAsync<List<TherapistDto>>("/api/therapists") ?? [];

    public async Task<TherapistDto?> CreateTherapistAsync(CreateTherapistRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/therapists", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TherapistDto>()
            : null;
    }

    public async Task<bool> UpdateTherapistAsync(int id, UpdateTherapistRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/therapists/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteTherapistAsync(int id)
    {
        var response = await http.DeleteAsync($"/api/therapists/{id}");
        return response.IsSuccessStatusCode;
    }

    // Locations
    public async Task<List<LocationDto>> GetLocationsAsync() =>
        await http.GetFromJsonAsync<List<LocationDto>>("/api/locations") ?? [];

    public async Task<LocationDto?> CreateLocationAsync(CreateLocationRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/locations", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<LocationDto>()
            : null;
    }

    public async Task<bool> UpdateLocationAsync(int id, UpdateLocationRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/locations/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteLocationAsync(int id)
    {
        var response = await http.DeleteAsync($"/api/locations/{id}");
        return response.IsSuccessStatusCode;
    }

    // Rooms
    public async Task<List<RoomDto>> GetRoomsAsync() =>
        await http.GetFromJsonAsync<List<RoomDto>>("/api/rooms") ?? [];

    public async Task<RoomDto?> CreateRoomAsync(CreateRoomRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/rooms", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RoomDto>()
            : null;
    }

    public async Task<bool> UpdateRoomAsync(int id, UpdateRoomRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/rooms/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        var response = await http.DeleteAsync($"/api/rooms/{id}");
        return response.IsSuccessStatusCode;
    }

    // Therapy Types
    public async Task<List<TherapyTypeDto>> GetTherapyTypesAsync() =>
        await http.GetFromJsonAsync<List<TherapyTypeDto>>("/api/therapytypes") ?? [];

    public async Task<TherapyTypeDto?> CreateTherapyTypeAsync(CreateTherapyTypeRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/therapytypes", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TherapyTypeDto>()
            : null;
    }

    public async Task<bool> UpdateTherapyTypeAsync(int id, UpdateTherapyTypeRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/therapytypes/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteTherapyTypeAsync(int id)
    {
        var response = await http.DeleteAsync($"/api/therapytypes/{id}");
        return response.IsSuccessStatusCode;
    }

    // Appointments
    public async Task<List<AppointmentDto>> GetAppointmentsAsync() =>
        await http.GetFromJsonAsync<List<AppointmentDto>>("/api/appointments") ?? [];

    public async Task<(AppointmentDto? Appointment, string? ErrorMessage)> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/appointments", request);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<AppointmentDto>(), null);
        var error = await response.Content.ReadAsStringAsync();
        return (null, string.IsNullOrWhiteSpace(error) ? "Failed to create appointment." : error);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAppointmentAsync(int id, UpdateAppointmentRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/appointments/{id}", request);
        if (response.IsSuccessStatusCode) return (true, null);
        var error = await response.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(error) ? "Failed to update appointment." : error);
    }

    public async Task<bool> DeleteAppointmentAsync(int id)
    {
        var response = await http.DeleteAsync($"/api/appointments/{id}");
        return response.IsSuccessStatusCode;
    }

    // Treatment Plans
    public async Task<List<TreatmentPlanDto>> GetTreatmentPlansAsync() =>
        await http.GetFromJsonAsync<List<TreatmentPlanDto>>("/api/treatmentplans") ?? [];

    public async Task<TreatmentPlanDto?> CreateTreatmentPlanAsync(CreateTreatmentPlanRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/treatmentplans", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TreatmentPlanDto>()
            : null;
    }

    public async Task<bool> UpdateTreatmentPlanAsync(int id, UpdateTreatmentPlanRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/treatmentplans/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteTreatmentPlanAsync(int id)
    {
        var response = await http.DeleteAsync($"/api/treatmentplans/{id}");
        return response.IsSuccessStatusCode;
    }
}
