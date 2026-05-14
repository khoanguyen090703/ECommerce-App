using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Exceptions;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.SharedViewModels.DTOs.Request;
using ECommerce.SharedViewModels.DTOs.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Globalization;
using System.Linq;
using DomainPayment = ECommerce.Domain.Entities.Payment;
using DomainPaymentMethod = ECommerce.Domain.Entities.PaymentMethod;

namespace ECommerce.Infrastructure.Services
{
    public class StripePaymentService : IStripePaymentService
    {
        private const string EventCheckoutSessionCompleted = "checkout.session.completed";
        private const string EventCheckoutSessionExpired = "checkout.session.expired";
        private const string EventPaymentIntentPaymentFailed = "payment_intent.payment_failed";

        private static readonly HashSet<string> ZeroDecimalCurrencies = new(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        private readonly StripeSettings _stripeSettings;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(
            IOptions<StripeSettings> stripeOptions,
            IPaymentRepository paymentRepository,
            IOrderRepository orderRepository,
            ICurrentUserService currentUserService,
            ILogger<StripePaymentService> logger)
        {
            _stripeSettings = stripeOptions.Value;
            _paymentRepository = paymentRepository;
            _orderRepository = orderRepository;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public Task<StripeCheckoutResponse> CreateCheckoutAsync(CreateStripeCheckoutRequest request, CancellationToken cancellationToken = default) =>
            CreateOrRefreshCheckoutAsync(request, cancellationToken);

        public Task<StripeCheckoutResponse> RetryCheckoutAsync(CreateStripeCheckoutRequest request, CancellationToken cancellationToken = default) =>
            CreateOrRefreshCheckoutAsync(request, cancellationToken);

        public async Task<StripePaymentStatusResponse> GetPaymentStatusAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var userId = RequireCustomerUserId();
            var order = await _orderRepository.GetDetailsByIdAsync(orderId, cancellationToken)
                ?? throw new NotFoundException($"Order with id {orderId} not found.");

            if (order.Customer.IdentityId != userId)
                throw new ForbiddenException("You can only view payment status for your own orders.");

            var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault()
                ?? throw new NotFoundException("Payment was not found for this order.");

            string? stripeSessionStatus = null;
            Session? stripeSession = null;
            if (!string.IsNullOrEmpty(payment.StripeCheckoutSessionId))
            {
                StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
                try
                {
                    stripeSession = await new SessionService().GetAsync(payment.StripeCheckoutSessionId, cancellationToken: cancellationToken);
                    stripeSessionStatus = stripeSession.Status;
                    if (IsStripeCheckoutSessionPaid(stripeSession))
                    {
                        await CompleteSuccessfulPaymentAsync(payment, stripeSession.PaymentIntentId, cancellationToken);

                        order = await _orderRepository.GetDetailsByIdAsync(orderId, cancellationToken) ?? order;
                        payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault() ?? payment;
                    }
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve Stripe session {SessionId}", payment.StripeCheckoutSessionId);
                }
            }

            return new StripePaymentStatusResponse
            {
                OrderId = order.Id,
                OrderPaymentStatus = order.PaymentStatus.ToString(),
                OrderStatus = order.Status.ToString(),
                PaymentStatus = payment.Status.ToString(),
                StripeCheckoutSessionId = payment.StripeCheckoutSessionId,
                StripePaymentIntentId = payment.StripePaymentIntentId,
                StripeSessionStatus = stripeSessionStatus,
                CheckoutSessionExpiresAt = payment.CheckoutSessionExpiresAt,
                PaidAt = payment.PaidAt,
                TransactionId = payment.TransactionId,
                FailureReason = payment.FailureReason
            };
        }

        public async Task ProcessWebhookAsync(string jsonBody, string stripeSignatureHeader, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_stripeSettings.WebhookSecret))
                throw new InvalidOperationException("Stripe WebhookSecret is not configured.");

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(jsonBody, stripeSignatureHeader, _stripeSettings.WebhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Invalid Stripe webhook payload or signature.");
                throw;
            }

