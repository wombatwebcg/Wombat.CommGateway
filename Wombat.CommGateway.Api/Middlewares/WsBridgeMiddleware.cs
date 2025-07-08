using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Wombat.CommGateway.API;
using Wombat.CommGateway.Application.Services.DataCollection;

namespace Wombat.CommGateway.Api.Middlewares
{

    //[AutoInject<WsBridgeMiddleware>(ServiceLifetime = ServiceLifetime.Scoped)]
    public class WsBridgeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISubscriptionManager _subs;
        private readonly ILogger<WsBridgeMiddleware> _logger;
        private static readonly ConcurrentDictionary<WebSocket, string> _sockets = new();

        public WsBridgeMiddleware(RequestDelegate next,
                                  ISubscriptionManager subs,
                                  ILogger<WsBridgeMiddleware> logger)
        {
            _next = next;
            _subs = subs;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx, IDataPushBus pushBus)
        {
            if (!ctx.WebSockets.IsWebSocketRequest ||
                !ctx.Request.Path.Equals("/ws/bridge", StringComparison.OrdinalIgnoreCase))
            {
                await _next(ctx);
                return;
            }

            using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
            var connKey = Guid.NewGuid().ToString("N");      // 给 ws 侧生成独立 ID
            _sockets.TryAdd(ws, connKey);
            _logger.LogInformation("WebSocket桥接连接已建立 - 路径: /ws/bridge, 连接ID: {Key}", connKey);

            // ① 监听 DataPush 事件 → 推送给该 ws
            await using var _ = pushBus.RegisterAsync(async msg =>
            {
                var json = JsonSerializer.Serialize(msg);
                _logger.LogDebug("WebSocket桥接收到数据推送: {Json}", json);
                await SendTextAsync(ws, json);
            });

            // ② 读客户端命令
            await ReceiveLoop(ws, connKey);
        }

        private async Task ReceiveLoop(WebSocket ws, string key)
        {
            var buffer = new byte[4 * 1024];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var cmd = JsonSerializer.Deserialize<WsCommand>(json);

                switch (cmd?.Action)
                {
                    case "subscribe": _subs.Add(key, cmd.Id); break;
                    case "unsubscribe": _subs.Remove(key, cmd.Id); break;
                    case "ping": await SendTextAsync(ws, "{\"type\":\"pong\"}"); break;
                    default: await SendTextAsync(ws, "{\"error\":\"unknown action\"}"); break;
                }
            }

            _subs.RemoveConnection(key);
            _sockets.TryRemove(ws, out _);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }

        private static Task SendTextAsync(WebSocket ws, string text, CancellationToken ct = default)
            => ws.SendAsync(Encoding.UTF8.GetBytes(text),
                            WebSocketMessageType.Text, true, ct);

        private sealed record WsCommand(string Action, int Id);
    }
}
