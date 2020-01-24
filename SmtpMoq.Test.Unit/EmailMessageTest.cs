using SmtpMoq.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SmtpMoq.NET.Test.Unit
{
    public class EmailMessageTest
    {
        [Fact]
        public void TestEmailRecipientsCommaSeparated()
        {
            EmailMessage email = new EmailMessage();
            email.Recipients = new string[] { "first@recipient.com", "second@recipient.com" };

            Assert.Equal("first@recipient.com, second@recipient.com", email.RecipientsCommaSeparated);
        }
    }
}
