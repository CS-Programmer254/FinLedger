using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Serilog;

using FinLedger.API.MiddleWare;
using FinLedger.Application.Behaviors;
using FinLedger.Application.Handlers;
using FinLedger.Application.Interfaces;
using FinLedger.Infrastructure.Persistence;
using FinLedger.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

#region Serilog Configuration

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/finledger-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

#endregion

try
{
    Log.Information("FinLedger API starting...");

    #region Database

    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' not found in appsettings.json");
    builder.Services.AddDbContext<PaymentsDbContext>(options =>
    {

        options.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "finledger"));


        if (builder.Environment.IsDevelopment())
        {
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        }
    });

 
    #endregion

    #region Dependency Injection

    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
    builder.Services.AddScoped<IReconciliationRepository, ReconciliationRepository>();
    builder.Services.AddScoped<IEventStore, EfEventStore>();

    #endregion

    #region MediatR + Pipeline Behaviors

    builder.Services.AddMediatR(config =>
    {
        config.RegisterServicesFromAssembly(typeof(CreatePaymentHandler).Assembly);
        config.AddOpenBehavior(typeof(LoggingBehavior<,>));
        config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });

    #endregion

    #region API Versioning

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    #endregion

    #region Controllers & OpenAPI

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "FinLedger Payment API",
            Version = "v1.0",
            Description = "Enterprise Payment Processing System (Clean Architecture & DDD)",
            Contact = new()
            {
                Name = "FinLedger Team",
                Url = new Uri("https://github.com/CS-Programmer254/FinLedger")
            }
        });
    });

    #endregion

    #region CORS

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowUI", policy =>
        {
            policy
                .WithOrigins("http://localhost:3000", "http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });

        options.AddPolicy("AllowAll", policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    #endregion

    var app = builder.Build();

    #region Middleware Pipeline

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
        Log.Information("HTTPS redirection enabled");
    }

    app.UseCors("AllowUI");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FinLedger v1");
            options.RoutePrefix = string.Empty;
        });

        Log.Information("Swagger UI enabled");
    }

    app.UseRouting();
    app.MapControllers();

    #endregion

    #region Database Migration

    Log.Information("Checking database migrations...");

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        if (dbContext.Database.GetPendingMigrations().Any())
        {
            Log.Information("Applying pending migrations...");
            dbContext.Database.Migrate();
            Log.Information("Migrations applied successfully");
        }
        else
        {
            Log.Information("Database already up to date");
        }
    }

    #endregion

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("FinLedger API shut down");
    await Log.CloseAndFlushAsync();
}
