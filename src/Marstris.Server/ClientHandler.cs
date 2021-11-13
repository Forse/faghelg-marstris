using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Marstris.Core;
using Marstris.Core.Communication;
using Microsoft.Xna.Framework.Input;

namespace Marstris.Server
{
    public class ClientHandler : IDisposable
    {
        public int Id { get; }
        private readonly GameServer _server;
        
        private readonly Communicator _communicator;

        private Task _task;

        public ClientHandler(int id, GameServer server, Communicator communicator)
        {
            Id = id;
            _server = server;
            _communicator = communicator;
        }

        public void Start(CancellationToken cancellationToken)
        {
            _task = Task.Run(() => HandleCommands(cancellationToken), cancellationToken);
        }

        private async Task HandleCommands(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _communicator.ReadAsync<CommandMessage>();
                    switch (message.Keys)
                    {
                        case Keys.Right:
                            _server.MovePlayerRight(Id);
                            break;
                        case Keys.Left:
                            _server.MovePlayerLeft(Id);
                            break;
                        case Keys.Down:
                            _server.MovePlayerDown(Id);
                            break;
                        case Keys.A:
                            _server.MoveBusLeft(Id);
                            break;
                        case Keys.D:
                            _server.MoveBusRight(Id);
                            break;
                        case Keys.Q:
                            _server.FireLeft(Id);
                            break;
                        case Keys.E:
                            _server.FireRight(Id);
                            break;
                    }

                    //Console.WriteLine($"got {JsonSerializer.Serialize(message)}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        public void Dispose()
        {
            Console.WriteLine("ClientHandler disposing");
            _communicator.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task UpdateStateAsync(GameState state)
        {
            try
            {
                await _communicator.SendAsync(state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task<ClientHandler> HandshakeAsync(PlayerData playerData, Socket socket, GameServer gameServer, GameLayout layout)
        {
            var stream = new NetworkStream(socket);
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream);
            var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any,0));
            
            Console.WriteLine("Send playerdata");
            await writer.WriteAndFlushAsync(playerData);
            
            Console.WriteLine("Send layut");
            await writer.WriteAndFlushAsync(layout);
            
            
            var localEndpoint = udpClient.GetLocalIpEndpoint();
            var udpMessage = new UdpMessage
            {
                Port = localEndpoint.Port
            };
            Console.WriteLine($"Send server udp port: {udpMessage.Port}");
            await writer.WriteAndFlushAsync(udpMessage);

            var clientUdpEndpoint = await reader.ReadAsync<UdpMessage>();
            Console.WriteLine($"Got client udp port: {clientUdpEndpoint.Port}");

            var remoteEndpoint = new IPEndPoint(socket.GetRemoteIpEndpoint().Address, clientUdpEndpoint.Port);
            var communicator = new Communicator(socket, reader, writer, udpClient, remoteEndpoint);

            Console.WriteLine("Handshake done");
            return new ClientHandler(playerData.Id, gameServer, communicator);
        }
    }
}