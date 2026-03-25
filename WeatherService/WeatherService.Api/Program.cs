using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using System.Text;
using WeatherService.Api.Middleware;
using WeatherService.Api.Services;
using WeatherService.Infrastructure.ExternalProviders;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Infrastructure.Persistence;
using WeatherService.Infrastructure.Repositories;
using WeatherService.Infrastructure.Services;
using WeatherService.Model.Dtos;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/weather-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var dbPath = builder.Configuration["Database:Path"] ?? "weather.db";
    builder.Services.AddDbContext<WeatherDbContext>(i => i.UseSqlite($"Data Source={dbPath}"));

    builder.Services.AddScoped<IWeatherRepository, WeatherRepository>();
    builder.Services.AddScoped<IForecastRepository, ForecastRepository>();
    builder.Services.AddScoped<IAlertRepository, AlertRepository>();

    builder.Services.AddHttpClient<IWeatherProvider, DataGovSgProvider>()
        .AddStandardResilienceHandler(i =>
        {
            i.Retry.MaxRetryAttempts = 3;
            i.Retry.Delay = TimeSpan.FromSeconds(2);
            i.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        });

    builder.Services.AddHttpClient<IGeocodingProvider, GeoCodingProvider>()
        .AddStandardResilienceHandler(i =>
        {
            i.Retry.MaxRetryAttempts = 2;
            i.Retry.Delay = TimeSpan.FromSeconds(2);
        });
    builder.Services.AddScoped<IWeatherService>(ctx => new WeatherServices(
                    ctx.GetRequiredService<IWeatherRepository>(), ctx.GetRequiredService<IForecastRepository>(),
                    ctx.GetRequiredService<IWeatherProvider>(), ctx.GetRequiredService<IGeocodingProvider>(),
                    ctx.GetRequiredService<IExportService>(), ctx.GetRequiredService<IMemoryCache>(), 
                    ctx.GetRequiredService<ILogger<WeatherServices>>()));

    builder.Services.AddScoped<IAlertService>(ctx => new AlertService(
                    ctx.GetRequiredService<IAlertRepository>(), ctx.GetRequiredService<IWeatherRepository>(),
                    ctx.GetRequiredService<IGeocodingProvider>(), ctx.GetRequiredService<INotificationService>(),
                    ctx.GetRequiredService<ILogger<AlertService>>()));

    builder.Services.AddHttpClient<INotificationService, NotificationService>()
        .AddStandardResilienceHandler(i =>
        {
            i.Retry.MaxRetryAttempts = 2;
            i.Retry.Delay = TimeSpan.FromSeconds(2);
        });

    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

    builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddMemoryCache();

    builder.Services.AddHostedService<AlertProcessingBackgroundService>();
    builder.Services.AddHostedService<WeatherRefreshBackgroundService>();

    builder.Services.AddOptions();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();


    var jwtKey = builder.Configuration["Jwt:Key"] ?? "default_secret_key_please_change";
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(i => i.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "WeatherService",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "WeatherServiceUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        });
    builder.Services.AddAuthorization();

    var origin = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5181", "http://localhost:5173"];
    builder.Services.AddCors(i => i.AddPolicy("Default", p => p
                            .WithOrigins(origin)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .WithExposedHeaders("Content-Disposition")));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(i => { 
        i.SwaggerDoc("v1", 
            new OpenApiInfo { 
                Title = "WeatherService MicroService API", 
                Version = "v1",
                Description = "API for WeatherService Microservice. Provides endpoints for retrieving weather data, managing alerts, and exporting data.",
            });

        i.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT token"
        });

        i.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        var xmlPath = Path.Combine(AppContext.BaseDirectory, "WeatherService.xml");
        if (File.Exists(xmlPath))
        {
            i.IncludeXmlComments(xmlPath);
        }
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<WeatherDbContext>("Database");

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
        dbContext.Database.EnsureCreated();
        Log.Information($"Database Ready at DbPath: {dbPath}");
    }

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    //if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(i =>
        {
            i.SwaggerEndpoint("/swagger/v1/swagger.json", "Weather API v1");
            i.RoutePrefix = string.Empty;   // Swagger at root: http://localhost:5181
            i.DocumentTitle = "🌤 Weather API";
            i.DefaultModelsExpandDepth(-1);
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("Default");
    app.UseIpRateLimiting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Weather Microservice started");
    app.Run();
}
catch(Exception ex)
{
    Log.Fatal(ex, "Application fail to start");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }