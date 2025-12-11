namespace Payments.Application.Errors;

public static class ErrorCodes
{
    public static class PAY
    {
        public const string GenericUnexpected = "PAY-000";
        public const string PaymentAlreadyInitiated = "PAY-001";
        public const string DoPaymentFailed = "PAY-003";
        public const string PaymentNotFound = "PAY-004";
    }
}