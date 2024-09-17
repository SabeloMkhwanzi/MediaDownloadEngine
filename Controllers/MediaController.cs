using MediaDownloaderAPI.Models;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly MediaDownloadService _downloadService;
    private readonly MediaConvertService _convertService;

    public MediaController(MediaDownloadService downloadService, MediaConvertService convertService)
    {
        _downloadService = downloadService;
        _convertService = convertService;
    }

    [HttpPost("download")]
    public async Task<IActionResult> DownloadMedia([FromBody] DownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("URL is required.");
        }

        request.Format ??= "mp4";

        var result = await _downloadService.DownloadMedia(request.Url, request.Format);
        return Ok(new { message = "Download initiated. Check the console for progress.", result });
    }

    [HttpPost("convert")]
    public async Task<IActionResult> ConvertMedia([FromBody] ConvertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InputFilePath))
        {
            return BadRequest("Input file path is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OutputFormat))
        {
            return BadRequest("Output format is required.");
        }

        var result = await _convertService.ConvertMedia(request.InputFilePath, request.OutputFormat);
        return Ok(new { message = "Conversion initiated. Check the console for progress.", result });
    }
}
