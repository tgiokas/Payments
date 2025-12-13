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
    public async Task<Result<PaymentInitiateResponse>> InitiatePaymentAsync(
        PaymentInitiateRequest request,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        // Idempotency replay
        var existing = await _repo.FindByIdempotencyAsync(idempotencyKey, ct);
        if (existing is not null && existing.Status == PaymentStatus.Redirected && existing.GatewayOrderId != null)
        {
            return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.PaymentAlreadyInitiated);
        }

        var method = Enum.Parse<PaymentMethod>(request.Method, ignoreCase: true);

        // Create Payment in DB (Pending) 
        var payment = new Payment
        {
            OrderNumber = request.OrderNumber,
            AmountValue = request.Amount,
            AmountCurrency = request.Currency,
            Method = method,
            IdempotencyKey = idempotencyKey,
            Status = PaymentStatus.Pending,
            OrderStatus = JccOrderStatus.RegisteredNotPaid
        };

        await _repo.AddAsync(payment, ct);

        var jccReq = new JccRegisterOrderRequest
        {
            OrderNumber = payment.OrderNumber,
            Amount = payment.AmountValue,
            Currency = payment.AmountCurrency,
            Description = $"Order {payment.OrderNumber}"
        };

        // Call register.do
        var registerOrderDto = await _jcc.RegisterOrderAsync(jccReq, ct);

        if (!registerOrderDto.Success || registerOrderDto.GatewayOrderId is null || registerOrderDto.FormUrl is null)
        {
            payment.Status = PaymentStatus.Error;
            payment.ErrorCode = registerOrderDto.ErrorCode;
            payment.ErrorMessage = registerOrderDto.ErrorMessage;
            payment.ModifiedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(payment, ct);

            return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.DoPaymentFailed);
        }

        // Store GatewayOrderId + Status Redirected    
        payment.GatewayOrderId = registerOrderDto.GatewayOrderId;
        payment.Status = PaymentStatus.Redirected;
        payment.ModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment, ct);

        // Return formUrl to frontend
        var result = new PaymentInitiateResponse
        {
            PaymentId = payment.Id,
            GatewayOrderId = registerOrderDto.GatewayOrderId,
            FormUrl = registerOrderDto.FormUrl,
            Status = payment.Status.ToString()
        };

        return Result<PaymentInitiateResponse>.Ok(result);
    }

    /// Step B: Callback/Return verification - JCC redirects to returnUrl with orderId   
    public async Task<Result<PaymentConfirmResponse>> ConfirmPaymentAsync(
        string gatewayOrderId,
        CancellationToken ct = default)
    {
        var payment = await _repo.FindByGatewayOrderIdAsync(gatewayOrderId, ct);
        if (payment is null)
        {
            return _errors.Fail<PaymentConfirmResponse>(ErrorCodes.PAY.PaymentNotFound);
        }

        // Call getOrderStatusExtended.do to verify final status       
        var orderStatusDto = await _jcc.GetOrderStatusAsync(gatewayOrderId, ct);

        if (!orderStatusDto.Success || orderStatusDto.OrderStatus is null)
        {
            payment.Status = PaymentStatus.Error;
            payment.ErrorCode = orderStatusDto.ErrorCode;
            payment.ErrorMessage = orderStatusDto.ErrorMessage;
        }
        else
        {
            // Persist raw JCC info
            payment.OrderStatus = (JccOrderStatus)orderStatusDto.OrderStatus.Value;
            payment.ActionCode = orderStatusDto.ActionCode?.ToString();

            // Map to business status
            payment.Status = payment.OrderStatus == JccOrderStatus.AuthorizedAndCaptured
                ? PaymentStatus.Approved
                : PaymentStatus.Declined;
        }

        payment.ModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment, ct);

        var result = new PaymentConfirmResponse
        {
            PaymentId = payment.Id,
            OrderNumber = payment.OrderNumber,
            GatewayOrderId = gatewayOrderId,
            Status = payment.Status.ToString(),
            ActionCode = orderStatusDto.ActionCode?.ToString(),
            ErrorCode = payment.ErrorCode,
            ErrorMessage = payment.ErrorMessage
        };

        return Result<PaymentConfirmResponse>.Ok(result);
    }
}