using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeskIQ.TicketSystem.Infrastructure.Data;
using DeskIQ.TicketSystem.API.Services;
using DeskIQ.TicketSystem.API.Hubs;
using DeskIQ.TicketSystem.API.Configuration;
using DeskIQ.TicketSystem.API.Middleware;
using DeskIQ.TicketSystem.Application.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

string GetRequiredSetting(string key)
{
    var value = builder.Configuration[key];
    if (string.IsNullOrWhiteSpace(value) || value.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Required configuration '{key}' is missing or invalid.");
    }

    return value;
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var defaultConnection = GetRequiredSetting("ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConnection));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = GetRequiredSetting("Jwt:Key");
var issuer = GetRequiredSetting("Jwt:Issuer");
var audience = GetRequiredSetting("Jwt:Audience");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hub/tickets"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITicketIdGenerator, TicketIdGenerator>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ITicketActivityService, TicketActivityService>();
builder.Services.AddScoped<TicketService>();

// Configuration
builder.Services.Configure<FileStorageSettings>(
    builder.Configuration.GetSection(FileStorageSettings.SectionName));

// SignalR
builder.Services.AddSignalR();

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ConfiguredCors");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<TicketHub>("/hub/tickets");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    context.Database.ExecuteSqlRaw("""
        ALTER TABLE IF EXISTS "Departments"
        ADD COLUMN IF NOT EXISTS "Code" VARCHAR(4);
    """);

    context.Database.ExecuteSqlRaw("""
        UPDATE "Departments"
        SET "Code" = CASE
            WHEN LENGTH(clean_name) >= 2 THEN LEFT(clean_name, 4)
            WHEN LENGTH(clean_name) = 1 THEN clean_name || 'X'
            ELSE 'GE'
        END
        FROM (
            SELECT
                "Id",
                UPPER(REGEXP_REPLACE(COALESCE("Name", ''), '[^A-Za-z0-9]', '', 'g')) AS clean_name
            FROM "Departments"
        ) generated
        WHERE "Departments"."Id" = generated."Id"
          AND ("Departments"."Code" IS NULL OR "Departments"."Code" = '');
    """);

    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS "TicketSequences" (
            "Id" uuid NOT NULL,
            "DepartmentId" uuid NOT NULL,
            "Year" integer NOT NULL,
            "LastValue" integer NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_TicketSequences" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_TicketSequences_Departments_DepartmentId"
                FOREIGN KEY ("DepartmentId") REFERENCES "Departments" ("Id") ON DELETE CASCADE
        );
    """);

    context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

    context.Database.ExecuteSqlRaw("""
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_TicketSequences_DepartmentId_Year"
        ON "TicketSequences" ("DepartmentId", "Year");
    """);

    context.Database.ExecuteSqlRaw("""
        INSERT INTO "TicketSequences" ("Id", "DepartmentId", "Year", "LastValue", "UpdatedAt")
        SELECT
            uuid_generate_v4(),
            t."DepartmentId",
            EXTRACT(YEAR FROM t."CreatedAt")::int,
            COUNT(*)::int,
            NOW()
        FROM "Tickets" t
        GROUP BY t."DepartmentId", EXTRACT(YEAR FROM t."CreatedAt")::int
        ON CONFLICT ("DepartmentId", "Year") DO UPDATE
        SET "LastValue" = EXCLUDED."LastValue",
            "UpdatedAt" = NOW();
    """);

    context.Database.ExecuteSqlRaw("""
        ALTER TABLE IF EXISTS "Tickets"
        ADD COLUMN IF NOT EXISTS "ParentTicketId" uuid;
    """);

    context.Database.ExecuteSqlRaw("""
        ALTER TABLE IF EXISTS "Tickets"
        ADD COLUMN IF NOT EXISTS "IsBlocked" boolean NOT NULL DEFAULT false;
    """);

    context.Database.ExecuteSqlRaw("""
        ALTER TABLE IF EXISTS "Tickets"
        ADD COLUMN IF NOT EXISTS "BlockedReason" character varying(1000);
    """);

    context.Database.ExecuteSqlRaw("""
        CREATE INDEX IF NOT EXISTS "IX_Tickets_ParentTicketId"
        ON "Tickets" ("ParentTicketId");
    """);

    context.Database.ExecuteSqlRaw("""
        CREATE INDEX IF NOT EXISTS "IX_Tickets_IsBlocked"
        ON "Tickets" ("IsBlocked");
    """);

    context.Database.ExecuteSqlRaw("""
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conname = 'FK_Tickets_Tickets_ParentTicketId'
            ) THEN
                ALTER TABLE "Tickets"
                ADD CONSTRAINT "FK_Tickets_Tickets_ParentTicketId"
                FOREIGN KEY ("ParentTicketId") REFERENCES "Tickets" ("Id") ON DELETE RESTRICT;
            END IF;
        END $$;
    """);

    context.Database.ExecuteSqlRaw("""
        ALTER TABLE IF EXISTS "TicketMessages"
        ADD COLUMN IF NOT EXISTS "ParentMessageId" uuid;
    """);

    context.Database.ExecuteSqlRaw("""
        CREATE INDEX IF NOT EXISTS "IX_TicketMessages_ParentMessageId"
        ON "TicketMessages" ("ParentMessageId");
    """);

    context.Database.ExecuteSqlRaw("""
        DO $$
        BEGIN
            IF NOT EXISTS (
                SELECT 1 FROM pg_constraint
                WHERE conname = 'FK_TicketMessages_TicketMessages_ParentMessageId'
            ) THEN
                ALTER TABLE "TicketMessages"
                ADD CONSTRAINT "FK_TicketMessages_TicketMessages_ParentMessageId"
                FOREIGN KEY ("ParentMessageId") REFERENCES "TicketMessages" ("Id") ON DELETE CASCADE;
            END IF;
        END $$;
    """);
}

try
{
    Log.Information("Starting DeskIQ Ticket System API");
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
