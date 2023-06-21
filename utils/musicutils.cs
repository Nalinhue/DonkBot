using System.Web;
using System.Xml;
using DonkBot.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace DonkBot.utils
{
    public class BaseMusic : BaseCommandModule
    {
        protected Dictionary<ulong, CommandContext> CommandContexts = new Dictionary<ulong, CommandContext>();
        protected Dictionary<ulong, Queue<LavalinkTrack>> Queues = new Dictionary<ulong, Queue<LavalinkTrack>>();
        protected LavalinkNodeConnection? node;
        protected LavalinkGuildConnection? conn;
        protected DiscordChannel? userVC;
        protected DSharpPlus.Lavalink.LavalinkTrack? playin;
        public static int repeat;
        protected static CommandContext? musicchannel;
        
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
                        ImageUrl = "https://steamuserimages-a.akamaihd.net/ugc/2021600978554483660/FA692BD0639B398A4030BEFDDC4B646520DA11E8/"
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

        public async Task OnPlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        {
            if (musicchannel == null)
            {
                Console.WriteLine("no ctx for playback finnished");
                return;
            }
            CommandContext ctx = musicchannel;
            if (repeat > 0)
            {
                await e.Player.PlayAsync(playin!);
                repeat--;
                string musicDescription = $"{playin!.Title}\n" +
                                  $"Author: {playin!.Author}\n" +
                                  $"URL: {playin!.Uri}\n" +
                                  $"Length: {playin!.Length}";
                var nowPlayingEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Yellow,
                    Title = $"{repeat} more",
                    Description = musicDescription
                };
                await ctx.Channel.SendMessageAsync(embed: nowPlayingEmbed);
            }
            else if (Queues.ContainsKey(ctx.Guild.Id) && Queues[ctx.Guild.Id].Count > 0)
            {
                var nextTrack = Queues[ctx.Guild.Id].Dequeue();
                await e.Player.PlayAsync(nextTrack);
                playin = nextTrack;
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
            else if (SongRecommender.apiKeys == null)
                return;
            else
            {
                var recomd = await SongRecommender.Recommendation(playin!.Uri);
                if (recomd == "error")
                {
                    await ctx.Channel.SendMessageAsync("Error you probably ran out of tokens");
                    return;
                }
                if (recomd == "notFound")
                {
                    await ctx.Channel.SendMessageAsync("Nothing found :(");
                    return;
                }
                string youtubeUrl = $"https://www.youtube.com/watch?v={recomd}";
                var node = ctx.Client.GetLavalink().ConnectedNodes.Values.First();
                var loadResult = await GetLoadResult(youtubeUrl, node);
                var track = loadResult.Tracks.First();
                await Pusic(ctx, track);
            }
            sender.PlaybackFinished -= OnPlaybackFinished;
            sender.PlaybackFinished += OnPlaybackFinished;
        }

        public async Task Pusic(CommandContext ctx, LavalinkTrack musicTrack, bool isPlaylist = false)
        {
            var conn = ctx.Client.GetLavalink().ConnectedNodes.Values.First().GetGuildConnection(ctx.Guild);
            if (conn!.CurrentState.CurrentTrack != null)
            { 
                Queues[ctx.Guild.Id].Enqueue(musicTrack);
                if (!isPlaylist)
                {
                    await ctx.Channel.SendMessageAsync($"Queued: {musicTrack.Title}");
                }
            }
            else
            {
                await conn.PlayAsync(musicTrack);
                playin = musicTrack;
                string musicDescription = $"{musicTrack.Title}\n" +
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
        }

        public async Task<LavalinkLoadResult> GetLoadResult(string query, LavalinkNodeConnection node)
        {
            bool isUrl = Uri.TryCreate(query, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            LavalinkLoadResult searchQuery;
            if (isUrl)
                searchQuery = await node.Rest.GetTracksAsync(uriResult!);
            else
                searchQuery = await node.Rest.GetTracksAsync(query);

            return searchQuery;
        }
    }

    public class SongRecommender
    {
        private static List<string> RecommendedVideoIds = new List<string>();
        private static int keyIndex = 0;
        public static string[] apiKeys = Environment.GetEnvironmentVariable("YoutubeAPI")?.Split(',') ?? new string[0];
        public static async Task<string> Recommendation(Uri trackUri)
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = apiKeys[keyIndex],
                });
                keyIndex = (keyIndex + 1) % apiKeys.Length;
                var videoId = HttpUtility.ParseQueryString(trackUri.Query).Get("v");
                if (videoId == null)
                    return "notFound"; 
                var relatedVideosResponse = await GetRelatedVideos(youtubeService, videoId);
                var cringelist = new List<string>(File.ReadAllLines("CringeLists/cringelist.txt"));
                var cringepeoplelist = new List<string>(File.ReadAllLines("CringeLists/cringepeoplelist.txt"));
                List<SearchResult> eligibleVideos = new List<SearchResult>();
                foreach (var video in relatedVideosResponse.Items)
                {
                    if (await IsVideoRecommended(video, youtubeService, cringelist, cringepeoplelist))
                    {
                        eligibleVideos.Add(video);
                    }
                }
                if (eligibleVideos.Count == 0)
                {
                    return "notFound";
                }
                var rand = new Random();
                var selectedVideo = eligibleVideos[rand.Next(eligibleVideos.Count)];
                lock (RecommendedVideoIds)
                {
                    RecommendedVideoIds.Add(selectedVideo.Id.VideoId);
                }
                return selectedVideo.Id.VideoId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "error";
            }
        }

        private static async Task<SearchListResponse> GetRelatedVideos(YouTubeService youtubeService, string videoId)
        {
            var relatedVideosRequest = youtubeService.Search.List("snippet");
            relatedVideosRequest.RelatedToVideoId = videoId;
            relatedVideosRequest.Type = "video";
            relatedVideosRequest.VideoCategoryId = "10"; // Music category
            relatedVideosRequest.MaxResults = 8;
            relatedVideosRequest.QuotaUser = Guid.NewGuid().ToString();
            return await relatedVideosRequest.ExecuteAsync();
        }

        private static async Task<bool> IsVideoRecommended(SearchResult video, YouTubeService youtubeService, List<string> cringelist, List<string> cringepeoplelist)
        {
            var videoRequest = youtubeService.Videos.List("contentDetails");
            videoRequest.Id = video.Id.VideoId;
            videoRequest.QuotaUser = Guid.NewGuid().ToString();
            var videoResponse = await videoRequest.ExecuteAsync();
            var duration = XmlConvert.ToTimeSpan(videoResponse.Items[0].ContentDetails.Duration);
            var totalMinutes = duration.TotalMinutes;
            var cringe = !cringelist.Any(title => video.Snippet.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
            var cringepeople = !cringepeoplelist.Any(chanel => video.Snippet.ChannelTitle.Contains(chanel, StringComparison.OrdinalIgnoreCase));

            lock (RecommendedVideoIds)
            {
                return totalMinutes > 2 && totalMinutes < 6 && !RecommendedVideoIds.Contains(video.Id.VideoId) && cringe && cringepeople;
            }
        }
    }
}
