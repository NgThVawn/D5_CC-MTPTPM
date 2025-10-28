using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace WebBanHang.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string plainMessage)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["EmailSettings:From"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var currentTimeVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            var formattedTime = currentTimeVN.ToString("HH:mm:ss dd/MM/yyyy", new CultureInfo("vi-VN"));

            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; font-size: 14px; color: #333;'>
                    <h2 style='color: #007bff;'>Thông báo từ Technology Shop</h2>
                    <p><strong>Thời gian:</strong> {formattedTime}</p>
                    <p><strong>Nội dung thông báo:</strong></p>
                    <p>{plainMessage}</p>
                    <br />
                    <p style='color: #555;'>Vui lòng liên hệ <strong>037780167</strong> để được hỗ trợ.</p>
                    <hr />
                    <p style='font-size: 12px; color: #999;'>Đây là email tự động, vui lòng không phản hồi.</p>
                </div>";

            var builder = new BodyBuilder
            {
                HtmlBody = htmlMessage,
                TextBody = plainMessage
            };

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration["EmailSettings:Smtp"], int.Parse(_configuration["EmailSettings:Port"]), true);
            await smtp.AuthenticateAsync(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
