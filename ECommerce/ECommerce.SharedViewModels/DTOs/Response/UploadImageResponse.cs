using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Response
{
    public class UploadImageResponse
    {
        public string PublicId { get; set; } = default!;

        public string SecureUrl { get; set; } = default!;

        public string Format { get; set; } = default!;

        public long Bytes { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
