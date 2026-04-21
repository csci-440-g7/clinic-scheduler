using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicScheduler.Web.Tests.Fixtures;
using FluentAssertions;

namespace ClinicScheduler.Web.Tests.Api;

[Collection("WebApp")]
public class AppointmentsApiTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;

    // A fixed Monday in the future; no existing appointment data can conflict.
    private static readonly DateTime Slot9am = SeedData.FutureMonday9am();

    public AppointmentsApiTests(WebAppFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(int patientId, int therapistId, int roomId)> SeedAsync(string suffix = "")
    {
        var (patientId, therapistId, roomId, _) = await SeedData.SeedCoreEntitiesAsync(_fixture.Client, suffix);
        return (patientId, therapistId, roomId);
    }

    private async Task<HttpResponseMessage> BookAsync(
        int patientId, int therapistId, int roomId,
        DateTime startTime, DateTime? endTime = null)
    {
        return await _fixture.Client.PostAsJsonAsync("/api/appointments", new
        {
            PatientId = patientId,
            TherapistId = therapistId,
            RoomId = roomId,
            StartTime = startTime,
            EndTime = endTime ?? startTime.AddMinutes(30)
        });
    }

    // ── Happy-path tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task POST_ValidAppointment_Returns201()
    {
        var (p, t, r) = await SeedAsync();
        var response = await BookAsync(p, t, r, Slot9am);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Scheduled");
    }

    [Fact]
    public async Task GET_AllAppointments_Returns200()
    {
        var (p, t, r) = await SeedAsync("ga");
        await BookAsync(p, t, r, Slot9am);

        var response = await _fixture.Client.GetAsync("/api/appointments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task DELETE_Appointment_Returns204()
    {
        var (p, t, r) = await SeedAsync("del");
        var createResponse = await BookAsync(p, t, r, Slot9am);
        var doc = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var id = doc.RootElement.GetProperty("id").GetInt32();

        var deleteResponse = await _fixture.Client.DeleteAsync($"/api/appointments/{id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Business rule: 30-minute slots ───────────────────────────────────────

    [Fact]
    public async Task POST_60MinSlot_Returns400()
    {
        var (p, t, r) = await SeedAsync("60m");
        var response = await BookAsync(p, t, r, Slot9am, endTime: Slot9am.AddMinutes(60));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("30 minutes");
    }

    [Fact]
    public async Task POST_15MinSlot_Returns400()
    {
        var (p, t, r) = await SeedAsync("15m");
        var response = await BookAsync(p, t, r, Slot9am, endTime: Slot9am.AddMinutes(15));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Business rule: weekdays only ──────────────────────────────────────────

    [Fact]
    public async Task POST_OnSaturday_Returns400()
    {
        var (p, t, r) = await SeedAsync("sat");
        var saturday = new DateTime(2030, 6, 8, 9, 0, 0, DateTimeKind.Utc); // June 8 2030 = Saturday
        var response = await BookAsync(p, t, r, saturday);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("outside the configured schedule");
    }

    [Fact]
    public async Task POST_OnSunday_Returns400()
    {
        var (p, t, r) = await SeedAsync("sun");
        var sunday = new DateTime(2030, 6, 9, 9, 0, 0, DateTimeKind.Utc); // June 9 2030 = Sunday
        var response = await BookAsync(p, t, r, sunday);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("outside the configured schedule");
    }

    // ── Business rule: 8am–5pm window ────────────────────────────────────────

    [Fact]
    public async Task POST_Before8am_Returns400()
    {
        var (p, t, r) = await SeedAsync("early");
        var slot7am = Slot9am.Date.AddHours(7).ToUniversalTime();
        var response = await BookAsync(p, t, r, slot7am);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("outside the configured schedule");
    }

    [Fact]
    public async Task POST_SlotEndingAt5pm_Returns201()
    {
        // 16:30–17:00 is the last valid slot (ends exactly at 5pm)
        var (p, t, r) = await SeedAsync("last");
        var slot430pm = Slot9am.Date.AddHours(16).AddMinutes(30).ToUniversalTime();
        var response = await BookAsync(p, t, r, slot430pm);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_SlotEndingAfter5pm_Returns400()
    {
        // 17:00–17:30 starts at 5pm which is outside the configured schedule
        var (p, t, r) = await SeedAsync("late");
        var slot5pm = Slot9am.Date.AddHours(17).ToUniversalTime();
        var response = await BookAsync(p, t, r, slot5pm);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("outside the configured schedule");
    }

    // ── Business rule: 12-patient cap ────────────────────────────────────────

    [Fact]
    public async Task POST_TwelfthConcurrentPatient_Returns201()
    {
        var locationId = await SeedData.CreateLocationAsync(_fixture.Client);

        // Book 11 appointments at the same slot with unique patients/therapists/rooms
        for (var i = 1; i <= 11; i++)
        {
            var p = await SeedData.CreatePatientAsync(_fixture.Client, $"cap{i}");
            var t = await SeedData.CreateTherapistAsync(_fixture.Client, $"cap{i}");
            var r = await SeedData.CreateRoomAsync(_fixture.Client, locationId, $"cap{i}");
            (await BookAsync(p, t, r, Slot9am)).StatusCode.Should().Be(HttpStatusCode.Created,
                $"booking #{i} should succeed");
        }

        // 12th should also succeed
        var p12 = await SeedData.CreatePatientAsync(_fixture.Client, "cap12");
        var t12 = await SeedData.CreateTherapistAsync(_fixture.Client, "cap12");
        var r12 = await SeedData.CreateRoomAsync(_fixture.Client, locationId, "cap12");
        var response12 = await BookAsync(p12, t12, r12, Slot9am);

        response12.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_ThirteenthConcurrentPatient_Returns409()
    {
        var locationId = await SeedData.CreateLocationAsync(_fixture.Client);

        // Fill the 12-patient cap
        for (var i = 1; i <= 12; i++)
        {
            var p = await SeedData.CreatePatientAsync(_fixture.Client, $"full{i}");
            var t = await SeedData.CreateTherapistAsync(_fixture.Client, $"full{i}");
            var r = await SeedData.CreateRoomAsync(_fixture.Client, locationId, $"full{i}");
            (await BookAsync(p, t, r, Slot9am)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // 13th must be rejected
        var p13 = await SeedData.CreatePatientAsync(_fixture.Client, "full13");
        var t13 = await SeedData.CreateTherapistAsync(_fixture.Client, "full13");
        var r13 = await SeedData.CreateRoomAsync(_fixture.Client, locationId, "full13");
        var response13 = await BookAsync(p13, t13, r13, Slot9am);

        response13.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response13.Content.ReadAsStringAsync();
        body.Should().Contain("12 patients");
    }

    // ── Auto-reschedule on missed appointment ─────────────────────────────────

    [Fact]
    public async Task POST_MarkMissed_Returns200WithRescheduledAppointment()
    {
        var (p, t, r) = await SeedAsync("miss");
        var createResponse = await BookAsync(p, t, r, Slot9am);
        var doc = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var id = doc.RootElement.GetProperty("id").GetInt32();

        var missResponse = await _fixture.Client.PostAsync($"/api/appointments/{id}/mark-missed", null);

        missResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var missDoc = await JsonDocument.ParseAsync(await missResponse.Content.ReadAsStreamAsync());

        // Original appointment should be Missed
        missDoc.RootElement.GetProperty("missedAppointment")
            .GetProperty("status").GetString().Should().Be("Missed");

        // Rescheduled appointment should be Scheduled
        var rescheduled = missDoc.RootElement.GetProperty("rescheduledAppointment");
        rescheduled.GetProperty("status").GetString().Should().Be("Scheduled");
        rescheduled.GetProperty("patientId").GetInt32().Should().Be(p);
        rescheduled.GetProperty("therapistId").GetInt32().Should().Be(t);
        rescheduled.GetProperty("roomId").GetInt32().Should().Be(r);

        // Rescheduled time must be after the missed slot
        var rescheduledStart = rescheduled.GetProperty("startTime").GetDateTime();
        rescheduledStart.Should().BeAfter(Slot9am);
    }

    [Fact]
    public async Task POST_MarkMissedOnNonExistentAppointment_Returns404()
    {
        var response = await _fixture.Client.PostAsync("/api/appointments/99999/mark-missed", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
