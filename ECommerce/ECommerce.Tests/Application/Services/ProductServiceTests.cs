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

public class ProductServiceTests
{
    [Fact]
    public async Task DeleteProductByIdAsync_WhenMissing_ThrowsNotFoundException()
    {
        // Arrange
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<bool>())).ReturnsAsync((Product?)null);
        var sut = CreateSut(productRepo);

        // Act
        var act = async () => await sut.DeleteProductByIdAsync(1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteProductByIdAsync_WhenNotDraft_ThrowsConflictException()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "P", Status = ProductStatus.Active, Description = "d", Brand = new Brand { Name = "B" }, Line = "L" };
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<bool>())).ReturnsAsync(product);
        var sut = CreateSut(productRepo);

        // Act
        var act = async () => await sut.DeleteProductByIdAsync(1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        productRepo.Verify(r => r.DeleteAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenMissing_ThrowsNotFoundException()
    {
        // Arrange
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(2, true)).ReturnsAsync((Product?)null);
        var sut = CreateSut(productRepo);

        // Act
        var act = async () => await sut.GetProductByIdAsync(2);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddAsync_WhenDuplicateVariantKeysInRequest_ThrowsConflictException()
    {
        // Arrange
        var brand = new Brand { Id = 1, Name = "Chanel" };
        var category = new Category { Id = 1, Name = "Cat" };
        var scent = new ScentFamily { Id = 1, Name = "Woody" };
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.IsNameExistedAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        var categoryRepo = new Mock<ICategoryRepository>();
        categoryRepo.Setup(r => r.GetById(1)).ReturnsAsync(category);
        var brandRepo = new Mock<IBrandRepository>();
        brandRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(brand);
        var scentRepo = new Mock<IScentFamilyRepository>();
        scentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(scent);
        var createVal = new AlwaysValidValidator<CreateProductRequest>();
        var updateVal = new AlwaysValidValidator<UpdateProductRequest>();
        var sut = new ProductService(productRepo.Object, categoryRepo.Object, brandRepo.Object, scentRepo.Object, createVal, updateVal);

        var request = new CreateProductRequest
        {
            Description = "D",
            BrandId = 1,
            CategoryIds = new List<int> { 1 },
            ScentFamilyIds = new List<int> { 1 },
            Line = "Line",
            Concentration = ProductConcentration.EDP,
            Variants = new List<CreateProductVariantRequest>
            {
                new() { Format = VariantFormat.FullBottle, Volumn = 50, Price = 1m, StockQuantity = 1 },
                new() { Format = VariantFormat.FullBottle, Volumn = 50, Price = 2m, StockQuantity = 1 }
            }
        };

        // Act
        var act = async () => await sut.AddAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Duplicate variants*");
        productRepo.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductStatusAsync_WhenActivatingWithoutAvailableVariant_ThrowsConflictException()
    {
        // Arrange
        var variant = new ProductVariant
        {
            Id = 1,
            Status = VariantStatus.OutOfStock,
            StockQuantity = 0,
            Name = "V",
            Product = null!
        };
        var product = new Product
        {
            Id = 3,
            Name = "P",
            Status = ProductStatus.Draft,
            Description = "d",
            Brand = new Brand { Name = "B" },
            Line = "L",
            ProductVariants = new List<ProductVariant> { variant }
        };
        variant.Product = product;

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(3, It.IsAny<bool>())).ReturnsAsync(product);
        var sut = CreateSut(productRepo);

        // Act
        var act = async () => await sut.UpdateProductStatusAsync(3, ProductStatus.Active);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Available*");
        productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task GetProductVariantsByProductIdAsync_WhenProductMissing_ThrowsNotFoundException()
    {
        // Arrange
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.ExistsAsync(9)).ReturnsAsync(false);
        var sut = CreateSut(productRepo);

        // Act
        var act = async () => await sut.GetProductVariantsByProductIdAsync(9, new ProductVariantsQueryParams());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static ProductService CreateSut(Mock<IProductRepository> productRepo)
    {
        var categoryRepo = new Mock<ICategoryRepository>();
        var brandRepo = new Mock<IBrandRepository>();
        var scentRepo = new Mock<IScentFamilyRepository>();
        var createVal = new AlwaysValidValidator<CreateProductRequest>();
        var updateVal = new AlwaysValidValidator<UpdateProductRequest>();
        return new ProductService(productRepo.Object, categoryRepo.Object, brandRepo.Object, scentRepo.Object, createVal, updateVal);
    }
}
