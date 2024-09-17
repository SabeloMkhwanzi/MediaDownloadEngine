using Microsoft.AspNetCore.SignalR;

public class MediaHub : Hub
{
    // This method can be called from the client to notify the server about events
    public async Task NotifyConversionProgress(string progress)
    {
        // Broadcast the progress to all connected clients
        await Clients.All.SendAsync("ReceiveProgress", progress);
    }

    // Add additional methods to communicate with clients as needed
}
