using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DonkBot.utils;
using System.Web;
using DSharpPlus.Lavalink.EventArgs;

namespace DonkBot.Code.utils
{
    public class EventHandlerer
    {
        public static async Task OnPlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        {
            if (BaseMusic.musicchannel == null)
            {
                Console.WriteLine("no ctx for playback finnished");
                return;
            }
            CommandContext ctx = BaseMusic.musicchannel;
            if (BaseMusic.repeat > 0 && BaseMusic.playin != null)
            {
                await e.Player.PlayAsync(BaseMusic.playin);
                BaseMusic.repeat--;
                string musicDescription = $"{BaseMusic.playin.Title}\n" +
                                  $"Author: {BaseMusic.playin.Author}\n" +
                                  $"URL: {BaseMusic.playin.Uri}\n" +
                                  $"Length: {BaseMusic.playin.Length}";
                var nowPlayingEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Yellow,
                    Title = $"{BaseMusic.repeat} more",
                    Description = musicDescription
                };
                await ctx.Channel.SendMessageAsync(embed: nowPlayingEmbed);
            }
            else if (BaseMusic.Queues.ContainsKey(ctx.Guild.Id) && BaseMusic.Queues[ctx.Guild.Id].Count > 0)
            {
                var nextTrack = BaseMusic.Queues[ctx.Guild.Id].Dequeue();
                await e.Player.PlayAsync(nextTrack);
                BaseMusic.playin = nextTrack;
                string nextMusicDescription = $"{nextTrack.Title}\n" +
                                          $"Author: {nextTrack.Author}\n" +
                                          $"URL: {nextTrack.Uri}\n" +
                                          $"Length: {nextTrack.Length}";
                var nextPlayingEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Yellow,
                    Title = "Now Playing",
                    Description = nextMusicDescription
                };
                await ctx.Channel.SendMessageAsync(embed: nextPlayingEmbed);
                return;
            }
            else 
            {
                if (Yotube.uniqueVideoIds == null || Yotube.uniqueVideoIds.Count == 0)
                {
                    if (BaseMusic.playin == null)
                        return;
                    string videoid = HttpUtility.ParseQueryString(BaseMusic.playin.Uri.Query).Get("v")!;
                    await Yotube.yotube(videoid);
                }
                if (Yotube.uniqueVideoIds == null || Yotube.uniqueVideoIds.Count == 0)
                {
                    await ctx.Channel.SendMessageAsync("somehow palpatine returned");
                    return;
                }
                string youtubeUrl = $"https://www.youtube.com/watch?v={Yotube.uniqueVideoIds[0]}";
                Yotube.uniqueVideoIds.RemoveAt(0);
                var node = ctx.Client.GetLavalink().ConnectedNodes.Values.FirstOrDefault();
                if (node == null)
                {
                    Console.WriteLine("lavalinks down");
                    return;
                }
                var loadResult = await BaseMusic.GetLoadResult(youtubeUrl, node);
                if (loadResult.Tracks == null || loadResult.Tracks.Count() == 0)
                {
                    Console.WriteLine("idk prob no interweb");
                    return;
                }
                var track = loadResult.Tracks.First();                
                await BaseMusic.Pusic(ctx, track);
                return;
            }
        }

        public static async Task reactions(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            CommandsNextExtension commands = sender.GetCommandsNext();
            CommandContext ctx = commands.CreateContext(e.Message, null!, null);
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
        public static async Task emojitime(DiscordClient sender, MessageCreateEventArgs e)
        {
            CommandsNextExtension commands = sender.GetCommandsNext();
            CommandContext ctx = commands.CreateContext(e.Message, null!, null);
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

        public static async Task OnCommandError(CommandsNextExtension sender, CommandErrorEventArgs e)
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
                await e.Context.Channel.SendMessageAsync($"{e.Exception.Message}\n {e.Exception.StackTrace}");
            }
        }

        public static async Task OnVoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
        {
            if (e.User == sender.CurrentUser)
                return;
            var lava = sender.GetLavalink();
            var node = lava.ConnectedNodes.Values.FirstOrDefault();
            if (node == null)
            {
                Console.WriteLine("lavalinks down");
                return;
            }
            var conn = node.GetGuildConnection(e.Guild);
            if (conn == null)
                return;
            if (conn.Channel.Users.Count() == 1 && conn.Channel.Users.First() == sender.CurrentUser)
            {
                await conn.DisconnectAsync();
            }
        }

        public static async Task OnSocketClosed(DiscordClient s, SocketCloseEventArgs e)
        {
            Console.WriteLine("Disconnected! Attempting to reconnect...");
            bool internet = false;
            while (internet == false)
            {
                using var client = new HttpClient();
                var response = await client.GetAsync("http://clients3.google.com/generate_204");
                if (response.IsSuccessStatusCode == true)
                {
                    internet = true;
                }
                else
                {
                    Console.WriteLine("Internet connection is not available. Retrying in 5 seconds...");
                    await Task.Delay(5000);
                }
            }
            await s.ConnectAsync();
            await s.UseLavalink().ConnectAsync(DonkBot.Program.lavalinkConfig!);
        }
    }
}