using Microsoft.AspNetCore.SignalR;

namespace MoDLibrary.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinLibrarianGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Librarians");
        }

        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        public async Task JoinMembersGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Members");
        }
    }
}
