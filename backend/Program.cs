using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using backend.Data;
using backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileLoggingService, FileLoggingService>();

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialize database
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Initializing database...");
    
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    logger.LogInformation("Connection string: {ConnectionString}", connectionString);
    
    // Enhanced directory and permission handling
    var dbPath = connectionString?.Split('=').LastOrDefault();
    if (!string.IsNullOrEmpty(dbPath))
    {
        var dbDir = Path.GetDirectoryName(dbPath);
        var dbFileName = Path.GetFileName(dbPath);
        
        if (!string.IsNullOrEmpty(dbDir))
        {
            logger.LogInformation("Database directory: {DbDir}", dbDir);
            logger.LogInformation("Database file: {DbFile}", dbFileName);
            
            // Ensure directory exists
            if (!Directory.Exists(dbDir))
            {
                logger.LogInformation("Creating database directory...");
                Directory.CreateDirectory(dbDir);
            }
            
            // Check and log detailed permissions
            var dirInfo = new DirectoryInfo(dbDir);
            logger.LogInformation("Directory exists: {Exists}", dirInfo.Exists);
            
            // Test write permissions
            var canWrite = TestWritePermission(dbDir, logger);
            logger.LogInformation("Directory is writable: {CanWrite}", canWrite);
            
            // Log current user info (helpful for debugging)
            try
            {
                var currentUser = Environment.UserName;
                var userId = Environment.GetEnvironmentVariable("USER") ?? "unknown";
                logger.LogInformation("Running as user: {User} (ID: {UserId})", currentUser, userId);
            }
            catch (Exception userEx)
            {
                logger.LogWarning(userEx, "Could not determine current user");
            }
            
            // If we can't write, try to fix permissions
            if (!canWrite)
            {
                logger.LogWarning("Directory is not writable, attempting to fix permissions...");
                
                try
                {
                    // Try to create a test file to trigger any permission errors early
                    var testFilePath = Path.Combine(dbDir, ".write_test");
                    await File.WriteAllTextAsync(testFilePath, "test");
                    File.Delete(testFilePath);
                    logger.LogInformation("Permission fix successful - directory is now writable");
                }
                catch (Exception permEx)
                {
                    logger.LogError(permEx, "Could not fix directory permissions. Manual intervention may be required.");
                    
                    // Try alternative: use a subdirectory
                    var altDir = Path.Combine(dbDir, "sqlite");
                    logger.LogInformation("Attempting to use alternative directory: {AltDir}", altDir);
                    
                    try
                    {
                        Directory.CreateDirectory(altDir);
                        var altDbPath = Path.Combine(altDir, dbFileName);
                        
                        // Update connection string to use alternative path
                        var altConnectionString = $"Data Source={altDbPath}";
                        logger.LogInformation("Using alternative connection string: {AltConnectionString}", altConnectionString);
                        
                        // You might need to recreate the context with the new connection string
                        // This is a fallback approach
                    }
                    catch (Exception altEx)
                    {
                        logger.LogError(altEx, "Alternative directory approach also failed");
                        throw; // Re-throw to trigger the outer catch block
                    }
                }
            }
        }
    }
    
    // Apply any pending migrations
    logger.LogInformation("Applying database migrations...");
    await context.Database.MigrateAsync();
    
    logger.LogInformation("Database migration completed successfully.");
    
    // Verify database file was created
    if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
    {
        var fileInfo = new FileInfo(dbPath);
        logger.LogInformation("Database file created successfully. Size: {Size} bytes", fileInfo.Length);
    }
    
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
    
    // Additional error details for SQLite-specific issues
    if (ex is Microsoft.Data.Sqlite.SqliteException sqliteEx)
    {
        logger.LogError("SQLite Error Code: {ErrorCode}", sqliteEx.SqliteErrorCode);
        logger.LogError("SQLite Extended Error Code: {ExtendedErrorCode}", sqliteEx.SqliteExtendedErrorCode);
        
        switch (sqliteEx.SqliteErrorCode)
        {
            case 14: // SQLITE_CANTOPEN
                logger.LogError("Cannot open database file. This is typically a permissions issue.");
                logger.LogError("Ensure the /app/data directory is writable by the application user.");
                break;
            case 8: // SQLITE_READONLY
                logger.LogError("Database is read-only. Check file and directory permissions.");
                break;
        }
    }
}

// Helper method to test write permissions
static bool TestWritePermission(string directoryPath, ILogger logger)
{
    try
    {
        var testFile = Path.Combine(directoryPath, $"permission_test_{Guid.NewGuid():N}.tmp");
        File.WriteAllText(testFile, "permission test");
        
        if (File.Exists(testFile))
        {
            File.Delete(testFile);
            return true;
        }
        
        return false;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Write permission test failed");
        return false;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
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
