using ECommerce.Api.Controllers;

namespace ECommerce.Tests.Api.Controllers
{
    public class WeatherForecastControllerTests
    {
        [Fact]
        public void Get_ReturnsFiveForecastItems()
        {
            // Arrange
            var controller = new WeatherForecastController();

            // Act
            var result = controller.Get().ToList();

            // Assert
            Assert.Equal(5, result.Count);
            Assert.All(result, item => Assert.NotNull(item.Summary));
        }
    }
}
