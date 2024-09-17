using MediaDownloaderAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR(); // Register SignalR service
builder.Services.AddScoped<MediaDownloadService>();
builder.Services.AddScoped<MediaConvertService>();


builder.Services.AddCustomCors();

var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


app.UseCustomCors();
app.MapControllers();


app.MapHub<MediaHub>("/mediaHub");

app.Run();
