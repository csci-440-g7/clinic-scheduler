using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClinicScheduler.Web.Tests.Fixtures;
using FluentAssertions;

namespace ClinicScheduler.Web.Tests.Api;

[Collection("WebApp")]
public class LocationsApiTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;

    public LocationsApiTests(WebAppFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_Location_Returns201()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/locations", new
        {
            Name = "North Clinic",
            Address = "500 Main St",
            City = "Dallas",
            State = "TX",
            ZipCode = "75001",
            TimeZone = "America/Chicago"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("city").GetString().Should().Be("Dallas");
    }

    [Fact]
    public async Task GET_AllLocations_Returns200()
    {
        await SeedData.CreateLocationAsync(_fixture.Client);

        var response = await _fixture.Client.GetAsync("/api/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task DELETE_Location_Returns204()
    {
        var id = await SeedData.CreateLocationAsync(_fixture.Client);

        var response = await _fixture.Client.DeleteAsync($"/api/locations/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

[Collection("WebApp")]
public class RoomsApiTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;

    public RoomsApiTests(WebAppFixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_Room_Returns201()
    {
        var locationId = await SeedData.CreateLocationAsync(_fixture.Client);

        var response = await _fixture.Client.PostAsJsonAsync("/api/rooms", new
        {
            Name = "Therapy Room 1",
            Capacity = 3,
            LocationId = locationId
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("locationId").GetInt32().Should().Be(locationId);
    }

    [Fact]
    public async Task GET_RoomsByLocation_ReturnsFilteredList()
    {
        var loc1 = await SeedData.CreateLocationAsync(_fixture.Client);
        await SeedData.CreateRoomAsync(_fixture.Client, loc1, "A");
        await SeedData.CreateRoomAsync(_fixture.Client, loc1, "B");

        var response = await _fixture.Client.GetAsync($"/api/rooms/location/{loc1}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        doc.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GET_RoomsById_NotFound_Returns404()
    {
        var response = await _fixture.Client.GetAsync("/api/rooms/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Room_Returns204()
    {
        var locationId = await SeedData.CreateLocationAsync(_fixture.Client);
        var roomId = await SeedData.CreateRoomAsync(_fixture.Client, locationId, "Del");

        var response = await _fixture.Client.DeleteAsync($"/api/rooms/{roomId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
