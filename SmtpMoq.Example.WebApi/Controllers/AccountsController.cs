using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmtpMoq.Example.WebApi.Model;

namespace SmtpMoq.Example.WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public AccountsController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet("register2")]
        public IActionResult Register2()
        {
            return Ok("OK");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest("Password and Confirm Password do not match");
            }

            await SendMessage("my@address.com", model.Email, "Account Confirmation",
                "<a href=\"https://yourdomain.com/accounts/confirm/token123\">Please click here to confirm your account</a>");

            return Ok();
        }

        protected async Task SendMessage(string fromAddress, string toAddress, string subject, string body,
            bool isHtmlBody = false)
        {
            MailMessage message = new MailMessage(fromAddress, toAddress, subject, body);
            message.IsBodyHtml = isHtmlBody;

            using (SmtpClient smtpClient = new SmtpClient())
            {
                smtpClient.Host = configuration["SmtpClient:Host"];
                smtpClient.Port = Convert.ToInt32(configuration["SmtpClient:Port"]);
                smtpClient.EnableSsl = Convert.ToBoolean(configuration["SmtpClient:EnableSsl"]);

                await smtpClient.SendMailAsync(message);
            }
        }

    }
}