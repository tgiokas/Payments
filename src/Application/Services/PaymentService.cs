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
    public async Task<Result<PaymentInitiateResponse>> InitiatePaymentAsync(PaymentInitiateRequest request, string idempotencyKey,
        CancellationToken ct = default)
    {
        // Idempotency replay
        var existingIdempotency = await _repo.GetByIdempotencyAsync(idempotencyKey, ct);
        if (existingIdempotency is not null)
        {
            if (existingIdempotency.Status == PaymentStatus.Redirected &&
                !string.IsNullOrWhiteSpace(existingIdempotency.GatewayOrderId))
            {
                return Result<PaymentInitiateResponse>.Ok(new PaymentInitiateResponse
                {
                    PaymentId = existingIdempotency.Id,
                    GatewayOrderId = existingIdempotency.GatewayOrderId!,
                    FormUrl = string.Empty,
                    Status = existingIdempotency.Status.ToString()
                });
            }

            return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.PaymentAlreadyInitiated);
        }

        // OrderNumber uniqueness protection
        var existingOrder = await _repo.GetByOrderNumberAsync(request.OrderNumber, ct);
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

        // Create Payment in DB (Status Pending) 
        var payment = new Payment
        {
            Description = request.Description,
            OrderNumber = request.OrderNumber,
            Amount = request.Amount,
            Currency = request.Currency,
            Method = method,
            IdempotencyKey = idempotencyKey,
            Status = PaymentStatus.Pending,
            OrderStatus = JccOrderStatus.RegisteredNotPaid
        };
        await _repo.AddAsync(payment, ct);

        // Call JCC register.do
        var jccReq = new JccRegisterOrderRequest
        {
            OrderNumber = payment.OrderNumber,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Description = $"Order {payment.OrderNumber}"
        };
        var register = await _jcc.RegisterOrderAsync(jccReq, ct);

        if (!register.Success || register.GatewayOrderId is null || register.FormUrl is null)
        {
            payment.Status = PaymentStatus.Error;
            payment.ErrorCode = register.ErrorCode;
            payment.ErrorMessage = register.ErrorMessage;
            payment.ModifiedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(payment, ct);

            return _errors.Fail<PaymentInitiateResponse>(ErrorCodes.PAY.DoPaymentFailed);
        }

        // Update Payment in DB (Status Redirected + GatewayOrderId)
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
    public async Task<Result<PaymentConfirmResponse>> ConfirmPaymentAsync(string gatewayOrderId,
        CancellationToken ct = default)
    {
        // Load payment
        var payment = await _repo.GetByGatewayOrderIdAsync(gatewayOrderId, ct);
        if (payment is null)
        {
            return _errors.Fail<PaymentConfirmResponse>(ErrorCodes.PAY.PaymentNotFound);
        }

        // Idempotency guard - do NOT reprocess final states
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
            payment.ErrorCode = orderStatusDto.ErrorCode;
            payment.ErrorMessage = orderStatusDto.ErrorMessage;

            payment.ModifiedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(payment, ct);

            return Result<PaymentConfirmResponse>.Ok(BuildConfirmResponse(payment));
        }

        // Persist raw gateway data
        payment.OrderStatus = (JccOrderStatus)orderStatusDto.OrderStatus.Value;
        payment.ActionCode = orderStatusDto.ActionCode?.ToString();
        payment.ErrorCode = orderStatusDto.ErrorCode;
        payment.ErrorMessage = orderStatusDto.ErrorMessage;

        // Bank Info
        payment.BankName = orderStatusDto.BankName;
        payment.BankCountryCode = orderStatusDto.BankCountryCode;
        payment.BankCountryName = orderStatusDto.BankCountryName;

        // Card Info
        payment.MaskedPan = orderStatusDto.MaskedPan;
        payment.Expiration = orderStatusDto.Expiration;
        payment.CardholderName = orderStatusDto.CardholderName;
        payment.ApprovalCode = orderStatusDto.ApprovalCode;
        payment.PaymentSystem = orderStatusDto.PaymentSystem;

        // Payment Amount Info
        payment.PaymentState = orderStatusDto.PaymentState;
        payment.ApprovedAmount = FromMinorUnits(orderStatusDto.ApprovedAmount);
        payment.DepositedAmount = FromMinorUnits(orderStatusDto.DepositedAmount);
        payment.RefundedAmount = FromMinorUnits(orderStatusDto.RefundedAmount);
        payment.FeeAmount = FromMinorUnits(orderStatusDto.FeeAmount);
        payment.TotalAmount = FromMinorUnits(orderStatusDto.TotalAmount);

        // Other
        payment.PaymentWay = orderStatusDto.PaymentWay;
        payment.Email = orderStatusDto.Email;

        // Map JCC status to business status
        MapJccToBusinessStatus(payment);

        payment.ModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment, ct);

        return Result<PaymentConfirmResponse>.Ok(BuildConfirmResponse(payment));
    }

    public async Task<Result<PaymentDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await _repo.GetByIdAsync(id, ct);
        if (payment is null)
            return _errors.Fail<PaymentDto>(ErrorCodes.PAY.PaymentNotFound);

        var paymentDto = MapToPaymentDto(payment);

        return Result<PaymentDto>.Ok(paymentDto);
    }

    public async Task<Result<PaymentDto>> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        var payment = await _repo.GetByOrderNumberAsync(orderNumber, ct);
        if (payment is null)
            return _errors.Fail<PaymentDto>(ErrorCodes.PAY.PaymentNotFound);

        var paymentDto = MapToPaymentDto(payment);

        return Result<PaymentDto>.Ok(paymentDto);
    }

    private static PaymentConfirmResponse BuildConfirmResponse(Payment payment)
    {
        return new PaymentConfirmResponse
        {
            PaymentId = payment.Id,
            OrderNumber = payment.OrderNumber,
            GatewayOrderId = payment.GatewayOrderId ?? string.Empty,
            Status = payment.Status.ToString(),
            ActionCode = payment.ActionCode,
            ErrorCode = payment.ErrorCode,
            ErrorMessage = payment.ErrorMessage
        };
    }

    private static PaymentDto MapToPaymentDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            Description = payment.Description,
            OrderNumber = payment.OrderNumber,
            GatewayOrderId = payment.GatewayOrderId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Method = payment.Method.ToString(),
            Status = payment.Status.ToString(),

            // Bank Info
            BankName = payment.BankName,
            BankCountryCode = payment.BankCountryCode,
            BankCountryName = payment.BankCountryName,

            // Card Info
            MaskedPan = payment.MaskedPan,
            Expiration = payment.Expiration,
            CardholderName = payment.CardholderName,
            ApprovalCode = payment.ApprovalCode,
            PaymentSystem = payment.PaymentSystem,

            // Payment Amount Info
            PaymentState = payment.PaymentState,
            ApprovedAmount = payment.ApprovedAmount,
            DepositedAmount = payment.DepositedAmount,
            RefundedAmount = payment.RefundedAmount,
            FeeAmount = payment.FeeAmount,
            TotalAmount = payment.TotalAmount,

            PaymentWay = payment.PaymentWay,
            ActionCode = payment.ActionCode,
            ErrorCode = payment.ErrorCode,
            ErrorMessage = payment.ErrorMessage,
            Email = payment.Email,

            CreatedAt = payment.CreatedAt,
            ModifiedAt = payment.ModifiedAt
        };
    }

    private static void MapJccToBusinessStatus(Payment payment)
    {
        switch (payment.OrderStatus)
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

    private static decimal? FromMinorUnits(long? minorAmount)
    => minorAmount.HasValue ? minorAmount.Value / 100m : null;
}