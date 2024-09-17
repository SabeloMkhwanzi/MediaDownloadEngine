namespace MediaDownloaderAPI.Models
{
    public class DownloadRequest
    {
        public string? Url { get; set; }
        public string? Format { get; set; }
        public string? Resolution { get; set; }
    }
}