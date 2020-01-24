using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Moq;
using SmtpMoq.AspNetCore;
using SmtpMoq.Model;
using SmtpMoq.Repository;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SmtpMoq.NET.Test.Unit
{
    public class SmtpMoqMiddlewareTest
    {
        private readonly SmtpMoqMiddleware middleware;
        private readonly EmailMessage testEmail;
        private readonly Mock<IEmailRepository> emailRepositoryDependency;

        const string NOT_INTERCEPTED = "NotIntercepted";

        public SmtpMoqMiddlewareTest()
        {
            this.middleware = new SmtpMoqMiddleware(
                next: (innerHttpContext) =>
                {
                    innerHttpContext.Response.WriteAsync(NOT_INTERCEPTED);
                    return Task.CompletedTask;
                });

            this.testEmail = new EmailMessage();
            this.testEmail.From = "from@sender.com";
            this.testEmail.Recipients = new string[] { "first@recipient.com" };
            this.testEmail.Guid = new System.Guid("34966b49-6833-48b9-8d62-89e07fa32fec");
            this.testEmail.Data =
                @"MIME-Version: 1.0
                From: from@sender.com
                To: first@recipient.com
                Date: 15 Jan 2020 17:24:14 +0200
                Subject: Test Subject
                Content-Type: text/plain; charset=us-ascii
                Content-Transfer-Encoding: quoted-printable

                Test Body";

            this.emailRepositoryDependency = new Mock<IEmailRepository>();
            emailRepositoryDependency.Setup(x => x.ReceivedMessages).Returns(new List<EmailMessage> { testEmail });
            emailRepositoryDependency.Setup(x => x.LastMessage).Returns(testEmail);
        }

        [Theory]
        [InlineData("/smtpmoq/ui", true)]
        [InlineData("/smtpmoq/ui/emails", true)]
        [InlineData("/smtpmoq/api/emails", true)]
        [InlineData("/smtpmoq/api/emails/last", true)]
        [InlineData("/smtpmoq", false)]
        [InlineData("/index", false)]
        public async Task TestUrlIntercepted(string urlPath, bool isIntercepted)
        {
            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = urlPath;
            Mock<IEmailRepository> emailRepositoryDependency = new Mock<IEmailRepository>();

            await this.middleware.InvokeAsync(context, emailRepositoryDependency.Object);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string body = new StreamReader(context.Response.Body).ReadToEnd();

            if (isIntercepted)
            {
                Assert.NotEqual(NOT_INTERCEPTED, body);
            }
            else
            {
                Assert.Equal(NOT_INTERCEPTED, body);
            }
        }

        [Fact]
        public async Task TestEmailDetailsGenerated()
        {
            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/smtpmoq/ui/emails";
            context.Request.Query = new QueryCollection(
                new Dictionary<string, StringValues>()
                    {
                        { "id", new StringValues("34966b49-6833-48b9-8d62-89e07fa32fec") }
                    });

            await middleware.InvokeAsync(context, emailRepositoryDependency.Object);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string body = new StreamReader(context.Response.Body).ReadToEnd();

            Assert.Contains("<title>SmptMoq.NET Email Details</title>", body);
            Assert.Contains(testEmail.From, body);
            Assert.Contains(testEmail.RecipientsCommaSeparated, body);
            Assert.Contains(testEmail.Data, body);
        }

        [Fact]
        public async Task TestEmailListGenerated()
        {
            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/smtpmoq/ui";

            await middleware.InvokeAsync(context, emailRepositoryDependency.Object);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string body = new StreamReader(context.Response.Body).ReadToEnd();

            Assert.Contains("<title>SmptMoq.NET Received Emails</title>", body);
            Assert.Contains(testEmail.From, body);
            Assert.Contains(testEmail.RecipientsCommaSeparated, body);
        }

        [Fact]
        public async Task TestApiEmailsList()
        {
            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/smtpmoq/api/emails";

            await middleware.InvokeAsync(context, emailRepositoryDependency.Object);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string body = new StreamReader(context.Response.Body).ReadToEnd();

            IList<EmailMessage> receivedEmailsList = JsonSerializer.Deserialize<IList<EmailMessage>>(body);

            Assert.Equal(1, receivedEmailsList.Count);
            AssertEmail(this.testEmail, receivedEmailsList[0]);
        }

        [Fact]
        public async Task TestApiLastEmail()
        {
            HttpContext context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            context.Request.Path = "/smtpmoq/api/emails/last";

            await middleware.InvokeAsync(context, emailRepositoryDependency.Object);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            string body = new StreamReader(context.Response.Body).ReadToEnd();

            EmailMessage receivedEmail = JsonSerializer.Deserialize<EmailMessage>(body);

            AssertEmail(this.testEmail, receivedEmail);
        }

        private void AssertEmail(EmailMessage expectedEmail, EmailMessage actualEmail)
        {
            Assert.Equal(expectedEmail.From, actualEmail.From);
            Assert.Equal(expectedEmail.RecipientsCommaSeparated, actualEmail.RecipientsCommaSeparated);
            Assert.Equal(expectedEmail.Guid, actualEmail.Guid);
            Assert.Equal(expectedEmail.Data, actualEmail.Data);
        }
    }
}