            switch (stripeEvent.Type)
            {
                case EventCheckoutSessionCompleted:
                    if (stripeEvent.Data.Object is Session completedSession)
                        await HandleCheckoutSessionCompletedAsync(completedSession, cancellationToken);
                    break;
                case EventCheckoutSessionExpired:
                    if (stripeEvent.Data.Object is Session expiredSession)
                        await HandleCheckoutSessionExpiredAsync(expiredSession, cancellationToken);
                    break;
                case EventPaymentIntentPaymentFailed:
                    if (stripeEvent.Data.Object is PaymentIntent failedIntent)
                        await HandlePaymentIntentFailedAsync(failedIntent, cancellationToken);
                    break;
                default:
                    _logger.LogInformation("Unhandled Stripe webhook type: {Type}", stripeEvent.Type);
                    break;
            }
        }

        private async Task<StripeCheckoutResponse> CreateOrRefreshCheckoutAsync(CreateStripeCheckoutRequest request, CancellationToken cancellationToken)
        {
            var userId = RequireCustomerUserId();

            if (request.OrderId < 1)
                throw new ConflictException("Invalid order id.");

            var payment = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken)
                ?? throw new NotFoundException($"Payment for order {request.OrderId} was not found.");

            var order = payment.Order;
            if (order.Customer.IdentityId != userId)
                throw new ForbiddenException("You can only pay for your own orders.");

            if (order.Status == OrderStatus.Cancelled)
                throw new ConflictException("This order was cancelled and cannot be paid.");

            if (!IsStripeCheckoutPaymentMethod(payment.PaymentMethod))
                throw new ConflictException("This order does not use an online card payment method (Stripe / VnPay).");

            if (order.PaymentStatus == OrderPaymentStatus.Paid)
                throw new ConflictException("This order is already paid.");

            if (payment.Status == PaymentStatus.Completed)
                throw new ConflictException("Payment has already completed.");

            if (order.PaymentStatus != OrderPaymentStatus.Unpaid && order.PaymentStatus != OrderPaymentStatus.Failed)
                throw new ConflictException("Order is not in a payable state.");

