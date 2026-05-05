using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Request
{
    public class UploadImageRequest
    {
        public Stream FileStream { get; set; } = default!;

        public string FileName { get; set; } = default!;

        public string? Folder { get; set; } = null;
    }
}
