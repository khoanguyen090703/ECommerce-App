using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Application.Interfaces
{
    public interface IImageService
    {
        Task<UploadImageResponse> UploadAsync(
            UploadImageRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string imageUrl,
            CancellationToken cancellationToken = default);
    }
}
