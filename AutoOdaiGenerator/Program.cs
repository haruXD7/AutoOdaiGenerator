using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating daily topic image...");

        // 実行フォルダ取得
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        // JSONパス
        var jsonPath = Path.Combine(basePath, "topics.json");

        var json = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<TopicData>(json);

        if (data?.Topics == null || data.Topics.Count == 0)
            throw new Exception("No topics found.");

        // 日付で固定シード
        string seed = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var random = new Random(seed.GetHashCode());

        string topic = data.Topics[random.Next(data.Topics.Count)];

        // 画像サイズ
        int width = 1000;
        int height = 500;

        using var image = new Image<Rgba32>(width, height, new Rgba32(30, 30, 40));

        // ✅ フォントパスを実行フォルダ基準で取得
        var fontPath = Path.Combine(basePath, "Fonts", "NotoSansJP-Regular.ttf");

        var fontCollection = new FontCollection();
        var fontFamily = fontCollection.Add(fontPath);

        var titleFont = fontFamily.CreateFont(40);
        var bodyFont = fontFamily.CreateFont(36);

        image.Mutate(ctx =>
        {
            ctx.DrawText("今日のお題", titleFont, Color.Orange, new PointF(50, 100));
            ctx.DrawText(topic, bodyFont, Color.White, new PointF(50, 200));
        });

        // 出力も実行フォルダ基準にしておくと安全
        var repoRoot = Directory.GetCurrentDirectory();
        var outputPath = Path.Combine(repoRoot, "today.png");
        image.Save(outputPath);

        Console.WriteLine("Done.");
    }
}

class TopicData
{
    [JsonPropertyName("topics")]
    public List<string> Topics { get; set; } = new();
}