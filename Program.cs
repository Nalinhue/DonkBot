using System.Web;
using System.Xml;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;
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
                EnableDms = true,
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
            var lavalink = Client.UseLavalink();
            await Client.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);
            await Task.Delay(-1);

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
                await e.Context.Channel.SendMessageAsync(e.Exception.StackTrace);
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
                await conn.DisconnectAsync();
            }
        }

        private static Task OnClientReady(ReadyEventArgs e)
        {
            return Task.CompletedTask;
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
                var videoId = ExtractVideoId(trackUri);
                var relatedVideosResponse = await GetRelatedVideos(youtubeService, videoId);
                var cringelist = ReadCringeList("cringelist.txt");
                var cringepeoplelist = ReadCringeList("cringepeoplelist.txt");
                List<SearchResult> eligibleVideos = new List<SearchResult>();
                foreach (var video in relatedVideosResponse.Items)
                {
                    if (IsVideoRecommended(video, youtubeService, cringelist, cringepeoplelist))
                    {
                        eligibleVideos.Add(video);
                    }
                }
                if (eligibleVideos.Count == 0)
                {
                    Console.WriteLine("Didn't find anything");
                    return "notFound";
                }
                var rand = new Random();
                var selectedVideo = eligibleVideos[rand.Next(eligibleVideos.Count)];
                lock (RecommendedVideoIds)
                {
                    RecommendedVideoIds.Add(selectedVideo.Id.VideoId);
                }
                return selectedVideo.Snippet.Title;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return "error";
            }
        }

        private static string ExtractVideoId(Uri trackUri)
        {
            return HttpUtility.ParseQueryString(trackUri.Query).Get("v")!;
        }

        private static async Task<SearchListResponse> GetRelatedVideos(YouTubeService youtubeService, string videoId)
        {
            var relatedVideosRequest = youtubeService.Search.List("snippet");
            relatedVideosRequest.RelatedToVideoId = videoId;
            relatedVideosRequest.Type = "video";
            relatedVideosRequest.VideoCategoryId = "10"; // Music category
            relatedVideosRequest.MaxResults = 12;

            relatedVideosRequest.QuotaUser = Guid.NewGuid().ToString();
            return await relatedVideosRequest.ExecuteAsync();
        }

        private static List<string> ReadCringeList(string filePath)
        {
            return new List<string>(File.ReadAllLines(filePath.ToLower()));
        }

        private static bool IsVideoRecommended(SearchResult video, YouTubeService youtubeService, List<string> cringelist, List<string> cringepeoplelist)
        {
            var videoRequest = youtubeService.Videos.List("contentDetails");
            videoRequest.Id = video.Id.VideoId;
            videoRequest.QuotaUser = Guid.NewGuid().ToString();
            var videoResponse = videoRequest.ExecuteAsync().Result;

            var duration = XmlConvert.ToTimeSpan(videoResponse.Items[0].ContentDetails.Duration);
            var totalMinutes = duration.TotalMinutes;
            var cringe = !cringelist.Any(title => video.Snippet.Title.ToLower().Contains(title));
            var cringepeople = !cringepeoplelist.Any(chanel => video.Snippet.ChannelTitle.ToLower().Contains(chanel));

            lock (RecommendedVideoIds)
            {
                return totalMinutes > 2 && totalMinutes < 5 && !RecommendedVideoIds.Contains(video.Id.VideoId) && cringe && cringepeople;
            }
        }
    }
}
