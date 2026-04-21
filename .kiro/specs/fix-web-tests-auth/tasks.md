# Fix Web Tests Auth — Tasks

## Tasks

- [x] 1. Create TestAuthHandler class
  - [x] 1.1 Create a `TestAuthHandler` class in `ClinicScheduler/ClinicScheduler.Web.Tests/Fixtures/TestAuthHandler.cs` that extends `AuthenticationHandler<AuthenticationSchemeOptions>`
  - [x] 1.2 Override `HandleAuthenticateAsync` to return `AuthenticateResult.Success` with a `ClaimsPrincipal` containing claims: `ClaimTypes.NameIdentifier` = `"test-admin-id"`, `ClaimTypes.Name` = `"testadmin@clinic.com"`, `ClaimTypes.Role` = `"Admin"`
- [x] 2. Register test authentication scheme in WebAppFixture
  - [x] 2.1 In `WebAppFixture.cs` `WithWebHostBuilder` → `ConfigureServices`, add `services.AddAuthentication("TestScheme").AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { })` to override the default authentication with the test handler
  - [x] 2.2 Add required `using` statements for `Microsoft.AspNetCore.Authentication`, `System.Security.Claims`, and `Microsoft.Extensions.Options`
- [x] 3. Verify all 43 integration tests pass
  - [x] 3.1 Run `dotnet test ClinicScheduler/ClinicScheduler.Web.Tests` and confirm all tests pass with zero failures
  - [x] 3.2 Verify that business rule validation tests still correctly reject invalid inputs (e.g., weekend scheduling returns 400, 13th concurrent patient returns 409)
