using System.Net;

namespace Eatah.Api.Features.Auth.Email;

/// <summary>
/// HTML/text templates for transactional auth emails. Body texts are in Swedish
/// (user-facing), styled to match the Eatah brand: dark glassy card on light grey background.
/// </summary>
public static class EmailTemplates
{
    public record EmailContent(string Subject, string HtmlBody, string TextBody);

    public static EmailContent ConfirmEmail(string confirmationUrl)
    {
        const string subject = "Bekräfta din e-post för Eatah";
        var textBody =
            "Välkommen till Eatah!\n\n" +
            "Bekräfta din e-postadress och välj lösenord genom att öppna länken nedan:\n" +
            confirmationUrl + "\n\n" +
            "Länken är giltig i 24 timmar. Om du inte registrerade dig, ignorera detta mail.";

        var htmlBody = Wrap(
            heading: "Välkommen till Eatah",
            intro: "Tack för att du registrerade dig. Klicka på knappen nedan för att bekräfta din e-postadress och välja lösenord.",
            buttonLabel: "Bekräfta e-post",
            buttonUrl: confirmationUrl,
            footer: "Länken är giltig i 24 timmar. Registrerade du dig inte? Ignorera detta mail.");

        return new EmailContent(subject, htmlBody, textBody);
    }

    public static EmailContent PasswordReset(string resetUrl)
    {
        const string subject = "Återställ ditt lösenord för Eatah";
        var textBody =
            "Hej!\n\n" +
            "Vi fick en begäran att återställa lösenordet för ditt Eatah-konto.\n" +
            "Öppna länken nedan för att välja ett nytt lösenord:\n" +
            resetUrl + "\n\n" +
            "Länken är giltig i 1 timme. Om du inte begärde detta, ignorera mailet — ditt lösenord ändras inte.";

        var htmlBody = Wrap(
            heading: "Återställ lösenord",
            intro: "Vi fick en begäran att återställa lösenordet för ditt Eatah-konto. Klicka på knappen nedan för att välja ett nytt.",
            buttonLabel: "Välj nytt lösenord",
            buttonUrl: resetUrl,
            footer: "Länken är giltig i 1 timme. Begärde du inte återställning? Ignorera mailet.");

        return new EmailContent(subject, htmlBody, textBody);
    }

    private static string Wrap(string heading, string intro, string buttonLabel, string buttonUrl, string footer)
    {
        var safeHeading = WebUtility.HtmlEncode(heading);
        var safeIntro = WebUtility.HtmlEncode(intro);
        var safeLabel = WebUtility.HtmlEncode(buttonLabel);
        var safeFooter = WebUtility.HtmlEncode(footer);
        var safeUrl = WebUtility.HtmlEncode(buttonUrl);

        return $$"""
        <!DOCTYPE html>
        <html lang="sv">
        <head>
            <meta charset="utf-8" />
            <title>{{safeHeading}}</title>
        </head>
        <body style="margin:0;padding:0;background:#f4f5f7;font-family:'Helvetica Neue',Arial,sans-serif;color:#1f2330;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f5f7;padding:32px 16px;">
                <tr>
                    <td align="center">
                        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background:#ffffff;border-radius:18px;box-shadow:0 18px 50px -28px rgba(15,23,42,0.45);overflow:hidden;">
                            <tr>
                                <td style="background:linear-gradient(135deg,#1f2330 0%,#3b4458 100%);padding:28px 28px 22px;color:#ffffff;">
                                    <div style="font-size:22px;font-weight:700;letter-spacing:0.5px;">Eatah</div>
                                    <div style="font-size:13px;opacity:0.7;margin-top:2px;">Veckoplanera din mat</div>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:32px 32px 12px;">
                                    <h1 style="margin:0 0 12px;font-size:22px;font-weight:600;color:#1f2330;">{{safeHeading}}</h1>
                                    <p style="margin:0 0 24px;font-size:15px;line-height:1.55;color:#3b4458;">{{safeIntro}}</p>
                                    <a href="{{safeUrl}}" style="display:inline-block;background:#1f2330;color:#ffffff;text-decoration:none;padding:14px 26px;border-radius:12px;font-weight:600;font-size:15px;">{{safeLabel}}</a>
                                    <p style="margin:28px 0 0;font-size:12px;line-height:1.6;color:#7a8094;word-break:break-all;">Funkar inte knappen? Kopiera adressen:<br /><span style="color:#3b4458;">{{safeUrl}}</span></p>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:24px 32px 28px;border-top:1px solid #eef0f4;">
                                    <p style="margin:0;font-size:12px;color:#7a8094;line-height:1.5;">{{safeFooter}}</p>
                                </td>
                            </tr>
                        </table>
                        <p style="margin:18px 0 0;font-size:11px;color:#9aa0b3;">© Eatah</p>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }
}
