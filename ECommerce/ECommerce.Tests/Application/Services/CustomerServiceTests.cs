using ECommerce.Application.Services;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Domain.QueryParameters;
using FluentAssertions;
using Moq;

namespace ECommerce.Tests.Application.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task GetCustomersAsync_WhenRepositoryReturnsCustomers_MapsEmailsFromLookup()
    {
        // Arrange
        var identityId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            FullName = "Nguyen Van A",
            Address = "HN",
            AvatarUrl = null,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = null
        };
        var paged = new PagedResult<Customer>(new List<Customer> { customer }, 1, 1, 20);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo
            .Setup(r => r.GetAsync(It.IsAny<CustomerQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);
        customerRepo
            .Setup(r => r.GetEmailsByIdentityIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string?> { { identityId, "a@example.com" } });
        var sut = new CustomerService(customerRepo.Object);

        // Act
        var result = await sut.GetCustomersAsync(new CustomerQueryParams());

        // Assert
        result.Items.Should().ContainSingle();
        result.Items[0].Email.Should().Be("a@example.com");
        result.Items[0].FullName.Should().Be("Nguyen Van A");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCustomersAsync_WhenEmailMissingInLookup_ReturnsNullEmail()
    {
        // Arrange
        var identityId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            FullName = "B",
            Address = null,
            AvatarUrl = null,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = null
        };
        var paged = new PagedResult<Customer>(new List<Customer> { customer }, 1, 1, 20);
        var customerRepo = new Mock<ICustomerRepository>();
        customerRepo
            .Setup(r => r.GetAsync(It.IsAny<CustomerQueryParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);
        customerRepo
            .Setup(r => r.GetEmailsByIdentityIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string?>());
        var sut = new CustomerService(customerRepo.Object);

        // Act
        var result = await sut.GetCustomersAsync(new CustomerQueryParams());

        // Assert
        result.Items[0].Email.Should().BeNull();
    }
}
