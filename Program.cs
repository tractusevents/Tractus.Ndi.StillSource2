using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;
using System.Text.Json;
using Tractus.Ndi.StillSource2.WebModels;

namespace Tractus.Ndi.StillSource2;

internal class Program
{
    public static string ImageRootPath;

    static async Task Main(string[] args)
    {
        var imageRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
        Directory.CreateDirectory(imageRootPath);
        ImageRootPath = imageRootPath;

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddCors(o =>
        {
            o.AddDefaultPolicy(b =>
            {
                b.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true);
            });
        });
        builder.WebHost.UseUrls("http://*:8909");
        builder.Services.ConfigureHttpJsonOptions(o => { o.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals; });

        var app = builder.Build();
        app.UseCors();
        app.UseRouting();

        var imageSources = 
            LoadConcurrentDict<ImageSource>("images.json") 
            ?? new ConcurrentDictionary<string, ImageSource>();

        var ndiSenders = 
            LoadConcurrentDict<NdiSender>("senders.json")
            ?? new ConcurrentDictionary<string, NdiSender>();


        foreach (var item in ndiSenders.Values)
        {
            var imageSource = imageSources[item.ImageSourceCode];
            item.UpdateSource(imageSource);
        }

        app.MapGet("/images", () => imageSources.Values.Select(x => new
        {
            x.Code,
            x.Name,
            x.Path,
            x.Width,
            x.Height,
            Url = $"/image/{x.Code}"
        }));

        app.MapGet("/senders", () => ndiSenders.Values.Select(x => new
        {
            x.Code,
            x.Name,
            x.FrameRateNumerator,
            x.FrameRateDenominator,
            x.SendActualFrameRate,
            x.ImageSourceCode,
            ImageName = x.Source.Name,
        }));


        app.MapPost("/upload", async (IFormFile image, string name, string code) =>
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code) || image == null)
            {
                return Results.BadRequest("Name, Code, and Image are required.");
            }

            if (imageSources.ContainsKey(code))
            {
                return Results.Conflict("Code already exists.");
            }

            var extension = Path.GetExtension(image.FileName);
            var imageName = $"{code}{extension}";
            await using var stream = new FileStream(Path.Combine(imageRootPath, imageName), FileMode.Create);

            await image.CopyToAsync(stream);

            var entry = new ImageSource
            {
                Name = name,
                Code = code,
                Path = imageName
            };

            imageSources[code] = entry;

            SaveConcurrentDict("images.json", imageSources);

            return Results.Ok(entry);
        }).DisableAntiforgery();

        app.MapPost("/sender/setup", async (SetupNdiSenderModel senderDetails) =>
        {
            if(!imageSources.TryGetValue(senderDetails.ImageSourceCode, out var image))
            {
                return Results.BadRequest("No such image.");
            }

            if(ndiSenders.TryGetValue(senderDetails.SenderCode, out var sender))
            {
                sender.FrameRateNumerator = senderDetails.FrameRateNumerator;
                sender.FrameRateDenominator = senderDetails.FrameRateDenominator;
                sender.SendActualFrameRate = senderDetails.SendActualFrameRate;
                sender.UpdateSource(image);
            }
            else
            {
                sender = new NdiSender(
                    image,
                    senderDetails.Name,
                    senderDetails.SenderCode,
                    senderDetails.SendActualFrameRate,
                    senderDetails.FrameRateNumerator,
                    senderDetails.FrameRateDenominator);

                ndiSenders[senderDetails.SenderCode] = sender;
            }

            SaveConcurrentDict<NdiSender>("senders.json", ndiSenders);
            return Results.Ok();

        }).DisableAntiforgery();

        app.MapDelete("/sender/stop/{code}", (string code) =>
        {
            if (!ndiSenders.TryGetValue(code, out var sender)) 
            {
                return Results.NotFound("Sender not found.");
            }

            ndiSenders.Remove(code, out var _);

            sender.Dispose();

            SaveConcurrentDict("senders.json", ndiSenders);

            return Results.Ok();
        });

        app.MapGet("/image/{code}", (string code) =>
        {
            if (!imageSources.TryGetValue(code, out var image))
            {
                return Results.NotFound("Image not found.");
            }

            var fileExtension = Path.GetExtension(image.Path).ToLower();
            var contentType = fileExtension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };

            return Results.File(Path.Combine(ImageRootPath, image.Path), contentType);
        });

        var cts = new CancellationTokenSource();

        var hostTask = app.RunAsync(cts.Token);

        Console.WriteLine("Tractus Still Source 2 for NDI - Enter to exit.");
        Console.ReadLine();

        cts.Cancel();

        await app.StopAsync();
        await app.DisposeAsync();

        Console.WriteLine("Finished.");
    }

    private static ConcurrentDictionary<string, T>? LoadConcurrentDict<T>(string filePath)
    {
        filePath = Path.Combine(ImageRootPath, filePath);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = File.ReadAllText(filePath);

        return JsonSerializer.Deserialize<ConcurrentDictionary<string, T>>(json);
    }

    private static void SaveConcurrentDict<T>(string filePath, ConcurrentDictionary<string, T> dictionary)
    {
        filePath = Path.Combine(ImageRootPath, filePath);
        var json = JsonSerializer.Serialize(dictionary);
        File.WriteAllText(filePath, json);
    }
}
