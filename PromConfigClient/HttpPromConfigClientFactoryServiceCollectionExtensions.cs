using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.Design;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using HttpDiskCache;
using Microsoft.Extensions.Logging;

namespace PromConfigClient
{
    public static class HttpPromConfigClientFactoryServiceCollectionExtensions
    {
        public static IHttpClientBuilder AddPromConfigHttpClient(this IServiceCollection services, Action<PromConfigHttpClient> configAction )
        {
            return  services

                // ajout de la gestion d'un message handler de gestion de la cache
                .AddTransient<CachedRequestHttpHandler>()
                .AddTransient<ICache, FileCache>()

                // le client d'accès à la configuration des proms
                .AddScoped<PromConfigHttpClient>(sp =>
                {
                    var newPromConfigClient = new PromConfigHttpClient(sp.GetService<IHttpClientFactory>(), sp.GetService<ILoggerFactory>());
                    configAction(newPromConfigClient);
                    return newPromConfigClient;
                })

                // un client http
                .AddHttpClient(typeof(PromConfigHttpClient).Name)

                // le message handler testé
                .AddHttpMessageHandler<CachedRequestHttpHandler>();
        }
    }
}
