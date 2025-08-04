using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using backend.Data;
using backend.Services;
    
// Configure PostgreSQL to use timestamp without time zone for DateTime compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileLoggingService, FileLoggingService>();

// Configure Garmin OAuth
builder.Services.Configure<backend.Models.GarminOAuthConfig>(builder.Configuration.GetSection("GarminOAuth"));

builder.Services.AddHttpClient("GarminOAuth", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IGarminOAuthService, GarminOAuthService>();

// Add Garmin webhook services
builder.Services.AddScoped<IGarminWebhookService, GarminWebhookService>();
builder.Services.AddScoped<IGarminActivityProcessingService, GarminActivityProcessingService>();

// Add background service for retry processing
builder.Services.AddHostedService<GarminWebhookBackgroundService>();

// Add Controllers
builder.Services.AddControllers();

// Add JWT Authentication
var jwtKey = builder.Configuration["Auth:JwtKey"] ?? "DefaultJwtKey123456789012345678901234567890";
var key = Encoding.ASCII.GetBytes(jwtKey);

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

builder.Services.AddAuthorization();

// Add CORS for frontend integration
var allowedOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()); 
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

foreach (var origin in allowedOrigins)
{
    Console.WriteLine($"Allowed Origin: {origin}");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize database
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Initializing PostgreSQL database...");
    
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    logger.LogInformation("Connection string: {ConnectionString}", connectionString);
    
    // Apply any pending migrations
    logger.LogInformation("Applying database migrations...");
    await context.Database.MigrateAsync();
    
    logger.LogInformation("Database migration completed successfully.");
    
    // Seed only in development
    if (app.Environment.IsDevelopment())
    {
        var seeder = new DatabaseSeeder(context);
        await seeder.SeedAsync();
        logger.LogInformation("Database seeding completed.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while initializing the database. Application will continue but may not function correctly.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// Only use HTTPS redirection in development or when behind a reverse proxy
if (app.Environment.IsDevelopment() || !string.IsNullOrEmpty(app.Configuration["ASPNETCORE_HTTPS_PORT"]))
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// API Routes will be added here
app.MapGet("/api/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName,
    Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
})
.WithName("HealthCheck")
.WithOpenApi();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
