using System;
using System.Collections.Generic;
using System.Text;

namespace ECommerce.SharedViewModels.DTOs.Auth
{
    public class ResendEmailConfirmationRequest
    {
        public string Email { get; set; } = default!;
    }
}
