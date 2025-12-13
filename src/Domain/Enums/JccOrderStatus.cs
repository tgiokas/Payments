namespace Payments.Domain.Enums;

public enum JccOrderStatus
{
    // Order was registered but not paid
    RegisteredNotPaid = 0,

    // Order was authorized only and not captured yet (two-phase payment)
    AuthorizedNotCaptured = 1,

    // Order was authorized and captured (successful payment)
    AuthorizedAndCaptured = 2,

    // Authorization was canceled
    AuthorizationCanceled = 3,

    // Transaction was refunded
    Refunded = 4,

    // Issuing bank ACS initiated authorization procedure (e.g. 3DS)
    IssuerAuthorizationInProgress = 5,

    // Authorization declined by issuing bank
    AuthorizationDeclined = 6,

    // Order payment is pending
    Pending = 7,

    // Intermediate completion for multiple partial completions
    PartialCompletion = 8
}