# MediaDownloadEngine

**MediaDownloadEngine** is a powerful tool designed to download and convert media from popular streaming platforms like YouTube. It supports downloading videos and audio in various formats and resolutions, including MP4, MP3, and more.

## Features

- **Video and Audio Downloads**: Supports downloading videos in MP4 format and audio in MP3 format, providing flexibility for different media needs.
- **Playlist Support**: Download entire playlists effortlessly, maintaining the order and organization of videos.
- **Multiple Resolutions**: Choose from various resolutions, including 2160p, 1440p, 1080p, 720p, and 480p, to match your preferred quality settings.
- **Custom Download Path**: Option to download media to the user's local Downloads folder or specify a custom download location.
- **Progress Feedback**: Real-time download progress updates using SignalR, allowing users to monitor their download status seamlessly.
- **Error Handling**: Robust error handling for unsupported formats, network issues, permission errors, and merging errors, enhancing the reliability of the downloading process.
  .

## Getting Started

## Prerequisites

Before starting, ensure you have the following tools installed on your system:

- **.NET 8.0 SDK** or later: Required to run the backend of the MediaDownloadEngine.

  - [Download .NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

- **yt-dlp**: A command-line video downloader required for media download operations.

  - Installation:

    ```bash
    # For Windows:
    python -m pip install -U yt-dlp

    # For macOS (using Homebrew):
    brew install yt-dlp

    # For Linux:
    sudo curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
    sudo chmod a+rx /usr/local/bin/yt-dlp
    ```

- **FFmpeg**: Required for merging video and audio streams and format conversion tasks.

  - Installation:

    ```bash
    # For Windows:
    # Download FFmpeg from https://ffmpeg.org/download.html and add it to your system's PATH.

    # For macOS (using Homebrew):
    brew install ffmpeg

    # For Linux:
    sudo apt update
    sudo apt install ffmpeg
    ```

### Installation

1. **Clone the repository**:

   ```bash
   git clone https://github.com/SabeloMkhwanzi/MediaDownloadEngine.git
   cd MediaDownloadEngine

   ```

2. **Restore .NET dependencies:**

   ```bash
   dotnet restore

   ```

3. **Run the backend:**:

   ```bash
     dotnet run
   ```

### Usage

- Open the application in your browser.
- Enter the media URL you want to download.
- Select the desired format (MP4, MP3).
- Choose the resolution for videos.
- Click "Download" to start the process.

### Download Media

To download media, make a POST request to the `/download` endpoint with the media URL, format, and resolution.

**Example Request:**

```http
POST http://localhost:5177/api/media/download
Content-Type: application/json

{
  "url": "https://www.youtube.com/watch?v=NrO0CJCbYLA",
  "format": "mp4",
  "resolution": "2160p" // Example resolution (can be "best", "2160p", "1440p", "1080p", "720p", "480p")
}

```

### Convert Media Format

To convert a downloaded file to another format, make a POST request to the /convert endpoint with the input file path and desired output format.

**Example Request:**

```http
POST http://localhost:5177/api/media/convert
Content-Type: application/json

{
  "inputFilePath": "C:\\Users\\USER\\Desktop\\C# Ptojects\\MediaDownloaderAPI\\Downloads\\Rust in 100 Seconds.webm",
  "outputFormat": "mp3"
}
```

### Error Handling

- If an error occurs during the download or conversion, the application will log the error and provide a descriptive message.
- Retry options are available on the frontend or via API calls.

### License

This project is licensed under the MIT License.
