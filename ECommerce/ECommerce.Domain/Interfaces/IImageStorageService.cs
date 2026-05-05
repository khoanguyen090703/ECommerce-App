using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Interfaces
{
    public interface IImageStorageService
    {
        Task<UploadedImage> UploadAsync(
        Stream fileStream,
        string fileName,
        string? folder = null,
        CancellationToken cancellationToken = default);

        Task DeleteAsync(string imageUrl, CancellationToken cancellationToken = default);
    }
}
