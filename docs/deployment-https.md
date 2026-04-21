# HTTPS and Production Deployment

## HTTPS Termination Strategy

In production, TLS/HTTPS is terminated at the load balancer, not at the application container. The application runs over plain HTTP internally.

### Why `UseHttpsRedirection()` Is Skipped

In `Program.cs`, HTTPS redirection is conditionally disabled in production:

```csharp
// HTTPS termination is handled by the load balancer in production; skip redirect in container
if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();
```

**Rationale:** The load balancer handles TLS termination and forwards requests to the app container over HTTP on port 8080. If the app also tried to redirect HTTP → HTTPS, it would create an infinite redirect loop, since from the app's perspective every request arrives over HTTP.

In Development mode, `UseHttpsRedirection()` is active so developers get HTTPS locally via the Kestrel dev certificate.

## Request Flow

```
┌──────────┐       HTTPS (443)       ┌──────────────────┐       HTTP (8080)       ┌─────────────────┐
│          │ ──────────────────────►  │                  │ ──────────────────────►  │                 │
│  Client  │                          │  Load Balancer   │                          │  App Container  │
│ (Browser)│ ◄──────────────────────  │  (TLS Termination│ ◄──────────────────────  │  (Kestrel)      │
│          │       HTTPS (443)        │   + Certificate) │       HTTP (8080)        │                 │
└──────────┘                          └──────────────────┘                          └─────────────────┘
                                              │
                                              │ Health check
                                              │ GET /healthz or /
                                              ▼
                                      ┌─────────────────┐
                                      │  App Container   │
                                      │  HTTP 200 OK     │
                                      └─────────────────┘
```

1. **Client → Load Balancer:** The browser connects over HTTPS (port 443). The load balancer holds the TLS certificate and terminates the encrypted connection.
2. **Load Balancer → App Container:** The load balancer forwards the decrypted request to the app container over HTTP (port 8080). The `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` headers are added by the load balancer.
3. **App Container → Load Balancer → Client:** The response travels back the same path. The load balancer re-encrypts the response for the client.

## HSTS

In production, the app enables HTTP Strict Transport Security:

```csharp
app.UseHsts();
```

This tells browsers to always use HTTPS for future requests. The default max-age is 30 days.

## Docker Container Configuration

The app container is built from the multi-stage `Dockerfile`:

```dockerfile
# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClinicScheduler.Web.dll"]
```

The container listens on HTTP port 8080 only. No TLS certificate is configured inside the container.

### Docker Compose

```yaml
app:
  build:
    context: .
    dockerfile: Dockerfile
  ports:
    - "8081:8080"
  environment:
    ASPNETCORE_ENVIRONMENT: Production
    ConnectionStrings__DefaultConnection: "Host=db;Database=clinic_scheduler;..."
    SeedAdmin__Password: "${SEED_ADMIN_PASSWORD}"
```

## Load Balancer Configuration

### Required Settings

| Setting | Value | Notes |
|---------|-------|-------|
| Listener protocol | HTTPS (443) | Accepts client connections |
| Target protocol | HTTP (8080) | Forwards to app container |
| TLS certificate | Valid certificate for your domain | Use ACM (AWS), Let's Encrypt, or your CA |
| TLS minimum version | TLS 1.2 | TLS 1.3 preferred |
| Health check path | `/` or a custom `/healthz` endpoint | HTTP GET, expect 200 |
| Health check interval | 30 seconds | Adjust based on requirements |
| Unhealthy threshold | 3 consecutive failures | Removes container from rotation |
| Sticky sessions | Not required | The app uses cookie-based auth; any instance can handle requests |

### Forwarded Headers

If the app needs to know the original client IP or protocol (e.g., for logging or generating absolute URLs), configure the forwarded headers middleware. Add to `Program.cs` if needed:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

This should be placed early in the middleware pipeline, before authentication.

### AWS ALB Example

For AWS Application Load Balancer:

1. Create a target group with protocol HTTP, port 8080.
2. Register the ECS tasks or EC2 instances running the app container.
3. Create an HTTPS listener on port 443 with your ACM certificate.
4. Set the default action to forward to the target group.
5. Optionally add an HTTP listener on port 80 that redirects to HTTPS.

### Health Check Endpoint

The app serves the Blazor app at `/`, which returns `200 OK` for authenticated and unauthenticated requests (the login page). This works as a basic health check. For a dedicated health check, consider adding ASP.NET Core health checks:

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

app.MapHealthChecks("/healthz");
```

## Environment Variables for Production

| Variable | Required | Description |
|----------|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Yes | Set to `Production` |
| `ConnectionStrings__DefaultConnection` | Yes | PostgreSQL connection string |
| `SeedAdmin__Password` | Yes | Admin password (min 10 chars, uppercase, digit, special char) |
| `POSTGRES_PASSWORD` | Yes | Database password (used by both db and app containers) |
| `AllowedOrigins__0` | No | CORS origin if API is accessed from a different domain |
