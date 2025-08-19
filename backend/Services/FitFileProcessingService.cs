using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Dynastream.Fit;

namespace backend.Services;

public interface IFitFileProcessingService
{
    Task<bool> ProcessFitFileAsync(byte[] fileContent, string fileName);
}

public class FitFileProcessingService : IFitFileProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FitFileProcessingService> _logger;

    public FitFileProcessingService(ApplicationDbContext context, ILogger<FitFileProcessingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ProcessFitFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            _logger.LogInformation("Processing FIT file: {fileName}", fileName);

            // Check if file already exists
            var existingFitFile = await _context.FitFileActivities
                .FirstOrDefaultAsync(f => f.FileName == fileName);
            
            if (existingFitFile != null)
            {
                _logger.LogInformation("FIT file already exists: {fileName}, skipping", fileName);
                return true;
            }

            // Parse the FIT file
            var fitData = await ParseFitFileAsync(fileContent, fileName);
            if (fitData == null)
            {
                _logger.LogWarning("Failed to parse FIT file: {fileName}", fileName);
                return false;
            }

            // Find the user by ZwiftUserId
            User? user = null;
            FitFileProcessingStatus status = FitFileProcessingStatus.Unprocessed;
            
            if (!string.IsNullOrEmpty(fitData.ZwiftUserId))
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.ZwiftUserId == fitData.ZwiftUserId);
                
                if (user == null)
                {
                    status = FitFileProcessingStatus.UserNotFound;
                    _logger.LogWarning("User not found for ZwiftUserId: {zwiftUserId} in file: {fileName}", 
                        fitData.ZwiftUserId, fileName);
                }
                else
                {
                    status = FitFileProcessingStatus.Processed;
                }
            }
            else
            {
                _logger.LogWarning("No ZwiftUserId found in FIT file: {fileName}", fileName);
            }

            // Create FitFileActivity record
            var fitFileActivity = new FitFileActivity
            {
                UserId = user?.Id,
                FileName = fileName,
                ZwiftUserId = fitData.ZwiftUserId,
                ActivityName = fitData.WorkoutName ?? $"Zwift Activity - {fitData.ActivityStartTime:yyyy-MM-dd HH:mm}",
                ActivityType = fitData.ActivityType,
                StartTime = fitData.ActivityStartTime,
                EndTime = fitData.ActivityEndTime,
                ActivityDate = fitData.ActivityStartTime.Date,
                DistanceKm = fitData.TotalDistanceMeters / 1000.0,
                ElevationGainM = fitData.TotalElevationGainMeters,
                DurationMinutes = (int)fitData.TotalTime.TotalMinutes,
                AverageSpeed = fitData.AverageSpeed,
                MaxSpeed = fitData.MaxSpeed,
                AverageHeartRate = fitData.AverageHeartRate,
                MaxHeartRate = fitData.MaxHeartRate,
                AveragePower = fitData.AveragePower,
                MaxPower = fitData.MaxPower,
                AverageCadence = fitData.AverageCadence,
                Status = status,
                ProcessedAt = status == FitFileProcessingStatus.Processed ? System.DateTime.UtcNow : null,
                LastProcessingAttempt = System.DateTime.UtcNow
            };

            _context.FitFileActivities.Add(fitFileActivity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved FIT file activity: {fileName} with status: {status}", fileName, status);

            // Process against challenges if user was found
            if (user != null && status == FitFileProcessingStatus.Processed)
            {
                await ProcessChallengesForFitFileAsync(user, fitFileActivity);
                await CreateActivityFromFitFileAsync(user, fitFileActivity);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing FIT file: {fileName}", fileName);
            return false;
        }
    }

    private Task<FitFileData?> ParseFitFileAsync(byte[] fileContent, string fileName)
    {
        try
        {
            _logger.LogInformation("Parsing FIT file with Garmin SDK: {fileName}", fileName);

            var fitData = new FitFileData
            {
                FileName = fileName,
                ActivityType = "cycling" // Default for Zwift
            };

            // Variables to collect data
            var recordMessages = new List<RecordMesg>();
            var sessionMessage = default(SessionMesg);
            var activityMessage = default(ActivityMesg);
            var fileIdMessage = default(FileIdMesg);
            var deviceInfoMessages = new List<DeviceInfoMesg>();

            // Decode the FIT file
            using (var fitSource = new MemoryStream(fileContent))
            {
                // Create the Decode Object
                var decode = new Decode();

                // Check that this is a FIT file
                if (!decode.IsFIT(fitSource))
                {
                    _logger.LogWarning("File is not a valid FIT file: {fileName}", fileName);
                    return Task.FromResult<FitFileData?>(null);
                }

                _logger.LogDebug("Decoding FIT file: {fileName}", fileName);
                if (!decode.CheckIntegrity(fitSource))
                {
                    _logger.LogWarning("FIT file integrity check failed: {fileName}", fileName);
                    // Continue anyway - some files may still be parseable
                }

                // Create the Message Broadcaster Object
                var mesgBroadcaster = new MesgBroadcaster();

                // Connect the Decode and Message Broadcaster Objects
                decode.MesgEvent += mesgBroadcaster.OnMesg;

                // Set up message event handlers
                mesgBroadcaster.RecordMesgEvent += (sender, e) => 
                {
                    var record = e.mesg as RecordMesg;
                    if (record != null) recordMessages.Add(record);
                };
                mesgBroadcaster.SessionMesgEvent += (sender, e) => sessionMessage = e.mesg as SessionMesg;
                mesgBroadcaster.ActivityMesgEvent += (sender, e) => activityMessage = e.mesg as ActivityMesg;
                mesgBroadcaster.FileIdMesgEvent += (sender, e) => fileIdMessage = e.mesg as FileIdMesg;
                mesgBroadcaster.DeviceInfoMesgEvent += (sender, e) => 
                {
                    var deviceInfo = e.mesg as DeviceInfoMesg;
                    if (deviceInfo != null) deviceInfoMessages.Add(deviceInfo);
                };

                // Reset stream position and decode
                fitSource.Position = 0;
                bool readOK = decode.Read(fitSource);
                
                if (!readOK)
                {
                    _logger.LogWarning("Failed to decode FIT file: {fileName}", fileName);
                    return Task.FromResult<FitFileData?>(null);
                }

                _logger.LogDebug("Successfully decoded FIT file: {fileName}", fileName);
            }

            // Extract basic session information
            if (sessionMessage != null)
            {
                fitData.ActivityStartTime = sessionMessage.GetStartTime()?.GetDateTime() ?? System.DateTime.UtcNow;
                fitData.TotalTime = TimeSpan.FromSeconds(sessionMessage.GetTotalElapsedTime() ?? 0);
                fitData.ActivityEndTime = fitData.ActivityStartTime.Add(fitData.TotalTime);
                fitData.TotalDistanceMeters = sessionMessage.GetTotalDistance() ?? 0;
                fitData.TotalElevationGainMeters = sessionMessage.GetTotalAscent() ?? 0;
                fitData.AverageSpeed = (sessionMessage.GetAvgSpeed() ?? 0) * 3.6; // Convert m/s to km/h
                fitData.MaxSpeed = (sessionMessage.GetMaxSpeed() ?? 0) * 3.6; // Convert m/s to km/h
                fitData.AverageHeartRate = sessionMessage.GetAvgHeartRate();
                fitData.MaxHeartRate = sessionMessage.GetMaxHeartRate();
                fitData.AveragePower = sessionMessage.GetAvgPower();
                fitData.MaxPower = sessionMessage.GetMaxPower();
                fitData.AverageCadence = sessionMessage.GetAvgCadence();

                _logger.LogDebug("Session data - Distance: {distance}m, Duration: {duration}, Elevation: {elevation}m", 
                    fitData.TotalDistanceMeters, fitData.TotalTime, fitData.TotalElevationGainMeters);
            }
            else
            {
                _logger.LogWarning("No session message found in FIT file: {fileName}", fileName);
            }

            // Extract activity information
            if (activityMessage != null)
            {
                // Activity message can provide additional metadata
                var timestamp = activityMessage.GetTimestamp()?.GetDateTime();
                if (timestamp.HasValue && fitData.ActivityStartTime == System.DateTime.MinValue)
                {
                    fitData.ActivityStartTime = timestamp.Value;
                }
            }

            // Try to extract Zwift user ID from the file ID or device info
            fitData.ZwiftUserId = ExtractZwiftUserIdFromFitData(fileIdMessage, deviceInfoMessages, fileName);

            // Extract workout name from filename as fallback
            if (string.IsNullOrEmpty(fitData.WorkoutName))
            {
                fitData.WorkoutName = ExtractWorkoutNameFromFile(fileName);
            }

            // Validate we have minimum required data
            if (fitData.ActivityStartTime == System.DateTime.MinValue)
            {
                // Cannot use File.GetCreationTimeUtc anymore, so we'll have to rely on the FIT data or current time
                if(sessionMessage == null)
                {
                    fitData.ActivityStartTime = System.DateTime.UtcNow;
                }
            }

            _logger.LogInformation("Successfully parsed FIT file: {fileName} - Distance: {distance}km, Duration: {duration}, User: {userId}", 
                fileName, fitData.TotalDistanceMeters / 1000.0, fitData.TotalTime, fitData.ZwiftUserId ?? "Unknown");

            return Task.FromResult<FitFileData?>(fitData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing FIT file with Garmin SDK: {fileName}", fileName);
            return Task.FromResult<FitFileData?>(null);
        }
    }

    private string? ExtractZwiftUserIdFromFitData(FileIdMesg? fileIdMessage, List<DeviceInfoMesg> deviceInfoMessages, string fileName)
    {
        try
        {
            // Method 1: Check file ID message for user data
            if (fileIdMessage != null)
            {
                // Some FIT files store user ID in file ID serial number
                var serialNumber = fileIdMessage.GetSerialNumber();
                if (serialNumber.HasValue && serialNumber > 0)
                {
                    _logger.LogDebug("Found serial number in FIT file: {serialNumber}", serialNumber);
                    // For Zwift files, the serial number might be related to user ID
                    return serialNumber.ToString();
                }
            }

            // Method 2: Check device info messages
            foreach (var deviceInfo in deviceInfoMessages)
            {
                var serialNumber = deviceInfo.GetSerialNumber();
                if (serialNumber.HasValue && serialNumber > 0)
                {
                    _logger.LogDebug("Found device serial number: {serialNumber}", serialNumber);
                    return serialNumber.ToString();
                }
            }

            // Method 3: Fallback to filename extraction
            return ExtractZwiftUserIdFromFileName(fileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting Zwift user ID from FIT data");
            return ExtractZwiftUserIdFromFileName(fileName);
        }
    }

    private string? ExtractZwiftUserIdFromFileName(string fileName)
    {
        try
        {
            // Method 1: Check if filename contains user ID pattern
            // Zwift files often have patterns like: "2024-01-15-12-34-56-userid-Some_Workout.fit"
            var parts = fileName.Replace(".fit", "").Split('-', '_');
            
            // Look for a part that looks like a user ID (numeric or specific pattern)
            foreach (var part in parts)
            {
                if (part.Length >= 6 && part.All(c => char.IsDigit(c)))
                {
                    return part;
                }
            }

            // Method 2: For demonstration, we'll look for a specific pattern in the filename
            // In a real FIT parser, you would extract this from the FIT file's user data fields
            if (fileName.Contains("zwift", StringComparison.OrdinalIgnoreCase))
            {
                // Extract number patterns that might be user IDs
                var numbers = System.Text.RegularExpressions.Regex.Matches(fileName, @"\d{6,}")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Value)
                    .ToList();
                
                if (numbers.Any())
                {
                    return numbers.First();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private string? ExtractWorkoutNameFromFile(string fileName)
    {
        try
        {
            // Extract workout name from filename
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            
            // Remove date/time patterns and common prefixes
            var cleaned = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, 
                @"^\d{4}-\d{2}-\d{2}[-_]\d{2}[-_]\d{2}[-_]\d{2}[-_]?", "");
            
            // Remove user ID patterns
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\d{6,}[-_]?", "");
            
            // Replace underscores and dashes with spaces
            cleaned = cleaned.Replace('_', ' ').Replace('-', ' ');
            
            // Clean up multiple spaces
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
            
            return string.IsNullOrEmpty(cleaned) ? null : cleaned;
        }
        catch
        {
            return null;
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
                        _logger.LogInformation("Updated distance progress for user {userId} in challenge {challengeId}: +{distance}km", 
                            user.Id, challenge.Id, fitFileActivity.DistanceKm);
                        break;

                    case ChallengeType.Elevation:
                        participation.CurrentElevation += fitFileActivity.ElevationGainM;
                        progressUpdated = true;
                        _logger.LogInformation("Updated elevation progress for user {userId} in challenge {challengeId}: +{elevation}m", 
                            user.Id, challenge.Id, fitFileActivity.ElevationGainM);
                        break;

                    case ChallengeType.Time:
                        participation.CurrentTime += fitFileActivity.DurationMinutes;
                        progressUpdated = true;
                        _logger.LogInformation("Updated time progress for user {userId} in challenge {challengeId}: +{time}min", 
                            user.Id, challenge.Id, fitFileActivity.DurationMinutes);
                        break;
                }

                if (progressUpdated)
                {
                    participation.LastUpdated = System.DateTime.UtcNow;
                    
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
                        participation.CompletedAt = System.DateTime.UtcNow;
                        _logger.LogInformation("User {userId} completed challenge {challengeId}!", 
                            user.Id, challenge.Id);
                    }
                }
            }

            // Mark challenges as processed for this fit file activity
            fitFileActivity.ChallengesProcessed = true;
            fitFileActivity.ChallengesProcessedAt = System.DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing challenges for user {userId} and fit file {fileName}", user.Id, fitFileActivity.FileName);
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
                DurationMinutes = fitFileActivity.DurationMinutes,
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
                CreatedAt = System.DateTime.UtcNow
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
        }
    }
}