using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Infrastructure.Services
{
    public class CloudinaryUrlHelper
    {
        public static string ExtractPublicId(string cloudinaryUrl)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(cloudinaryUrl);

            var uri = new Uri(cloudinaryUrl);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // AbsolutePath: /demo/image/upload/v123/products/avatar_abc.jpg
            // segments:     ["demo", "image", "upload", "v123", "products", "avatar_abc.jpg"]

            int uploadIndex = Array.IndexOf(segments, "upload");

            if (uploadIndex < 0 || uploadIndex >= segments.Length - 1)
                throw new ArgumentException($"URL không đúng định dạng Cloudinary: {cloudinaryUrl}");

            // Bỏ qua "upload" và version token (bắt đầu bằng 'v' + số)
            var afterUpload = segments[(uploadIndex + 1)..];

            if (afterUpload.Length > 0 && IsVersionSegment(afterUpload[0]))
                afterUpload = afterUpload[1..];

            // Ghép lại folder + filename, bỏ extension
            var lastSegment = Path.GetFileNameWithoutExtension(afterUpload[^1]);
            var publicIdParts = afterUpload[..^1].Append(lastSegment);

            return string.Join("/", publicIdParts);
        }

        private static bool IsVersionSegment(string segment) =>
            segment.StartsWith('v') && long.TryParse(segment[1..], out _);
    }
}
