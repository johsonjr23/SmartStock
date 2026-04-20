namespace SmartStock.Services
{
    public interface IEmailService
    {
        Task SendApprovalEmailAsync(string toEmail, string ownerName, string shopName, string password);
        Task SendTwoFactorCodeAsync(string toEmail, string displayName, string code);
    }
}
