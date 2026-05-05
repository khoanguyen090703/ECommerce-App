using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImagesController : Controller
    {
        private readonly IImageService _imageService;

        public ImagesController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? folder = null, CancellationToken cancellationToken = default)
        {
            await using var stream = file.OpenReadStream();

            var request = new UploadImageRequest{FileStream = stream, FileName = file.FileName, Folder = folder};
            var response = await _imageService.UploadAsync(request, cancellationToken);

            return CreatedAtAction(nameof(Upload), value: response);
        }

        [HttpDelete("{imageUrl}")]
        public async Task<IActionResult> Delete(
        string imageUrl,
        CancellationToken cancellationToken = default)
        {
            await _imageService.DeleteAsync(imageUrl, cancellationToken);

            return NoContent();
        }
    }
}
