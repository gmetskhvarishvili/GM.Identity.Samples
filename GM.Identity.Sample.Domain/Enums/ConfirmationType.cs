namespace GM.Identity.Sample.Domain.Enums;

/// <summary>
/// The kind of contact being confirmed and the channel the one-time code is delivered over.
/// Email confirms the email address; SMS and WhatsApp confirm the phone number.
/// This is contact confirmation only — identity/KYC verification is a separate concept.
/// </summary>
public enum ConfirmationType
{
    Email = 0,
    SMS = 1,
    WhatsApp = 2,
}

public static class ConfirmationTypeExtensions
{
    /// <summary>
    /// SMS and WhatsApp confirm the user's phone number; Email confirms the email address.
    /// </summary>
    public static bool ConfirmsPhoneNumber(this ConfirmationType confirmationType) =>
        confirmationType is ConfirmationType.SMS or ConfirmationType.WhatsApp;
}
