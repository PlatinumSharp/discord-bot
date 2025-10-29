// See https://aka.ms/new-console-template for more information

Console.WriteLine("=== Discord Bot - Environment Variables ===");
Console.WriteLine();

// Read environment variables
var discordBotToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
var discordChannelId = Environment.GetEnvironmentVariable("DISCORD_CHANNEL_ID");
var appEnvironment = Environment.GetEnvironmentVariable("APP_ENVIRONMENT") ?? "Development";

// Display environment variables (masking sensitive data)
Console.WriteLine($"APP_ENVIRONMENT: {appEnvironment}");
Console.WriteLine($"DISCORD_CHANNEL_ID: {discordChannelId ?? "(not set)"}");

// Only show first/last few characters of token for security
if (!string.IsNullOrEmpty(discordBotToken))
{
    var maskedToken = discordBotToken.Length > 10
        ? $"{discordBotToken[..5]}...{discordBotToken[^5..]}"
        : "***";
    Console.WriteLine($"DISCORD_BOT_TOKEN: {maskedToken}");
}
else
{
    Console.WriteLine("DISCORD_BOT_TOKEN: (not set)");
}

Console.WriteLine();
Console.WriteLine("=== Application Started ===");
