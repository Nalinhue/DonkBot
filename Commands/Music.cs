using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;

namespace DonkBot.Commands
{
    public class BaseMusic : BaseCommandModule
    {
        protected Dictionary<ulong, CommandContext> CommandContexts = new Dictionary<ulong, CommandContext>();
        protected Dictionary<ulong, Queue<LavalinkTrack>> Queues = new Dictionary<ulong, Queue<LavalinkTrack>>();
        protected LavalinkNodeConnection? node;
        protected LavalinkGuildConnection? conn;
        protected DiscordChannel? userVC;
        protected System.Uri? playin;

        public async Task<bool> PreCom(CommandContext ctx)
        {
            string? AllowedChannelName = Environment.GetEnvironmentVariable("AllowedChannelName");
            CommandContexts[ctx.Guild.Id] = ctx;
            if (AllowedChannelName != null)
            {
                if (!ctx.Channel.Name.Contains(AllowedChannelName))
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        ImageUrl = "https://i.imgflip.com/7mfp69.jpg"
                    }.Build();
                    await ctx.Channel.SendMessageAsync(embed: embed);
                    return false;
                }
            }
            if (ctx.Member!.VoiceState == null!)
            {
                await ctx.Channel.SendMessageAsync("getin here");
                return false;
            }
            userVC = ctx.Member!.VoiceState.Channel;
            var lavalinkInstance = ctx.Client.GetLavalink();
            node = lavalinkInstance.ConnectedNodes.Values.First();
            await node.ConnectAsync(userVC);
            conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (userVC == null!)
            {
                await ctx.Channel.SendMessageAsync("Doesn't work like that, it could, but it doesn't. Why?");
                return false;
            }
            if (lavalinkInstance == null || !lavalinkInstance.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("Lavalink's down.");
                return false;
            }
            if (userVC.Type != ChannelType.Voice)
            {
                await ctx.Channel.SendMessageAsync("I don't even know how it's possible.");
                return false;
            }
            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Im not in the VoiceChannel");
                return false;
            }
            if (!Queues.ContainsKey(ctx.Guild.Id))
            {
                Queues[ctx.Guild.Id] = new Queue<LavalinkTrack>();
            }
            return true;   
        }
        
        public async Task Pusic(CommandContext ctx, string query) //TODO remove this fucking pusic i shouldnt need it 
        {
            var searchQuery = await node!.Rest.GetTracksAsync(query);
            if (searchQuery.LoadResultType == LavalinkLoadResultType.NoMatches || searchQuery.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                await ctx.Channel.SendMessageAsync($"Failed to find music with query: {query}");
                return;
            }
            var musicTrack = searchQuery.Tracks.First();
            await conn!.PlayAsync(musicTrack);
            playin = musicTrack.Uri;
            string musicDescription = $"Now Playing: {musicTrack.Title}\n" +
                                      $"Author: {musicTrack.Author}\n" +
                                      $"URL: {musicTrack.Uri}\n" +
                                      $"Length: {musicTrack.Length}";
            var nowPlayingEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Yellow,
                Title = "Now Playing",
                Description = musicDescription
            };
            await ctx.Channel.SendMessageAsync(embed: nowPlayingEmbed);
        }

        public async Task OnPlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        {
            if (CommandContexts.TryGetValue(e.Player.Guild.Id, out CommandContext? ctx))
            {
                if (Queues.ContainsKey(ctx.Guild.Id) && Queues[ctx.Guild.Id].Count > 0)
                {
                    var nextTrack = Queues[ctx.Guild.Id].Dequeue();
                    await e.Player.PlayAsync(nextTrack);
                    playin = nextTrack.Uri;
                    string nextMusicDescription = $"Now Playing: {nextTrack.Title}\n" +
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
                    var recomd = await DonkBot.SongRecommender.Recommendation(playin!);
                    if (recomd == "error")
                    {
                        await ctx.Channel.SendMessageAsync("No youtube tokens left");
                        return;
                    }
                    if (recomd == "notFound")
                    {
                        await ctx.Channel.SendMessageAsync("did not find a single fucking song");
                        return;
                    }
                    await Pusic(ctx, query: recomd);
                }
                sender.PlaybackFinished -= OnPlaybackFinished;
                sender.PlaybackFinished += OnPlaybackFinished;
            }
            else
            {
                Console.WriteLine("CommandContext not found for guild id: " + e.Player.Guild.Id);
            }
        }



    }

    public class MusicCommand : BaseMusic
    {

        [Command("play")]
        [Aliases("p")]
        public async Task PlayMusic(CommandContext ctx, [RemainingText] string query)
        {
            if (!await PreCom(ctx)) return;
            if (conn!.Channel.Id != userVC!.Id)
            {
                await conn.DisconnectAsync();
                await node!.ConnectAsync(userVC);
                await PlayMusic(ctx, query);
                return;
            }
            if (conn.CurrentState.CurrentTrack != null)
            {
                var searchQuery = await node!.Rest.GetTracksAsync(query);
                if (searchQuery.LoadResultType == LavalinkLoadResultType.NoMatches || searchQuery.LoadResultType == LavalinkLoadResultType.LoadFailed)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to find music with query: {query}");
                    return;
                }

                var musicTrack = searchQuery.Tracks.First();
                Queues[ctx.Guild.Id].Enqueue(musicTrack);
                await ctx.Channel.SendMessageAsync($"Queued: {musicTrack.Title}");
                return;
            }
            else
            {
                var searchQuery = await node!.Rest.GetTracksAsync(query);
                if (searchQuery.LoadResultType == LavalinkLoadResultType.NoMatches || searchQuery.LoadResultType == LavalinkLoadResultType.LoadFailed)
                {
                    await ctx.Channel.SendMessageAsync($"Failed to find music with query: {query}");
                    return;
                }

                var musicTrack = searchQuery.Tracks.First();
                await conn.PlayAsync(musicTrack);
                playin = musicTrack.Uri;
                string musicDescription = $"Now Playing: {musicTrack.Title}\n" +
                                          $"Author: {musicTrack.Author}\n" +
                                          $"URL: {musicTrack.Uri}\n" +
                                          $"Length: {musicTrack.Length}";

                var nowPlayingEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Yellow,
                    Title = "Now Playing",
                    Description = musicDescription
                };
                await ctx.Channel.SendMessageAsync(embed: nowPlayingEmbed);
            }
            if (SongRecommender.apiKeys != null)
            {
                conn.PlaybackFinished -= OnPlaybackFinished;
                conn.PlaybackFinished += OnPlaybackFinished;
            }
        }    
    
        [Command("skip")]
        [Aliases("s", "garbage", "trash")]
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
    
        [Command("cease")]
        [Aliases("c", "stop")]
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
        [Aliases("q")]
        public async Task ListQueue(CommandContext ctx)
        {
            if (Queues.ContainsKey(ctx.Guild.Id))
            {
                string queuelist = string.Join("\n", Queues[ctx.Guild.Id].Select(x => x.Title));
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
    }
}