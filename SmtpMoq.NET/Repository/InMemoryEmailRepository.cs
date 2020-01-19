using SmtpMoq.Model;
using System.Collections.Generic;
using System.Linq;

namespace SmtpMoq.Repository
{
    public class InMemoryEmailRepository : IEmailRepository
    {
        private List<EmailMessage> emailList;
        private readonly object emailListLock = new object();

        public InMemoryEmailRepository()
        {
            lock (emailListLock)
            {
                this.emailList = new List<EmailMessage>();
            }
        }

        public void AddEmail(EmailMessage email)
        {
            lock (emailListLock)
            {
                this.emailList.Add(email);
            }
        }

        public IEnumerable<EmailMessage> ReceivedMessages
        {
            get
            {
                lock (emailListLock)
                {
                    return this.emailList;
                }
            }
        }

        public EmailMessage LastMessage
        {
            get
            {
                lock (emailListLock)
                {
                    return this.emailList.LastOrDefault();
                }
            }
        }

        public int MessageCount
        {
            get
            {
                lock (emailListLock)
                {
                    return this.emailList.Count;
                }
            }
        }
    }
}