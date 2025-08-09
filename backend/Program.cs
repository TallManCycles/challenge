using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Amazon.SimpleEmail;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using backend.Data;
using backend.Services;
    
// Configure PostgreSQL to use timestamp without time zone for DateTime compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileLoggingService, FileLoggingService>();

// Configure Email Services - Check if AWS credentials are provided
var hasAwsCredentials = !string.IsNullOrEmpty(builder.Configuration["AWS:AccessKey"]) && 
                       !string.IsNullOrEmpty(builder.Configuration["AWS:SecretKey"]);

if (hasAwsCredentials)
{
    // Configure AWS SES when credentials are available
    Console.WriteLine("AWS credentials found - configuring AWS SES for email service");
    var awsOptions = builder.Configuration.GetAWSOptions();
    awsOptions.Credentials = new BasicAWSCredentials(
        builder.Configuration["AWS:AccessKey"], 
        builder.Configuration["AWS:SecretKey"]);
    
    builder.Services.AddDefaultAWSOptions(awsOptions);
    builder.Services.AddAWSService<IAmazonSimpleEmailService>();
    builder.Services.AddScoped<IEmailService, AwsSesEmailService>();
    Console.WriteLine($"AWS SES configured for region: {awsOptions.Region}");
}
else
{
    // Use Mock service when AWS credentials are not provided
    Console.WriteLine("No AWS credentials found - using Mock email service");
    builder.Services.AddScoped<IEmailService, MockEmailService>();
}
builder.Services.AddScoped<IChallengeNotificationService, ChallengeNotificationService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();

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

// Add Garmin daily activity fetch service
builder.Services.AddScoped<IGarminDailyActivityFetchService, GarminDailyActivityFetchService>();

// Add background services
builder.Services.AddHostedService<GarminWebhookBackgroundService>();
builder.Services.AddHostedService<GarminDailyActivityBackgroundService>();

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
Console.WriteLine("Starting database initialization...");
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    Console.WriteLine("Initializing PostgreSQL database...");
    
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"Connection string: {connectionString}");
    
    // Apply any pending migrations
    Console.WriteLine("Applying database migrations...");
    await context.Database.MigrateAsync();
    
    Console.WriteLine("Database migration completed successfully.");
    
    // Seed only in development
    if (app.Environment.IsDevelopment())
    {
        var seeder = new DatabaseSeeder(context);
        await seeder.SeedAsync();
        Console.WriteLine("Database seeding completed.");
    }
    
    // Now try to initialize file logging service
    try 
    {
        var logger = scope.ServiceProvider.GetRequiredService<IFileLoggingService>();
        await logger.LogInfoAsync("Application startup completed successfully.");
    }
    catch (Exception logEx)
    {
        Console.WriteLine($"Warning: File logging service initialization failed: {logEx.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred while initializing the database. Application will continue but may not function correctly. Error: {ex}");
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
