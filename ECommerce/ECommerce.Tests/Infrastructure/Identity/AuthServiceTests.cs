using System.Security.Claims;
using System.Text;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using ECommerce.SharedViewModels.DTOs.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ECommerce.Tests.Infrastructure.Identity;

public class AuthServiceTests
{
    private static IConfiguration CreateClientConfiguration()
    {
        // Arrange helper: frontend URL required when sending confirmation emails
        var cfg = new Mock<IConfiguration>();
        cfg.Setup(c => c["ClientSettings:FrontendUrl"]).Returns("https://client.test");
        cfg.Setup(c => c["ClientSettings:EmailConfirmationPath"]).Returns("/auth/confirm-email");
        return cfg.Object;
    }

    private static async Task<(ServiceProvider Sp, UserManager<AppUser> UserManager, RoleManager<IdentityRole<Guid>> RoleManager)> CreateIdentityHostAsync()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddDataProtection();
        services.AddIdentity<AppUser, IdentityRole<Guid>>(_ => { })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync("Customer"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>("Customer"));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to seed Customer role: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        return (sp, userManager, roleManager);
    }

    private static AuthService CreateSut(
        UserManager<AppUser> userManager,
        ITokenService tokenService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ICustomerRepository customerRepository,
        IConfiguration configuration)
    {
        return new AuthService(userManager, tokenService, emailService, unitOfWork, customerRepository, configuration);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenUserMissing_ReturnsFailure()
    {
        // Arrange
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        await db.Database.EnsureCreatedAsync();
        var userStore = new UserStore<AppUser, IdentityRole<Guid>, AppDbContext, Guid>(db);
        var userManager = new UserManager<AppUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<AppUser>>.Instance);
        var token = new Mock<ITokenService>();
        var email = new Mock<IEmailService>();
        var uow = new Mock<IUnitOfWork>();
        var customers = new Mock<ICustomerRepository>();
        var sut = CreateSut(userManager, token.Object, email.Object, uow.Object, customers.Object, CreateClientConfiguration());

        // Act
        var response = await sut.ConfirmEmailAsync(Guid.NewGuid().ToString(), WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("x")));

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Be("User not found.");
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenTokenValid_ReturnsSuccess()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "u@test.com", Email = "u@test.com", EmailConfirmed = false };
            await userManager.CreateAsync(user, "TestPass1!");
            var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
            var token = new Mock<ITokenService>();
            var email = new Mock<IEmailService>();
            var uow = new Mock<IUnitOfWork>();
            var customers = new Mock<ICustomerRepository>();
            var sut = CreateSut(userManager, token.Object, email.Object, uow.Object, customers.Object, CreateClientConfiguration());

            // Act
            var response = await sut.ConfirmEmailAsync(user.Id.ToString(), encoded);

            // Assert
            response.IsSuccess.Should().BeTrue();
            response.Message.Should().Be("Email confirmation success.");
        }
    }

    [Fact]
    public async Task LogoutAsync_WhenUserMissing_ReturnsFailure()
    {
        // Arrange
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        await db.Database.EnsureCreatedAsync();
        var userStore = new UserStore<AppUser, IdentityRole<Guid>, AppDbContext, Guid>(db);
        var userManager = new UserManager<AppUser>(
            userStore,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<AppUser>>.Instance);
        var sut = CreateSut(
            userManager,
            Mock.Of<ITokenService>(),
            Mock.Of<IEmailService>(),
            Mock.Of<IUnitOfWork>(),
            Mock.Of<ICustomerRepository>(),
            CreateClientConfiguration());

        // Act
        var response = await sut.LogoutAsync(Guid.NewGuid().ToString());

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.Message.Should().Be("User not found.");
    }

    [Fact]
    public async Task LogoutAsync_WhenUserExists_ClearsRefreshTokenAndReturnsSuccess()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser
            {
                UserName = "out@test.com",
                Email = "out@test.com",
                EmailConfirmed = true,
                RefreshToken = "old-rt",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
            };
            await userManager.CreateAsync(user, "TestPass1!");
            var sut = CreateSut(
                userManager,
                Mock.Of<ITokenService>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.LogoutAsync(user.Id.ToString());

            // Assert
            response.IsSuccess.Should().BeTrue();
            var reloaded = await userManager.FindByIdAsync(user.Id.ToString());
            reloaded!.RefreshToken.Should().BeNull();
            reloaded.RefreshTokenExpiryTime.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenPrincipalNull_ReturnsFailure()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var token = new Mock<ITokenService>();
            token.Setup(t => t.GetPrincipalFromExpiredToken("bad")).Returns((ClaimsPrincipal?)null);
            var sut = CreateSut(
                userManager,
                token.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.RefreshTokenAsync(new RefreshTokenRequest { AccessToken = "bad", RefreshToken = "rt" });

            // Assert
            response.IsSuccess.Should().BeFalse();
            response.Message.Should().Be("Invalid access token.");
        }
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenRefreshInvalid_ReturnsFailure()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "rt@test.com", Email = "rt@test.com", EmailConfirmed = true };
            await userManager.CreateAsync(user, "TestPass1!");
            user.RefreshToken = "stored";
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1);
            await userManager.UpdateAsync(user);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "rt@test.com") }));
            var token = new Mock<ITokenService>();
            token.Setup(t => t.GetPrincipalFromExpiredToken("expired")).Returns(principal);

            var sut = CreateSut(
                userManager,
                token.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.RefreshTokenAsync(new RefreshTokenRequest { AccessToken = "expired", RefreshToken = "wrong" });

            // Assert
            response.IsSuccess.Should().BeFalse();
            response.Message.Should().Be("Invalid refresh token or refresh token is expired.");
        }
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenValid_ReturnsNewTokens()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "ok@test.com", Email = "ok@test.com", EmailConfirmed = true };
            await userManager.CreateAsync(user, "TestPass1!");
            user.RefreshToken = "old-refresh";
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1);
            await userManager.UpdateAsync(user);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, "ok@test.com") }));
            var token = new Mock<ITokenService>();
            token.Setup(t => t.GetPrincipalFromExpiredToken("access")).Returns(principal);
            token.Setup(t => t.GenerateJwtToken(user.Id.ToString(), "ok@test.com", It.IsAny<IList<string>>())).Returns("new-jwt");
            token.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");

            var sut = CreateSut(
                userManager,
                token.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.RefreshTokenAsync(new RefreshTokenRequest { AccessToken = "access", RefreshToken = "old-refresh" });

            // Assert
            response.IsSuccess.Should().BeTrue();
            response.Token.Should().Be("new-jwt");
            response.RefreshToken.Should().Be("new-refresh");
            var reloaded = await userManager.FindByEmailAsync("ok@test.com");
            reloaded!.RefreshToken.Should().Be("new-refresh");
        }
    }

    [Fact]
    public async Task ResendEmailConfirmationAsync_WhenUserUnknown_ReturnsGenericSuccess()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var email = new Mock<IEmailService>();
            var sut = CreateSut(
                userManager,
                Mock.Of<ITokenService>(),
                email.Object,
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.ResendEmailConfirmationAsync(new ResendEmailConfirmationRequest { Email = "nobody@test.com" });

            // Assert
            response.IsSuccess.Should().BeTrue();
            email.Verify(
                e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }
    }

    [Fact]
    public async Task ResendEmailConfirmationAsync_WhenAlreadyConfirmed_ReturnsFailure()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "conf@test.com", Email = "conf@test.com", EmailConfirmed = true };
            await userManager.CreateAsync(user, "TestPass1!");
            var sut = CreateSut(
                userManager,
                Mock.Of<ITokenService>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.ResendEmailConfirmationAsync(new ResendEmailConfirmationRequest { Email = "conf@test.com" });

            // Assert
            response.IsSuccess.Should().BeFalse();
            response.Message.Should().Contain("already confirmed");
        }
    }

    [Fact]
    public async Task SignInAsync_WhenCredentialsInvalid_ReturnsFailure()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "si@test.com", Email = "si@test.com", EmailConfirmed = true };
            await userManager.CreateAsync(user, "TestPass1!");
            var sut = CreateSut(
                userManager,
                Mock.Of<ITokenService>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.SignInAsync(new SignInRequest { Email = "si@test.com", Password = "WrongPass1!" });

            // Assert
            response.IsSuccess.Should().BeFalse();
            response.Message.Should().Be("Username or password is not correct.");
        }
    }

    [Fact]
    public async Task SignInAsync_WhenEmailNotConfirmed_ReturnsFailure()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "nc@test.com", Email = "nc@test.com", EmailConfirmed = false };
            await userManager.CreateAsync(user, "TestPass1!");
            var sut = CreateSut(
                userManager,
                Mock.Of<ITokenService>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.SignInAsync(new SignInRequest { Email = "nc@test.com", Password = "TestPass1!" });

            // Assert
            response.IsSuccess.Should().BeFalse();
            response.Message.Should().Contain("not confirmed");
        }
    }

    [Fact]
    public async Task SignInAsync_WhenValid_ReturnsTokens()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "ok2@test.com", Email = "ok2@test.com", EmailConfirmed = false };
            await userManager.CreateAsync(user, "TestPass1!");
            var confirm = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await userManager.ConfirmEmailAsync(user, confirm);

            var token = new Mock<ITokenService>();
            token.Setup(t => t.GenerateJwtToken(user.Id.ToString(), "ok2@test.com", It.IsAny<IList<string>>())).Returns("jwt-1");
            token.Setup(t => t.GenerateRefreshToken()).Returns("rt-1");

            var sut = CreateSut(
                userManager,
                token.Object,
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.SignInAsync(new SignInRequest { Email = "ok2@test.com", Password = "TestPass1!" });

            // Assert
            response.IsSuccess.Should().BeTrue();
            response.Token.Should().Be("jwt-1");
            response.RefreshToken.Should().Be("rt-1");
        }
    }

    [Fact]
    public async Task SignUpAsync_WhenEmailTaken_ReturnsFailure()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            await userManager.CreateAsync(
                new AppUser { UserName = "dup@test.com", Email = "dup@test.com", EmailConfirmed = true },
                "TestPass1!");
            var sut = CreateSut(
                userManager,
                Mock.Of<ITokenService>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.SignUpAsync(new SignUpRequest
            {
                Email = "dup@test.com",
                Password = "OtherPass1!",
                FullName = "Dup"
            });

            // Assert
            response.IsSuccess.Should().BeFalse();
            response.Message.Should().Be("Email is already registered.");
        }
    }

    [Fact]
    public async Task SignUpAsync_WhenValid_CreatesUserCustomerSendsEmailAndCommits()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var token = Mock.Of<ITokenService>();
            var email = new Mock<IEmailService>();
            email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            var customers = new Mock<ICustomerRepository>();
            customers.Setup(c => c.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var sut = CreateSut(userManager, token, email.Object, uow.Object, customers.Object, CreateClientConfiguration());

            // Act
            var response = await sut.SignUpAsync(new SignUpRequest
            {
                Email = "newuser@test.com",
                Password = "TestPass1!",
                FullName = "New User"
            });

            // Assert
            response.IsSuccess.Should().BeTrue();
            (await userManager.FindByEmailAsync("newuser@test.com")).Should().NotBeNull();
            customers.Verify(c => c.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
            uow.Verify(u => u.BeginTransactionAsync(), Times.Once);
            uow.Verify(u => u.CommitTransactionAsync(), Times.Once);
            uow.Verify(u => u.RollbackTransactionAsync(), Times.Never);
            email.Verify(e => e.SendEmailAsync("newuser@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenTokenInvalid_ReturnsFailure()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "badtok@test.com", Email = "badtok@test.com", EmailConfirmed = false };
            await userManager.CreateAsync(user, "TestPass1!");
            var badEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("not-the-real-token"));
            var sut = CreateSut(
                userManager,
                Mock.Of<ITokenService>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.ConfirmEmailAsync(user.Id.ToString(), badEncoded);

            // Assert
            response.IsSuccess.Should().BeFalse();
            response.Message.Should().Be("Email confirmation failed.");
        }
    }

    [Fact]
    public async Task SignUpAsync_WhenFrontendUrlMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var cfg = new Mock<IConfiguration>();
            cfg.Setup(c => c["ClientSettings:FrontendUrl"]).Returns(string.Empty);
            cfg.Setup(c => c["ClientSettings:EmailConfirmationPath"]).Returns("/Auth/ConfirmEmail");
            var uow = new Mock<IUnitOfWork>();
            uow.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
            uow.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            var customers = new Mock<ICustomerRepository>();
            customers.Setup(c => c.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var sut = CreateSut(userManager, Mock.Of<ITokenService>(), Mock.Of<IEmailService>(), uow.Object, customers.Object, cfg.Object);

            // Act
            var act = async () => await sut.SignUpAsync(new SignUpRequest
            {
                Email = "nofe@test.com",
                Password = "TestPass1!",
                FullName = "N"
            });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }

    [Fact]
    public async Task ResendEmailConfirmationAsync_WhenEmailUnconfirmed_SendsEmail()
    {
        // Arrange
        var (sp, userManager, _) = await CreateIdentityHostAsync();
        await using (sp)
        {
            var user = new AppUser { UserName = "res2@test.com", Email = "res2@test.com", EmailConfirmed = false };
            await userManager.CreateAsync(user, "TestPass1!");
            var email = new Mock<IEmailService>();
            email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            var sut = CreateSut(
                userManager,
                Mock.Of<ITokenService>(),
                email.Object,
                Mock.Of<IUnitOfWork>(),
                Mock.Of<ICustomerRepository>(),
                CreateClientConfiguration());

            // Act
            var response = await sut.ResendEmailConfirmationAsync(new ResendEmailConfirmationRequest { Email = "res2@test.com" });

            // Assert
            response.IsSuccess.Should().BeTrue();
            email.Verify(e => e.SendEmailAsync("res2@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
