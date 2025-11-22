namespace Payments.Application.Errors;

public class PaymentException : Exception
{
    public PaymentException(string message) : base(message) { }
}
