using System.Reflection;
using System.Text;
using BankingSystem.config;
using BankingSystem.exception;
using BankingSystem.mapper;
using BankingSystem.repository;
using BankingSystem.security;
using BankingSystem.service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// Load .env configuration if present (traverses parent directories)
try
{
    DotNetEnv.Env.TraversePath().Load();
}
catch
{
    // Ignore if not present or cannot traverse
}

var builder = WebApplication.CreateBuilder(args);

// 1. Structured Logging with Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/banking_system-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// 2. DI Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(typeof(AutoMapperProfile));

// Options Pattern
builder.Services.Configure<JwtOptions>(options =>
{
    builder.Configuration.GetSection(JwtOptions.SectionName).Bind(options);
    var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
    if (!string.IsNullOrWhiteSpace(envSecret)) options.SecretKey = envSecret;
});
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection(OtpOptions.SectionName));

// Database Context
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
});

// Repositories & Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<,>), typeof(GenericRepository<,>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

// Security Services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// Application Services
builder.Services.AddScoped<ISmsSender, SmsSender>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddHostedService<OtpCleanupService>();

// JWT Authentication
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var envJwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
if (!string.IsNullOrWhiteSpace(envJwtSecret))
{
    jwtOptions.SecretKey = envJwtSecret;
}
if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
{
    jwtOptions.SecretKey = "EnterpriseBankingSystemUltraSecureSecretKeyForJwt2026!LongEnoughForSha256";
}
var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "Teller"));
});

// Swagger UI with JWT Bearer
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Banking System Web API",
        Version = "v1",
        Description = "Enterprise-Grade Core Banking System Web API.",
        Contact = new OpenApiContact
        {
            Name = "ស្ងោរជ្រក់ជើងត្រី Team",
            Email = "yimlemeng069@gmail.com",
            Url = new Uri("https://github.com/Lemeng")
        }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token format: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// 3. HTTP Pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Banking System API v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database Migration & Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Automated database migration skipped on startup. Run 'dotnet ef database update' manually.");
    }
}

app.Run();

public partial class Program { }
