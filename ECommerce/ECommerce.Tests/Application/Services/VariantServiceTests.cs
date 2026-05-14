using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.Tests.Application;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Application.Services;

public class VariantServiceTests
{
    [Fact]
    public async Task GetVariantDetailsByIdAsync_WhenMissing_ReturnsNull()
    {
        // Arrange
        var productRepo = new Mock<IProductRepository>();
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ProductVariant?)null);
        var sut = CreateSut(productRepo, variantRepo);

        // Act
        var result = await sut.GetVariantDetailsByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateVariantAsync_WhenDuplicateFormatAndVolume_ThrowsConflictException()
    {
        // Arrange
        var existing = new ProductVariant { Id = 1, Format = VariantFormat.Mini, Volumn = 30, Name = "Old", Product = null! };
        var product = new Product
        {
            Id = 5,
            Name = "Prod",
            Status = ProductStatus.Draft,
            Description = "d",
            Brand = new Brand { Name = "B" },
            Line = "L",
            ProductVariants = new List<ProductVariant> { existing }
        };
        existing.Product = product;

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(product);
        var variantRepo = new Mock<IProductVariantRepository>();
        var sut = CreateSut(productRepo, variantRepo);

        var request = new CreateVariantRequest
        {
            Format = VariantFormat.Mini,
            Volumn = 30,
            Price = 9m,
            StockQuantity = 1,
            Images = new List<string>()
        };

        // Act
        var act = async () => await sut.CreateVariantAsync(5, request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateVariantStatusByIdAsync_WhenSameStatus_ThrowsConflictException()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "P", Description = "d", Brand = new Brand { Name = "B" }, Line = "L" };
        var variant = new ProductVariant
        {
            Id = 2,
            Status = VariantStatus.Available,
            StockQuantity = 2,
            Name = "V",
            Product = product
        };
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdForUpdateAsync(2)).ReturnsAsync(variant);
        var productRepo = new Mock<IProductRepository>();
        var sut = CreateSut(productRepo, variantRepo);

        // Act
        var act = async () => await sut.UpdateVariantStatusByIdAsync(2, VariantStatus.Available);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteVariantByIdAsync_WhenProductNotDraft_ThrowsConflictException()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "P", Status = ProductStatus.Active, Description = "d", Brand = new Brand { Name = "B" }, Line = "L" };
        var variant = new ProductVariant { Id = 3, Name = "V", Product = product };
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(variant);
        var productRepo = new Mock<IProductRepository>();
        var sut = CreateSut(productRepo, variantRepo);

        // Act
        var act = async () => await sut.DeleteVariantByIdAsync(3);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        variantRepo.Verify(r => r.DeleteAsync(It.IsAny<ProductVariant>()), Times.Never);
    }

    [Fact]
    public async Task GetVariantsForStockRestockAsync_WhenInvalidStatusFilter_ThrowsArgumentException()
    {
        // Arrange
        var productRepo = new Mock<IProductRepository>();
        var variantRepo = new Mock<IProductVariantRepository>();
        var sut = CreateSut(productRepo, variantRepo);
        var parameters = new RestockVariantQueryParams { Status = VariantStatus.Discontinued };

        // Act
        var act = async () => await sut.GetVariantsForStockRestockAsync(parameters);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        productRepo.Verify(
            r => r.GetVariantsForRestockListingAsync(It.IsAny<RestockVariantQueryParams>()),
            Times.Never);
    }

    [Fact]
    public async Task AddStockToVariantsAsync_WhenVariantDiscontinued_ThrowsConflictException()
    {
        // Arrange
        var v = new ProductVariant { Id = 10, StockQuantity = 1, Status = VariantStatus.Discontinued, Name = "X", Product = new Product { Name = "P", Description = "d", Brand = new Brand { Name = "B" }, Line = "L" } };
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<ProductVariant> { v });
        var productRepo = new Mock<IProductRepository>();
        var addStockVal = new AlwaysValidValidator<AddVariantStockBatchRequest>();
        var sut = new VariantService(
            productRepo.Object,
            variantRepo.Object,
            new AlwaysValidValidator<UpdateVariantRequest>(),
            new AlwaysValidValidator<CreateVariantRequest>(),
            addStockVal);

        var request = new AddVariantStockBatchRequest
        {
            Items = new List<AddVariantStockLineRequest> { new() { VariantId = 10, QuantityToAdd = 5 } }
        };

        // Act
        var act = async () => await sut.AddStockToVariantsAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*discontinued*");
    }

    [Fact]
    public async Task UpdateVariantStatusByIdAsync_WhenOutOfStockButStockPositive_ThrowsConflictException()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "P", Description = "d", Brand = new Brand { Name = "B" }, Line = "L" };
        var variant = new ProductVariant
        {
            Id = 7,
            Status = VariantStatus.Available,
            StockQuantity = 3,
            Name = "V",
            Product = product
        };
        var variantRepo = new Mock<IProductVariantRepository>();
        variantRepo.Setup(r => r.GetByIdForUpdateAsync(7)).ReturnsAsync(variant);
        var productRepo = new Mock<IProductRepository>();
        var sut = CreateSut(productRepo, variantRepo);

        // Act
        var act = async () => await sut.UpdateVariantStatusByIdAsync(7, VariantStatus.OutOfStock);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*greater than 0*");
    }

    private static VariantService CreateSut(Mock<IProductRepository> productRepo, Mock<IProductVariantRepository> variantRepo)
    {
        return new VariantService(
            productRepo.Object,
            variantRepo.Object,
            new AlwaysValidValidator<UpdateVariantRequest>(),
            new AlwaysValidValidator<CreateVariantRequest>(),
            new AlwaysValidValidator<AddVariantStockBatchRequest>());
    }
}
