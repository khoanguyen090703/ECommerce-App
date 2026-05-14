using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.SharedViewModels.DTOs.Response;
using System.Linq;

namespace ECommerce.Application.Mappings
{
    public static class OrderMappings
    {
        public static bool IsOnlineCheckoutPaymentMethodName(string? paymentMethodName) =>
            string.Equals(paymentMethodName, "Stripe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(paymentMethodName, "VnPay", StringComparison.OrdinalIgnoreCase);

        public static bool ComputeCanRetryOnlinePayment(Order order)
        {
            if (order.Status == OrderStatus.Cancelled)
                return false;
            if (order.PaymentStatus == OrderPaymentStatus.Paid)
                return false;
            if (order.PaymentStatus != OrderPaymentStatus.Unpaid && order.PaymentStatus != OrderPaymentStatus.Failed)
                return false;
            var payment = order.Payments?.OrderByDescending(p => p.Id).FirstOrDefault();
            return IsOnlineCheckoutPaymentMethodName(payment?.PaymentMethod?.Name);
        }

        public static MyOrderResponse ToMyOrderResponse(this Order order)
        {
            return new MyOrderResponse
            {
                Id = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                OrderDate = order.OrderDate,
                CanRetryOnlinePayment = ComputeCanRetryOnlinePayment(order),
                OrderItems = order.OrderItems.Select(oi => oi.ToItem4MyOrderResponse()).ToList()
            };
        }

        public static OrderResponse ToOrderResponse(this Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                RecipientName = order.RecipientName,
                OrderDate = order.OrderDate,
                CompletedDate = order.CompletedDate,
                CancelledDate = order.CancelledDate
            };
        }

        public static OrderDetailsResponse ToOrderDetailsResponse(this Order order)
        {
            var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
            return new OrderDetailsResponse
            {
                Id = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                CanRetryOnlinePayment = ComputeCanRetryOnlinePayment(order),
                RecipientName = order.RecipientName,
                OrderDate = order.OrderDate,
                CompletedDate = order.CompletedDate,
                CancelledDate = order.CancelledDate,
                PhoneNumber = order.PhoneNumber,
                ShippingAddress = order.ShippingAddress,
                SubTotal = order.SubTotal,
                ShippingFee = order.ShippingFee,
                OrderItems = order.OrderItems.Select(oi => oi.ToItem4MyOrderResponse()).ToList(),
                Payment = payment == null
                    ? null
                    : new OrderPaymentDetailsResponse
                    {
                        PaymentId = payment.Id,
                        PaymentMethodName = payment.PaymentMethod.Name,
                        Status = payment.Status.ToString(),
                        Amount = payment.Amount,
                        PaidAt = payment.PaidAt,
                        TransactionId = payment.TransactionId,
                        StripeCheckoutSessionId = payment.StripeCheckoutSessionId,
                        StripePaymentIntentId = payment.StripePaymentIntentId,
                        FailureReason = payment.FailureReason,
                        CheckoutSessionExpiresAt = payment.CheckoutSessionExpiresAt,
                        LastStripeWebhookAt = payment.LastStripeWebhookAt
                    }
            };
        }
    }
}
