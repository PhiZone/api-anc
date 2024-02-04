using System.Web;
using LeanCloud;
using LeanCloud.Storage;
using Microsoft.Extensions.Options;
using PhiZoneApi.Configurations;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
using Romanization;

namespace PhiZoneApi.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IServiceProvider _provider;

    public FileStorageService(IOptions<TapTapSettings> options, IServiceProvider provider)
    {
        _provider = provider;
        LCApplication.Initialize(options.Value.ClientId, options.Value.ClientToken, options.Value.FileStorageUrl);
    }

    public async Task<(string, string)> Upload<T>(string fileName, IFormFile formFile)
    {
        using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);
        var extension = FileTypeResolver.GetFileExtension(FileTypeResolver.GetMimeType(formFile));
        var file = new LCFile(
            $"{typeof(T).Name}_{NormalizeFileName(fileName)}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{extension}",
            memoryStream.ToArray());
        await file.Save();
        return (file.Url, (string)file.MetaData["_checksum"]);
    }

    public async Task<(string, string)> UploadImage<T>(string fileName, IFormFile formFile, (int, int) aspectRatio)
    {
        await using var scope = _provider.CreateAsyncScope();
        var multimediaService = scope.ServiceProvider.GetService<IMultimediaService>()!;
        var stream = multimediaService.CropImage(formFile, aspectRatio);
        return await Upload<T>(fileName, stream, "webp");
    }

    public async Task<(string, string)> UploadImage<T>(string fileName, byte[] buffer, (int, int) aspectRatio)
    {
        await using var scope = _provider.CreateAsyncScope();
        var multimediaService = scope.ServiceProvider.GetService<IMultimediaService>()!;
        var stream = multimediaService.CropImage(buffer, aspectRatio);
        return await Upload<T>(fileName, stream, "webp");
    }

    public async Task<(string, string)> Upload<T>(string fileName, MemoryStream stream, string extension)
    {
        var file = new LCFile(
            $"{typeof(T).Name}_{NormalizeFileName(fileName)}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.{extension}",
            stream.ToArray());
        await file.Save();
        return (file.Url, (string)file.MetaData["_checksum"]);
    }

    public async Task SendUserInput(string url, string memberName, HttpRequest request, User user)
    {
        await using var scope = _provider.CreateAsyncScope();
        var messengerService = scope.ServiceProvider.GetService<IMessengerService>()!;
        await messengerService.SendUserInput(new UserInputDelivererDto
        {
            Content = url,
            IsImage = true,
            MemberName = memberName,
            ResourceId = (string?)request.RouteValues["id"],
            ActionName = (string)request.RouteValues["action"]!,
            ControllerName = (string)request.RouteValues["controller"]!,
            RequestMethod = request.Method,
            RequestPath = request.Path,
            UserId = user.Id,
            UserName = user.UserName,
            UserAvatar = user.Avatar,
            DateCreated = DateTimeOffset.UtcNow
        });
    }

    private static string NormalizeFileName(string input)
    {
        var chars = Path.GetInvalidFileNameChars().Concat([' ']);
        return HttpUtility.UrlEncode(chars.Aggregate(Romanize(input),
            (current, invalidChar) => current.Replace(invalidChar.ToString(), string.Empty)));
    }

    private static string Romanize(string input)
    {
        var japanese = new Japanese.ModifiedHepburn();
        input = japanese.Process(input);
        var chinese = new Chinese.HanyuPinyin();
        input = chinese.Process(input);
        var dictionary = new Dictionary<List<char>, char>
        {
            { ['ā', 'á', 'ǎ', 'à', 'ɑ'], 'a' },
            { ['ê', 'ē', 'é', 'ě', 'è'], 'e' },
            { ['ī', 'í', 'ǐ', 'ì'], 'i' },
            { ['ō', 'ó', 'ǒ', 'ò'], 'o' },
            { ['ū', 'ú', 'ǔ', 'ù', 'ǖ', 'ǘ', 'ǚ', 'ǜ', 'ü'], 'u' },
            { ['ń', 'ň'], 'n' }
        };
        foreach (var entry in dictionary)
        foreach (var character in entry.Key.Where(character => input.Contains(character)))
            input = input.Replace(character, entry.Value);
        var korean = new Korean.RevisedRomanization();
        input = korean.Process(input);
        var russian = new Russian.BgnPcgn();
        input = russian.Process(input);
        return input;
    }
}