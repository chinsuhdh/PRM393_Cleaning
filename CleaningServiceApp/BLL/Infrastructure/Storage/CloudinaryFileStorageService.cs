using Cleaning.BLL.Common;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Cleaning.BLL.Infrastructure.Storage
{
    public class CloudinaryFileStorageService : IFileStorageService
    {
        private readonly CloudinarySettings _config;
        private readonly Cloudinary? _cloudinary;

        public CloudinaryFileStorageService(IOptions<CloudinarySettings> config)
        {
            _config = config.Value;
            _cloudinary = _config.UseMock
                ? null
                : new Cloudinary(new Account(_config.CloudName, _config.ApiKey, _config.ApiSecret)) { Api = { Secure = true } };
        }

        public async Task<string> UploadAsync(Stream content, string fileName, string folder)
        {
            if (_cloudinary == null)
            {
                return $"https://res.cloudinary.com/mock/{folder}/{fileName}";
            }

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, content),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }
    }
}
