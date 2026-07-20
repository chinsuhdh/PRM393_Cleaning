namespace Cleaning.BLL.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync(Stream content, string fileName, string folder);
    }
}
