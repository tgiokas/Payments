using Payments.Application.Dtos;
using Payments.Application.Errors;
using Payments.Application.Interfaces;
using Payments.Domain.Entities;
using Payments.Domain.Enums;
using Payments.Domain.Interfaces;

namespace Payments.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IJccRedirectGateway _jcc;
    private readonly IPaymentRepository _repo;
    private readonly IErrorCatalog _errors;

    public PaymentService(IJccRedirectGateway jcc, IPaymentRepository repo, IErrorCatalog errors)
    {
        _jcc = jcc;
        _repo = repo;
        _errors = errors;
    }

    /// Step A: Initiate payment   
    public async Task<Result<PaymentInitiateResponseDto>> InitiateAsync(
        PaymentInitiateRequestDto req,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        // Idempotency replay
        var existing = await _repo.FindByIdempotencyAsync(idempotencyKey, ct);
        if (existing is not null && existing.Status == PaymentStatus.Redirected && existing.GatewayOrderId != null)
        {
            return _errors.Fail<PaymentInitiateResponseDto>(ErrorCodes.PAY.PaymentAlreadyInitiated);
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

            return _errors.Fail<PaymentInitiateResponseDto>(ErrorCodes.PAY.DoPaymentFailed);
        }

        // Store GatewayOrderId + Status Redirected    
        payment.GatewayOrderId = reg.GatewayOrderId;
        payment.Status = PaymentStatus.Redirected;
        payment.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment, ct);

        // Return formUrl to frontend
        var result = new PaymentInitiateResponseDto
        {
            PaymentId = payment.Id,
            GatewayOrderId = reg.GatewayOrderId,
            FormUrl = reg.FormUrl,
            Status = payment.Status.ToString()
        };

        return Result<PaymentInitiateResponseDto>.Ok(result);
    }

    /// Step B: Callback/Return verification
    /// JCC redirects to returnUrl with orderId   
    public async Task<Result<PaymentResultDto>> ConfirmByGatewayOrderIdAsync(
        string gatewayOrderId,
        CancellationToken ct = default)
    {
        var payment = await _repo.FindByGatewayOrderIdAsync(gatewayOrderId, ct);
        if (payment is null)
        {
            return _errors.Fail<PaymentResultDto>(ErrorCodes.PAY.PaymentNotFound);
        }

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

        var result = new PaymentResultDto
        {
            PaymentId = payment.Id,
            OrderNumber = payment.OrderNumber,
            GatewayOrderId = gatewayOrderId,
            Status = payment.Status.ToString(),
            ActionCode = status.ActionCode?.ToString(),
            ErrorCode = payment.ErrorCode,
            ErrorMessage = payment.ErrorMessage
        };

        return Result<PaymentResultDto>.Ok(result);
    }
}
