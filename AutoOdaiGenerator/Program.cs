using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;
using System.Text.Json.Serialization;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating daily topic image...");

        // JSON読み込み
        string json = File.ReadAllText("topics.json");
        var data = JsonSerializer.Deserialize<TopicData>(json);

        if (data?.Topics == null || data.Topics.Count == 0)
            throw new Exception("No topics found.");

        // 日付で固定
        string seed = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var random = new Random(seed.GetHashCode());

        string topic = data.Topics[random.Next(data.Topics.Count)];

        // 画像生成
        int width = 1000;
        int height = 500;

        using var image = new Image<Rgba32>(width, height, new Rgba32(30, 30, 40));

        var fontCollection = new FontCollection();
        var fontFamily = fontCollection.Add("NotoSansJP-Regular.ttf");
        var titleFont = fontFamily.CreateFont(40);
        var bodyFont = fontFamily.CreateFont(36);

        image.Mutate(ctx =>
        {
            ctx.DrawText("今日のお題", titleFont, Color.Orange, new PointF(50, 100));
            ctx.DrawText(topic, bodyFont, Color.White, new PointF(50, 200));
        });

        image.Save("today.png");

        Console.WriteLine("Done.");
    }
}

class TopicData
{
    [JsonPropertyName("topics")]
    public List<string> Topics { get; set; } = new();
}