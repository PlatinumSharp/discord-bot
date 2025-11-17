using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DiscordBot
{
    public class DiscordWebSocketClient(string botToken)
    {
        private readonly ClientWebSocket _webSocket = new();
        private CancellationTokenSource? _cancellationTokenSource;
        private Timer? _heartbeatTimer;
        private int? _heartbeatInterval;
        private int? _lastSequence;
        private string? _sessionId;
        private readonly ConcurrentQueue<string> _messageQueue = new();

        // Discord Gateway URLとインテント
        private const string GatewayUrl = "wss://gateway.discord.gg/?v=10&encoding=json";
        private const int Intents = 513; // GUILDS | GUILD_MESSAGES

        public async Task ConnectAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                Console.WriteLine("Connecting to Discord Gateway...");
                await _webSocket.ConnectAsync(new Uri(GatewayUrl), _cancellationTokenSource.Token);
                Console.WriteLine("Connected to Discord Gateway");

                // 受信ループとメッセージ処理を開始
                _ = Task.Run(ReceiveLoop);
                _ = Task.Run(ProcessMessageQueue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
                throw;
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            var messageBuilder = new StringBuilder();

            try
            {
                while (_webSocket.State == WebSocketState.Open && _cancellationTokenSource is { Token.IsCancellationRequested: false })
                {
                    WebSocketReceiveResult result;
                    messageBuilder.Clear();

                    do
                    {
                        result = await _webSocket.ReceiveAsync(buffer, _cancellationTokenSource.Token);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var text = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                            Console.WriteLine(text);
                            messageBuilder.Append(text);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await DisconnectAsync();
                            return;
                        }
                    }
                    while (!result.EndOfMessage);

                    if (messageBuilder.Length > 0)
                    {
                        _messageQueue.Enqueue(messageBuilder.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex.Message}");
            }
        }

        private async Task ProcessMessageQueue()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_messageQueue.TryDequeue(out string message))
                {
                    await HandleMessage(message);
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        }

        private async Task HandleMessage(string message)
        {
            try
            {
                using var document = JsonDocument.Parse(message);
                var root = document.RootElement;

                // OPコードを取得
                var opCode = root.GetProperty("op").GetInt32();

                // シーケンス番号を更新
                if (root.TryGetProperty("s", out var sequenceElement) && sequenceElement.ValueKind != JsonValueKind.Null)
                {
                    _lastSequence = sequenceElement.GetInt32();
                }

                switch (opCode)
                {
                    case 0: // Dispatch Event
                        await HandleDispatchEvent(root);
                        break;
                    case 1: // Heartbeat Request
                        await SendHeartbeatAsync();
                        break;
                    case 7: // Reconnect
                        Console.WriteLine("Received reconnect request");
                        await ReconnectAsync();
                        break;
                    case 9: // Invalid Session
                        Console.WriteLine("Invalid session, reconnecting...");
                        await Task.Delay(1000 + new Random().Next(4000)); // 1-5秒待機
                        await ConnectAsync();
                        break;
                    case 10: // Hello
                        await HandleHello(root);
                        break;
                    case 11: // Heartbeat ACK
                        Console.WriteLine("Heartbeat acknowledged");
                        break;
                    default:
                        Console.WriteLine($"Unknown opcode: {opCode}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Message handling error: {ex.Message}");
            }
        }

        private async Task HandleHello(JsonElement root)
        {
            var data = root.GetProperty("d");
            _heartbeatInterval = data.GetProperty("heartbeat_interval").GetInt32();

            Console.WriteLine($"Received Hello. Heartbeat interval: {_heartbeatInterval}ms");

            // ハートビートタイマーを開始
            StartHeartbeat();

            // Identifyメッセージを送信
            await SendIdentifyAsync();
        }

        private async Task HandleDispatchEvent(JsonElement root)
        {
            var eventName = root.GetProperty("t").GetString();
            var data = root.GetProperty("d");

            switch (eventName)
            {
                case "READY":
                    _sessionId = data.GetProperty("session_id").GetString();
                    var user = data.GetProperty("user");
                    var username = user.GetProperty("username").GetString();
                    Console.WriteLine($"Bot ready! Logged in as {username}");
                    Console.WriteLine($"Session ID: {_sessionId}");
                    break;

                case "MESSAGE_CREATE":
                    await HandleMessageCreate(data);
                    break;

                case "GUILD_CREATE":
                    var guildName = data.GetProperty("name").GetString();
                    Console.WriteLine($"Joined guild: {guildName}");
                    break;

                default:
                    Console.WriteLine($"Received event: {eventName}");
                    break;
            }
        }

        private async Task HandleMessageCreate(JsonElement data)
        {
            // メッセージ内容を取得
            var content = data.GetProperty("content").GetString();
            var author = data.GetProperty("author");

            // Botのメッセージは無視
            if (author.TryGetProperty("bot", out var botElement) && botElement.GetBoolean())
            {
                return;
            }

            var username = author.GetProperty("username").GetString();
            var channelId = data.GetProperty("channel_id").GetString();

            Console.WriteLine($"[MESSAGE] {username}: {content}");

            // pingコマンドに応答
            if (content.ToLower() == "!ping")
            {
                await SendMessageAsync(channelId, "Pong! 🏓");
            }
        }

        private async Task SendMessageAsync(string channelId, string content)
        {
            // Discord REST APIを使用してメッセージを送信
            // 注: 実際の実装ではHttpClientを使用してREST APIにリクエストを送信する必要があります
            Console.WriteLine($"Would send message to channel {channelId}: {content}");
            // ここにREST API実装を追加
        }

        private void StartHeartbeat()
        {
            if (_heartbeatInterval.HasValue)
            {
                _heartbeatTimer?.Dispose();

                // ランダムなジッターを追加
                var jitter = new Random().Next(0, (int)(_heartbeatInterval.Value * 0.1));

                _heartbeatTimer = new Timer(async _ => await SendHeartbeatAsync(),
                    null,
                    _heartbeatInterval.Value - jitter,
                    _heartbeatInterval.Value);
            }
        }

        private async Task SendHeartbeatAsync()
        {
            var heartbeat = new
            {
                op = 1,
                d = _lastSequence
            };

            await SendAsync(JsonSerializer.Serialize(heartbeat));
            Console.WriteLine("Heartbeat sent");
        }

        private async Task SendIdentifyAsync()
        {
            var identify = new
            {
                op = 2,
                d = new
                {
                    token = botToken,
                    intents = Intents,
                    properties = new
                    {
                        os = "windows",
                        browser = "custom_bot",
                        device = "custom_bot"
                    }
                }
            };

            await SendAsync(JsonSerializer.Serialize(identify));
            Console.WriteLine("Identify sent");
        }

        private async Task SendResumeAsync()
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                await SendIdentifyAsync();
                return;
            }

            var resume = new
            {
                op = 6,
                d = new
                {
                    token = botToken,
                    session_id = _sessionId,
                    seq = _lastSequence
                }
            };

            await SendAsync(JsonSerializer.Serialize(resume));
            Console.WriteLine("Resume sent");
        }

        private async Task SendAsync(string message)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource.Token);
            }
        }

        public async Task ReconnectAsync()
        {
            Console.WriteLine("Reconnecting...");
            await DisconnectAsync();
            await Task.Delay(1000);
            await ConnectAsync();
            await SendResumeAsync();
        }

        public async Task DisconnectAsync()
        {
            Console.WriteLine("Disconnecting...");

            _heartbeatTimer?.Dispose();
            _cancellationTokenSource?.Cancel();

            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing connection",
                    CancellationToken.None);
            }

            _webSocket?.Dispose();
            Console.WriteLine("Disconnected");
        }
    }
}