using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Marstris.Core.Communication
{
    public class Communicator
    {
        public bool IsConnected => _socket.Connected;
        public IPEndPoint LocalEndpoint { get; }
        public IPEndPoint RemoteEndoint { get; }
        private readonly UdpClient _udpClient;
        private readonly Socket _socket;
        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;
        private Task _readTask;

        public Communicator(Socket socket, StreamReader reader, StreamWriter writer, UdpClient udpClient, EndPoint remoteEp)
        {
            _socket = socket;
            _reader = reader;
            _writer = writer;
            _udpClient = udpClient;

            LocalEndpoint = (IPEndPoint) udpClient.Client.LocalEndPoint;
            RemoteEndoint = (IPEndPoint) remoteEp;
            _readTask = Task.Run(ReceiveTcpAsync);
        }

        public Task SendAsync<T>(T o)
        {
            var json = JsonSerializer.Serialize(o);
            var message = Encoding.UTF8.GetBytes(json);
            return SendAsync(message);
        }

        public async Task SendAsync(byte[] message)
        {
            await _udpClient.SendAsync(message, message.Length, RemoteEndoint);
        }

        public async Task<T> ReadAsync<T>()
        {
            var bytes = await ReceiveAsync();
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task<byte[]> ReceiveAsync()
        {
            var data = await _udpClient.ReceiveAsync();
            while (!RemoteEndoint.Equals(data.RemoteEndPoint))
            {
                Console.WriteLine($"Wrong remote. Got {data.RemoteEndPoint.Address}:{data.RemoteEndPoint.Port}, expected {RemoteEndoint.Address}:{RemoteEndoint.Port}");
                data = await _udpClient.ReceiveAsync();
            }
            return data.Buffer;
        }

        public void Disconnect()
        {
            try
            {
                Console.WriteLine("Communicator disconnecting");
                _writer.WriteLine("DISCONNECT");
                _writer.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                Console.WriteLine("Communicator disconnecting");
                await _writer.WriteLineAndFlushAsync("DISCONNECT");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task ReceiveTcpAsync()
        {
            try
            {
                while (_socket is { Connected: true })
                {
                    var message = await _reader.ReadLineAsync();
                    switch (message)
                    {
                        case "PING":
                            await _writer.WriteLineAndFlushAsync("PONG");
                            break;
                        case "PONG":
                            break;
                        case "DISCONNECT":
                            Close();
                            break;
                        default:
                            break;
                    }    
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Recieve TCP exception");
                Console.WriteLine(e);
                Close();
            }
        }

        private void Close()
        {
            Console.WriteLine("Communicator closing");
            try
            {
                _socket.Disconnect(true);
                _reader.Close();
                _writer.Close();
                _socket.Close();
                _udpClient.Close();
            }
            catch
            {
                //
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Communicator disposing");
            Close();
            _socket.Dispose();
            _reader.Dispose();
            _writer.Dispose();
            _udpClient.Dispose();
        }
    }
}