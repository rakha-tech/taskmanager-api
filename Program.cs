using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TaskManager.Api.Data;
using TaskManager.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Serilog logs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Controllers + enum JSON string
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
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
            new string[] {}
        }
    });
});

// Database - Debugging
Console.WriteLine("--- Database Configuration Debug ---");
var envConnectionString = Environment.GetEnvironmentVariable("RAILWAY_DATABASE_URL");
var configConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine($"DEBUG: RAILWAY_DATABASE_URL env var value: '{envConnectionString}'");
Console.WriteLine($"DEBUG: DefaultConnection from config: '{configConnectionString}'");

var connectionString = envConnectionString ?? configConnectionString;

Console.WriteLine($"DEBUG: Final connection string before fix: '{connectionString}'");

// Fix format if necessary
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgresql://"))
{
    connectionString = connectionString.Replace("postgresql://", "postgres://");
    Console.WriteLine("Fixed connection string format to postgres://");
}
else
{
    Console.WriteLine("Connection string format did not require fixing or was null/empty.");
}

// Validation
if (string.IsNullOrEmpty(connectionString) || connectionString == "DefaultConnection")
{
    var errorMsg = $"CRITICAL ERROR: Connection string is invalid: '{connectionString}'. Ensure RAILWAY_DATABASE_URL environment variable is set correctly in Railway dashboard.";
    Console.WriteLine(errorMsg);
    throw new InvalidOperationException(errorMsg);
}

Console.WriteLine($"DEBUG: Final connection string after fix and validation: '{connectionString}'");
Console.WriteLine("--- End Database Configuration Debug ---");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    Console.WriteLine("DEBUG: AppDbContext is being registered with the connection string.");
    options.UseNpgsql(connectionString);
});


// Dependency Injection
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();

// JWT - Debugging
Console.WriteLine("--- JWT Configuration Debug ---");
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Key"];
Console.WriteLine($"DEBUG: JWT_SECRET env var or Jwt:Key config value length: {jwtKey?.Length ?? 0} characters");
Console.WriteLine($"DEBUG: JWT Key starts with 'JWT_SECRET': {jwtKey?.StartsWith("JWT_SECRET") ?? false}"); // Check if placeholder from config

if (string.IsNullOrEmpty(jwtKey) || jwtKey.StartsWith("JWT_SECRET")) // Assuming "JWT_SECRET" or similar placeholder
{
    var errorMsg = "CRITICAL ERROR: JWT Key is not configured correctly. Ensure JWT_SECRET environment variable is set with a strong key.";
    Console.WriteLine(errorMsg);
    throw new InvalidOperationException(errorMsg);
}
Console.WriteLine("--- End JWT Configuration Debug ---");

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, 
            ValidateAudience = false, 
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "https://taskmanager-rakha.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Swagger for dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();