using Payments.Application.Dtos;
using Payments.Application.Errors;
using Payments.Domain.Entities;
using Payments.Domain.Enums;
using Payments.Domain.Interfaces;

namespace Payments.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly IJccRedirectGateway _jcc;

    public PaymentService(IPaymentRepository repo, IJccRedirectGateway jcc)
    {
        _repo = repo;
        _jcc = jcc;
    }

    /// Step A: Initiate payment   
    public async Task<PaymentInitiateResponseDto> InitiateAsync(
        PaymentInitiateRequestDto req,
        string idempotencyKey,       
        CancellationToken ct = default)
    {
        // Idempotency replay
        var existing = await _repo.FindByIdempotencyAsync(idempotencyKey, ct);
        if (existing is not null && existing.Status == PaymentStatus.Redirected && existing.GatewayOrderId != null)
        {           
            throw new PaymentException("Payment already initiated with same idempotency key.");
        }

        var method = Enum.Parse<PaymentMethod>(req.Method, ignoreCase: true);

        // Create Payment in DB (Pending) 
        var payment = new Payment
        {
            OrderNumber = req.OrderNumber,
            AmountValue = req.Amount,
            AmountCurrency = req.Currency,
            Method = method,
            IdempotencyKey = idempotencyKey,           
            Status = PaymentStatus.Pending
        };

        await _repo.AddAsync(payment, ct);

        var jccReq = new JccRegisterOrderRequestDto
        {
            OrderNumber = payment.OrderNumber,
            Amount = payment.AmountValue,
            Currency = payment.AmountCurrency,
            Description = $"Order {payment.OrderNumber}"
        };

        // Call register.do
        var reg = await _jcc.RegisterOrderAsync(jccReq, ct);
       
        if (!reg.Success || reg.GatewayOrderId is null || reg.FormUrl is null)
        {
            payment.Status = PaymentStatus.Error;
            payment.ErrorCode = reg.ErrorCode;
            payment.ErrorMessage = reg.ErrorMessage;
            payment.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(payment, ct);

            throw new PaymentException($"JCC register.do failed: {reg.ErrorCode} {reg.ErrorMessage}");
        }

        // Store GatewayOrderId + Status Redirected    
        payment.GatewayOrderId = reg.GatewayOrderId;
        payment.Status = PaymentStatus.Redirected;
        payment.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment, ct);

        // Return formUrl to frontend
        return new PaymentInitiateResponseDto
        {
            PaymentId = payment.Id,
            GatewayOrderId = reg.GatewayOrderId,
            FormUrl = reg.FormUrl,
            Status = payment.Status.ToString()
        };
    }

    /// Step B: Callback/Return verification
    /// JCC redirects user to returnUrl with orderId   
    public async Task<PaymentResultDto> ConfirmByGatewayOrderIdAsync(
        string gatewayOrderId,
        CancellationToken ct = default)
    {
        var payment = await _repo.FindByGatewayOrderIdAsync(gatewayOrderId, ct);
        if (payment is null)
            throw new PaymentException("Payment not found for returned orderId.");

        // Call getOrderStatusExtended.do to verify final status       
        var status = await _jcc.GetOrderStatusExtendedAsync(gatewayOrderId, ct);

        if (!status.Success || status.OrderStatus is null)
        {
            payment.Status = PaymentStatus.Error;
            payment.ErrorCode = status.ErrorCode;
            payment.ErrorMessage = status.ErrorMessage;
        }
        else if (status.OrderStatus == 2)
        {
            payment.Status = PaymentStatus.Approved;
        }
        else
        {
            payment.Status = PaymentStatus.Declined;
        }

        payment.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment, ct);

        return new PaymentResultDto(
            payment.Id,
            payment.OrderNumber,
            gatewayOrderId,
            payment.Status.ToString(),
            status.ActionCode.ToString(),
            payment.ErrorCode,
            payment.ErrorMessage
        );
    }   
}
