using Payments.Application.Dtos;
using Payments.Domain.Entities;

public interface IJccRedirectGateway
{
    ///  - register.do to create JCC order and get formUrl + orderId
    Task<RegisterOrderResult> RegisterOrderAsync(Payment payment, CancellationToken ct = default);
    ///  - getOrderStatusExtended.do to verify final status
    Task<OrderStatusResult> GetOrderStatusExtendedAsync(string gatewayOrderId, CancellationToken ct = default);
}