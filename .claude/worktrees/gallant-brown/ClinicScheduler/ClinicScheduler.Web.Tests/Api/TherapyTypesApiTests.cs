using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicScheduler.Web.Tests.Fixtures;
using FluentAssertions;

namespace ClinicScheduler.Web.Tests.Api;

[Collection("WebApp")]
public class TherapyTypesApiTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;

    public TherapyTypesApiTests(WebAppFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_TherapyType_Returns201()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/therapytypes", new
        {
            Name = "Manual Therapy",
            Description = "Hands-on treatment",
            Specialty = "Physical Therapy",
            ColorCode = "#3366FF"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("colorCode").GetString().Should().Be("#3366FF");
    }

    [Fact]
    public async Task POST_TherapyType_InvalidHexColor_Returns400()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/therapytypes", new
        {
            Name = "BadColor",
            ColorCode = "notacolor"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("AABBCC")]   // missing #
    [InlineData("#ZZZZZZ")]  // invalid hex digits
    [InlineData("#12")]      // too short
    public async Task POST_TherapyType_VariousInvalidColors_Returns400(string badColor)
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/therapytypes", new
        {
            Name = $"Test{badColor}",
            ColorCode = badColor
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_AllTherapyTypes_Returns200()
    {
        await SeedData.CreateTherapyTypeAsync(_fixture.Client, "A");
        await SeedData.CreateTherapyTypeAsync(_fixture.Client, "B");

        var response = await _fixture.Client.GetAsync("/api/therapytypes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task DELETE_TherapyType_Returns204()
    {
        var id = await SeedData.CreateTherapyTypeAsync(_fixture.Client, "Del");

        var response = await _fixture.Client.DeleteAsync($"/api/therapytypes/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
