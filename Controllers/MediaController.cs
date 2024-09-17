using MediaDownloaderAPI.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")] // This means the route is api/media
public class MediaController : ControllerBase
{
    private readonly MediaDownloadService _downloadService;
    private readonly MediaConvertService _convertService;

    // Inject both MediaDownloadService and MediaConvertService
    public MediaController(MediaDownloadService downloadService, MediaConvertService convertService)
    {
        _downloadService = downloadService;
        _convertService = convertService;
    }

    // Endpoint to handle media download requests
    [HttpPost("download")] // Full path: api/media/download
    public async Task<IActionResult> DownloadMedia([FromBody] DownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("URL is required.");
        }

        request.Format ??= "mp4";

        // Use the MediaDownloadService to download media
        var result = await _downloadService.DownloadMedia(request.Url, request.Format);
        return Ok(new { message = "Download initiated. Check the console for progress.", result });
    }

    // Endpoint to handle media conversion requests
    [HttpPost("convert")] // Full path: api/media/convert
    public async Task<IActionResult> ConvertMedia([FromBody] ConvertRequest request)
    {
        // Validate the input data
        if (string.IsNullOrWhiteSpace(request.InputFilePath))
        {
            return BadRequest("Input file path is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OutputFormat))
        {
            return BadRequest("Output format is required.");
        }

        // Use the MediaConvertService to convert media
        var result = await _convertService.ConvertMedia(request.InputFilePath, request.OutputFormat);
        return Ok(new { message = "Conversion initiated. Check the console for progress.", result });
    }
}
