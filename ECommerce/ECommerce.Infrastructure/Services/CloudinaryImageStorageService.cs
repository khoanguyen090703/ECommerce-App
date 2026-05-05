using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Services
{
    public class CloudinaryImageStorageService : IImageStorageService
    {
        private readonly Cloudinary _cloudinary;

        private readonly ILogger<CloudinaryImageStorageService> _logger;

        public CloudinaryImageStorageService(
            IOptions<CloudinarySettings> options,
            ILogger<CloudinaryImageStorageService> logger)
        {
            _logger = logger;

            var cfg = options.Value;

            var account = new Account(cfg.CloudName, cfg.ApiKey, cfg.ApiSecret);
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        public async Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default)
        {
            var publicId = CloudinaryUrlHelper.ExtractPublicId(imageUrl);

            var deletionParams = new DeletionParams(publicId);

            var result = await _cloudinary.DestroyAsync(deletionParams);

            if(result.Error is not null)
            {
                _logger.LogError("Cloudinary delete failed: {Message}", result.Error.Message);
                throw new InvalidOperationException($"Cloudinary delete failed: {result.Error.Message}");
            }

            if (result.Result != "ok")
            {
                _logger.LogWarning(
                    "Cloudinary delete returned unexpected result for PublicId {PublicId}: {Result}",
                    publicId,
                    result.Result);
            }
        }

        public async Task<UploadedImage> UploadAsync(
            Stream fileStream, 
            string fileName, 
            string? folder = null, 
            CancellationToken cancellationToken = default)
        {
            var publicId = BuildPublicId(fileName);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                PublicId = publicId,
                Folder = folder,
                Overwrite = false
            };

            _logger.LogDebug("Sending upload request to Cloudinary. PublicId: {publicId}", publicId);

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if(result.Error is not null)
            {
                _logger.LogError("Cloudinary upload failed: {Message}", result.Error.Message);
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");
            }

            return UploadedImage.Create(
                result.PublicId,
                result.SecureUrl.ToString(),
                result.Format,
                result.Bytes,
                result.Width,
                result.Height);
        }

        private static string BuildPublicId(string fileName)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var sanitized = string.Concat(nameWithoutExt.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            return $"{sanitized}_{Guid.NewGuid():N}";
        }
    }
}
