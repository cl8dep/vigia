using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vigia.Application.Agents;
using Vigia.Application.Auth;
using Vigia.Application.Common.Interfaces;
using Vigia.Application.Webhooks;
using Vigia.Application.Plugins;
using Vigia.Infrastructure.Agents;
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

        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.Section));
        services.AddSingleton<IProbeExecutor, ProbeExecutor>();
        services.AddSingleton<IAssignmentSource, DbAssignmentSource>();
        services.AddSingleton<IResultSink, DbResultSink>();
        services.AddSingleton<IWebhookReceiptStore, DbWebhookReceiptStore>();
        services.AddMetrics();
        services.AddSingleton<AgentMetrics>();
        services.AddSingleton<AgentScheduler>();
        services.AddHostedService<BuiltInAgentService>();

        services.Configure<RetentionOptions>(configuration.GetSection(RetentionOptions.Section));
        services.AddScoped<ResultMaintenance>();
        services.AddHostedService<ResultMaintenanceService>();

        return services;
    }
}
