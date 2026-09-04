var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Json(new HealthStatus("Healthy", "Kpmg.Web")));

app.MapGet("/api/about", () => Results.Json(new AboutInfo(
    Organization: "KPMG",
    Application: "Kpmg.Web",
    Environment: app.Environment.EnvironmentName)));

app.Map("/error", () => Results.Problem("An unexpected error occurred."));

app.Run();

/// <summary>Health check payload returned by <c>GET /health</c>.</summary>
public record HealthStatus(string Status, string Application);

/// <summary>Application metadata returned by <c>GET /api/about</c>.</summary>
public record AboutInfo(string Organization, string Application, string Environment);

/// <summary>Exposed so the integration test project can bootstrap the application host.</summary>
public partial class Program;
