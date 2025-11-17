using System;
using System.Threading.Tasks;
using DiscordBot;

var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
Console.WriteLine(token);
if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Please set DISCORD_BOT_TOKEN environment variable");
    return;
}

var client = new DiscordWebSocketClient(token);

try
{
    await client.ConnectAsync();

    // Ctrl+Cでの終了を処理
    Console.CancelKeyPress += async (sender, e) =>
    {
        e.Cancel = true;
        await client.DisconnectAsync();
        Environment.Exit(0);
    };

    // プログラムを実行し続ける
    await Task.Delay(Timeout.Infinite);
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error: {ex.Message}");
}


