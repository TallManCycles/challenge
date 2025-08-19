using Microsoft.AspNetCore.Mvc;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FitFilesController : ControllerBase
{
    private readonly ILogger<FitFilesController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IFitFileQueue _fitFileQueue;
    private readonly IFitFileReprocessingService _reprocessingService;

    public FitFilesController(
        ILogger<FitFilesController> logger, 
        IConfiguration configuration,
        IFitFileQueue fitFileQueue,
        IFitFileReprocessingService reprocessingService)
    {
        _logger = logger;
        _configuration = configuration;
        _fitFileQueue = fitFileQueue;
        _reprocessingService = reprocessingService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFitFile(IFormFile file)
    {
        try
        {
            // Validate secret key
            if (!Request.Headers.TryGetValue("X-API-Secret", out var secretKey))
            {
                return Unauthorized("API secret key is required");
            }

            var expectedSecretKey = _configuration["FitFileUpload:SecretKey"];
            if (string.IsNullOrEmpty(expectedSecretKey) || secretKey != expectedSecretKey)
            {
                return Unauthorized("Invalid API secret key");
            }

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            if (!file.FileName.EndsWith(".fit", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Only .fit files are allowed");
            }

            // Read the file into a byte array
            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            // Generate a filename for logging and identification purposes
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{file.FileName}";

            _logger.LogInformation("Fit file received: {fileName} ({fileSize} bytes)", 
                fileName, file.Length);

            // Queue the file content for processing
            _fitFileQueue.QueueFileForProcessing(fileContent, fileName);

            return Ok(new 
            { 
                message = "File uploaded successfully and queued for processing", 
                fileName = fileName,
                fileSize = file.Length,
                uploadTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading fit file");
            return StatusCode(500, "An error occurred while uploading the file");
        }
    }

    [HttpGet("list")]
    public IActionResult ListFitFiles([FromHeader(Name = "X-API-Secret")] string? secretKey)
    {
        _logger.LogWarning("The ListFitFiles endpoint is deprecated as files are no longer stored on the server.");
        try
        {
            // Validate secret key
            var expectedSecretKey = _configuration["FitFileUpload:SecretKey"];
            if (string.IsNullOrEmpty(expectedSecretKey) || secretKey != expectedSecretKey)
            {
                return Unauthorized("Invalid API secret key");
            }

            // This endpoint is now deprecated as we no longer store files on disk.
            // Returning an empty list to avoid breaking clients that might still use it.
            return Ok(new { files = new string[0] });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing fit files");
            return StatusCode(500, "An error occurred while listing files");
        }
    }

    [HttpPost("reprocess-all")]
    public async Task<IActionResult> ReprocessAllFitFiles([FromHeader(Name = "X-API-Secret")] string? secretKey)
    {
        try
        {
            // Validate secret key
            var expectedSecretKey = _configuration["FitFileUpload:SecretKey"];
            if (string.IsNullOrEmpty(expectedSecretKey) || secretKey != expectedSecretKey)
            {
                return Unauthorized("Invalid API secret key");
            }

            _logger.LogInformation("Starting reprocessing of all unprocessed fit files");

            // Reprocess all unprocessed fit files
            var processedCount = await _reprocessingService.ReprocessUnprocessedFitFilesAsync();

            _logger.LogInformation("Completed reprocessing. Processed {count} fit files", processedCount);

            return Ok(new 
            { 
                message = "Fit file reprocessing completed successfully",
                processedCount = processedCount,
                processedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reprocessing fit files");
            return StatusCode(500, "An error occurred while reprocessing fit files");
        }
    }
}