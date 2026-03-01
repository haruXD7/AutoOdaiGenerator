using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
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
        // v1beta ではなく v1 を使用するのが現在の推奨です
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
            new
            {
                role = "user", // roleを明示的に指定するとより確実です
                parts = new[]
                {
                    new { text = "VRCHATでフレンドと話すお題を5つ、日本語で箇条書きで出力してください。" +
                    "ですます口調はやめてください。" +
                    "ちょっと深い話ができるようなものにしてください" +
                    "VRCHATのような仮想空間をテーマにしてみてください。" +
                    "毎日違うテーマにしてください" +
                    "余計な解説は不要です。" }
                    　
                }
            }
        }
        };

        var response = await client.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();

        // エラーハンドリング：成功しなかった場合に内容を表示して中断する
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error: {response.StatusCode}");
            Console.WriteLine(json);
            return new List<string>();
        }

        using var doc = JsonDocument.Parse(json);

        // JSONの階層を安全にたどる
        try
        {
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text!
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.TrimStart('-', '・', '1', '2', '3', '4', '5', '.', ' '))
                .ToList();
        } catch (Exception ex)
        {
            Console.WriteLine("JSON Parsing Error: " + ex.Message);
            return new List<string>();
        }
    }
    static void GenerateImage(List<string> topics)
    {
        var basePath = Directory.GetCurrentDirectory();
        var fontPath = Path.Combine(basePath, "Fonts", "NotoSansJP-Regular.ttf");
        var outputPath = Path.Combine(basePath, "today.png");

        var fontCollection = new FontCollection();
        var fontFamily = fontCollection.Add(fontPath);

        // 解像度を1920x1080に変更
        int imageWidth = 1920;
        int imageHeight = 1080;

        // 画面サイズに合わせてフォントサイズを少し調整
        var titleFont = fontFamily.CreateFont(50); // 42 -> 50 に少し大きく
        var bodyFont = fontFamily.CreateFont(34); // 30 -> 34 に少し大きく

        using var image = new Image<Rgba32>(imageWidth, imageHeight, new Rgba32(20, 20, 30));

        image.Mutate(ctx =>
        {
            // タイトルの描画（座標を少し調整）
            ctx.DrawText("Today's AI Topics", titleFont, Color.Orange, new PointF(100, 80));

            float y = 200; // 本文の開始位置を少し下げる
            float paddingSide = 120; // 左右の余白を少し広く
            float wrapWidth = image.Width - (paddingSide * 2); // 折り返し幅

            foreach (var topic in topics.Take(5))
            {
                // 折り返し設定用のオプション
                var options = new RichTextOptions(bodyFont)
                {
                    Origin = new PointF(paddingSide, y),
                    WrappingLength = wrapWidth, // この幅を超えたら改行
                    LineSpacing = 1.3f // 行間を少し広めに設定（1.2 -> 1.3）
                };

                ctx.DrawText(options, "・" + topic, Color.White);

                // 描画後の高さを計算して次の開始位置(y)をズラす
                var size = TextMeasurer.MeasureSize("・" + topic, options);
                y += size.Height + 60; // 文の高さ + トピック間の余白を多めに（40 -> 60）
            }
        });

        image.Save(outputPath);
    }
}