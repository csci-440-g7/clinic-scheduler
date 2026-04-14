using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicScheduler.Web.Tests.Fixtures;
using FluentAssertions;

namespace ClinicScheduler.Web.Tests.Api;

[Collection("WebApp")]
public class PatientsApiTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;

    public PatientsApiTests(WebAppFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_Patient_Returns201WithId()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/patients", new
        {
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            DateOfBirth = "1985-03-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("email").GetString().Should().Be("alice@example.com");
    }

    [Fact]
    public async Task GET_AllPatients_Returns200WithList()
    {
        // Arrange — create two patients
        await SeedData.CreatePatientAsync(_fixture.Client, "A");
        await SeedData.CreatePatientAsync(_fixture.Client, "B");

        var response = await _fixture.Client.GetAsync("/api/patients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task PUT_Patient_Returns204()
    {
        var patientId = await SeedData.CreatePatientAsync(_fixture.Client, "C");

        var response = await _fixture.Client.PutAsJsonAsync($"/api/patients/{patientId}", new
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updatedC@example.com",
            DateOfBirth = "1985-03-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_PatientById_AfterUpdate_ReflectsChanges()
    {
        var patientId = await SeedData.CreatePatientAsync(_fixture.Client, "D");

        await _fixture.Client.PutAsJsonAsync($"/api/patients/{patientId}", new
        {
            FirstName = "Renamed",
            LastName = "Patient",
            Email = "renamedD@example.com",
            DateOfBirth = "1990-01-01"
        });

        var getResponse = await _fixture.Client.GetAsync($"/api/patients/{patientId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await JsonDocument.ParseAsync(await getResponse.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("firstName").GetString().Should().Be("Renamed");
    }
}
