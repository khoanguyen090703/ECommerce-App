using ECommerce.Application.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers
{
    [ApiController]
    [Route("api/payments/stripe")]
    public class StripePaymentController : ControllerBase
    {
        private readonly IStripePaymentService _stripePaymentService;

        public StripePaymentController(IStripePaymentService stripePaymentService)
        {
            _stripePaymentService = stripePaymentService;
        }

        /// <summary>Returns Stripe Checkout URL (payment link) for an existing order.</summary>
        [HttpPost("checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateCheckout([FromBody] CreateStripeCheckoutRequest request, CancellationToken cancellationToken)
        {
            var result = await _stripePaymentService.CreateCheckoutAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("orders/{orderId:int:min(1)}/status")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetStatus(int orderId, CancellationToken cancellationToken)
        {
            var result = await _stripePaymentService.GetPaymentStatusAsync(orderId, cancellationToken);
            return Ok(result);
        }

        [HttpPost("orders/{orderId:int:min(1)}/retry")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Retry(int orderId, CancellationToken cancellationToken)
        {
            var dto = new CreateStripeCheckoutRequest { OrderId = orderId };
            var result = await _stripePaymentService.RetryCheckoutAsync(dto, cancellationToken);
            return Ok(result);
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync(cancellationToken);
            var signature = Request.Headers["Stripe-Signature"].ToString();
            await _stripePaymentService.ProcessWebhookAsync(json, signature, cancellationToken);
            return Ok();
        }
    }
}
