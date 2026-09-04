using Kpmg.Web.Options;
using Microsoft.Extensions.Options;

namespace Kpmg.Web.Services;

public record StoredPhoto(string StoredFileName, string ContentType, long SizeInBytes);

public interface IPhotoStorage
{
    Task<StoredPhoto> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Opens a previously stored photo, or returns <c>null</c> when it no longer exists.</summary>
    Stream? OpenRead(string storedFileName);

    /// <summary>Removes a stored photo, ignoring files that are already gone.</summary>
    void Delete(string storedFileName);
}

public class FileSystemPhotoStorage : IPhotoStorage
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/heic"] = ".heic"
    };

    private readonly string _rootPath;

    public FileSystemPhotoStorage(IOptions<PhotoStorageOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.RootPath;
        _rootPath = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredPhoto> SaveAsync(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        // The extension is derived from the validated content type, never from user input.
        if (!ExtensionByContentType.TryGetValue(contentType, out var extension))
        {
            throw new InvalidOperationException($"Unsupported photo content type '{contentType}'.");
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, storedFileName);

        await using (var file = File.Create(fullPath))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        var size = new FileInfo(fullPath).Length;
        return new StoredPhoto(storedFileName, contentType, size);
    }

    public void Delete(string storedFileName)
    {
        var fullPath = ResolvePath(storedFileName);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    public Stream? OpenRead(string storedFileName)
    {
        var fullPath = ResolvePath(storedFileName);
        return fullPath is not null && File.Exists(fullPath)
            ? File.OpenRead(fullPath)
            : null;
    }

    private string? ResolvePath(string storedFileName)
    {
        // Defensive: only ever read a bare file name from inside the storage root.
        var safeName = Path.GetFileName(storedFileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return null;
        }

        return Path.Combine(_rootPath, safeName);
    }
}
