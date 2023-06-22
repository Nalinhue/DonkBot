using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Yotube
{
    public static List<string> uniqueVideoIds = new List<string>();
    protected static int index = 0;
    public static List<string> spentvideoids = new List<string>();
    

    public static async Task yotube(string videoid)
    {
        CreateJson(videoid);
        var json = File.ReadAllText("data.json");
        using var client = new HttpClient();
        var initialResponse = await client.GetAsync($"https://music.youtube.com/watch?v={videoid}");
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://music.youtube.com/youtubei/v1/next?key=AIzaSyC9XL3ZjWddXya6X74dJoCTL-WEYFDNX30&prettyPrint=false", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseJson = JObject.Parse(responseBody);
        using (StringReader stringReader = new StringReader(responseJson.ToString()))
        using (JsonTextReader jsonReader = new JsonTextReader(stringReader))
        {
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.PropertyName && (string)jsonReader.Value! == "videoId")
                {
                    var vidid = jsonReader.Read();
                    if (!uniqueVideoIds.Contains((string)jsonReader.Value!))
                    {
                        uniqueVideoIds.Add((string)jsonReader.Value!);
                    }
                }
            }
        }
    }

    public static void CreateJson(string videoid)
    {
        var externalIpTask = GetExternalIpAddress();
        GetExternalIpAddress().Wait();
        string ipAddress = externalIpTask.Result!.ToString() ?? IPAddress.Loopback.ToString();
        List<VideoInteraction> videoInteractions = new List<VideoInteraction>();
        foreach (string spentvideoid in spentvideoids)
        {
            VideoInteraction newVideoInteraction = new VideoInteraction
            {
                queueImpress = new QueueImpress(),
                videoId = spentvideoid,
                queueIndex = index
            };
            index++;
            videoInteractions.Add(newVideoInteraction);
        }
        Root root = new Root
        {
            playlistId = $"RDAMVM{videoid}",
            responsiveSignals = new ResponsiveSignals
            {
                videoInteraction = videoInteractions
            },
            context = new Context
            {
                client = new Client
                {
                    remoteHost = ipAddress,
                    clientName = "WEB_REMIX",
                    clientVersion = "1.20230614.00.00",
                    originalUrl = $"https://music.youtube.com/watch?v={videoid}"
                }
            }
        };
        string json = JsonConvert.SerializeObject(root);
        File.WriteAllText("data.json", json);
    }

    public static async Task<IPAddress?> GetExternalIpAddress()
    {
        var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
            .Replace("\\r\\n", "").Replace("\\n", "").Trim();
        if(!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
        return ipAddress;
    }
}

public class QueueImpress
{
}

public class VideoInteraction
{
    public QueueImpress? queueImpress { get; set; }
    public string? videoId { get; set; }
    public int? queueIndex { get; set; }
}

public class ResponsiveSignals
{
    public List<VideoInteraction>? videoInteraction { get; set; }
}

public class Client
{
    public string? remoteHost { get; set; }
    public string? clientName { get; set; }
    public string? clientVersion { get; set; }
    public string? originalUrl { get; set; }
}

public class Context
{
    public Client? client { get; set; }
}

public class Root
{
    public string? playlistId { get; set; }
    public ResponsiveSignals? responsiveSignals { get; set; }
    public Context? context { get; set; }
}