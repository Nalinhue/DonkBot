using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DonkBot.Commands;

namespace DonkBot 
{
    public sealed class Program 
    {
        public static DiscordClient?  Client { get; private set; } 
        public static InteractivityExtension? Interactivity { get ; private set; }
        public static CommandsNextExtension? Commands { get; private set; }

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
                AutoReconnect = true,
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
            Commands.CommandErrored += OnCommandError;
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
                Hostname = LHOSTNAME!,
                Port = int.Parse(LPORT!),
                Secured = bool.Parse(LSecure!)
            };
            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = LPASSWORD!,
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            Client.VoiceStateUpdated += OnVoiceStateUpdated;
            Client.MessageCreated += emojitime;
            Client.MessageReactionAdded += reactions;
            var lavalink = Client.UseLavalink();
            await Client.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);
            await Task.Delay(-1);

        }

        static async Task reactions(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            var commands = sender.GetCommandsNext();
            var ctx = commands.CreateContext(e.Message, null!, null);
            string? UserID = Environment.GetEnvironmentVariable("UserID");
            if (e.User.IsBot)
                return;
            var emoji = e.Emoji;
            switch (emoji.Name)
            {
                case "🐀":
                    await ctx.Message.DeleteAllReactionsAsync();
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇧"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇦"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇸"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇪"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇩"));
                    break;

                case "❌":
                    if (UserID == null)
                        return;
                    if (e.User.Id.ToString() != UserID)
                        return;
                    await ctx.Message.DeleteAsync();
                    break;

                case "🤡":
                    await ctx.Message.DeleteAllReactionsAsync();
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇼"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇷"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇴"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇳"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🇬"));
                    Thread.Sleep(400);
                    await ctx.Message.CreateReactionAsync(DSharpPlus.Entities.DiscordEmoji.FromUnicode("🤡"));
                    
                    break;
            }
        }
        static async Task emojitime(DiscordClient sender, MessageCreateEventArgs e)
        {
            var commands = sender.GetCommandsNext();
            var ctx = commands.CreateContext(e.Message, null!, null);
            if (e.Author.IsBot)
                return;
            var emoji = e.Message.Content;
            switch (emoji)
            {
                case "🐀":
                    await ctx.Channel.SendMessageAsync("good rat.");
                    break;
            }
        }

        private static async Task OnCommandError(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            if (e.Exception is ChecksFailedException)
            {
                var castedException = (ChecksFailedException)e.Exception;
                string cooldownTimer = string.Empty;

                foreach (var check in castedException.FailedChecks)
                {
                    var cooldown = (CooldownAttribute)check;
                    TimeSpan timeleft = cooldown.GetRemainingCooldown(e.Context);
                    cooldownTimer = timeleft.ToString(@"ss");
                }
                var cooldownMessage = new DiscordEmbedBuilder()
                {
                    Title = "Give me a fucking break",
                    Description = cooldownTimer,
                    Color = DiscordColor.IndianRed
                };
                await e.Context.Channel.SendMessageAsync(embed: cooldownMessage);
            }
            if (e.Exception.StackTrace == null)
            {
                await e.Context.Channel.SendMessageAsync("god only knows");
                return;
            }
            else 
            {
                await e.Context.Channel.SendMessageAsync($"{e.Exception.Message} {e.Exception.StackTrace}");
            }
        }

        private static async Task OnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            if (e.User == sender.CurrentUser)
                return;
            var lava = sender.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(e.Guild);
            if (conn == null)
                return;
            if (conn.Channel.Users.Count() == 1 && conn.Channel.Users.First() == sender.CurrentUser)
            {
                Yotube.spentvideoids.Clear();
                await conn.DisconnectAsync();
            }
        }

        private static Task OnClientReady(ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
}
