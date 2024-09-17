using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

public class MediaDownloadService
{
    private readonly IHubContext<MediaHub> _hubContext;

    public MediaDownloadService(IHubContext<MediaHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task<string> DownloadMedia(string url, string? format, string resolution = "best")
    {
        format ??= "mp4";
        try
        {
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");
            Directory.CreateDirectory(outputDir);

            string outputTemplate = "%(title)s.%(ext)s";
            string outputFile = Path.Combine(outputDir, outputTemplate);

            // Map the resolution string to the correct height filter for yt-dlp
            string resolutionFilter = resolution switch
            {
                "2160p" => "height<=2160",
                "1440p" => "height<=1440",
                "1080p" => "height<=1080",
                "720p" => "height<=720",
                "480p" => "height<=480",
                _ => "" // Default to no specific filter if "best" is chosen or an unsupported resolution is used
            };

            // Configure yt-dlp arguments based on selected format and resolution
            string ytDlpArgs = format switch
            {
                // For MP3 audio format
                "mp3" => $"-x --audio-format mp3 \"{url}\" -o \"{outputFile}\"",

                // For MP4 video format, ensure correct video and audio combination, applying the resolution filter only if specified
                "mp4" => !string.IsNullOrEmpty(resolutionFilter)
                    ? $"-f \"bestvideo[ext=mp4][{resolutionFilter}]+bestaudio[ext=m4a]/best[ext=mp4]\" \"{url}\" -o \"{outputFile}\""
                    : $"-f \"bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]\" \"{url}\" -o \"{outputFile}\"",

                // Default case: best available video and audio
                _ => $"-f bestvideo+bestaudio/best \"{url}\" -o \"{outputFile}\""
            };

            Console.WriteLine($"Starting download for {url} with args: {ytDlpArgs}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = ytDlpArgs,
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
                    Console.WriteLine(e.Data);
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
                    Console.WriteLine($"Error: {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            Console.WriteLine($"Download completed for {url}");

            if (process.ExitCode == 0)
            {
                var downloadedFiles = Directory.GetFiles(outputDir);
                string downloadedFilePath = downloadedFiles.FirstOrDefault() ?? "Unknown file";

                // Safely handle the file extension and determine the format type
                string fileExtension = (Path.GetExtension(downloadedFilePath) ?? string.Empty).ToLowerInvariant();
                string formatType = fileExtension switch
                {
                    ".mp3" => "MP3 (Audio)",
                    ".mp4" => "MP4 (Video)",
                    ".webm" => "WEBM (Video)", // Add more conditions as needed
                    _ => "Unknown Format"
                };

                // Log the correct format type in the result message
                return $"Downloaded successfully: {downloadedFilePath} ({formatType})";
            }
            else
            {
                return "Failed to download the media.";
            }

        }
        catch (Exception ex)
        {
            return $"An error occurred: {ex.Message}";
        }
    }

    // Extracts progress percentage from yt-dlp output
    private string ExtractProgressPercentage(string data)
    {
        var match = Regex.Match(data, @"(\d+(\.\d+)?)%");
        return match.Success ? match.Value : "0%";
    }
}
