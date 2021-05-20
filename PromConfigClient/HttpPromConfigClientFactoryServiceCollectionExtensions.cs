using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace PromConfigClient
{
    public static class HttpPromConfigClientFactoryServiceCollectionExtensions
    {
        public static IHttpClientBuilder AddPromConfigHttpClient(this IServiceCollection services, Action<PromConfigHttpClient> configAction )
        {
            return services

                // le client d'accès à la configuration des proms
                .AddTransient<PromConfigHttpClient>(sp =>
                {
                    var newPromConfigClient = new PromConfigHttpClient(sp.GetService<IHttpClientFactory>(), sp.GetService<ILoggerFactory>());
                    configAction(newPromConfigClient);
                    return newPromConfigClient;
                })

                // un client http
                .AddHttpClient(typeof(PromConfigHttpClient).Name);

        }
    }
}
