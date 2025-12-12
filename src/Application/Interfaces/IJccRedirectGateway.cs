using Payments.Application.Dtos;
using Payments.Domain.Entities;

public interface IJccRedirectGateway
{
    /// register.do to create JCC order and get formUrl + orderId
    public Task<RegisterOrderResultDto> RegisterOrderAsync(JccRegisterOrderRequest request, CancellationToken ct = default);
    /// getOrderStatusExtended.do to verify final status
    Task<OrderStatusResultDto> GetOrderStatusAsync(string gatewayOrderId, CancellationToken ct = default);
}