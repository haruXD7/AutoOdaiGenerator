using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Reflection;
using System.Text;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Generating AI topics...");

        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("API Key not found.");

        var topics = await GenerateTopics(apiKey);

        if (topics.Count == 0)
            throw new Exception("No topics generated.");

        GenerateImage(topics);

        Console.WriteLine("Done.");
    }

    static async Task<List<string>> GenerateTopics(string apiKey)
    {
        using var client = new HttpClient();

        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = "哲学的で深い議論用のお題を5つ、日本語で箇条書きで出力してください。" }
                    }
                }
            }
        };

        var response = await client.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();

        var doc = JsonDocument.Parse(json);

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text!
            .Split('\n')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.TrimStart('-', '・', '1', '2', '3', '4', '5', '.', ' '))
            .ToList();
    }

    static void GenerateImage(List<string> topics)
    {
        var basePath = Directory.GetCurrentDirectory();
        var fontPath = Path.Combine(basePath, "Fonts", "NotoSansJP-Regular.ttf");
        var outputPath = Path.Combine(basePath, "today.png");

        var fontCollection = new FontCollection();
        var fontFamily = fontCollection.Add(fontPath);

        var titleFont = fontFamily.CreateFont(42);
        var bodyFont = fontFamily.CreateFont(34);

        using var image = new Image<Rgba32>(1200, 800, new Rgba32(20, 20, 30));

        image.Mutate(ctx =>
        {
            ctx.DrawText("Today's AI Topics", titleFont, Color.Orange, new PointF(60, 80));

            float y = 180;

            foreach (var topic in topics.Take(5))
            {
                ctx.DrawText("・" + topic, bodyFont, Color.White, new PointF(80, y));
                y += 90;
            }
        });

        image.Save(outputPath);
    }
}