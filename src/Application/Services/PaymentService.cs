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
        var existingIdempotency = await _repo.FindByIdempotencyAsync(idempotencyKey, ct);
        if (existingIdempotency is not null)
        {
            if (existingIdempotency.Status == PaymentStatus.Redirected &&
                !string.IsNullOrWhiteSpace(existingIdempotency.GatewayOrderId))
            {
                return Result<PaymentInitiateResponse>.Ok(new PaymentInitiateResponse
                {
                    PaymentId = existingIdempotency.Id,
                    GatewayOrderId = existingIdempotency.GatewayOrderId!,
                    FormUrl = string.Empty, // frontend already has it
                    Status = existingIdempotency.Status.ToString()
                });
            }

            return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.PaymentAlreadyInitiated);
        }

        // OrderNumber uniqueness protection
        var existingOrder = await _repo.FindByOrderNumberAsync(request.OrderNumber, ct);
        if (existingOrder is not null)
        {
            // Already successfully paid
            if (existingOrder.Status == PaymentStatus.Approved)
                return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.OrderAlreadyPaid);

            // User never completed payment --> allow retry
            if (existingOrder.Status == PaymentStatus.Error ||
                existingOrder.Status == PaymentStatus.Declined)
                return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.OrderPaymentFailedPreviously);

            // Redirect already issued --> do NOT create new one
            if (existingOrder.Status == PaymentStatus.Redirected)
                return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.PaymentAlreadyInitiated);
        }

        // Parse & validate method
        if (!Enum.TryParse<PaymentMethod>(request.Method, true, out var method))
            return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.InvalidPaymentMethod);

        // Create Payment in DB (Pending) 
        var payment = new Payment
        {
            OrderNumber = request.OrderNumber,
            AmountValue = request.Amount,
            AmountCurrency = request.Currency,
            Method = method,
            IdempotencyKey = idempotencyKey,
            Status = PaymentStatus.Pending,
            JccOrderStatus = JccOrderStatus.RegisteredNotPaid
        };

        await _repo.AddAsync(payment, ct);

        // Call JCC register.do
        var jccReq = new JccRegisterOrderRequest
        {
            OrderNumber = payment.OrderNumber,
            Amount = payment.AmountValue,
            Currency = payment.AmountCurrency,
            Description = $"Order {payment.OrderNumber}"
        };

        var register = await _jcc.RegisterOrderAsync(jccReq, ct);

        if (!register.Success || register.GatewayOrderId is null || register.FormUrl is null)
        {
            payment.Status = PaymentStatus.Error;
            payment.JccErrorCode = register.ErrorCode;
            payment.JccErrorMessage = register.ErrorMessage;
            payment.ModifiedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(payment, ct);

            return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.DoPaymentFailed);
        }

        // Store GatewayOrderId + Status Redirected    
        payment.GatewayOrderId = register.GatewayOrderId;
        payment.Status = PaymentStatus.Redirected;
        payment.ModifiedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(payment, ct);

        return Result<PaymentInitiateResponse>.Ok(new PaymentInitiateResponse
        {
            PaymentId = payment.Id,
            GatewayOrderId = payment.GatewayOrderId,
            FormUrl = register.FormUrl,
            Status = payment.Status.ToString()
        });
    }        

    /// Step B: Callback/Return verification - JCC redirects to returnUrl with orderId   
    public async Task<Result<PaymentConfirmResponse>> ConfirmPaymentAsync(
        string gatewayOrderId,
        CancellationToken ct = default)
    {
        // Load payment
        var payment = await _repo.FindByGatewayOrderIdAsync(gatewayOrderId, ct);
        if (payment is null)
        {
            return _errors.Fail<PaymentConfirmResponse>(ErrorCodes.PAY.PaymentNotFound);
        }

        // Idempotency guard — do NOT reprocess final states
        if (payment.Status == PaymentStatus.Approved ||
            payment.Status == PaymentStatus.Declined)
        {
            return Result<PaymentConfirmResponse>.Ok(BuildConfirmResponse(payment));
        }

        // Call JCC getOrderStatusExtended.do to verify final status
        var orderStatusDto = await _jcc.GetOrderStatusAsync(gatewayOrderId, ct);

        if (!orderStatusDto.Success || orderStatusDto.OrderStatus is null)
        {
            payment.Status = PaymentStatus.Error;
            payment.JccErrorCode = orderStatusDto.ErrorCode;
            payment.JccErrorMessage = orderStatusDto.ErrorMessage;

            payment.ModifiedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(payment, ct);

            return Result<PaymentConfirmResponse>.Ok(BuildConfirmResponse(payment));
        }

        // Persist raw gateway data
        payment.JccOrderStatus = (JccOrderStatus)orderStatusDto.OrderStatus.Value;
        payment.JccActionCode = orderStatusDto.ActionCode?.ToString();
        payment.JccErrorCode = orderStatusDto.ErrorCode;
        payment.JccErrorMessage = orderStatusDto.ErrorMessage;

        // Map JCC status → business status
        MapJccToBusinessStatus(payment);

        payment.ModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment, ct);
        
        return Result<PaymentConfirmResponse>.Ok(BuildConfirmResponse(payment));
    }

    private static void MapJccToBusinessStatus(Payment payment)
    {
        switch (payment.JccOrderStatus)
        {
            case JccOrderStatus.AuthorizedAndCaptured:
                payment.Status = PaymentStatus.Approved;
                break;

            case JccOrderStatus.AuthorizationDeclined:
            case JccOrderStatus.AuthorizationCanceled:
            case JccOrderStatus.Refunded:
                payment.Status = PaymentStatus.Declined;
                break;

            case JccOrderStatus.RegisteredNotPaid:
            case JccOrderStatus.IssuerAuthorizationInProgress:
            case JccOrderStatus.Pending:
            case JccOrderStatus.PartialCompletion:
            case JccOrderStatus.AuthorizedNotCaptured:
                payment.Status = PaymentStatus.Pending;
                break;

            default:
                payment.Status = PaymentStatus.Error;
                break;
        }
    }

    private static PaymentConfirmResponse BuildConfirmResponse(Payment payment)
    {
        return new PaymentConfirmResponse
        {
            PaymentId = payment.Id,
            OrderNumber = payment.OrderNumber,
            GatewayOrderId = payment.GatewayOrderId ?? string.Empty,
            Status = payment.Status.ToString(),
            ActionCode = payment.JccActionCode,
            ErrorCode = payment.JccErrorCode,
            ErrorMessage = payment.JccErrorMessage
        };
    }
}