using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services
{
    public class ImageService : IImageService
    {
        private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff"];

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        private readonly IImageStorageService _imageStorageService;

        private readonly ILogger<ImageService> _logger;

        public ImageService(IImageStorageService imageStorageService, ILogger<ImageService> logger)
        {
            _imageStorageService = imageStorageService;
            _logger = logger;
        }

        public async Task DeleteAsync(
            string imageUrl, 
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl, nameof(imageUrl));

            _logger.LogInformation("Deleting image: {imageUrl}", imageUrl);

            await _imageStorageService.DeleteAsync(imageUrl, cancellationToken);

            _logger.LogInformation("Image deleted: {imageUrl}", imageUrl);
        }

        public async Task<UploadImageResponse> UploadAsync(
            UploadImageRequest request, 
            CancellationToken cancellationToken = default)
        {
            ValidateFile(request);

            _logger.LogInformation(
            "Uploading image {FileName} ({Bytes} bytes) to folder '{Folder}'",
            request.FileName,
            request.FileStream.Length,
            request.Folder ?? "root");

            await using var stream = request.FileStream;

            var uploaded = await _imageStorageService.UploadAsync(
                    stream,
                    request.FileName,
                    request.Folder,
                    cancellationToken
                );

            _logger.LogInformation("Image uploaded successfully. PublicId: {PublicId}", uploaded.PublicId);

            return new UploadImageResponse
            {
                PublicId = uploaded.PublicId,
                SecureUrl = uploaded.SecureUrl,
                Format = uploaded.Format,
                Bytes = uploaded.Bytes,
                Width = uploaded.Width,
                Height = uploaded.Height,
                CreatedAt = uploaded.CreatedAt
            };
        }

        private static void ValidateFile(UploadImageRequest request)
        {
            if (request.FileStream is null || request.FileStream.Length == 0)
                throw new ArgumentException("File is empty or missing.");

            if (request.FileStream.Length > MaxFileSizeBytes)
                throw new ArgumentException(
                    $"File size exceeds the maximum allowed size of {MaxFileSizeBytes / 1024 / 1024} MB.");

            var extension = Path.GetExtension(request.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException(
                    $"File extension '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");
        }
    }
}
