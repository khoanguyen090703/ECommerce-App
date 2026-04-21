using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ECommerce.Application.Helpers
{
    public static class VariantHelper
    {
        public static bool ExistsFormatVolumnDuplicate(IEnumerable<ProductVariant> variants, VariantFormat format, int volumn, int? excludeVariantId = null)
        {
            if (variants == null) return false;
            return variants.Any(v => (!excludeVariantId.HasValue || v.Id != excludeVariantId.Value)
                                      && v.Format == format
                                      && v.Volumn == volumn);
        }
    }
}
