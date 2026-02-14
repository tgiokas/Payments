# Payments API

**.NET 8 Web API** for handling payment operations using the **JCC Payment Gateway**.

- Initiates payment sessions with JCC
- Handles JCC callbacks after payment completion
- Persists and exposes payment status
- Integrates with the **JCC Web SDK (Multi-frame)** on the frontend

---

## Controller Methods

Summary of the main controller methods exposed by the API.

### PaymentsController

- **POST** `/payments/initiate`  
  Initiates a new payment session with JCC.  
  Registers the order with the JCC gateway (`register.do`) and returns the
  `gatewayOrderId (mdOrder)` required by the frontend Web SDK.

- **GET** `/payments/callback`  
  Callback endpoint used by JCC after payment completion.  
  Verifies the final payment status by calling `getOrderStatusExtended.do`
  and updates the internal payment record.

- **GET** `/payments/{id}`  
  Retrieves a payment by internal payment ID.

- **GET** `/payments/order/{orderNumber}`  
  Retrieves a payment by business order number.

---

## Payment Flow

1. Frontend calls `POST /payments/initiate`
2. Backend registers the order with JCC
3. Backend returns `gatewayOrderId (mdOrder)`
4. Frontend initializes the JCC Web SDK using `mdOrder`
5. User completes payment in JCC-hosted frames
6. JCC redirects the browser to `/payments/callback`
7. Backend verifies payment status and updates the database
8. Backend redirects back to the frontend result page

---

## JCC Web SDK

This project integrates with the **JCC Web SDK (Multi-frame)** for secure card
payment processing.

> ⚠️ The backend **never handles card data**.  
> All sensitive card fields (PAN, expiry date, CVC) are collected securely by JCC
> via the Web SDK.

- **Official SDK Documentation:**  
  https://gateway.jcc.com.cy/developer/en/integration/sdk/web_sdk_multiframe.html

---

## Example Frontend Integration (jcc-payments.html)

An example HTML implementation using the JCC Web SDK is provided in `jcc-payments.html`.

The example demonstrates:
- Calling `POST /payments/initiate`
- Receiving the `gatewayOrderId (mdOrder)`
- Initializing the JCC `PaymentForm`
- Submitting the payment via the Web SDK
- Handling the redirect result after payment completion

This file should be used as a **reference implementation** for frontend teams.

---

## Configuration

Application configuration is managed via  `.env` & `appsettings.json`.

Configuration includes:
- JCC REST base URL
- Merchant credentials or token
- Callback / return URLs
- Database connection string

---

## Testing

For development and testing, use the official **JCC test cards**.

- **Test Cards Documentation:**  
  https://gateway.jcc.com.cy/developer/en/integration/structure/test-cards.html


---

## Notes

- Idempotency is enforced on payment initiation using the `X-Idempotency-Key` header.
- Each `OrderNumber` is unique and protected from duplicate payments.
- Payment status is verified server-to-server with JCC for security.
- The API is stateless and suitable for horizontal scaling (Kubernetes).

---

For further details, refer to the official **JCC documentation** and the source code.