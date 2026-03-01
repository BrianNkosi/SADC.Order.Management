using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SADC.Order.Management.Api;
using SADC.Order.Management.Api.Middleware;
using SADC.Order.Management.Application;
using SADC.Order.Management.Infrastructure;
using SADC.Order.Management.Infrastructure.Diagnostics;
using SADC.Order.Management.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

    // Microsoft Entra ID (Azure AD) JWT authentication
    if (builder.Environment.IsDevelopment() &&
        builder.Configuration["AzureAd:TenantId"] == "your-tenant-id")
    {
        // Dev mode without real Azure AD — allow all requests with a fake identity
        builder.Services.AddAuthentication("DevScheme")
            .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevScheme", null);
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Orders.Read", policy => policy.RequireAssertion(_ => true))
            .AddPolicy("Orders.Write", policy => policy.RequireAssertion(_ => true))
            .AddPolicy("Orders.Admin", policy => policy.RequireAssertion(_ => true));
    }
    else
    {
        builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

        // Authorization policies
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("Orders.Read", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("roles", "Orders.Read", "Orders.Write", "Orders.Admin"))
            .AddPolicy("Orders.Write", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("roles", "Orders.Write", "Orders.Admin"))
            .AddPolicy("Orders.Admin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("roles", "Orders.Admin"));
    }

    // Application & Infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // OpenTelemetry — Tracing & Metrics
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(TelemetryConstants.ServiceName))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(TelemetryConstants.ServiceName)
            .AddConsoleExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter(TelemetryConstants.ServiceName)
            .AddConsoleExporter());

    // Controllers
    builder.Services.AddControllers();

    // OpenAPI / Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "SADC Order Management API",
            Version = "v1",
            Description = "Order management system for SADC regional trade"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    });

    // Rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 10;
        });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<OrderManagementDbContext>("database");

    // CORS for React frontend
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                    builder.Configuration.GetValue<string>("Frontend:Url") ?? "http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SADC Order Management v1"));
    }

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "0");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
        context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
        await next();
    });

    app.UseHttpsRedirection();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
    app.UseCors("AllowFrontend");
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers().RequireRateLimiting("fixed");
    app.MapHealthChecks("/healthz").AllowAnonymous();
    app.MapHealthChecks("/readiness").AllowAnonymous();

    // Apply migrations in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();
        await db.Database.MigrateAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the Program class accessible for integration tests
public partial class Program { }
