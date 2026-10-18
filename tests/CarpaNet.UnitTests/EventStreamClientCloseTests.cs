using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using CarpaNet.EventStream;
using Xunit;

namespace CarpaNet.UnitTests;

public class EventStreamClientCloseTests
{
    // Frame: CBOR header {"op": 1, "t": "#info"} followed by an empty CBOR map payload.
    private static readonly byte[] InfoFrame =
    [
        0xA2, 0x62, 0x6F, 0x70, 0x01, 0x61, 0x74, 0x65, 0x23, 0x69, 0x6E, 0x66, 0x6F,
        0xA0,
    ];

    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(20);

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    // Accepts one connection, sends one frame, then hands the server socket to the test.
    private static (HttpListener Listener, Uri Uri, Task<WebSocket> Accepted) StartServer()
    {
        var port = FreePort();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        var accepted = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            var socketContext = await context.AcceptWebSocketAsync(null);
            await socketContext.WebSocket.SendAsync(InfoFrame, WebSocketMessageType.Binary, true, CancellationToken.None);
            return socketContext.WebSocket;
        });
        return (listener, new Uri($"ws://localhost:{port}"), accepted);
    }

    // What the server saw after the client finished: a close frame, or the connection dropping without one.
    private static async Task<string> ReadClientEndingAsync(WebSocket server)
    {
        var buffer = new byte[1024];
        try
        {
            var result = await server.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return "close frame";
            }

            return "data";
        }
        catch (WebSocketException)
        {
            return "dropped";
        }
    }

    [Fact]
    public async Task SubscribeRawAsync_ServerNeverAnswersTheCloseFrame_DisposeStillCompletes()
    {
        var (listener, uri, accepted) = StartServer();
        using var release = new CancellationTokenSource();
        var server = Task.Run(async () =>
        {
            await accepted;
            try
            {
                await Task.Delay(Timeout.Infinite, release.Token);
            }
            catch (OperationCanceledException)
            {
            }
        });

        using var client = new EventStreamClient(uri);
        var consume = Task.Run(async () =>
        {
            await foreach (var frame in client.SubscribeRawAsync("com.atproto.sync.subscribeRepos"))
            {
                Assert.Equal("#info", frame.Header.MessageType);
                break;
            }
        });

        var finished = await Task.WhenAny(consume, Task.Delay(Limit));
        release.Cancel();
        listener.Stop();

        Assert.Same(consume, finished);
        await consume;
        await server;
    }

    [Fact]
    public async Task SubscribeRawAsync_ConsumerStops_SendsACloseFrame()
    {
        var (listener, uri, accepted) = StartServer();
        using var client = new EventStreamClient(uri);

        await foreach (var frame in client.SubscribeRawAsync("com.atproto.sync.subscribeRepos"))
        {
            break;
        }

        var ending = ReadClientEndingAsync(await accepted);
        Assert.Same(ending, await Task.WhenAny(ending, Task.Delay(Limit)));
        listener.Stop();

        Assert.Equal("close frame", await ending);
    }

    [Fact]
    public async Task SubscribeRawAsync_CallerCancelled_AbortsWithoutWaitingToClose()
    {
        var (listener, uri, accepted) = StartServer();
        using var client = new EventStreamClient(uri);
        using var cts = new CancellationTokenSource();

        await foreach (var frame in client.SubscribeRawAsync("com.atproto.sync.subscribeRepos", cancellationToken: cts.Token))
        {
            cts.Cancel();
            break;
        }

        var ending = ReadClientEndingAsync(await accepted);
        Assert.Same(ending, await Task.WhenAny(ending, Task.Delay(Limit)));
        listener.Stop();

        Assert.Equal("dropped", await ending);
    }

    [Fact]
    public async Task SubscribeRawAsync_ServerStartsTheClose_ClientAnswersIt()
    {
        var (listener, uri, accepted) = StartServer();
        using var client = new EventStreamClient(uri);
        var serverClose = Task.Run(async () =>
        {
            var server = await accepted;
            using var timeout = new CancellationTokenSource(Limit);
            // The full handshake: completes only when the client sends its own close frame back.
            await server.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", timeout.Token);
            return server.State;
        });

        var frames = 0;
        await foreach (var frame in client.SubscribeRawAsync("com.atproto.sync.subscribeRepos"))
        {
            frames++;
        }

        var state = await serverClose;
        listener.Stop();

        Assert.Equal(1, frames);
        Assert.Equal(WebSocketState.Closed, state);
    }

    [Fact]
    public void CloseTimeout_DefaultsToFiveSecondsAndCanBeChanged()
    {
        using var client = new EventStreamClient(new Uri("ws://localhost"));
        Assert.Equal(TimeSpan.FromSeconds(5), client.CloseTimeout);

        client.CloseTimeout = TimeSpan.FromMilliseconds(250);

        Assert.Equal(TimeSpan.FromMilliseconds(250), client.CloseTimeout);
    }
}
