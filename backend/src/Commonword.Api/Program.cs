using System.Text.Json.Serialization;
using Commonword.Infrastructure;
using Commonword.Modules.Puzzles.Api;
using Commonword.Modules.Solving.Api;
using Commonword.Modules.Telemetry.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Services.AddInfrastructure(
    builder.Configuration,
    PuzzlesModule.Assembly,
    SolvingModule.Assembly,
    TelemetryModule.Assembly);

builder.Services.AddPuzzlesModule();
builder.Services.AddSolvingModule();
builder.Services.AddTelemetryModule();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("default");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithTags("System");

app.MapPuzzlesEndpoints();
app.MapSolvingEndpoints();
app.MapTelemetryEndpoints();

app.Run();
