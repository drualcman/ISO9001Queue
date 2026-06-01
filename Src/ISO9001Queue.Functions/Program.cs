using ISO9001Queue.Infrastructure;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.ConfigureFunctionsApplicationInsights();

builder.Services.AddIso9001Infrastructure(builder.Configuration);
builder.Services.AddReportingPresenterPdfServices();

builder.Services.AddAuditLogCoreServices();
builder.Services.AddIncidentReportCoreServices();
builder.Services.AddNonConformityCoreServices();
builder.Services.AddCustomerFeedbackCoreServices();
builder.Services.AddQualityDashboardCoreServices();
builder.Services.AddAuditReportCoreServices();
builder.Services.AddAuditEventCoreServices();

await builder.Build().RunAsync();
