using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vigia.Application.Workers;
using Vigia.Application.Auth;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Webhooks;
using Vigia.Application.Plugins;
using Vigia.Application.Results;
using Vigia.Application.Rules;
using Vigia.Infrastructure.Workers;
using Vigia.Infrastructure.Auth;
using Vigia.Infrastructure.Checks;
using Vigia.Infrastructure.Identity;
using Vigia.Infrastructure.Persistence;
using Vigia.Infrastructure.Plugins;
using Vigia.Infrastructure.Results;
using Vigia.Infrastructure.Webhooks;
using Vigia.Plugins;

namespace Vigia.Infrastructure;

/// <summary>
/// Registers infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds persistence, Identity, plugin loading and probe services.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<TimestampInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Vigia"));
            options.UseSnakeCaseNamingConvention();
            options.AddInterceptors(sp.GetRequiredService<TimestampInterceptor>());
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentityApiEndpoints<AppUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.Section));

        // Workers authenticate with their own scheme and are only authorized on worker endpoints.
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, WorkerAuthenticationHandler>(WorkerAuthentication.Scheme, null);
        services.AddAuthorizationBuilder()
            .AddPolicy(WorkerAuthentication.Policy, policy => policy
                .AddAuthenticationSchemes(WorkerAuthentication.Scheme)
                .RequireClaim(WorkerAuthentication.SlugClaim));
        services.AddScoped<IIdentityService, IdentityService>();

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.Section));

        services.Configure<PluginOptions>(configuration.GetSection(PluginOptions.Section));
        services.AddSingleton<PluginLoader>();
        services.AddSingleton<IPluginRegistry>(sp =>
        {
            var path = sp.GetRequiredService<IOptions<PluginOptions>>().Value.Path;
            var root = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
            return sp.GetRequiredService<PluginLoader>().Load(root);
        });

        // Per-request HttpClient logs would flood the log at probe rates; outcomes are recorded as results instead.
        services.AddHttpClient(HostCheckContext.HttpClientName).RemoveAllLoggers();
        services.AddSingleton<ICheckContext, HostCheckContext>();

        services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.Section));
        services.AddSingleton<IProbeExecutor, ProbeExecutor>();
        services.AddSingleton<IAssignmentSource, DbAssignmentSource>();
        services.Configure<AlertingOptions>(configuration.GetSection(AlertingOptions.Section));
        services.AddScoped<RuleEvaluator>();
        services.AddScoped<ResultIngestor>();
        services.AddSingleton<IResultSink, DbResultSink>();
        services.AddSingleton<IWebhookReceiptStore, DbWebhookReceiptStore>();
        services.AddMetrics();
        services.AddSingleton<WorkerMetrics>();
        services.AddSingleton<WorkerScheduler>();
        services.AddScoped<BuiltInWorkerRegistration>();
        services.AddHostedService<BuiltInWorkerService>();

        services.Configure<RetentionOptions>(configuration.GetSection(RetentionOptions.Section));
        services.AddScoped<ResultMaintenance>();
        services.AddHostedService<ResultMaintenanceService>();

        return services;
    }
}
