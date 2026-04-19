using ECommerce.Domain.Enums;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class NameGenerators
    {
        public static string GenerateProductName(string brandName, string? line, ProductConcentration concentration)
        {
            var parts = new[] { brandName, line, concentration.ToString() };
            return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        public static string GenerateVariantName(string productName, VariantFormat format, int volumn, string unit)
        {
            var parts = new[] { productName, format.ToString(), volumn.ToString(), unit };
            return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}
