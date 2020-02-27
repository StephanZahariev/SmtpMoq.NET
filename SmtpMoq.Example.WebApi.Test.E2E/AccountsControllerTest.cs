using Microsoft.AspNetCore.Mvc.Testing;
using SmtpMoq.Example.WebApi.Model;
using SmtpMoq.Model;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SmtpMoq.Example.WebApi.Test.E2E
{
    public class AccountsControllerTest: IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> webApplicationFactory;
        private static HttpClient httpClient;

        public AccountsControllerTest(WebApplicationFactory<Startup> webApplicationFactory)
        {
            this.webApplicationFactory = webApplicationFactory;
            httpClient ??= webApplicationFactory.CreateClient();
        }

        [Fact]
        public async Task TestAccountConfirmationEmailSent()
        {
            RegisterModel registerModel = new RegisterModel()
            {
                Email = "test@emial.com",
                Password = "testPassword",
                ConfirmPassword = "testPassword",
            };

            string modelAsJson = JsonSerializer.Serialize<RegisterModel>(registerModel);
            StringContent requestContent = new StringContent(modelAsJson, Encoding.UTF8, "application/json");

            var registerResponse = await httpClient.PostAsync("/accounts/register", requestContent);
            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

            var emailResponse = await httpClient.GetAsync("/smtpmoq/api/emails/last");
            Assert.Equal(HttpStatusCode.OK, emailResponse.StatusCode);

            string emailResultBody = await emailResponse.Content.ReadAsStringAsync();
            EmailMessage sentEmail = JsonSerializer.Deserialize<EmailMessage>(emailResultBody);
            Assert.Equal("my@address.com", sentEmail.From);
            Assert.Equal(registerModel.Email, sentEmail.RecipientsCommaSeparated);
            Assert.Contains("https://yourdomain.com/accounts/confirm/token123", sentEmail.Data);
        }
    }
}
