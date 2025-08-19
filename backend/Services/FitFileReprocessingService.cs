using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public interface IFitFileReprocessingService
{
    Task<int> ReprocessUnprocessedFitFilesAsync(int userId);
    Task<int> ReprocessUnprocessedFitFilesAsync();
    Task<int> ReprocessUserNotFoundFitFilesAsync();
}

public class FitFileReprocessingService : IFitFileReprocessingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FitFileReprocessingService> _logger;

    public FitFileReprocessingService(ApplicationDbContext context, ILogger<FitFileReprocessingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> ReprocessUnprocessedFitFilesAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || string.IsNullOrEmpty(user.ZwiftUserId))
            {
                _logger.LogWarning("User {userId} not found or has no ZwiftUserId", userId);
                return 0;
            }

            // Find unprocessed fit files for this specific user's ZwiftUserId
            var unprocessedFiles = await _context.FitFileActivities
                .Where(f => f.ZwiftUserId == user.ZwiftUserId && 
                           (f.Status == FitFileProcessingStatus.Unprocessed || 
                            f.Status == FitFileProcessingStatus.UserNotFound))
                .ToListAsync();

            int processedCount = 0;

            foreach (var fitFileActivity in unprocessedFiles)
            {
                try
                {
                    // Update user association
                    fitFileActivity.UserId = user.Id;
                    fitFileActivity.Status = FitFileProcessingStatus.Processed;
                    fitFileActivity.ProcessedAt = DateTime.UtcNow;
                    fitFileActivity.LastProcessingAttempt = DateTime.UtcNow;
                    fitFileActivity.ProcessingError = null;

                    // Process challenges
                    await ProcessChallengesForFitFileAsync(user, fitFileActivity);
                    
                    // Create Activity record
                    await CreateActivityFromFitFileAsync(user, fitFileActivity);
                    
                    processedCount++;
                    
                    _logger.LogInformation("Reprocessed fit file {fileName} for user {userId}", 
                        fitFileActivity.FileName, userId);
                }
                catch (Exception ex)
                {
                    fitFileActivity.Status = FitFileProcessingStatus.Failed;
                    fitFileActivity.ProcessingError = ex.Message;
                    fitFileActivity.LastProcessingAttempt = DateTime.UtcNow;
                    
                    _logger.LogError(ex, "Failed to reprocess fit file {fileName} for user {userId}", 
                        fitFileActivity.FileName, userId);
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Reprocessed {count} fit files for user {userId}", processedCount, userId);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing fit files for user {userId}", userId);
            return 0;
        }
    }

    public async Task<int> ReprocessUnprocessedFitFilesAsync()
    {
        try
        {
            // Find all unprocessed or user-not-found fit files that have a ZwiftUserId
            var unprocessedFiles = await _context.FitFileActivities
                .Where(f => !string.IsNullOrEmpty(f.ZwiftUserId) &&
                           (f.Status == FitFileProcessingStatus.Unprocessed || 
                            f.Status == FitFileProcessingStatus.UserNotFound))
                .ToListAsync();

            int processedCount = 0;

            foreach (var fitFileActivity in unprocessedFiles)
            {
                try
                {
                    // Try to find user by ZwiftUserId
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.ZwiftUserId == fitFileActivity.ZwiftUserId);

                    if (user != null)
                    {
                        // Update user association
                        fitFileActivity.UserId = user.Id;
                        fitFileActivity.Status = FitFileProcessingStatus.Processed;
                        fitFileActivity.ProcessedAt = DateTime.UtcNow;
                        fitFileActivity.LastProcessingAttempt = DateTime.UtcNow;
                        fitFileActivity.ProcessingError = null;

                        // Process challenges
                        await ProcessChallengesForFitFileAsync(user, fitFileActivity);
                        
                        // Create Activity record
                        await CreateActivityFromFitFileAsync(user, fitFileActivity);
                        
                        processedCount++;
                        
                        _logger.LogInformation("Reprocessed fit file {fileName} for user {userId} (ZwiftId: {zwiftUserId})", 
                            fitFileActivity.FileName, user.Id, user.ZwiftUserId);
                    }
                    else
                    {
                        // Still no user found
                        fitFileActivity.Status = FitFileProcessingStatus.UserNotFound;
                        fitFileActivity.LastProcessingAttempt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    fitFileActivity.Status = FitFileProcessingStatus.Failed;
                    fitFileActivity.ProcessingError = ex.Message;
                    fitFileActivity.LastProcessingAttempt = DateTime.UtcNow;
                    
                    _logger.LogError(ex, "Failed to reprocess fit file {fileName}", fitFileActivity.FileName);
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Reprocessed {count} fit files in bulk reprocessing", processedCount);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk fit file reprocessing");
            return 0;
        }
    }

    public async Task<int> ReprocessUserNotFoundFitFilesAsync()
    {
        try
        {
            // Find all fit files marked as UserNotFound that have a ZwiftUserId
            var userNotFoundFiles = await _context.FitFileActivities
                .Where(f => !string.IsNullOrEmpty(f.ZwiftUserId) &&
                           f.Status == FitFileProcessingStatus.UserNotFound)
                .ToListAsync();

            int processedCount = 0;

            foreach (var fitFileActivity in userNotFoundFiles)
            {
                try
                {
                    // Try to find user by ZwiftUserId
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.ZwiftUserId == fitFileActivity.ZwiftUserId);

                    if (user != null)
                    {
                        // Update user association
                        fitFileActivity.UserId = user.Id;
                        fitFileActivity.Status = FitFileProcessingStatus.Processed;
                        fitFileActivity.ProcessedAt = DateTime.UtcNow;
                        fitFileActivity.LastProcessingAttempt = DateTime.UtcNow;
                        fitFileActivity.ProcessingError = null;

                        // Process challenges
                        await ProcessChallengesForFitFileAsync(user, fitFileActivity);
                        
                        // Create Activity record
                        await CreateActivityFromFitFileAsync(user, fitFileActivity);
                        
                        processedCount++;
                        
                        _logger.LogInformation("Found and processed fit file {fileName} for new user {userId} (ZwiftId: {zwiftUserId})", 
                            fitFileActivity.FileName, user.Id, user.ZwiftUserId);
                    }
                    else
                    {
                        // Update last processing attempt but keep as UserNotFound
                        fitFileActivity.LastProcessingAttempt = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    fitFileActivity.Status = FitFileProcessingStatus.Failed;
                    fitFileActivity.ProcessingError = ex.Message;
                    fitFileActivity.LastProcessingAttempt = DateTime.UtcNow;
                    
                    _logger.LogError(ex, "Failed to reprocess user-not-found fit file {fileName}", fitFileActivity.FileName);
                }
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Reprocessed {count} user-not-found fit files", processedCount);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing user-not-found fit files");
            return 0;
        }
    }

    private async Task ProcessChallengesForFitFileAsync(User user, FitFileActivity fitFileActivity)
    {
        try
        {
            // Get user's active challenge participations
            var activeParticipations = await _context.ChallengeParticipants
                .Include(cp => cp.Challenge)
                .Where(cp => cp.UserId == user.Id && 
                           cp.Challenge.StartDate <= fitFileActivity.StartTime && 
                           cp.Challenge.EndDate >= fitFileActivity.StartTime)
                .ToListAsync();

            foreach (var participation in activeParticipations)
            {
                var challenge = participation.Challenge;
                bool progressUpdated = false;

                // Update progress based on challenge type
                switch (challenge.ChallengeType)
                {
                    case ChallengeType.Distance:
                        participation.CurrentDistance += fitFileActivity.DistanceKm;
                        progressUpdated = true;
                        break;

                    case ChallengeType.Elevation:
                        participation.CurrentElevation += fitFileActivity.ElevationGainM;
                        progressUpdated = true;
                        break;

                    case ChallengeType.Time:
                        participation.CurrentTime += fitFileActivity.DurationMinutes;
                        progressUpdated = true;
                        break;
                }

                if (progressUpdated)
                {
                    participation.LastUpdated = DateTime.UtcNow;
                    
                    // Update legacy CurrentTotal field for backward compatibility
                    participation.CurrentTotal = challenge.ChallengeType switch
                    {
                        ChallengeType.Distance => (decimal)participation.CurrentDistance,
                        ChallengeType.Elevation => (decimal)participation.CurrentElevation,
                        ChallengeType.Time => participation.CurrentTime / 60m, // Convert minutes to hours for display
                        _ => 0
                    };
                    
                    // Check if challenge is completed
                    bool isCompleted = challenge.ChallengeType switch
                    {
                        ChallengeType.Distance => participation.CurrentDistance >= challenge.TargetDistance,
                        ChallengeType.Elevation => participation.CurrentElevation >= challenge.TargetElevation,
                        ChallengeType.Time => participation.CurrentTime >= challenge.TargetTime,
                        _ => false
                    };

                    if (isCompleted && !participation.IsCompleted)
                    {
                        participation.IsCompleted = true;
                        participation.CompletedAt = DateTime.UtcNow;
                    }
                }
            }

            // Mark challenges as processed for this fit file activity
            fitFileActivity.ChallengesProcessed = true;
            fitFileActivity.ChallengesProcessedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing challenges for fit file {fileName} and user {userId}", 
                fitFileActivity.FileName, user.Id);
            throw;
        }
    }

    private async Task CreateActivityFromFitFileAsync(User user, FitFileActivity fitFileActivity)
    {
        try
        {
            // Check if an Activity record already exists for this FitFile
            var existingActivity = await _context.Activities
                .FirstOrDefaultAsync(a => a.UserId == user.Id && 
                                        a.ExternalId == fitFileActivity.FileName);
            
            if (existingActivity != null)
            {
                _logger.LogInformation("Activity already exists for FitFile: {fileName}, skipping creation", fitFileActivity.FileName);
                return;
            }

            // Create Activity record from FitFileActivity
            var activity = new backend.Models.Activity
            {
                UserId = user.Id,
                ExternalId = fitFileActivity.FileName, // Use filename as external ID to link back to FitFile
                ActivityName = fitFileActivity.ActivityName,
                ActivityType = fitFileActivity.ActivityType,
                Source = "FitFile", // Mark as coming from FitFile
                DistanceKm = fitFileActivity.DistanceKm,
                ElevationGainM = fitFileActivity.ElevationGainM,
                DurationSeconds = fitFileActivity.DurationMinutes * 60,
                StartTime = fitFileActivity.StartTime,
                EndTime = fitFileActivity.EndTime,
                ActivityDate = fitFileActivity.ActivityDate,
                AverageSpeed = fitFileActivity.AverageSpeed,
                MaxSpeed = fitFileActivity.MaxSpeed,
                AverageHeartRate = fitFileActivity.AverageHeartRate,
                MaxHeartRate = fitFileActivity.MaxHeartRate,
                AveragePower = fitFileActivity.AveragePower,
                MaxPower = fitFileActivity.MaxPower,
                AverageCadence = fitFileActivity.AverageCadence,
                CreatedAt = System.DateTime.UtcNow,
                MovingTime = fitFileActivity.DurationMinutes * 60,
                Distance = (decimal)fitFileActivity.DistanceKm,
                ElevationGain = (decimal)fitFileActivity.ElevationGainM
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created Activity record from FitFile: {fileName} for user {userId}", 
                fitFileActivity.FileName, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Activity from FitFile {fileName} for user {userId}", 
                fitFileActivity.FileName, user.Id);
            throw;
        }
    }
}