using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TrueAltitude.Application.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string toName, string otpCode);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string toEmail, string toName, string otpCode)
    {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.titan.email";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        var smtpUsername = _configuration["Email:SmtpUsername"] ?? _configuration["Email:FromEmail"] ?? "athul@truealtitude.in";
        var fromEmail = _configuration["Email:FromEmail"] ?? "athul@truealtitude.in";
        var fromPassword = _configuration["Email:Password"] ?? string.Empty;
        var fromName = _configuration["Email:FromName"] ?? "TrueAltitude";

        var subject = "Your TrueAltitude Verification Code";
        var body = $@"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;"">
  <div style=""max-width: 480px; margin: auto; background: #ffffff; border-radius: 8px; padding: 32px;"">
    <h2 style=""color: #1a1a2e; margin-bottom: 4px;"">Email Verification</h2>
    <p style=""color: #555;"">Hi {toName},</p>
    <p style=""color: #555;"">Use the code below to verify your TrueAltitude account. It expires in <strong>10 minutes</strong>.</p>
    <div style=""text-align: center; margin: 32px 0;"">
      <span style=""font-size: 36px; font-weight: bold; letter-spacing: 8px; color: #4f46e5; background: #eef2ff; padding: 16px 24px; border-radius: 8px;"">{otpCode}</span>
    </div>
    <p style=""color: #888; font-size: 13px;"">If you didn't create an account, you can ignore this email.</p>
    <hr style=""border: none; border-top: 1px solid #eee; margin: 24px 0;"">
    <p style=""color: #aaa; font-size: 12px; text-align: center;"">© 2026 TrueAltitude. All rights reserved.</p>
  </div>
</body>
</html>";

        try
        {
            // Log OTP to console for debugging (remove in production)
            _logger.LogWarning("DEBUG OTP for {Email}: {Otp}", toEmail, otpCode);

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, fromPassword),
                EnableSsl = true,  // STARTTLS on port 587
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mail.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(mail);
            _logger.LogInformation("OTP email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            throw;
        }
    }
}
