using SmtpMoq.Model;
using System.Collections.Generic;

namespace SmtpMoq.Repository
{
    public interface IEmailRepository
    {
        void AddEmail(EmailMessage email);
        IEnumerable<EmailMessage> ReceivedMessages { get; }
        EmailMessage LastMessage { get; }
        int MessageCount { get; }
    }
}
