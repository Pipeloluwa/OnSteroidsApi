using OnSteroidsApi.Domain.Constants.Enums;
using OnSteroidsApi.WebApi.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──────────────────────────────────────────────
builder.AddKestrelConfig();
builder.AddSerilogConfig();
builder.AddControllerConfig();
builder.AddSwaggerConfig();
builder.AddProjectCors();

// ── Dependency Injection ───────────────────────────────────────
builder.Services.AddProjectValidators();
builder.Services.AddProjectServices();

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddlewareExtensions();
app.UseCors(nameof(CorsEnum._allowFrontend));
app.UseAuthorization();
app.MapControllers();

app.Run();
