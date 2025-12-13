namespace Payments.Domain.Enums;

public enum PaymentStatus
{
    Pending,        // created in DB, register.do not called yet
    Redirected,     // register.do ok, user got formUrl
    Approved,       // orderStatus == 2 from getOrderStatusExtended.do
    Declined,       // any non-success final state
    Error
}