            if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Failed)
                throw new ConflictException("Payment is not in a state that allows checkout.");

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            if (!string.IsNullOrEmpty(payment.StripeCheckoutSessionId))
            {
                try
                {
                    await new SessionService().ExpireAsync(payment.StripeCheckoutSessionId, cancellationToken: cancellationToken);
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex, "Could not expire Stripe session {SessionId}", payment.StripeCheckoutSessionId);
                }
            }

            var currency = string.IsNullOrWhiteSpace(_stripeSettings.DefaultCurrency)
                ? "usd"
                : _stripeSettings.DefaultCurrency.Trim().ToLowerInvariant();

            var successUrl = ExpandCheckoutUrl(_stripeSettings.SuccessUrl, order.Id, required: true);
            successUrl = EnsureCheckoutSessionToken(successUrl);
            var cancelUrl = ExpandCheckoutUrl(_stripeSettings.CancelUrl, order.Id, required: true);

            var metadata = new Dictionary<string, string>
            {
                ["order_id"] = order.Id.ToString(CultureInfo.InvariantCulture),
                ["payment_id"] = payment.Id.ToString(CultureInfo.InvariantCulture),
            };

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = order.Id.ToString(CultureInfo.InvariantCulture),
                Metadata = metadata,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = metadata,
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = ToStripeUnitAmount(order.TotalAmount, currency),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order #{order.Id}",
                            },
                        },
                    },
                },
            };

            var session = await new SessionService().CreateAsync(options, cancellationToken: cancellationToken);

            payment.StripeCheckoutSessionId = session.Id;
            payment.StripePaymentIntentId = session.PaymentIntentId;
            payment.CheckoutSessionExpiresAt = session.ExpiresAt;
            payment.FailureReason = null;
            payment.Status = PaymentStatus.Pending;

            if (order.PaymentStatus == OrderPaymentStatus.Failed)
                order.PaymentStatus = OrderPaymentStatus.Unpaid;

            await _paymentRepository.UpdateAsync(payment);

            return new StripeCheckoutResponse
            {
                CheckoutUrl = session.Url,
                SessionId = session.Id,
                SessionExpiresAt = session.ExpiresAt,
            };
        }

        private async Task HandleCheckoutSessionCompletedAsync(Session session, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByStripeCheckoutSessionIdAsync(session.Id, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Checkout session completed but no payment matched session {SessionId}", session.Id);
                return;
            }

            await CompleteSuccessfulPaymentAsync(payment, session.PaymentIntentId, cancellationToken);
        }

        private async Task CompleteSuccessfulPaymentAsync(
            DomainPayment payment,
            string? paymentIntentId,
            CancellationToken cancellationToken)
        {
            var trackedPayment = payment.Id > 0
                ? await _paymentRepository.GetByIdAsync(payment.Id, cancellationToken)
                : await _paymentRepository.GetByOrderIdAsync(payment.OrderId, cancellationToken);

            if (trackedPayment == null)
                return;

            payment = trackedPayment;

            if (payment.Status == PaymentStatus.Completed)
                return;

            payment.Status = PaymentStatus.Completed;
            payment.PaidAt = DateTime.UtcNow;
            payment.TransactionId = paymentIntentId ?? payment.TransactionId;
            payment.StripePaymentIntentId = paymentIntentId ?? payment.StripePaymentIntentId;
            payment.LastStripeWebhookAt = DateTime.UtcNow;
            payment.FailureReason = null;

            var order = payment.Order;
            order.PaymentStatus = OrderPaymentStatus.Paid;
            if (IsStripeCheckoutPaymentMethod(payment.PaymentMethod)
                && order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Processing;
            }

            await _paymentRepository.UpdateAsync(payment);
        }

        private static bool IsStripeCheckoutSessionPaid(Session session) =>
            string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(session.Status, "complete", StringComparison.OrdinalIgnoreCase);

        private async Task HandleCheckoutSessionExpiredAsync(Session session, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByStripeCheckoutSessionIdAsync(session.Id, cancellationToken);
            if (payment == null)
                return;

            if (payment.Status == PaymentStatus.Completed)
                return;

            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = "Checkout session expired.";
            payment.LastStripeWebhookAt = DateTime.UtcNow;

            var order = payment.Order;
            if (order.PaymentStatus != OrderPaymentStatus.Paid)
                order.PaymentStatus = OrderPaymentStatus.Failed;

            await _paymentRepository.UpdateAsync(payment);
        }

        private async Task HandlePaymentIntentFailedAsync(PaymentIntent intent, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByStripePaymentIntentIdAsync(intent.Id, cancellationToken);

            if (payment == null
                && intent.Metadata != null
                && intent.Metadata.TryGetValue("payment_id", out var pidStr)
                && int.TryParse(pidStr, out var paymentId))
            {
                payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
            }

            if (payment == null)
            {
                _logger.LogWarning("Payment intent failed but no local payment matched. Intent {IntentId}", intent.Id);
                return;
            }

            if (payment.Status == PaymentStatus.Completed)
                return;

            payment.Status = PaymentStatus.Failed;
            payment.StripePaymentIntentId = intent.Id;
            payment.FailureReason = intent.LastPaymentError?.Message ?? "Payment failed.";
            payment.LastStripeWebhookAt = DateTime.UtcNow;

            var order = payment.Order;
            if (order.PaymentStatus != OrderPaymentStatus.Paid)
                order.PaymentStatus = OrderPaymentStatus.Failed;

            await _paymentRepository.UpdateAsync(payment);
        }

        private Guid RequireCustomerUserId()
        {
            if (!_currentUserService.IsAuthenticated)
                throw new UnauthorizedAccessException("User is not authenticated.");

            if (!_currentUserService.IsInRole("Customer"))
                throw new ForbiddenException("Only customers can use Stripe checkout.");

            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("User is not authenticated.");

            return userId.Value;
        }

        /// <summary>Matches online checkout used after create order (Stripe or legacy VnPay label).</summary>
        private static bool IsStripeCheckoutPaymentMethod(DomainPaymentMethod method) =>
            string.Equals(method.Name, "Stripe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(method.Name, "VnPay", StringComparison.OrdinalIgnoreCase);

        private static string ExpandCheckoutUrl(string template, int orderId, bool required)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                if (required)
                    throw new ConflictException("Stripe SuccessUrl and CancelUrl must be configured (appsettings Stripe section). Use {ORDER_ID} where the order id should appear.");
                return string.Empty;
            }

            return template.Replace("{ORDER_ID}", orderId.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }

        private static string EnsureCheckoutSessionToken(string url)
        {
            if (url.Contains("{CHECKOUT_SESSION_ID}", StringComparison.Ordinal))
                return url;
            var sep = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            return $"{url}{sep}session_id={{CHECKOUT_SESSION_ID}}";
        }

        private static long ToStripeUnitAmount(decimal amount, string currency)
        {
            var code = currency.ToUpperInvariant();
            if (ZeroDecimalCurrencies.Contains(code))
                return (long)Math.Round(amount, MidpointRounding.AwayFromZero);
            return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        }
    }
}
