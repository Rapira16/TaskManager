using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using Scalar.AspNetCore;
using TaskManager.API.Middleware;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Application.Validators;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Repositories;
using TaskManager.Infrastructure.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// ============ SERILOG (обнови для Production) ============
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", 
        builder.Environment.IsProduction() 
            ? Serilog.Events.LogEventLevel.Warning 
            : Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", 
        builder.Environment.IsProduction() 
            ? Serilog.Events.LogEventLevel.Error 
            : Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TaskManager")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/taskmanager-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: builder.Environment.IsProduction() ? 30 : 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Запуск приложения TaskManager");


    builder.Host.UseSerilog();

    // ============ JWT AUTHENTICATION ============
    var jwtSecret = Environment.GetEnvironmentVariable("Jwt__Secret") 
        ?? builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT Secret не настроен");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = builder.Environment.IsProduction(); // ← В production только HTTPS
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // ============ CORS ============
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000",      // React
                    "http://localhost:4200",      // Angular
                    "http://localhost:5173"       // Vite
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });

        // Для production
        options.AddPolicy("Production", policy =>
        {
            policy
                .WithOrigins("https://yourdomain.com")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        // Глобальный лимит для всех endpoints
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,  // 100 запросов
                    Window = TimeSpan.FromMinutes(1)  // За 1 минуту
                }));

        // Политика для аутентификации (более строгая)
        options.AddFixedWindowLimiter("auth", options =>
        {
            options.PermitLimit = 5;  // 5 попыток входа
            options.Window = TimeSpan.FromMinutes(1);  // За минуту
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 2;
        });

        // Политика для публичных endpoints
        options.AddFixedWindowLimiter("public", options =>
        {
            options.PermitLimit = 30;
            options.Window = TimeSpan.FromMinutes(1);
        });

        // Сообщение при превышении лимита
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;  // Too Many Requests
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                message = "Слишком много запросов. Попробуйте позже.",
                retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) 
                    ? retryAfter.ToString() 
                    : "60 секунд"
            }, cancellationToken: token);
        };
    });

    // ============ БД ============
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ============ HEALTH CHECKS ============
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string not found"),
            name: "postgresql",
            tags: new[] { "db", "database" })
        .AddCheck("self", () => 
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API работает"));

    // ============ CACHING ============
    builder.Services.AddMemoryCache();

    // ============ СЕРВИСЫ ============
    builder.Services.AddScoped<ITaskRepository, TaskRepository>();
    builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();

    builder.Services.AddScoped<ITaskService, TaskService>();
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IAuthService, AuthService>();

    builder.Services.AddScoped<IJwtService, JwtService>();

    // ============ VALIDATION ============
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskDtoValidator>();

    builder.Services.AddControllers();

    // ============ RESPONSE COMPRESSION ============
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
        options.Providers.Add<BrotliCompressionProvider>();
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Fastest;
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = System.IO.Compression.CompressionLevel.Fastest;
    });

    // ============ OPENAPI / SCALAR ============
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // ============ MIDDLEWARE ============
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    app.UseResponseCompression();

    app.UseSerilogRequestLogging();
//
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Task Manager API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
//    
    if (!app.Environment.IsProduction())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors("AllowFrontend");

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseRateLimiter();

    // ============ HEALTH CHECK ENDPOINTS ============
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.ToString()
                }),
                totalDuration = report.TotalDuration.ToString()
            };
            await context.Response.WriteAsJsonAsync(response);
        }
    });

    app.MapControllers();

    Log.Information("Приложение успешно запущено");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение не удалось запустить");
}
finally
{
    Log.CloseAndFlush();
}