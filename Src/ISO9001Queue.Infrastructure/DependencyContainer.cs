using ISO9001.Core.Interfaces.AuditLogs;
using ISO9001.Core.Interfaces.CustomerFeedbacks;
using ISO9001.Core.Interfaces.IncidentReports;
using ISO9001.Core.Interfaces.NonConformitys;
using ISO9001Queue.Database.EF.Contexts;
using ISO9001Queue.Database.EF.Options;
using ISO9001Queue.Infrastructure.Email;
using ISO9001Queue.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ISO9001Queue.Infrastructure;

public static class DependencyContainer
{
    public static IServiceCollection AddIso9001Infrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionKey));
        services.Configure<RetentionOptions>(configuration.GetSection(RetentionOptions.SectionKey));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionKey));

        services.AddDbContext<Iso9001DbContext>();

        // Each ISO9001.Core data context interface resolves to the same scoped DbContext instance
        services.AddScoped<IWritableAuditLogDataContext>(sp => sp.GetRequiredService<Iso9001DbContext>());
        services.AddScoped<IQueryableAuditLogDataContext>(sp => sp.GetRequiredService<Iso9001DbContext>());

        services.AddScoped<IWritableIncidentReportDataContext>(sp => sp.GetRequiredService<Iso9001DbContext>());
        services.AddScoped<IQueryableIncidentReportDataContext>(sp => sp.GetRequiredService<Iso9001DbContext>());

        services.AddScoped<IWritableNonConformityDataContext>(sp => sp.GetRequiredService<Iso9001DbContext>());
        services.AddScoped<IQueryableNonConformityDataContext>(sp => sp.GetRequiredService<Iso9001DbContext>());

        services.AddScoped<IWritableCustomerFeedbackDataContext>(sp => sp.GetRequiredService<Iso9001DbContext>());
        services.AddScoped<IQueryableCustomerFeedbackDataContext>(sp => sp.GetRequiredService<Iso9001DbContext>());

        services.AddScoped<IRetentionMaintenanceContext>(sp => sp.GetRequiredService<Iso9001DbContext>());

        services.AddHttpClient(EmailSender.HttpClientName, (sp, client) =>
        {
            EmailOptions opts = sp.GetRequiredService<IOptions<EmailOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opts.Url))
                client.BaseAddress = new Uri(opts.Url);
        });
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IFeedbackEmailService, FeedbackEmailService>();
        services.AddScoped<IUserDataEmailService, UserDataEmailService>();

        return services;
    }
}
