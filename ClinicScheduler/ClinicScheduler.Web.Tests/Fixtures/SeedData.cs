using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClinicScheduler.Web.Tests.Fixtures;

/// <summary>
/// Helpers to POST seed entities via the API and return their IDs.
/// </summary>
public static class SeedData
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<int> CreatePatientAsync(HttpClient client, string suffix = "")
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            FirstName = "Test",
            LastName = $"Patient{suffix}",
            Email = $"patient{suffix}@test.com",
            DateOfBirth = "1990-01-01"
        });
        response.EnsureSuccessStatusCode();
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreateTherapistAsync(HttpClient client, string suffix = "")
    {
        var response = await client.PostAsJsonAsync("/api/therapists", new
        {
            FirstName = "Dr",
            LastName = $"Therapist{suffix}",
            Email = $"therapist{suffix}@clinic.com",
            Specialty = "Physical Therapy"
        });
        response.EnsureSuccessStatusCode();
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreateLocationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/locations", new
        {
            Name = "Main Clinic",
            Address = "123 Test St",
            City = "Dallas",
            State = "TX"
        });
        response.EnsureSuccessStatusCode();
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreateRoomAsync(HttpClient client, int locationId, string suffix = "")
    {
        var response = await client.PostAsJsonAsync("/api/rooms", new
        {
            Name = $"Room{suffix}",
            Capacity = 1,
            LocationId = locationId
        });
        response.EnsureSuccessStatusCode();
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    public static async Task<int> CreateTherapyTypeAsync(HttpClient client, string suffix = "")
    {
        var response = await client.PostAsJsonAsync("/api/therapytypes", new
        {
            Name = $"PhysicalTherapy{suffix}",
            Description = "Standard physical therapy",
            Specialty = "Physical Therapy",
            ColorCode = "#FF5733"
        });
        response.EnsureSuccessStatusCode();
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return doc.RootElement.GetProperty("id").GetInt32();
    }

    /// <summary>
    /// Seeds one location + one each of patient/therapist/room/therapytype.
    /// Returns (patientId, therapistId, roomId, therapyTypeId).
    /// </summary>
    public static async Task<(int patientId, int therapistId, int roomId, int therapyTypeId)> SeedCoreEntitiesAsync(
        HttpClient client, string suffix = "")
    {
        var locationId = await CreateLocationAsync(client);
        var patientId = await CreatePatientAsync(client, suffix);
        var therapistId = await CreateTherapistAsync(client, suffix);
        var roomId = await CreateRoomAsync(client, locationId, suffix);
        var therapyTypeId = await CreateTherapyTypeAsync(client, suffix);
        return (patientId, therapistId, roomId, therapyTypeId);
    }

    /// <summary>Returns the next Monday at 09:00 UTC on or after 2030-06-02.</summary>
    public static DateTime FutureMonday9am() => new(2030, 6, 3, 9, 0, 0, DateTimeKind.Utc); // June 3, 2030 is a Monday
}
