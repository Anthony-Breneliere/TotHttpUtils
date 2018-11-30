using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IMAUtils.Extension;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using PromConfig;
using Xunit;
using PromConfigClient;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using HttpDiskCache;
using Newtonsoft.Json;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using System.Collections.Generic;

namespace TestPromConfigClient
{
    public class PromConfigClientSuite
    {
        private IServiceProvider serviceProvider;

        private ILogger log;

        public PromConfigClientSuite()
        {
            LogManager.LoadConfiguration("nlog.config");

            // Ajout du client prom config
            serviceProvider =
                new ServiceCollection()

                    .AddLogging(lb => { lb.AddNLog().SetMinimumLevel( LogLevel.Trace); })

                    // ajout du messages handler qui intercepte les appels 
                    .AddScoped<TestEquipmentRequestHandler>()

                    // ajout du messages handler de cache pour la mise en cache
                    .AddTransient<CachedRequestHttpHandler>()
                    .AddTransient<ICache, FileCache>()

                    // un client http
                    .AddScoped<PromConfigHttpClient>(sp =>
                    {
                        return new PromConfigHttpClient(sp.GetService<IHttpClientFactory>(), sp.GetService<ILoggerFactory>())
                        {
                            EquipmentUri = "http://urldetest/"
                        };
                    })

                    .AddHttpClient(typeof(PromConfigHttpClient).Name)

                    // le http handler qui intercepte les appels
                    .AddHttpMessageHandler<TestEquipmentRequestHandler>()

                    // le message handler testé
                    .AddHttpMessageHandler<CachedRequestHttpHandler>()

                    .Services
                    .BuildServiceProvider();

            log = serviceProvider.GetService<ILoggerFactory>().CreateLogger<PromConfigClientSuite>();
        }

        [Fact]
        public async Task TestCache()
        {
            // prepare
            var promScopeList = new[]
            {
                new PromScope {Country = "Fr", FirstProm = 1000, LastProm = 1100, Protocol = "SECOM3", Service = "PrestoPizza"},
                new PromScope {Country = "It", FirstProm = 2000, LastProm = 2100}
            };

            serviceProvider.GetService<TestEquipmentRequestHandler>().CheckRequest = message =>
            {
                Assert.Equal("http://urldetest//api/prom/promAllocationScopes", message.RequestUri.ToString() );
            };

            serviceProvider.GetService<TestEquipmentRequestHandler>().CheckResponse = async message =>
            {
                var content = await message.Content.ReadAsStringAsync();
                JsonConvert.DeserializeObject<IEnumerable<PromScope>>(content).Should().BeEquivalentTo(promScopeList);

            };

            await serviceProvider.GetService<ICache>().WriteToCache($"{CachedRequestHttpHandler.CacheDir}/promAllocationScopes.json", promScopeList.Json());

            // act
            var promConfigGet = await serviceProvider.GetService<PromConfigHttpClient>().PromConfig();

            // check
            promConfigGet.Should().BeEquivalentTo(promScopeList);
        }


    }
}
