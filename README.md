# ISO9001Queue

An Azure Functions-based quality management backend that processes ISO 9001:2015 events through typed Azure Storage Queues, persists them in SQL Server, and exposes an HTTP admin API for querying and reporting.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-blue)](https://learn.microsoft.com/en-us/azure/azure-functions/)

---

## What is this?

ISO 9001:2015 requires organizations to maintain records of quality-related events: audit trails (clause 9.1.3), incident reports (clause 8.7), customer feedback (clause 9.1.2), and non-conformities (clause 10.2). ISO9001Queue handles all of that for you.

Any application in your ecosystem can publish quality events to four typed Azure Storage Queues. ISO9001Queue consumes those messages, stores them in a dedicated SQL Server database, sends email notifications where applicable, and exposes a secure HTTP API for administration and reporting.

**Key properties:**

- **Decoupled** — publishers only need the `ISO9001Queue.Contracts` package; they do not talk to the database.
- **Multi-tenant** — every record carries a `companyId`, so a single deployment can serve multiple organizations.
- **Observable** — Application Insights integration out of the box.
- **Extensible** — built on [ISO9001.Core](https://github.com/drualcman/ISO9001), a library of domain commands and queries following the ISO 9001:2015 standard.

---

## Architecture

```
 Your Application(s)
        │
        │  publish Base64-JSON messages
        ▼
┌───────────────────────────────────────────┐
│          Azure Storage Queues             │
│                                           │
│  iso9001-auditlogs                        │
│  iso9001-incidents                        │
│  iso9001-feedbacks                        │
│  iso9001-nonconformities                  │
└────────────────┬──────────────────────────┘
                 │  QueueTrigger (isolated worker)
                 ▼
┌───────────────────────────────────────────┐
│         ISO9001Queue.Functions            │
│                                           │
│  Queue Triggers  ──►  ISO9001.Core        │
│  HTTP Admin API  ──►  ISO9001.Core        │
│                        │                  │
│                        ▼                  │
│              Iso9001DbContext             │
└────────────────┬──────────────────────────┘
                 │
                 ▼
        SQL Server Database
```

---

## Solution structure

| Project | Type | Purpose |
|---|---|---|
| `ISO9001Queue.Contracts` | Class library | Message records and queue name constants. Publishable as a NuGet for your publishers. |
| `ISO9001Queue.Database.EF` | Class library | EF Core entities, `Iso9001DbContext`, migrations. |
| `ISO9001Queue.Infrastructure` | Class library | DI registration, email notification service, options. |
| `ISO9001Queue.Functions` | Azure Functions (isolated worker, v4) | Queue triggers + HTTP admin API. |

---

## Getting started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) (local Azure Storage emulator)
- SQL Server or SQL Server LocalDB

### 1. Clone and restore

```bash
git clone https://github.com/drualcman/ISO9001Queue.git
cd iso9001queue
dotnet restore
```

### 2. Configure `appsettings.Development.json`

Edit `Src/ISO9001Queue.Functions/appsettings.Development.json`:

```json
{
  "AzureWebJobsStorage": "UseDevelopmentStorage=true",
  "DatabaseOptions": {
    "ConnectionString": "Server=(localdb)\\MSSQLLocalDB;Database=iso9001db;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "EmailOptions": {
    "Url": "https://your-email-api/messaging/",
    "CompanyId": 1,
    "AdminEmail": "admin@yourcompany.com",
    "AdminName": "Quality Admin"
  }
}
```

### 3. Create the database

```bash
cd Src/ISO9001Queue.Database.EF
dotnet ef database update --startup-project ../ISO9001Queue.Functions
```

This creates five tables: `Iso9001AuditLogs`, `Iso9001IncidentReports`, `Iso9001NonConformities`, `Iso9001NonConformityDetails`, `Iso9001CustomerFeedbacks`.

### 4. Start Azurite

```bash
azurite --silent --location .azurite --debug .azurite/debug.log
```

### 5. Run the Functions host

```bash
cd Src/ISO9001Queue.Functions
func start
```

---

## Publishing messages (from your application)

### Install the contracts package

Reference the `ISO9001Queue.Contracts` project or publish it as a private NuGet package and add it to your application:

```xml
<PackageReference Include="ISO9001Queue.Contracts" Version="1.0.0" />
```

### Queue names

```csharp
using ISO9001Queue.Contracts;

Iso9001QueueNames.AuditLogs         // "iso9001-auditlogs"
Iso9001QueueNames.Incidents         // "iso9001-incidents"
Iso9001QueueNames.CustomerFeedbacks // "iso9001-feedbacks"
Iso9001QueueNames.NonConformities   // "iso9001-nonconformities"
```

### Message format

Messages are serialized as JSON and encoded in Base64 before being placed on the queue — the standard behavior of the Azure Storage SDK.

#### Audit log — ISO 9001 clause 9.1.3

Records that an action was performed by a user or system process.

```csharp
var message = new AuditLogQueueMessage(
    Reference:   "order-789",
    CompanyId:   "acme",
    Action:      "OrderShipped",
    PerformedBy: "user@acme.com",
    Timestamp:   DateTime.UtcNow,
    Description: "Order dispatched to logistics provider",
    Data:        JsonSerializer.Serialize(orderDetails));  // optional payload

await queueClient.SendMessageAsync(
    BinaryData.FromObjectAsJson(message));
```

#### Incident report — ISO 9001 clause 8.7

Records an error, exception, or unexpected system event.

```csharp
var message = new IncidentReportQueueMessage(
    Reference:       "payment-service",
    CompanyId:       "acme",
    ReportedAt:      DateTime.UtcNow,
    UserId:          "user@acme.com",
    Description:     ex.Message,
    AffectedProcess: "PaymentProcessing",
    Severity:        "Error",
    Data:            JsonSerializer.Serialize(context),
    Exception:       ex.ToString());

await queueClient.SendMessageAsync(
    BinaryData.FromObjectAsJson(message));
```

#### Customer feedback — ISO 9001 clause 9.1.2

Records a customer satisfaction rating (1–5 stars) with optional comments. On receipt, the system automatically sends a thank-you email to the customer, and an alert email to the quality admin if the rating is ≤ 2.

```csharp
var message = new CustomerFeedbackQueueMessage(
    EntityId:            "album-42",
    CompanyId:           "acme",
    CustomerId:          "customer-001",
    CustomerName:        "Jane Doe",
    CustomerEmail:       "jane@example.com",
    CustomerAntiPhishing: "ACME-XK92",
    Rating:              4,
    Comments:            "Great service, fast delivery!",
    ReportedAt:          DateTime.UtcNow);

await queueClient.SendMessageAsync(
    BinaryData.FromObjectAsJson(message));
```

> **Note:** `CustomerAntiPhishing` is an anti-phishing code shown in the confirmation email so the customer can verify the email is genuine. Set it to `string.Empty` if you do not use this feature.

#### Non-conformity — ISO 9001 clause 10.2

Records a detected non-conformity for corrective action tracking. You can also create non-conformities directly via the HTTP API if your workflow does not go through a queue.

```csharp
var message = new NonConformityQueueMessage(
    EntityId:        "product-batch-55",
    CompanyId:       "acme",
    ReportedAt:      DateTime.UtcNow,
    ReportedBy:      "inspector@acme.com",
    Description:     "Surface finish does not meet specification IPC-A-600",
    AffectedProcess: "SurfaceTreatment",
    Cause:           "Incorrect chemical concentration in tank 3",
    Status:          "Open");

await queueClient.SendMessageAsync(
    BinaryData.FromObjectAsJson(message));
```

---

## HTTP Admin API

All endpoints require an Azure Functions **function key** passed as either:

- Query string: `?code=<your-function-key>`
- Header: `x-functions-key: <your-function-key>`

The base URL when running locally is `http://localhost:7071/api`.

### Dashboard

| Method | Route | Description |
|---|---|---|
| `GET` | `/iso9001/dashboard` | Quality KPIs summary |

**Query parameters:** `companyId` (required), `from` (datetime, optional), `end` (datetime, optional)

**Response example:**
```json
{
  "totalAuditLogs": 1423,
  "totalIncidents": 18,
  "openNonConformities": 3,
  "closedNonConformities": 12,
  "avgResolutionDays": 4.2,
  "totalFeedbacks": 207,
  "avgRating": 4.3,
  "monthlyKpis": [
    { "year": 2026, "month": 5, "auditLogs": 312, "incidents": 4, "feedbacks": 38 }
  ]
}
```

---

### Audit Logs

| Method | Route | Description |
|---|---|---|
| `GET` | `/iso9001/audit-logs` | All audit logs |
| `GET` | `/iso9001/audit-logs/entity/{entityId}` | Logs for a specific entity |
| `GET` | `/iso9001/audit-logs/action/{action}` | Logs filtered by action name |
| `GET` | `/iso9001/audit-events` | Audit events for an entity |

**Query parameters:** `companyId` (required), `from`, `end`. The `/audit-events` endpoint also requires `entityId`.

---

### Customer Feedback

| Method | Route | Description |
|---|---|---|
| `GET` | `/iso9001/feedbacks` | All feedback |
| `GET` | `/iso9001/feedbacks/rating/{rating}` | Feedback with a specific rating (1–5) |
| `GET` | `/iso9001/feedbacks/customer/{customerId}` | Feedback by customer |
| `GET` | `/iso9001/feedbacks/entity/{entityId}` | Feedback for a specific entity |

**Query parameters:** `companyId` (required), `from`, `end`.

---

### Non-Conformities

| Method | Route | Description |
|---|---|---|
| `GET` | `/iso9001/non-conformities` | All non-conformities |
| `GET` | `/iso9001/non-conformities/status/{status}` | Filtered by status (Open / Closed) |
| `GET` | `/iso9001/non-conformities/entity/{entityId}` | By entity |
| `GET` | `/iso9001/non-conformities/process/{process}` | By affected process |
| `POST` | `/iso9001/non-conformities` | Create a non-conformity |
| `POST` | `/iso9001/non-conformities/detail` | Add a corrective action detail |

**Query parameters (GET):** `companyId` (required), `from`, `end`.

**POST `/iso9001/non-conformities` body:**
```json
{
  "entityId": "product-batch-55",
  "companyId": "acme",
  "affectedProcess": "SurfaceTreatment",
  "cause": "Incorrect chemical concentration",
  "description": "Surface finish defect",
  "reportedBy": "inspector@acme.com",
  "status": "Open"
}
```

**POST `/iso9001/non-conformities/detail` body:**
```json
{
  "nonConformityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Adjusted tank 3 concentration to 8.5%",
  "reportedBy": "technician@acme.com",
  "status": "InProgress"
}
```

---

### Reports (PDF)

All report endpoints return a PDF binary (`application/pdf`).

| Method | Route | Description |
|---|---|---|
| `GET` | `/iso9001/reports/audit` | Audit report |
| `GET` | `/iso9001/reports/audit-log` | Audit log detailed report |
| `GET` | `/iso9001/reports/non-conformity/master` | Non-conformity master list |
| `GET` | `/iso9001/reports/non-conformity/detail` | Non-conformity detail (requires `ncId`) |
| `GET` | `/iso9001/reports/feedback` | Customer feedback report |
| `GET` | `/iso9001/reports/incident` | Incident report |

**Common query parameters:** `companyId` (required), `entityId` (optional), `from`, `end`.
The `/non-conformity/detail` endpoint also requires `ncId` (non-conformity GUID).

---

## Configuration reference

### `DatabaseOptions`

| Key | Description | Default |
|---|---|---|
| `ConnectionString` | SQL Server connection string | *(empty)* |

### `EmailOptions`

| Key | Description | Default |
|---|---|---|
| `Url` | Base URL of the email API | *(empty)* |
| `CompanyId` | Company identifier sent to the email API | `5` |
| `AdminEmail` | Email address that receives low-rating alerts | *(empty)* |
| `AdminName` | Display name for the admin recipient | `"Admin"` |

### `AzureWebJobsStorage`

Connection string for Azure Storage (queues). Use `"UseDevelopmentStorage=true"` with Azurite locally.

### Queue polling (`host.json`)

```json
"extensions": {
  "queues": {
    "maxPollingInterval": "00:00:02",
    "visibilityTimeout": "00:00:30",
    "batchSize": 16,
    "maxDequeueCount": 5
  }
}
```

Messages that fail after `maxDequeueCount` retries are moved to the poison queue (`<queue-name>-poison`).

---

## Database schema

```
Iso9001AuditLogs
  Id               uniqueidentifier  PK (NEWSEQUENTIALID)
  EntityId         nvarchar(512)     indexed
  CompanyId        nvarchar(256)     indexed
  Action           nvarchar(512)
  PerformedBy      nvarchar(256)
  Timestamp        datetime2
  CreatedAt        datetime2
  Details          nvarchar(2048)
  Data             nvarchar(max)

Iso9001IncidentReports
  Id               uniqueidentifier  PK (NEWSEQUENTIALID)
  CompanyId        nvarchar(256)     indexed
  EntityId         nvarchar(512)     indexed
  ReportedAt       datetime2
  CreatedAt        datetime2
  UserId           nvarchar(256)
  Description      nvarchar(2048)
  AffectedProcess  nvarchar(512)
  Severity         nvarchar(64)
  Data             nvarchar(max)

Iso9001NonConformities
  Id               uniqueidentifier  PK
  EntityId         nvarchar(512)
  CompanyId        nvarchar(256)     indexed
  AffectedProcess  nvarchar(512)
  Cause            nvarchar(1024)
  Status           nvarchar(64)      indexed
  ReportedAt       datetime2
  CreatedAt        datetime2

Iso9001NonConformityDetails          (child of NonConformities, cascade delete)
  Id               int               PK (identity)
  NonConformityId  uniqueidentifier  FK → Iso9001NonConformities.Id, indexed
  ReportedBy       nvarchar(256)
  Description      nvarchar(2048)
  Status           nvarchar(64)
  ReportedAt       datetime2
  CreatedAt        datetime2

Iso9001CustomerFeedbacks
  Id               int               PK (identity)
  EntityId         nvarchar(512)
  CompanyId        nvarchar(256)     indexed
  CustomerId       nvarchar(256)     indexed
  Rating           int               CHECK (Rating BETWEEN 1 AND 5)
  Comments         nvarchar(4000)
  ReportedAt       datetime2
  CreatedAt        datetime2
```

---

## Email notifications

When a `CustomerFeedbackQueueMessage` is processed and `CustomerEmail` is not empty, two emails are sent automatically:

1. **Thank-you email** → sent to the customer. Shows the rating as stars (★/☆), includes comments if provided, and displays the anti-phishing code.
2. **Low-rating alert** → sent to `EmailOptions.AdminEmail` when `Rating ≤ 2`. Includes the customer details, entity ID, rating, and comments.

Emails are sent by calling a REST API (configurable via `EmailOptions.Url`). The expected endpoint is `POST {Url}/send-mail` with this payload:

```json
{
  "Subject": "string",
  "CompanyId": 1,
  "Recipients": [{ "DisplayName": "string", "Adressee": "string" }],
  "Content": "string (HTML)",
  "AntiPhishing": "string"
}
```

You can swap the email provider by replacing `FeedbackEmailService` with your own `IFeedbackEmailService` implementation and re-registering it in `DependencyContainer`.

---

## ISO 9001:2015 clause mapping

| Event type | Clause | Queue |
|---|---|---|
| Audit log | 9.1.3 — Analysis and evaluation | `iso9001-auditlogs` |
| Incident report | 8.7 — Control of nonconforming outputs | `iso9001-incidents` |
| Customer feedback | 9.1.2 — Customer satisfaction | `iso9001-feedbacks` |
| Non-conformity | 10.2 — Nonconformity and corrective action | `iso9001-nonconformities` |

---

## Deploying to Azure

1. Create an Azure Storage account and note the connection string.
2. Create an Azure SQL Database and run the EF migration against it.
3. Deploy `ISO9001Queue.Functions` to an Azure Function App (runtime: dotnet-isolated, v4).
4. Set the following Application Settings in the Function App:
   - `AzureWebJobsStorage` — Azure Storage connection string
   - `DatabaseOptions__ConnectionString` — SQL Server connection string
   - `EmailOptions__Url` — your email API base URL
   - `EmailOptions__AdminEmail` — quality admin email address
   - `EmailOptions__CompanyId` — your company ID
5. Retrieve the function key from the Azure Portal and distribute it to API consumers.

---

## Extending the system

**Add a new queue type:**
1. Add a new message record to `ISO9001Queue.Contracts`.
2. Add a constant to `Iso9001QueueNames`.
3. Add the EF entity, configuration, and table migration to `ISO9001Queue.Database.EF`.
4. Register the new data context interfaces in `DependencyContainer`.
5. Add the queue trigger function to `ISO9001Queue.Functions/QueueTriggers`.

**Swap the email provider:**
Implement `IFeedbackEmailService` and replace the registration in `DependencyContainer.cs`:

```csharp
services.AddScoped<IFeedbackEmailService, YourEmailService>();
```

---

## Dependencies

| Package | Purpose |
|---|---|
| [ISO9001.Core](https://www.nuget.org/packages/ISO9001.Core) | Domain commands, queries, DTOs, and interfaces for ISO 9001:2015 |
| `Microsoft.Azure.Functions.Worker` | Azure Functions isolated worker host |
| `Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues` | QueueTrigger binding |
| `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` | HTTP trigger binding |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server data access |
| `DigitalDoor.Reporting.Presenters.PDF` | PDF report generation |
| `Microsoft.ApplicationInsights.WorkerService` | Application Insights telemetry |

---

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
