namespace Payments.Application.Errors;

public static class ErrorCodes
{
    public static class PAY
    {
        public const string GenericUnexpected = "PAY-000";
        public const string ErrorInErrorCodes = "PAY-001";
        public const string PaymentAlreadyInitiated = "PAY-002";
        public const string DoPaymentFailed = "PAY-003";
        public const string PaymentNotFound = "PAY-004";
        public const string OrderAlreadyPaid = "PAY-005";
        public const string OrderPaymentFailedPreviously = "PAY-006";
        public const string InvalidPaymentMethod = "PAY-007";
    }
}