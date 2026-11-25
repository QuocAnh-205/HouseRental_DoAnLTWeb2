namespace HouseRentalAPI.Services;

/// <summary>
/// Chưa gửi email thật: ghi nội dung ra log. Thay bằng SMTP / SendGrid... khi triển khai.
/// </summary>
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body)
    {
        logger.LogInformation("[Email] To: {To} | Subject: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
