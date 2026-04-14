using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicScheduler.Web.Tests.Fixtures;
using FluentAssertions;

namespace ClinicScheduler.Web.Tests.Api;

[Collection("WebApp")]
public class TreatmentPlansApiTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;

    public TreatmentPlansApiTests(WebAppFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_TreatmentPlan_Returns201()
    {
        var (patientId, therapistId, _, therapyTypeId) = await SeedData.SeedCoreEntitiesAsync(_fixture.Client);

        var response = await _fixture.Client.PostAsJsonAsync("/api/treatmentplans", new
        {
            PatientId = patientId,
            TherapistId = therapistId,
            FrequencyPerWeek = 3,
            TotalDays = 30,
            StartDate = "2030-06-03",
            TherapyTypeIds = new[] { therapyTypeId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("totalDays").GetInt32().Should().Be(30);
    }

    [Fact]
    public async Task POST_TreatmentPlanWith40Days_Returns201()
    {
        var (patientId, therapistId, _, therapyTypeId) = await SeedData.SeedCoreEntitiesAsync(_fixture.Client, "40d");

        var response = await _fixture.Client.PostAsJsonAsync("/api/treatmentplans", new
        {
            PatientId = patientId,
            TherapistId = therapistId,
            FrequencyPerWeek = 4,
            TotalDays = 40,
            StartDate = "2030-06-03",
            TherapyTypeIds = new[] { therapyTypeId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("totalDays").GetInt32().Should().Be(40);
    }

    [Fact]
    public async Task POST_TreatmentPlanWith25Days_Returns400()
    {
        var (patientId, therapistId, _, therapyTypeId) = await SeedData.SeedCoreEntitiesAsync(_fixture.Client, "25d");

        var response = await _fixture.Client.PostAsJsonAsync("/api/treatmentplans", new
        {
            PatientId = patientId,
            TherapistId = therapistId,
            FrequencyPerWeek = 3,
            TotalDays = 25,
            StartDate = "2030-06-03",
            TherapyTypeIds = new[] { therapyTypeId }
        });

        // The entity constructor throws ArgumentException → controller returns 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    public async Task POST_AllValidDurations_Return201(int totalDays)
    {
        var (patientId, therapistId, _, therapyTypeId) = await SeedData.SeedCoreEntitiesAsync(_fixture.Client, $"dur{totalDays}");

        var response = await _fixture.Client.PostAsJsonAsync("/api/treatmentplans", new
        {
            PatientId = patientId,
            TherapistId = therapistId,
            FrequencyPerWeek = 3,
            TotalDays = totalDays,
            StartDate = "2030-06-03",
            TherapyTypeIds = new[] { therapyTypeId }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"TotalDays={totalDays} should be a valid duration");
    }
}
