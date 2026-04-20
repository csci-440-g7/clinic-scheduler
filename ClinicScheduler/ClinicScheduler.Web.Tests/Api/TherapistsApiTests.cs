using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicScheduler.Web.Tests.Fixtures;
using FluentAssertions;

namespace ClinicScheduler.Web.Tests.Api;

[Collection("WebApp")]
public class TherapistsApiTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;

    public TherapistsApiTests(WebAppFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_Therapist_Returns201WithId()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/therapists", new
        {
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@clinic.com",
            Specialty = "Occupational Therapy"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("email").GetString().Should().Be("jane@clinic.com");
    }

    [Fact]
    public async Task GET_AllTherapists_Returns200WithList()
    {
        await SeedData.CreateTherapistAsync(_fixture.Client, "A");
        await SeedData.CreateTherapistAsync(_fixture.Client, "B");

        var response = await _fixture.Client.GetAsync("/api/therapists");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GET_TherapistById_Returns200()
    {
        var id = await SeedData.CreateTherapistAsync(_fixture.Client, "X");

        var response = await _fixture.Client.GetAsync($"/api/therapists/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("id").GetInt32().Should().Be(id);
    }

    [Fact]
    public async Task GET_TherapistById_NotFound_Returns404()
    {
        var response = await _fixture.Client.GetAsync("/api/therapists/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Therapist_Returns204()
    {
        var id = await SeedData.CreateTherapistAsync(_fixture.Client, "U");

        var response = await _fixture.Client.PutAsJsonAsync($"/api/therapists/{id}", new
        {
            FirstName = "Updated",
            LastName = "Name",
            Email = "updatedU@clinic.com",
            Specialty = "Sports Therapy"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Therapist_Returns204()
    {
        var id = await SeedData.CreateTherapistAsync(_fixture.Client, "D");

        var response = await _fixture.Client.DeleteAsync($"/api/therapists/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
