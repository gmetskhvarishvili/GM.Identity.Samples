# Integration event catalog

The contracts that tie the three services together. The Identity service never sends codes or messages itself — it publishes integration events through an **outbox** (written in the same transaction as the state change); the OTP and Notifications services consume them through an **inbox**. This is the only coupling between the services.

See the related decision in [ADR-006](architecture-decisions.md#adr-006--outboxinbox-with-separate-otp-and-notifications-services), and the choreography in the **GM Sample Flows** diagrams (OTP issuance, notification intake, the cross-service ecosystem view).

## Envelope

Every event is a `sealed record … : IntegrationEvent`. The base envelope carries the fields all consumers rely on:

| Field | Purpose |
|---|---|
| `EventId` | Unique id — the **idempotency key** the inbox dedups on. |
| `OccurredOn` | When the event was raised. |
| actor / correlation metadata | `UserId`, `CorrelationId`, and tenant context, stamped from the current actor. |

`Channel`, `ConfirmationType`, and `NotificationType` are integer-valued enums (e.g. `Channel` → Email / SMS / Push / Slack / WhatsApp).

## Catalog

### Identity → OTP service (triggers code issuance)

| Event | Payload | Published by | Consumed by |
|---|---|---|---|
| `UserConfirmationInitiatedIntegrationEvent` | `Subject`, `ConfirmationType` | `ConfirmUserInit` (confirm-init) | OTP service → `GenerateOtp` |
| `TwoFactorChallengeIssuedIntegrationEvent` | `Subject` | password grant when a contact 2FA method is enrolled | OTP service → `GenerateOtp` |
| `OtpRequestedIntegrationEvent` | `Subject`, `Destination`, `Purpose`, `Channel` | any service needing a code (the `Producer.Worker` sample demonstrates it) | OTP service → `GenerateOtp` |

### OTP → Notifications service (delivers the code)

| Event | Payload | Published by | Consumed by |
|---|---|---|---|
| `OtpGeneratedIntegrationEvent` | `Destination`, `Channel`, `Purpose`, `Message` | OTP `GenerateOtp` handler (after storing the hashed challenge) | Notifications service → `Send{Channel}` |

### Identity → Notifications service

| Event | Payload | Published by | Consumed by |
|---|---|---|---|
| `UserRegisteredIntegrationEvent` | `Email`, `Username`, `PhoneNumber`, `RoleIds`, `TwoFactorAuthTypeIds`, `Consents` | `CreateUser` (POST /Users) | Notifications service → welcome message |
| `UserConfirmedIntegrationEvent` | `Subject?`, `ConfirmationType` | `ConfirmUser` (confirm) | Notifications service |

### Published by Identity — consumer is an integration point

These are raised by the Identity service but **no consumer is wired in the sample workers**; plugging one in (typically a handler on the OTP or Notifications service) is left as an integration exercise.

| Event | Payload | Published by | Intended use |
|---|---|---|---|
| `PasswordResetRequestedIntegrationEvent` | `Subject`, `NotificationType` | `ResetUserPassword` | issue + deliver a reset code |
| `SecurityAlertRaisedIntegrationEvent` | `Subject`, `AlertType` | `QueueSecurityAlertAsync` (lockout, password change, …) | notify the user of a security event |
| `UserDeletedIntegrationEvent` | `UserId` (envelope) | `DeleteUser` | let downstream systems react to removal |

## Example chain — account confirmation

```
ConfirmUserInit  ──(UserConfirmationInitiated)──▶  OTP service
                                                     GenerateOtp → store hashed challenge
                                 ◀──(OtpGenerated)── publish
Notifications service  ──consume──▶  Send{Channel} → queue → channel worker delivers the code
```

Password reset follows the same shape from `PasswordResetRequested`, once a consumer is wired for it.

## Delivery semantics

- **At-least-once.** The outbox guarantees an event is published at least once; transient failures are retried.
- **Idempotent consumption.** Each consumer records processed `EventId`s in its inbox and skips duplicates, so re-delivery is safe.
- **Ordering.** Not guaranteed across events; handlers are written to be independent and idempotent rather than order-dependent.
- **Transactional outbox.** The event row is written in the same database transaction as the state change it describes, so an event is never published for a change that rolled back.

## Adding an event

1. Define a `sealed record X : IntegrationEvent` in the publisher's `Events/` folder.
2. Publish it from the command handler with `OutboxMessage.From(entityId, evt)` inside the same unit of work.
3. On each consumer, add the type to the inbox `EventTypeMap` and a handler that maps it to a command (`mediator.Send(...)`), keeping the handler idempotent.

## Related

- [architecture-decisions.md](architecture-decisions.md) · [security.md](security.md) · [tenancy.md](tenancy.md) · [configuration.md](configuration.md) · [extending.md](extending.md)
