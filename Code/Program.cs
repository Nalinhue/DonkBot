using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DonkBot.Commands;
using DonkBot.Code.utils;

namespace DonkBot 
{
    public class Program 
    {
        public static DiscordClient?  Client; 
        public static CommandsNextExtension? Commands;
        public static LavalinkConfiguration? lavalinkConfig;

        static async Task Main(string[] args) 
        {
            string? DiscordToken = Environment.GetEnvironmentVariable("DiscordToken");
            if (DiscordToken == null)
            {
                Console.WriteLine("Discord Token is null");
                return;
            }
            var config = new DiscordConfiguration() 
            {
                Intents = DiscordIntents.All,
                Token = DiscordToken,
                TokenType = TokenType.Bot,
                AutoReconnect = false,
            };
            Client = new DiscordClient(config);
            Client.UseInteractivity(new InteractivityConfiguration() 
            { 
                Timeout = TimeSpan.FromMinutes(2)            
            });
            string? Prefix = Environment.GetEnvironmentVariable("Prefix");
            if (Prefix == null)
            {
                Prefix = "-";
            }
            var commandsConfig = new CommandsNextConfiguration() 
            {
                StringPrefixes = new string[]{Prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                EnableDefaultHelp = false,
            };
            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.RegisterCommands<Fun>();
            Commands.RegisterCommands<MusicCommand>();
            string? LHOSTNAME = Environment.GetEnvironmentVariable("LavaLink_HostName");
            string? LPORT = Environment.GetEnvironmentVariable("LavaLink_Port");
            string? LPASSWORD = Environment.GetEnvironmentVariable("LavaLink_Password");
            if (LHOSTNAME == null || LPORT == null || LPASSWORD == null)
            {
                Console.WriteLine("Lavalink password, hostname or port is null.");
                return;
            }
            string? LSecure = Environment.GetEnvironmentVariable("LavaLink_SSL");
            if (LSecure == null)
            {
                LSecure = "false";
            }
            var endpoint = new ConnectionEndpoint
            {
                Hostname = LHOSTNAME,
                Port = int.Parse(LPORT),
                Secured = bool.Parse(LSecure)
            };
            lavalinkConfig = new LavalinkConfiguration
            {
                Password = LPASSWORD,
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            await Client.ConnectAsync();
            LavalinkNodeConnection lavalink = await Client.UseLavalink().ConnectAsync(lavalinkConfig);
            lavalink.PlaybackFinished += EventHandlerer.OnPlaybackFinished;
            Client.VoiceStateUpdated += EventHandlerer.OnVoiceStateUpdated;
            Client.MessageCreated += EventHandlerer.emojitime;
            Client.MessageReactionAdded += EventHandlerer.reactions;
            Client.SocketClosed += EventHandlerer.OnSocketClosed;
            Commands.CommandErrored += EventHandlerer.OnCommandError;
            await Task.Delay(-1);
        }
    }
}
