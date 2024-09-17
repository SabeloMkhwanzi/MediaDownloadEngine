using MediaDownloaderAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSignalR(); // Register SignalR service
builder.Services.AddScoped<MediaDownloadService>();
builder.Services.AddScoped<MediaConvertService>();

// Add Custom CORS policy
builder.Services.AddCustomCors();

var app = builder.Build();

// Disable HTTPS redirection in development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // Only use HTTPS redirection in non-development environments
}

// Use Custom CORS middleware
app.UseCustomCors();

app.MapControllers();

// Map SignalR Hub route
app.MapHub<MediaHub>("/mediaHub");

app.Run();
