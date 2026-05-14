using ECommerce.Application.Services;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Application.Services;

public class BrandServiceTests
{
    [Fact]
    public async Task GetAllBrandsAsync_ReturnsMappedResponses()
    {
        // Arrange
        var brands = new List<Brand>
        {
            new() { Id = 1, Name = "Brand A", ImageUrl = "https://x/a.png" }
        };
        var repo = new Mock<IBrandRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(brands);
        var sut = new BrandService(repo.Object);

        // Act
        var result = await sut.GetAllBrandsAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Brand A");
    }

    [Fact]
    public async Task GetBrandsAsync_ReturnsPagedMappedResponses()
    {
        // Arrange
        var paged = new PagedResult<Brand>(
            new List<Brand> { new() { Id = 2, Name = "B", ImageUrl = null } },
            1,
            1,
            10);
        var repo = new Mock<IBrandRepository>();
        repo.Setup(r => r.GetAsync(It.IsAny<BrandQueryParams>())).ReturnsAsync(paged);
        var sut = new BrandService(repo.Object);

        // Act
        var result = await sut.GetBrandsAsync(new BrandQueryParams());

        // Assert
        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("B");
    }
}
