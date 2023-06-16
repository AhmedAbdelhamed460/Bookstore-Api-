using Bookstore.DOT;

namespace Bookstore.Services
{
    public interface IEmailSender
    {
        void SendEmail(EmailDto request);
    }
}