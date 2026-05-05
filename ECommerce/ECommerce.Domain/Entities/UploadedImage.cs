using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.Domain.Entities
{
    public class UploadedImage
    {
        public string PublicId { get; private set; }
        public string SecureUrl { get; private set; }
        public string Format { get; private set; }
        public long Bytes { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private UploadedImage() { }

        public static UploadedImage Create(
        string publicId,
        string secureUrl,
        string format,
        long bytes,
        int width,
        int height)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(publicId);
            ArgumentException.ThrowIfNullOrWhiteSpace(secureUrl);

            return new UploadedImage
            {
                PublicId = publicId,
                SecureUrl = secureUrl,
                Format = format,
                Bytes = bytes,
                Width = width,
                Height = height,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
