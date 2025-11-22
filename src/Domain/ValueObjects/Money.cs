namespace Payments.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Create(decimal amount, string currency)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be > 0");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency required");
        return new Money(amount, currency.ToUpperInvariant());
    }
}