using System;
using System.Threading;
using Marstris.Core.Communication;
using Marstris.Server;

try
{
    Console.WriteLine("Marstris Server v0.1");
    using var source = new CancellationTokenSource();
    Console.CancelKeyPress += (o, e) =>
    {
        e.Cancel = true;
        source.Cancel();
    };

    var numberOfPlayers = args.Length > 0 && int.TryParse(args[0], out var v) && v >= 1 ? v : 2;

    using var server = new GameServer(CommunicationConstants.TcpPort, numberOfPlayers);
    await server.StartAsync(source.Token);
    return 0;
}
catch (OperationCanceledException)
{
    return 0;
}
catch (Exception e)
{
    Console.WriteLine("Oh noes!");
    Console.WriteLine(e);
    return -1;
}
finally
{
    Console.WriteLine("Bye!");
}