namespace Cleaning.BLL.Infrastructure.Storage
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync(Stream content, string fileName, string folder);
    }
}
