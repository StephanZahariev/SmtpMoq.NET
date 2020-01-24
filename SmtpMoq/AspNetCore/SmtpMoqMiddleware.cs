using Microsoft.AspNetCore.Http;
using SmtpMoq.Model;
using SmtpMoq.Repository;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmtpMoq.AspNetCore
{
    public class SmtpMoqMiddleware
    {
        private readonly RequestDelegate next;

        public SmtpMoqMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, IEmailRepository emailRepository)
        {
            if (context.Request.Path.ToString().ToLower() == "/smtpmoq/ui/emails")
            {
                InitResponse(context.Response);
                context.Response.ContentType = "text/html";

                string emailUI = BuildEmailDetailsPage(emailRepository, context.Request.Query["id"]);
                byte[] buffer = Encoding.UTF8.GetBytes(emailUI);
                await context.Response.Body.WriteAsync(buffer);

                return;
            }

            if (context.Request.Path.ToString().ToLower() == "/smtpmoq/ui")
            {
                InitResponse(context.Response);
                context.Response.ContentType = "text/html";

                string emailsListUI = BuildEmailsListPage(emailRepository, context.Request.Path);
                byte[] buffer = Encoding.UTF8.GetBytes(emailsListUI);
                await context.Response.Body.WriteAsync(buffer);

                return;
            }

            if (context.Request.Path.ToString().ToLower() == "/smtpmoq/api/emails/last")
            {
                InitResponse(context.Response);
                context.Response.ContentType = "application/json";

                EmailMessage email = emailRepository.LastMessage;
                await JsonSerializer.SerializeAsync<EmailMessage>(context.Response.Body, email);

                return;
            }

            if (context.Request.Path.ToString().ToLower() == "/smtpmoq/api/emails")
            {
                InitResponse(context.Response);
                context.Response.ContentType = "application/json";

                await JsonSerializer.SerializeAsync<IEnumerable<EmailMessage>>(
                    context.Response.Body, emailRepository.ReceivedMessages);

                return;
            }

            await next(context);
        }

        private static void InitResponse(HttpResponse response)
        {
            response.StatusCode = (int)HttpStatusCode.OK;
            response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            response.Headers.Add("Pragma", "no-cache");
            response.Headers.Add("Expires", "0");
        }

        private string BuildEmailsListPage(IEmailRepository emailRepository, string urlPath)
        {
            StringBuilder emailsList = new StringBuilder();
            foreach (var email in emailRepository.ReceivedMessages)
            {
                emailsList.Append("<tr>");
                emailsList.Append($"<td><a href=\"{urlPath + "/emails?id=" + email.Guid}\">View</a></td><td>{email.From}</td><td>{email.RecipientsCommaSeparated}</td></td>");
                emailsList.Append("</tr>");
            }
            string pageBody = ReadResourceFromAssembly("SmtpMoq.Resources.EmailsList.html");

            return pageBody.Replace("***emails***", emailsList.ToString());
        }

        private string BuildEmailDetailsPage(IEmailRepository emailRepository, string emailId)
        {
            EmailMessage email = null;
            foreach (var item in emailRepository.ReceivedMessages)
            {
                if (item.Guid.ToString() == emailId)
                {
                    email = item;
                    break;
                }
            }
            if (email == null)
            {
                return "Invalid email id";
            }

            StringBuilder pageBody = new StringBuilder();
            pageBody.Append(ReadResourceFromAssembly("SmtpMoq.Resources.EmailDetails.html"));
            pageBody.Replace("***fromemail***", email.From);
            pageBody.Replace("***toemail***", email.RecipientsCommaSeparated);
            pageBody.Replace("***emaildata***", email.Data);

            return pageBody.ToString();
        }

        private string ReadResourceFromAssembly(string name)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(name))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
