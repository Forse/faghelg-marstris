using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Marstris.Core.Communication
{
    public class GameClient : IDisposable
    {
        private PlayerData _playerData;
        
        private Task _updateTask;
        private Communicator _communicator;
        
        public Action<GameState> StateUpdated { get; set; } = _ => { };

        public GameData Connect(string host, int port, CancellationToken cancellationToken)
        {
            cancellationToken.Register(Abort);
            Console.WriteLine($"Connecting to {host}:{port}");
            var entry = Dns.GetHostEntry(host);
            var ipAddress = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            socket.Connect(ipAddress, port);
            var stream = new NetworkStream(socket);
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream);
            
            _playerData = reader.Read<PlayerData>();
            Console.WriteLine("Got playerdata");

            var layout = reader.Read<GameLayout>();
            Console.WriteLine("Got layout");
            
            var remoteUdpMessage = reader.Read<UdpMessage>();
            Console.WriteLine($"Got server udp port: {remoteUdpMessage.Port}");
            
            var serverUdp = new IPEndPoint(ipAddress, remoteUdpMessage.Port);
            var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any,0));

            var clientUdp = new UdpMessage
            {
                Port = udpClient.GetLocalIpEndpoint().Port
            };
            
            Console.WriteLine($"Send client udp port: {clientUdp.Port}");
            writer.WriteAndFlush(clientUdp);
            Console.WriteLine("Client udp port sent");
            
            _communicator = new Communicator(socket, reader, writer, udpClient, serverUdp);
            Console.WriteLine("Communicator created");
            Console.WriteLine("Handshake done");
            _updateTask = ListenForUpdatesAsync(cancellationToken);
            return new GameData
            {
                PlayerData = _playerData,
                Layout = layout
            };
        }

        private async Task ListenForUpdatesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var state = await _communicator.ReadAsync<GameState>();
                StateUpdated(state);
            }
        }

        public async Task SendAsync(CommandMessage message)
        {
            await _communicator.SendAsync(message);
        }

        private void Abort()
        {
            _communicator.Disconnect();
        }

        public void Dispose()
        {
            _communicator.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}