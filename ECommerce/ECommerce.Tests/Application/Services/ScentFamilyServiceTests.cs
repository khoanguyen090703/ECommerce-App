using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Application.Services;

public class ScentFamilyServiceTests
{
    [Fact]
    public async Task GetAllScentFamiliesAsync_ReturnsMappedResponses()
    {
        // Arrange
        var items = new List<ScentFamily>
        {
            new() { Id = 1, Name = "Woody", Description = "D" }
        };
        var repo = new Mock<IScentFamilyRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
        var sut = new ScentFamilyService(repo.Object);

        // Act
        var result = await sut.GetAllScentFamiliesAsync();

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Woody");
    }
}
