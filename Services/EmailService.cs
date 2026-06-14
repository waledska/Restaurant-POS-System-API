using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using WebApisApp.Helpers;

namespace WebApisApp.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendOtpEmailAsync(string toEmail, string otp);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                // Avoid hanging on certificate issues in some environments
                client.CheckCertificateRevocation = false; 
                
                _logger.LogInformation("Connecting to SMTP server {Host}:{Port}...", _emailSettings.Host, _emailSettings.Port);
                
                // Explicitly use StartTls for 587 and SslOnConnect for 465 (Gmail standards)
                var security = _emailSettings.Port == 587 ? SecureSocketOptions.StartTls : 
                              (_emailSettings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto);

                await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, security);

                _logger.LogInformation("Authenticating with {Username}...", _emailSettings.Username);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                
                _logger.LogInformation("Sending email to {Email}...", toEmail);
                await client.SendAsync(message);
                
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var subject = "Your Password Reset OTP - GlobalPOS";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background-color: #2c3e50; padding: 30px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .body {{ padding: 30px; }}
        .otp-box {{ background-color: #f0f4ff; border: 2px dashed #4a6cf7; border-radius: 8px; text-align: center; padding: 20px; margin: 20px 0; }}
        .otp-code {{ font-size: 36px; font-weight: bold; color: #4a6cf7; letter-spacing: 8px; }}
        .footer {{ background-color: #f4f4f4; text-align: center; padding: 15px; font-size: 12px; color: #888; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h1>🔐 GlobalPOS Security</h1></div>
        <div class='body'>
            <p>Hello,</p>
            <p>We received a request to reset your password. Use the OTP below to proceed:</p>
            <div class='otp-box'>
                <div class='otp-code'>{otp}</div>
                <p style='margin:10px 0 0; color:#666; font-size:14px;'>Valid for <strong>10 minutes</strong></p>
            </div>
            <p>If you did not request this, please ignore this email. Your account is safe.</p>
            <p>Best regards,<br /><strong>GlobalPOS Support Team</strong></p>
        </div>
        <div class='footer'>© 2025 GlobalPOS. All rights reserved.</div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
