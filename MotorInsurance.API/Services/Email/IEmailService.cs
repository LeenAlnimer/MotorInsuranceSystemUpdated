namespace MotorInsurance.API.Services.Email
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string body);
    }
}
