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
    /// 1) Create Payment (Pending)
    /// 2) Call register.do
    /// 3) Store GatewayOrderId + Redirected
    /// 4) Return formUrl to frontend
    public async Task<PaymentInitiateResponseDto> InitiateAsync(
        PaymentInitiateRequestDto req,
        string idempotencyKey,
        string? tenantKey,
        CancellationToken ct = default)
    {
        // Idempotency replay
        var existing = await _repo.FindByIdempotencyAsync(idempotencyKey, tenantKey, ct);
        if (existing is not null && existing.Status == PaymentStatus.Redirected && existing.GatewayOrderId != null)
        {
            // We don't have formUrl stored here; you could store it if you want.
            throw new PaymentException("Payment already initiated with same idempotency key.");
        }

        var method = Enum.Parse<PaymentMethod>(req.Method, ignoreCase: true);

        var payment = new Payment
        {
            OrderNumber = req.OrderNumber,
            AmountValue = req.Amount,
            AmountCurrency = req.Currency,
            Method = method,
            IdempotencyKey = idempotencyKey,
            TenantKey = tenantKey,
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

        // MultiECom: reg.GatewayOrderId == mdOrder
        payment.GatewayOrderId = reg.GatewayOrderId;
        payment.Status = PaymentStatus.Redirected;
        payment.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(payment, ct);

        return new PaymentInitiateResponseDto
        {
            PaymentId = payment.Id,
            GatewayOrderId = reg.GatewayOrderId,
            FormUrl = reg.FormUrl,
            Status = payment.Status.ToString()
        };
    }

    /// Step B: Callback/Return verification
    /// JCC redirects user to your returnUrl with orderId :contentReference[oaicite:3]{index=3}
    /// You must call getOrderStatusExtended.do to verify final status :contentReference[oaicite:4]{index=4}
    public async Task<PaymentResultDto> ConfirmByGatewayOrderIdAsync(
        string gatewayOrderId,
        CancellationToken ct = default)
    {
        var payment = await _repo.FindByGatewayOrderIdAsync(gatewayOrderId, ct);
        if (payment is null)
            throw new PaymentException("Payment not found for returned orderId.");

        // getOrderStatusExtended.do (JCC Step 10) :contentReference[oaicite:5]{index=5}
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

    public async Task<Payment?> GetAsync(Guid id, CancellationToken ct = default)
        => await _repo.FindByIdAsync(id, ct);

    private static Guid ParseGuidFallback(string input)
        => Guid.TryParse(input, out var g) ? g : Guid.Empty;
}
