using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Threading.Tasks;
using System.IO; // For Path management

public class MediaDownloadService
{
    private readonly IHubContext<MediaHub> _hubContext;
    private readonly ILogger<MediaDownloadService> _logger;

    public MediaDownloadService(IHubContext<MediaHub> hubContext, ILogger<MediaDownloadService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<string> DownloadMedia(string url, string? format, string resolution = "best", string? customPath = null)
    {
        format ??= "mp4";

        // Determine the download path: user-specified or default to the user's Downloads folder
        string outputDir = string.IsNullOrWhiteSpace(customPath)
            ? GetUserDownloadsFolder()
            : customPath;

        Directory.CreateDirectory(outputDir);

        string outputTemplate = "%(playlist_index)s - %(title)s.%(ext)s"; // Adjusted template for playlists
        string outputFile = Path.Combine(outputDir, outputTemplate);

        string resolutionFilter = resolution switch
        {
            "2160p" => "height<=2160",
            "1440p" => "height<=1440",
            "1080p" => "height<=1080",
            "720p" => "height<=720",
            "480p" => "height<=480",
            _ => ""
        };

        bool isPlaylist = IsPlaylistUrl(url);

        string ytDlpArgs = format switch
        {
            "mp3" => isPlaylist
                ? $"-x --audio-format mp3 --yes-playlist \"{url}\" -o \"{outputFile}\""
                : $"-x --audio-format mp3 \"{url}\" -o \"{outputFile}\"",

            "mp4" => !string.IsNullOrEmpty(resolutionFilter)
                ? isPlaylist
                    ? $"-f \"bestvideo[ext=mp4][{resolutionFilter}]+bestaudio[ext=m4a]/best[ext=mp4]\" --yes-playlist \"{url}\" -o \"{outputFile}\""
                    : $"-f \"bestvideo[ext=mp4][{resolutionFilter}]+bestaudio[ext=m4a]/best[ext=mp4]\" \"{url}\" -o \"{outputFile}\""
                : isPlaylist
                    ? $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]\" --yes-playlist \"{url}\" -o \"{outputFile}\""
                    : $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]\" \"{url}\" -o \"{outputFile}\"",

            _ => isPlaylist
                ? $"-f bestvideo+bestaudio/best --yes-playlist \"{url}\" -o \"{outputFile}\""
                : $"-f bestvideo+bestaudio/best \"{url}\" -o \"{outputFile}\""
        };

        try
        {
            var result = await ExecuteDownloadProcess(ytDlpArgs, outputDir);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DownloadMedia method.");
            return $"An error occurred: {ex.Message}. Please try again later.";
        }
    }

    private bool IsPlaylistUrl(string url)
    {
        return url.Contains("playlist") || url.Contains("list=");
    }

    private async Task<string> ExecuteDownloadProcess(string arguments, string outputDir)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.OutputDataReceived += async (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogInformation(e.Data);
                if (e.Data.Contains("%"))
                {
                    string progress = ExtractProgressPercentage(e.Data);
                    await _hubContext.Clients.All.SendAsync("ReceiveProgress", progress);
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogError("Error during download: {Error}", e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var processTimeout = Task.Delay(TimeSpan.FromMinutes(10));
            var processTask = process.WaitForExitAsync();

            if (await Task.WhenAny(processTask, processTimeout) == processTimeout)
            {
                process.Kill();
                _logger.LogWarning("Process timed out and was terminated.");
                return "Download process timed out. Please try again.";
            }

            if (process.ExitCode == 0)
            {
                var downloadedFiles = Directory.GetFiles(outputDir);
                if (downloadedFiles.Length > 1)
                {
                    _logger.LogInformation("Playlist downloaded with {Count} items.", downloadedFiles.Length);
                    return $"Playlist downloaded successfully with {downloadedFiles.Length} items.";
                }

                string downloadedFilePath = downloadedFiles.FirstOrDefault() ?? "Unknown file";
                string fileExtension = (Path.GetExtension(downloadedFilePath) ?? string.Empty).ToLowerInvariant();
                string formatType = fileExtension switch
                {
                    ".mp3" => "MP3 (Audio)",
                    ".mp4" => "MP4 (Video)",
                    ".webm" => "WEBM (Video)",
                    _ => "Unknown Format"
                };

                return $"Downloaded successfully: {downloadedFilePath} ({formatType})";
            }
            else
            {
                _logger.LogWarning("Process exited with code {ExitCode}.", process.ExitCode);
                return "Failed to download the media. Please try again.";
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during process start.");
            return "Failed to start the download process. Please ensure yt-dlp is correctly configured.";
        }
        catch (Win32Exception ex)
        {
            _logger.LogError(ex, "Win32 error during process start.");
            return "Error starting the download. Please check permissions and ensure yt-dlp is installed.";
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied during download.");
            return "Access denied: unable to start the download. Please check your permissions.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during download.");
            return $"An unexpected error occurred: {ex.Message}. Please try again.";
        }
    }

    private string ExtractProgressPercentage(string data)
    {
        var match = Regex.Match(data, @"(\d+(\.\d+)?)%");
        return match.Success ? match.Value : "0%";
    }

    private string GetUserDownloadsFolder()
    {
        // Default path to the current user's Downloads folder
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, "Downloads");
    }
}
