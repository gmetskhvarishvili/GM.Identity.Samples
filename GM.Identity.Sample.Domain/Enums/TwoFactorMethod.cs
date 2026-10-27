namespace GM.Identity.Sample.Domain.Enums;

/// <summary>
/// The second-factor methods that deliver a one-time code, by their seeded TwoFactorAuthType id. A user enrols in
/// any of them (Set User Two Factor); at login they pick which one the code is sent over. Other TwoFactorAuthType
/// rows still gate login but deliver nothing (e.g. an authenticator app generates its own code).
/// </summary>
public enum TwoFactorMethod
{
    Email = 1,
    Sms = 2,
    WhatsApp = 3,
}

public static class TwoFactorMethodExtensions
{
    /// <summary>The delivery channel for the code, numbered as the OTP and Notifications services expect.</summary>
    public static ConfirmationType ToChannel(this TwoFactorMethod method) => method switch
    {
        TwoFactorMethod.Sms => ConfirmationType.SMS,
        TwoFactorMethod.WhatsApp => ConfirmationType.WhatsApp,
        _ => ConfirmationType.Email,
    };

    /// <summary>SMS and WhatsApp send to the user's phone number; Email to the email address.</summary>
    public static bool UsesPhoneNumber(this TwoFactorMethod method) =>
        method is TwoFactorMethod.Sms or TwoFactorMethod.WhatsApp;
}
