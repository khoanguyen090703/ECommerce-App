using ECommerce.Application.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.Tests.Application;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Application.Services;

public class CategoryServiceTests
{
    [Fact]
    public async Task GetCategoryByIdAsync_WhenMissing_ThrowsNotFoundException()
    {
        // Arrange
        var categoryRepo = new Mock<ICategoryRepository>();
        categoryRepo.Setup(r => r.GetById(99)).ReturnsAsync((Category?)null);
        var createVal = new AlwaysValidValidator<CreateCategoryRequest>();
        var updateVal = new AlwaysValidValidator<UpdateCategoryRequest>();
        var sut = new CategoryService(categoryRepo.Object, createVal, updateVal);

        // Act
        var act = async () => await sut.GetCategoryByIdAsync(99);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteCategoryByIdAsync_WhenHasProducts_ThrowsConflictException()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "C", Description = "D", ImageUrl = null };
        var categoryRepo = new Mock<ICategoryRepository>();
        categoryRepo.Setup(r => r.GetById(1)).ReturnsAsync(category);
        categoryRepo.Setup(r => r.HasProductsAsync(1)).ReturnsAsync(true);
        var createVal = new AlwaysValidValidator<CreateCategoryRequest>();
        var updateVal = new AlwaysValidValidator<UpdateCategoryRequest>();
        var sut = new CategoryService(categoryRepo.Object, createVal, updateVal);

        // Act
        var act = async () => await sut.DeleteCategoryByIdAsync(1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        categoryRepo.Verify(r => r.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task CreateProductAsync_WhenNameExists_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateCategoryRequest { Name = "Dup", Description = "Valid description.", ImageUrl = null };
        var categoryRepo = new Mock<ICategoryRepository>();
        categoryRepo.Setup(r => r.IsNameExistedAsync("Dup")).ReturnsAsync(true);
        var createVal = new AlwaysValidValidator<CreateCategoryRequest>();
        var updateVal = new AlwaysValidValidator<UpdateCategoryRequest>();
        var sut = new CategoryService(categoryRepo.Object, createVal, updateVal);

        // Act
        var act = async () => await sut.CreateProductAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        categoryRepo.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsPagedMappedItems()
    {
        // Arrange
        var paged = new PagedResult<Category>(
            new List<Category> { new() { Id = 2, Name = "Books", Description = null, ImageUrl = null } },
            1,
            1,
            10);
        var categoryRepo = new Mock<ICategoryRepository>();
        categoryRepo.Setup(r => r.GetAsync(It.IsAny<CategoryQueryParams>())).ReturnsAsync(paged);
        var createVal = new AlwaysValidValidator<CreateCategoryRequest>();
        var updateVal = new AlwaysValidValidator<UpdateCategoryRequest>();
        var sut = new CategoryService(categoryRepo.Object, createVal, updateVal);

        // Act
        var result = await sut.GetCategoriesAsync(new CategoryQueryParams());

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Books");
    }
}
