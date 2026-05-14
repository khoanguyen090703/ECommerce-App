using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Application.Services;

public class PaymentMethodServiceTests
{
    [Fact]
    public async Task GetAllAsync_WithoutIncludeInactive_CallsRepositoryAndMaps()
    {
        // Arrange
        var methods = new List<PaymentMethod>
        {
            new() { Id = 1, Name = "COD", IsActive = true }
        };
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(methods);
        var sut = new PaymentMethodService(repo.Object);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].Name.Should().Be("COD");
        repo.Verify(r => r.GetAllAsync(false), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithIncludeInactive_PassesFlagToRepository()
    {
        // Arrange
        var repo = new Mock<IPaymentMethodRepository>();
        repo.Setup(r => r.GetAllAsync(true)).ReturnsAsync(new List<PaymentMethod>());
        var sut = new PaymentMethodService(repo.Object);

        // Act
        var result = await sut.GetAllAsync(includeInactive: true);

        // Assert
        result.Should().BeEmpty();
        repo.Verify(r => r.GetAllAsync(true), Times.Once);
    }
}
