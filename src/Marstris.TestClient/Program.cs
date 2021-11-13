// See https://aka.ms/new-console-template for more information

using System;
using System.Threading;
using Marstris.Core.Communication;

Console.WriteLine("Marstris TestClient");

try
{
    var host = args.Length == 0 ? "localhost" : args[0];
    using var source = new CancellationTokenSource();
    Console.CancelKeyPress += (o, e) =>
    {
        e.Cancel = true;
        source.Cancel();
    };

    using var client = new GameClient();
    client.Connect(host, CommunicationConstants.TcpPort, source.Token);
    await client.SendAsync(new CommandMessage());
    return 0;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Bye");
    return 0;
}
catch (Exception e)
{
    Console.WriteLine(e);
    return -1;
}