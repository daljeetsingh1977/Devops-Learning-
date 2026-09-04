namespace Kpmg.Web.Options;

public class PhotoStorageOptions
{
    public const string SectionName = "PhotoStorage";

    /// <summary>Directory (absolute, or relative to the content root) where photos are stored.</summary>
    public string RootPath { get; set; } = "App_Data/photos";

    public int MaxPhotosPerComplaint { get; set; } = 5;

    public long MaxFileSizeInBytes { get; set; } = 5 * 1024 * 1024;

    public List<string> AllowedContentTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic"
    };
}
