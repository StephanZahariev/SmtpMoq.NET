using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmtpMoq.Model;
using SmtpMoq.Repository;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SmtpMoq.AspNetCore
{
    public class SmtpMoqManagerService : BackgroundService
    {
        private readonly ILogger<SmtpMoqManagerService> logger;

        private SmtpServer server;

        public SmtpMoqManagerService(IEmailRepository emailRepository,
            IConfiguration configuration,
            ILogger<SmtpMoqManagerService> logger)
        {
            this.logger = logger;

            SmtpServerSettings smtpSettings = new SmtpServerSettings();
            if (configuration.GetSection("SmtpMoq").Exists())
            {
                smtpSettings.Endpoint = IPAddress.Parse(configuration["SmtpMoq:Endpoint"]);
                smtpSettings.Port = Convert.ToInt32(configuration["SmtpMoq:Port"]);
                smtpSettings.ServiceDomain = configuration["SmtpMoq:ServiceDomain"];
            }

            this.server = new SmtpServer(smtpSettings, emailRepository, logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var serverTask = this.server.StartAsync();
            var cancellationTask = Task.Run(() => stoppingToken.WaitHandle.WaitOne());

            if (await Task.WhenAny(serverTask, cancellationTask) == cancellationTask)
                this.server.Stop();
        }
    }
}
