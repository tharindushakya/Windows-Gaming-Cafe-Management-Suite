using Microsoft.AspNetCore.SignalR;

namespace GamingCafe.API.Hubs;

public class GameCafeHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task NotifyStationUpdate(int stationId, string status)
    {
        await Clients.All.SendAsync("StationUpdated", stationId, status);
    }

    public async Task NotifySessionUpdate(int sessionId, string status)
    {
        await Clients.All.SendAsync("SessionUpdated", sessionId, status);
    }

    public async Task NotifyPaymentUpdate(int transactionId, string status)
    {
        await Clients.All.SendAsync("PaymentUpdated", transactionId, status);
    }
}
