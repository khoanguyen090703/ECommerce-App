using ECommerce.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ECommerce.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var emailSettings = _config.GetSection("EmailSettings");

            // 1. Khởi tạo đối tượng MimeMessage
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]!));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            // 2. Thiết lập nội dung (Hỗ trợ HTML)
            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            // 3. Khởi tạo SmtpClient và gửi mail
            using var smtp = new SmtpClient();
            try
            {
                // Kết nối tới Server
                await smtp.ConnectAsync(
                    emailSettings["SmtpServer"]!,
                    int.Parse(emailSettings["SmtpPort"]!),
                    SecureSocketOptions.StartTls); // StartTls dùng cho cổng 587

                // Xác thực
                await smtp.AuthenticateAsync(emailSettings["SenderEmail"]!, emailSettings["AppPassword"]!);

                // Gửi và ngắt kết nối
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                // Tùy vào cách xử lý log của hệ thống mà bạn ghi log ở đây
                Console.WriteLine($"Error when sending email: {ex.Message}");
                throw; // Hoặc throw custom exception tùy ý
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
