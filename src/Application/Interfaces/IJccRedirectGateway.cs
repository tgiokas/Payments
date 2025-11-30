using Payments.Application.Dtos;
using Payments.Domain.Entities;

public interface IJccRedirectGateway
{
    /// register.do to create JCC order and get formUrl + orderId
    public Task<RegisterOrderResult> RegisterOrderAsync(JccRegisterOrderRequestDto request, CancellationToken ct = default);
    /// getOrderStatusExtended.do to verify final status
    Task<OrderStatusResult> GetOrderStatusExtendedAsync(string gatewayOrderId, CancellationToken ct = default);
}