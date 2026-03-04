using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
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

        // 共通のプロンプト（末尾の"テーマは..." 行は各テーマごとに渡す）
        var commonPrompt = string.Join("\n",
            "フレンドと話すお題を5つだしてください。",
            "日本語で箇条書きで出力してください。",
            "口調は優しい感じにしてください",
            "ちょっと深い話ができるようなものにしてください",
            "毎回ランダムなものにしてください、ただし時々以前と被ってもいいです。",
            "余計な解説は不要です。"
        );

        // ここにテーマごとの (出力ファイル名, テーマ固有の行) を追加する
        var themes = new[]
        {
            new { FileName = "today_vrchat.png", ThemeLine = "テーマはVRCHATのような仮想空間についてにしてください。" },
            new { FileName = "today_ai.png", ThemeLine = "テーマはAIと社会についてにしてください。" },
            new { FileName = "today_恋愛.png", ThemeLine = "テーマはVRCHATの恋愛についてにしてください" },
            new { FileName = "today_哲学.png", ThemeLine = "テーマは哲学についてにしてください" },
            new { FileName = "today_人間関係.png", ThemeLine = "テーマは人間関係についてにしてください" },
            new { FileName = "today_仮想シチュエーション.png", ThemeLine = "テーマは「もしも〇〇だったら・・・」のような仮想シュチュエーションにしてください" },
            new { FileName = "today_アイスブレイク.png", ThemeLine = "テーマはアイスブレイクのような軽い話題にしてください" },
            new { FileName = "today_深夜.png", ThemeLine = "テーマは深夜向けのちょっと大人な話題にしてください" },
        };

        foreach (var t in themes)
        {
            Console.WriteLine($"Processing theme -> {t.FileName} : {t.ThemeLine}");
            await ProcessTheme(apiKey, t.FileName, commonPrompt, t.ThemeLine);
        }

        Console.WriteLine("Done.");
    }

    static async Task ProcessTheme(string apiKey, string outputFileName, string commonPrompt, string themeLine)
    {
        var userPrompt = string.Join("\n", commonPrompt, themeLine);

        var topics = await GenerateTopics(apiKey, userPrompt);

        if (topics.Count == 0)
        {
            Console.WriteLine($"No topics generated for {outputFileName}. Check API response.");
            return;
        }

        GenerateImage(topics, outputFileName);
    }

    static async Task<List<string>> GenerateTopics(string apiKey, string userText)
    {
        using var client = new HttpClient();
        // ブラウザで確認した最新のモデル名を使用
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = userText }
                    }
                }
            }
        };

        var response = await client.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"API Error: {response.StatusCode}");
            Console.WriteLine(json);
            return new List<string>();
        }

        using var doc = JsonDocument.Parse(json);
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
    }

    static void GenerateImage(List<string> topics, string outputFileName)
    {
        // GitHub Actions環境とローカル両方で安定するパス取得
        var workingDir = Directory.GetCurrentDirectory();
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;

        // フォントはビルド出力（exeDir）にある Fonts フォルダを参照
        var fontPath = Path.Combine(exeDir, "Fonts", "NotoSansJP-Regular.ttf");

        // 画像はリポジトリ直下の Picture フォルダに保存（存在しなければ作成）
        var pictureDir = Path.Combine(workingDir, "Picture");
        Directory.CreateDirectory(pictureDir);
        var outputPath = Path.Combine(pictureDir, outputFileName);

        Console.WriteLine($"Loading font from: {fontPath}");
        Console.WriteLine($"Saving image to: {outputPath}");

        if (!File.Exists(fontPath))
            throw new FileNotFoundException($"Font not found: {fontPath}");

        var fontCollection = new FontCollection();
        var fontFamily = fontCollection.Add(fontPath);

        // 1920x1080 用のフォントサイズ
        var titleFont = fontFamily.CreateFont(50);
        var bodyFont = fontFamily.CreateFont(34);

        using var image = new Image<Rgba32>(1920, 1080, new Rgba32(20, 20, 30));

        image.Mutate(ctx =>
        {
            // タイトル
            ctx.DrawText("Today's AI Topics", titleFont, Color.Orange, new PointF(100, 80));

            float y = 200;
            float paddingSide = 120;
            float wrapWidth = image.Width - (paddingSide * 2);

            foreach (var topic in topics.Take(5))
            {
                var options = new RichTextOptions(bodyFont)
                {
                    Origin = new PointF(paddingSide, y),
                    WrappingLength = wrapWidth,
                    LineSpacing = 1.3f
                };

                ctx.DrawText(options, "・" + topic, Color.White);

                // 文字の高さに合わせて次の描画位置を計算
                var size = TextMeasurer.MeasureSize("・" + topic, options);
                y += size.Height + 60;
            }
        });

        image.Save(outputPath);
    }
}