using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

public class MediaConvertService
{
    private readonly IHubContext<MediaHub> _hubContext;

    public MediaConvertService(IHubContext<MediaHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // Method to convert downloaded media to another format using FFmpeg
    public async Task<string> ConvertMedia(string inputFilePath, string outputFormat)
    {
        try
        {
            string outputFilePath = Path.ChangeExtension(inputFilePath, outputFormat);
            string ffmpegArgs = $"-i \"{inputFilePath}\" \"{outputFilePath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Task to handle output and error streams
            var outputTask = Task.Run(() => ReadStreamAsync(process.StandardOutput));
            var errorTask = Task.Run(() => ParseProgressAsync(process.StandardError));

            // Wait for the process and progress tasks to complete
            await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync());

            if (process.ExitCode == 0)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveProgress", "Conversion completed successfully.");
                Console.WriteLine($"Converted successfully: {outputFilePath}");
                return $"Converted successfully: {outputFilePath}";
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveProgress", "Failed to convert the media.");
                Console.WriteLine("Failed to convert the media.");
                return "Failed to convert the media.";
            }
        }
        catch (Exception ex)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveProgress", $"An error occurred: {ex.Message}");
            Console.WriteLine($"An error occurred during conversion: {ex.Message}");
            return $"An error occurred during conversion: {ex.Message}";
        }
    }

    private async Task ReadStreamAsync(StreamReader streamReader)
    {
        while (!streamReader.EndOfStream)
        {
            var line = await streamReader.ReadLineAsync();
            if (!string.IsNullOrEmpty(line))
            {
                await _hubContext.Clients.All.SendAsync("ReceiveProgress", line);
                Console.WriteLine(line);
            }
        }
    }

    private async Task ParseProgressAsync(StreamReader errorReader)
    {
        string durationPattern = @"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d+)";
        TimeSpan totalDuration = TimeSpan.Zero;

        while (!errorReader.EndOfStream)
        {
            var line = await errorReader.ReadLineAsync();

            // Check if line is not null before performing any operations on it
            if (!string.IsNullOrEmpty(line))
            {
                // Parse total duration from FFmpeg output
                if (totalDuration == TimeSpan.Zero && Regex.IsMatch(line, durationPattern))
                {
                    var match = Regex.Match(line, durationPattern);
                    if (match.Success)
                    {
                        totalDuration = new TimeSpan(
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value),
                            int.Parse(match.Groups[3].Value)
                        ).Add(TimeSpan.FromMilliseconds(int.Parse(match.Groups[4].Value)));
                    }
                }

                // Parse progress output
                if (line.Contains("Progress: "))
                {
                    string progressPattern = @"Progress: (\d+\.\d+)%";
                    var progressMatch = Regex.Match(line, progressPattern);
                    if (progressMatch.Success)
                    {
                        string progress = progressMatch.Groups[1].Value;
                        string progressMessage = $"Progress: {progress}%";
                        await _hubContext.Clients.All.SendAsync("ReceiveProgress", progressMessage);
                        Console.WriteLine(progressMessage);
                    }
                }
            }
        }
    }

}
