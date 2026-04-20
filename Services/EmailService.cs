using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SmartStock.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendApprovalEmailAsync(string toEmail, string ownerName, string shopName, string password)
        {
            var host        = _config["Smtp:Host"]        ?? "smtp.gmail.com";
            var port        = int.Parse(_config["Smtp:Port"] ?? "587");
            var senderEmail = _config["Smtp:SenderEmail"] ?? "";
            var senderName  = _config["Smtp:SenderName"]  ?? "SmartStock";
            var appPassword = _config["Smtp:AppPassword"] ?? "";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(ownerName, toEmail));
            message.Subject = $"✅ Your SmartStock Account is Ready — {shopName}";

            message.Body = new TextPart("html")
            {
                Text = BuildApprovalEmailHtml(ownerName, shopName, toEmail, password)
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Approval email sent to {Email} for shop {Shop}", toEmail, shopName);
        }

        public async Task SendTwoFactorCodeAsync(string toEmail, string displayName, string code)
        {
            var host        = _config["Smtp:Host"]        ?? "smtp.gmail.com";
            var port        = int.Parse(_config["Smtp:Port"] ?? "587");
            var senderEmail = _config["Smtp:SenderEmail"] ?? "";
            var senderName  = _config["Smtp:SenderName"]  ?? "SmartStock";
            var appPassword = _config["Smtp:AppPassword"] ?? "";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(displayName, toEmail));
            message.Subject = $"🔐 Your SmartStock verification code: {code}";

            message.Body = new TextPart("html")
            {
                Text = Build2FAEmailHtml(displayName, code)
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("2FA code sent to {Email}", toEmail);
        }

        private static string Build2FAEmailHtml(string displayName, string code)
        {
            // Format code as "123 456" for readability
            var formatted = code.Length == 6 ? $"{code[..3]} {code[3..]}" : code;

            return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"/></head>
            <body style="margin:0;padding:0;background:#f1f4f8;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 16px;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:14px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">

                    <!-- Header -->
                    <tr>
                      <td style="background:#2e7de9;padding:28px 40px;text-align:center;">
                        <div style="font-size:26px;font-weight:800;color:#fff;margin-bottom:4px;">SmartStock</div>
                        <p style="margin:0;font-size:13px;color:rgba(255,255,255,0.75);">by Admire Technologies</p>
                      </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 40px;text-align:center;">
                        <div style="width:56px;height:56px;background:#eff6ff;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;margin-bottom:20px;font-size:26px;">🔐</div>
                        <h2 style="margin:0 0 8px;font-size:20px;font-weight:700;color:#1f2937;">Verification Code</h2>
                        <p style="margin:0 0 28px;font-size:14px;color:#6b7280;line-height:1.6;">
                          Hi <strong>{displayName}</strong>, use the code below to complete your login.<br/>
                          This code expires in <strong>10 minutes</strong>.
                        </p>

                        <!-- OTP box -->
                        <div style="background:#eff6ff;border:2px solid #bfdbfe;border-radius:14px;padding:24px 32px;margin-bottom:28px;display:inline-block;">
                          <div style="font-size:42px;font-weight:800;color:#2e7de9;letter-spacing:0.18em;line-height:1;">{formatted}</div>
                        </div>

                        <p style="margin:0 0 8px;font-size:13px;color:#9ca3af;">
                          Didn't request this? Someone may be trying to access your account.<br/>
                          You can safely ignore this email.
                        </p>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style="padding:18px 40px;border-top:1px solid #f1f5f9;text-align:center;">
                        <p style="margin:0;font-size:12px;color:#9ca3af;">
                          &copy; {DateTime.UtcNow.Year} Admire Technologies &nbsp;·&nbsp; SmartStock POS
                        </p>
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
        }

        private static string BuildApprovalEmailHtml(string ownerName, string shopName, string email, string password)
        {
            return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"/></head>
            <body style="margin:0;padding:0;background:#f1f4f8;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 16px;">
                <tr><td align="center">
                  <table width="580" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:14px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">

                    <!-- Header -->
                    <tr>
                      <td style="background:#2e7de9;padding:32px 40px;text-align:center;">
                        <div style="display:inline-block;width:48px;height:48px;background:rgba(255,255,255,0.2);border-radius:12px;line-height:48px;font-size:22px;font-weight:800;color:#fff;margin-bottom:12px;">S</div>
                        <h1 style="margin:0;font-size:20px;font-weight:700;color:#ffffff;">SmartStock POS</h1>
                        <p style="margin:4px 0 0;font-size:13px;color:rgba(255,255,255,0.75);">by Admire Technologies</p>
                      </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 40px;">
                        <h2 style="margin:0 0 8px;font-size:22px;font-weight:700;color:#1f2937;">
                          Your account is ready! 🎉
                        </h2>
                        <p style="margin:0 0 24px;font-size:14px;color:#6b7280;line-height:1.6;">
                          Hi <strong>{ownerName}</strong>, your SmartStock shop has been approved and your account is live. Here are your login details:
                        </p>

                        <!-- Credentials box -->
                        <table width="100%" cellpadding="0" cellspacing="0" style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;margin-bottom:28px;">
                          <tr>
                            <td style="padding:20px 24px;">
                              <p style="margin:0 0 4px;font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:0.06em;color:#94a3b8;">Shop Name</p>
                              <p style="margin:0 0 16px;font-size:16px;font-weight:700;color:#1f2937;">{shopName}</p>

                              <p style="margin:0 0 4px;font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:0.06em;color:#94a3b8;">Login Email</p>
                              <p style="margin:0 0 16px;font-size:15px;color:#2e7de9;font-weight:600;">{email}</p>

                              <p style="margin:0 0 4px;font-size:11px;font-weight:600;text-transform:uppercase;letter-spacing:0.06em;color:#94a3b8;">Initial Password</p>
                              <p style="margin:0;font-size:18px;font-weight:800;color:#1f2937;letter-spacing:0.04em;background:#e0f2fe;border-radius:8px;padding:10px 14px;display:inline-block;">{password}</p>
                            </td>
                          </tr>
                        </table>

                        <!-- Security notice -->
                        <table width="100%" cellpadding="0" cellspacing="0" style="background:#fef3c7;border:1px solid #fcd34d;border-radius:8px;margin-bottom:28px;">
                          <tr>
                            <td style="padding:14px 18px;font-size:13px;color:#92400e;">
                              <strong>⚠️ Security tip:</strong> Please change your password immediately after your first login.
                            </td>
                          </tr>
                        </table>

                        <p style="margin:0 0 8px;font-size:14px;color:#374151;">Need help getting started? Reply to this email and our support team will assist you.</p>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style="padding:20px 40px;border-top:1px solid #f1f5f9;text-align:center;">
                        <p style="margin:0;font-size:12px;color:#9ca3af;">
                          This email was sent by <strong>Admire Technologies</strong> on behalf of SmartStock POS.<br/>
                          &copy; {DateTime.UtcNow.Year} Admire Technologies. All rights reserved.
                        </p>
                      </td>
                    </tr>

                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
        }
    }
}
