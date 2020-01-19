# SmtpMoq.NET

SmtpMoq.NET is a lightweight SMTP Server that runs within the process of your .NET Core application and receives the outgoing emails. Using HTML UI or JSON API you can verify if the emails are correctly sent during development/integration testing.

## Supported SMTP commands
The following SMTP commands are suppported:
* EHLO
* HELO
* MAIL
* RCPT
* DATA
* RSET
* NOOP
* QUIT
* VRFY

SSL connections are not supported since the SMTP server runs on the same machine. Also no username/paswword authentication is required.

## Installing via NuGet
The easies way to install SmtpMoq.NET is via [NuGet](https://www.nuget.org/packages/SmtpMoq.NET/).

Enter the following command in Visual Studio's [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console):


    Install-Package SmtpMoq.NET
	
## Running the SMTP server
To run the SmtpMoq.NET server inside your application add the following line to the `ConfigureServices` method (`Startup.cs`)
```csharp
services.AddSmtpMoq();
```

To run the HTML UI and the JSON API add the following line to the `Configure` method (`Startup.cs`):
```csharp
app.UseSmtpMoq();
```

## Testing API that sends emails
Bellow is a code snipped from the SmtpMoq.Example.WebApi.Test.E2E sample part of the SmtpMoq.NET package. The test checks if the user registration API properly sends an account confirmation email:

```csharp
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
```

## Visualy inspect the emails sent by the application
You can visually access the emails processed by the server by navigating to the folloing URL address `/smtpmoq/ui`:

	http://YourAppUrl/smtpmoq/ui

## Reporting Bugs

To file a bug or new feature, [please open a new issue](https://github.com/StephanZahariev/SmtpMoq.NET/issues).
