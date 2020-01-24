using SmtpMoq.Model;
using Xunit;
using Xunit.Abstractions;

namespace SmtpMoq.NET.Test.E2E
{
    public class SmtpServerSimpleTest : SmtpServerBaseTest
    {
        public SmtpServerSimpleTest(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, MemberData(nameof(AvailableSmtpSenders))]
        public void TestSendingSimpleEmail(SmtpSenderDelegate smtpSender)
        {
            smtpSender(testFromAddress, testToAddress, testSubject, testBody);

            Assert.Equal(1, this.server.ReceivedMessages.MessageCount);

            EmailMessage email = this.server.ReceivedMessages.LastMessage;
            Assert.Equal(email.From, testFromAddress);
            Assert.Single(email.Recipients);
            Assert.Equal(testToAddress, email.Recipients[0]);
            Assert.Contains(testBody, email.Data);
        }

        [Theory, MemberData(nameof(AvailableSmtpSenders))]
        public void TestSendingSimpleEmailWithSenderFullname(SmtpSenderDelegate smtpSender)
        {
            smtpSender(testFromAddress, testToAddress, testSubject, testBody, false, true);

            Assert.Equal(1, this.server.ReceivedMessages.MessageCount);

            EmailMessage email = this.server.ReceivedMessages.LastMessage;
            Assert.Equal(email.From, testFromAddress);
            Assert.Single(email.Recipients);
            Assert.Equal(testToAddress, email.Recipients[0]);
            Assert.Contains(testBody, email.Data);
        }


        [Theory, MemberData(nameof(AvailableSmtpSenders))]
        public void TestSendingHtmlEmail(SmtpSenderDelegate smtpSender)
        {
            smtpSender(testFromAddress, testToAddress, testSubject, testHtmlBody, true);

            Assert.Equal(1, this.server.ReceivedMessages.MessageCount);

            EmailMessage email = this.server.ReceivedMessages.LastMessage;
            Assert.Contains(testHtmlBody, email.Data);
        }

        [Theory, MemberData(nameof(AvailableSmtpSenders))]
        public void TestSendingEmailWithAttachments(SmtpSenderDelegate smtpSender)
        {
            smtpSender(testFromAddress, testToAddress, testSubject, testBody, false, false,
                testAttachmentName, testAttachmentContent);

            Assert.Equal(1, this.server.ReceivedMessages.MessageCount);

            EmailMessage email = this.server.ReceivedMessages.LastMessage;
            Assert.Contains("Content-Type: application/octet-stream; name=" + testAttachmentName, email.Data);
        }

        [Theory, MemberData(nameof(AvailableSmtpSenders))]
        public void TestSendingMultipleEmails(SmtpSenderDelegate smtpSender)
        {
            int messagesCount = 11;

            for (int i = 1; i <= messagesCount; i++)
            {
                smtpSender(testFromAddress + i, testToAddress + i, testSubject + i, testBody + i);
            }

            Assert.Equal(messagesCount, this.server.ReceivedMessages.MessageCount);
        }
    }
}
