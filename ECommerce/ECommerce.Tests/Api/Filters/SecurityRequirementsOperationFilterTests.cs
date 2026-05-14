using System.Reflection;
using ECommerce.Api.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ECommerce.Tests.Api.Filters;

public class SecurityRequirementsOperationFilterTests
{
    private static OperationFilterContext CreateContext(MethodInfo methodInfo)
    {
        var apiDescription = new ApiDescription();
        var document = new OpenApiDocument();
        var schemaRepo = new SchemaRepository(documentName: "v1");
        var schemaGen = Mock.Of<ISchemaGenerator>();
        return new OperationFilterContext(apiDescription, schemaGen, schemaRepo, document, methodInfo);
    }

    [Fact]
    public void Apply_WhenMethodHasAuthorize_AddsBearerSecurityRequirement()
    {
        // Arrange
        var method = typeof(SecuredStubController).GetMethod(nameof(SecuredStubController.WithAuthorize))!;
        var context = CreateContext(method);
        var operation = new OpenApiOperation();
        var sut = new SecurityRequirementsOperationFilter();

        // Act
        sut.Apply(operation, context);

        // Assert
        operation.Security.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Apply_WhenNoAuthorize_DoesNotSetSecurity()
    {
        // Arrange
        var method = typeof(SecuredStubController).GetMethod(nameof(SecuredStubController.WithoutAuthorize))!;
        var context = CreateContext(method);
        var operation = new OpenApiOperation();
        var sut = new SecurityRequirementsOperationFilter();

        // Act
        sut.Apply(operation, context);

        // Assert
        operation.Security.Should().BeNull();
    }

    [Fact]
    public void Apply_WhenClassHasAuthorize_AddsBearerSecurityRequirement()
    {
        // Arrange
        var method = typeof(ClassLevelAuthorizeStubController).GetMethod(nameof(ClassLevelAuthorizeStubController.Action))!;
        var context = CreateContext(method);
        var operation = new OpenApiOperation();
        var sut = new SecurityRequirementsOperationFilter();

        // Act
        sut.Apply(operation, context);

        // Assert
        operation.Security.Should().NotBeNullOrEmpty();
    }

    private sealed class SecuredStubController : ControllerBase
    {
        [Authorize(Roles = "Customer")]
        public IActionResult WithAuthorize() => Ok();

        public IActionResult WithoutAuthorize() => Ok();
    }

    [Authorize]
    private sealed class ClassLevelAuthorizeStubController : ControllerBase
    {
        public IActionResult Action() => Ok();
    }
}
