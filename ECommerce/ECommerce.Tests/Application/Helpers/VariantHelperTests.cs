using ECommerce.Application.Helpers;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.Tests.Application.Helpers;

public class VariantHelperTests
{
    [Fact]
    public void ExistsFormatVolumnDuplicate_WhenVariantsNull_ReturnsFalse()
    {
        // Arrange
        IEnumerable<ProductVariant>? variants = null;

        // Act
        var result = VariantHelper.ExistsFormatVolumnDuplicate(variants!, VariantFormat.Mini, 30, null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExistsFormatVolumnDuplicate_WhenExcludeIdMatchesSelf_ReturnsFalse()
    {
        // Arrange
        var v1 = new ProductVariant { Id = 1, Format = VariantFormat.Mini, Volumn = 30 };
        var variants = new List<ProductVariant> { v1 };

        // Act
        var result = VariantHelper.ExistsFormatVolumnDuplicate(variants, VariantFormat.Mini, 30, excludeVariantId: 1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExistsFormatVolumnDuplicate_WhenAnotherVariantMatches_ReturnsTrue()
    {
        // Arrange
        var v1 = new ProductVariant { Id = 1, Format = VariantFormat.Mini, Volumn = 30 };
        var v2 = new ProductVariant { Id = 2, Format = VariantFormat.Mini, Volumn = 30 };
        var variants = new List<ProductVariant> { v1, v2 };

        // Act
        var result = VariantHelper.ExistsFormatVolumnDuplicate(variants, VariantFormat.Mini, 30, excludeVariantId: 1);

        // Assert
        result.Should().BeTrue();
    }
}
