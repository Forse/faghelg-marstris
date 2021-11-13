using System.Threading.Tasks;
using Marstris.Core.Communication;
using Microsoft.AspNetCore.SignalR;

namespace Marstris.Client.Web
{
    public class GameHub : Hub
    {
        private readonly GameClient gameClient;

        public GameHub(GameClient gameClient)
        {
            this.gameClient = gameClient;
            this.gameClient.StateUpdated = (gameState) =>
            {
                Clients.All.SendAsync("ReceiveMessage", gameState);
            };
        }
        
        public async Task Connected()
        {
            var gameData = gameClient.Connect("localhost", CommunicationConstants.TcpPort, Context.ConnectionAborted);
            await Clients.All.SendAsync("Connected", gameData);
        }
    }
}
