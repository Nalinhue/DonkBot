using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DonkBot.utils;

namespace DonkBot.Commands
{
    public class MusicCommand : BaseMusic
    {
        [Command("play")]
        [Aliases("p", "🧑‍🎤")]
        public async Task PlayMusic(CommandContext ctx, [RemainingText] string query)
        {
            musicchannel = ctx;
            if (!await PreCom(ctx)) return;
            if (conn!.Channel.Id != userVC!.Id)
            {
                await conn.DisconnectAsync();
                await node!.ConnectAsync(userVC);
                await PlayMusic(ctx, query);
                return;
            }
            var searchQuery = await GetLoadResult(query, node!);
            if (searchQuery.LoadResultType == LavalinkLoadResultType.NoMatches || searchQuery.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                await ctx.Channel.SendMessageAsync($"Failed to find music with query: {query}");
                return;
            }
            Yotube.uniqueVideoIds.Clear();
            if (searchQuery.LoadResultType == LavalinkLoadResultType.TrackLoaded || searchQuery.LoadResultType == LavalinkLoadResultType.SearchResult)
            {
                await Pusic(ctx, searchQuery.Tracks.First());
            }
            else
            {
                foreach (var musicTrack in searchQuery.Tracks)
                {
                    await Pusic(ctx, musicTrack, true);
                }
                await ctx.Channel.SendMessageAsync("Queued playlist");
            }
            conn.PlaybackFinished -= OnPlaybackFinished;
            conn.PlaybackFinished += OnPlaybackFinished;
        }    
    
        [Command("skip")]
        [Aliases("s", "garbage", "trash", "😒")]
        public async Task SkipTrack(CommandContext ctx, int trackIndexToSkip = 0)
        {
            if (!await PreCom(ctx)) return;
            if (conn!.Channel.Id != userVC!.Id) 
            {
                await  ctx.Channel.SendMessageAsync("You cant even hear it"); 
                return;
            }
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.Channel.SendMessageAsync("Skip what fool.");
                return;
            }
            var queueList = Queues[ctx.Guild.Id].ToList();
            if (trackIndexToSkip < 0 || trackIndexToSkip > queueList.Count())
            {
                await ctx.Channel.SendMessageAsync("Invalid track index.");
                return;
            }
            if (trackIndexToSkip == 0)
            {
                await conn.StopAsync();
                return;
            }
            
            queueList.RemoveAt(trackIndexToSkip - 1);
            Queues[ctx.Guild.Id] = new Queue<LavalinkTrack>(queueList);
        }
    
        [Command("Cease")]
        [Aliases("c", "stop", "🤮")]
        public async Task StopMusic(CommandContext ctx)
        {
            if (!await PreCom(ctx)) return;
            if (conn!.Channel.Id != userVC!.Id) 
            {
                await  ctx.Channel.SendMessageAsync("You cant even hear it"); 
                return;
            }
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.Channel.SendMessageAsync("Stop what fool.");
                return;
            }
            conn.PlaybackFinished -= OnPlaybackFinished;
            Queues[ctx.Guild.Id].Clear();
            await conn.StopAsync();
            

            var stopEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Orange,
                Title = "Stopped the Track",
                Description = "Cringe no more."
            };

            await ctx.Channel.SendMessageAsync(embed: stopEmbed);
        }
        [Command("queue")]
        [Aliases("q", "🤔")]
        public async Task ListQueue(CommandContext ctx)
        {
            if (Queues.ContainsKey(ctx.Guild.Id))
            {
                int i = 1;
                string queuelist = "";
                foreach (string song in Queues[ctx.Guild.Id].Select(x => x.Title))
                {
                    queuelist += $"{i++}. {song}\n";
                }
                var queueEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.PhthaloBlue,
                    Title = "Queue",
                    Description = queuelist
                };
                await ctx.Channel.SendMessageAsync(embed: queueEmbed);
            }
            else
            {
                await ctx.Channel.SendMessageAsync("No queue");
            }
        }

        [Command("Repeat")]
        [Aliases("r", "🤪")]
        public async Task Repeat(CommandContext ctx, string? amounttorepeat = null)
        {
            if (!await PreCom(ctx)) return;
            if (conn!.Channel.Id != userVC!.Id) 
            {
                await  ctx.Channel.SendMessageAsync("no"); 
                return;
            }
            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.Channel.SendMessageAsync("repeat what fool");
                return;
            }
            int.TryParse(amounttorepeat, out int result);
            if (result < 0 || result > 10000000)
            {
                await ctx.Channel.SendMessageAsync("cant do that");
                return;
            }
            if (amounttorepeat == null)
            {
                repeat = 100;
            }
            else
            {
                repeat = result;
            }
            await ctx.Channel.SendMessageAsync($"Repeating {playin} {repeat} times.");
        }

        [Command("AQ")]
        public async Task AQ(CommandContext ctx)
        {
            if (Yotube.uniqueVideoIds.Count() != 0 || Yotube.uniqueVideoIds != null)
            {
                int i = 1;
                string queuelist = "";
                foreach (string song in Yotube.uniqueVideoIds!)
                {
                    queuelist += $"{i++}. https://www.youtube.com/watch?v={song}\n";
                }
                var queueEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.PhthaloBlue,
                    Title = "AQueue",
                    Description = queuelist
                };
                await ctx.Channel.SendMessageAsync(embed: queueEmbed);
            }
            else
            {
                await ctx.Channel.SendMessageAsync("No queue");
            }
        }
    }
}