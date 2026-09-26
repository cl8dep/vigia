using Vigia.Api.Errors;
using Vigia.Application.Plugins;
using Vigia.Infrastructure;
using Vigia.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
builder.Services.AddExceptionHandler<ProblemExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

// Load plugins at startup instead of on first request, so failures show up in the startup log.
app.Services.GetRequiredService<IPluginRegistry>();
await app.Services.MigrateDatabaseAsync();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapControllers();

await app.RunAsync();

/// <summary>Entry point, exposed for integration tests.</summary>
public partial class Program;
