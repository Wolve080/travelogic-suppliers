using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Travelogic.Suppliers.Api.Infrastructure;
using Travelogic.Suppliers.Application;
using Travelogic.Suppliers.Infrastructure;
using Travelogic.Suppliers.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration)
    .AddObservability(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);

if (app.Configuration.GetValue("OpenApi:Enabled", defaultValue: app.Environment.IsDevelopment()))
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference(options => options
        .WithTitle("Travelogic Supplier Service")
        .AddDocument("v1")
        .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Fetch));
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.MapControllers();

// Liveness: the process is up. Readiness: it can reach its database and should receive traffic.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false, ResponseWriter = HealthCheckResponseWriter.WriteAsync });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready"), ResponseWriter = HealthCheckResponseWriter.WriteAsync });

await app.Services.InitialiseDatabaseAsync();
await app.RunAsync();

/// <summary>Exposed for WebApplicationFactory in the integration tests.</summary>
public partial class Program;
