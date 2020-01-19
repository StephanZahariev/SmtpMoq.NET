using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;
using Xunit.Abstractions;

namespace SmtpMoq.NET.Test.E2E
{
    public abstract class SmtpServerBaseTest : IDisposable
    {
        bool disposed = false;

        protected SmtpServer server;

        protected const string testFromAddress = "from@address.com";
        protected const string testToAddress = "to@address.com";
        protected const string testSubject = "Test Subject";
        protected const string testBody = "Test Body";
        protected const string testHtmlBody = "<b><i>Test</i> Body</b>";
        protected const string testAttachmentContent = "Sample attachment content";
        protected const string testAttachmentName = "SampleAttachmentName";

        const int senderTimeoutInMiliseconds = 1000 * 60 * 60;

        public SmtpServerBaseTest(ITestOutputHelper testOutputHelper)
        {
            disposed = false;

            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(testOutputHelper));
            this.server = new SmtpServer(loggerFactory.CreateLogger<SmtpServerSimpleTest>());

            _ = this.server.StartAsync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                this.server.Stop();
            }

            disposed = true;
        }

        protected static void SendSmtpClientMessage(string fromAddress, string toAddress, string subject, string body,
            bool isHtmlBody = false, bool isSenderNameRequired = false, string attachmentName = null, string attachmentContent = null)
        {
            MailMessage message = new MailMessage(fromAddress, toAddress, subject, body);
            message.IsBodyHtml = isHtmlBody;
            if (isSenderNameRequired)
            {
                message.From = new MailAddress(fromAddress, fromAddress.Replace("@", ""));
                message.To.Clear();
                message.To.Add(new MailAddress(toAddress, toAddress.Replace("@", "")));
            }

            using (SmtpClient smtpClient = new System.Net.Mail.SmtpClient())
            {
                smtpClient.Host = "127.0.0.1";
                smtpClient.Port = 25;
                smtpClient.EnableSsl = false;
#if DEBUG
                smtpClient.Timeout = senderTimeoutInMiliseconds;
#endif

                if (attachmentName != null)
                {
                    using (var attachmentStream = new MemoryStream(Encoding.UTF8.GetBytes(attachmentContent)))
                    {
                        Attachment attachment = new Attachment(attachmentStream, attachmentName);
                        message.Attachments.Add(attachment);

                        smtpClient.Send(message);
                        return;
                    }
                }

                smtpClient.Send(message);
            }
        }

        protected static void SendMailKitMessage(string fromAddress, string toAddress, string subject, string body,
            bool isHtmlBody = false, bool isSenderNameRequired = false, string attachmentName = null, string attachmentContent = null)
        {
            var message = new MimeMessage();
            InternetAddress from = new MailboxAddress(fromAddress);
            if (isSenderNameRequired)
            {
                from.Name = fromAddress.Replace("@", "");
            }
            message.From.Add(from);

            InternetAddress to = new MailboxAddress(toAddress);
            if (isSenderNameRequired)
            {
                to.Name = toAddress.Replace("@", "");
            }
            message.To.Add(to);

            message.Subject = subject;

            string textPartSubtype = isHtmlBody ? "html" : "plain";
            var builder = new BodyBuilder();
            if (isHtmlBody)
            {
                builder.HtmlBody = body;
            }
            else
            {
                builder.TextBody = body;
            }

            if (attachmentName != null)
            {
                builder.Attachments.Add(attachmentName, Encoding.UTF8.GetBytes(attachmentContent));
            }

            message.Body = builder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
#if DEBUG
                client.Timeout = senderTimeoutInMiliseconds;
#endif

                client.Connect("127.0.0.1", 25, false);

                client.Send(message);
                client.Disconnect(true);
            }
        }

        public delegate void SmtpSenderDelegate(string fromAddress, string toAddress, string subject, string body,
            bool isHtmlBody = false, bool isSenderNameRequired = false, string attachmentName = null,
            string attachmentContent = null);

        public static IEnumerable<object[]> AvailableSmtpSenders
        {
            get
            {
                return new[]
                {
                    new object[] { (SmtpSenderDelegate) SendSmtpClientMessage },
                    new object[] { (SmtpSenderDelegate) SendMailKitMessage }
                };
            }
        }
    }
}
