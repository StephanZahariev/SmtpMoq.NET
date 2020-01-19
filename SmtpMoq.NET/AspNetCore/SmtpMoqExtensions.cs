using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SmtpMoq.Repository;

namespace SmtpMoq.AspNetCore
{
    public static class SmtpMoqExtensions
    {
        public static IApplicationBuilder UseSmtpMoq(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SmtpMoqMiddleware>();
        }

        public static IServiceCollection AddSmtpMoq(this IServiceCollection services)
        {
            return services.AddHostedService<SmtpMoqManagerService>()
                .AddSingleton<IEmailRepository, InMemoryEmailRepository>();
        }
    }
}
