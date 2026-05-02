# Future Features

Planned enhancements identified during development but deferred to keep the current release stable.

## Security

### Multi-Factor Authentication (TOTP)
ASP.NET Core Identity has built-in TOTP support. Users would scan a QR code with Google Authenticator or similar, then enter a 6-digit code at login. No AWS services required — works offline. This is the recommended approach for HIPAA compliance.

### SMS-Based MFA (alternative)
If text message verification is preferred over authenticator apps, Amazon SNS can deliver 6-digit codes via SMS at ~$0.00645 per message. Requires implementing a custom `ISmsSender` interface.

## Integrations

### EHR Integration
Develop a direct interface with Electronic Health Record systems to sync patient medical history and treatment progress automatically. Would require HL7 FHIR or similar healthcare interoperability standard.

### Telehealth Expansion
Extend the scheduling platform to manage virtual appointments, allowing a hybrid model of in-person and remote therapy sessions. Would add a video conferencing link field to appointments and integrate with a telehealth provider (e.g., Twilio Video, Zoom API).

## Intelligence

### AI-Driven Schedule Optimization
Implement algorithms to analyze therapist workloads and patient attendance patterns, optimizing scheduling density and resource allocation. Could use historical appointment data to predict no-show risk and suggest overbooking strategies.

## Infrastructure

### Automated Backup & Disaster Recovery
Implement automated PostgreSQL backups (pg_dump on a cron schedule or AWS RDS automated backups) and a documented disaster recovery plan to meet the 99.5% uptime target from the original requirements.

### Health Check Endpoint
Add ASP.NET Core health checks (`/healthz`) with database connectivity verification for load balancer monitoring:
```csharp
builder.Services.AddHealthChecks().AddNpgSql(connectionString);
app.MapHealthChecks("/healthz");
```

### Performance Benchmarking
Establish automated performance tests to verify the 2-second response time target for scheduling operations. Could use k6 or NBomber for load testing.

## Data & Reporting

### Patient Insurance Policy Tracking
Add an `InsurancePolicyNumber` field to the Patient entity (identified in the original design class diagram but not yet implemented).

### Export Reports
Add PDF and CSV export options to the Reports page so clinic managers can download and share operational reports.

## Accessibility

### Full WCAG 2.1 Audit
The current implementation adds skip-nav, ARIA landmarks, and aria-labels. A complete WCAG 2.1 Level AA audit requires manual testing with screen readers (NVDA, VoiceOver) and keyboard-only navigation by an accessibility specialist.
