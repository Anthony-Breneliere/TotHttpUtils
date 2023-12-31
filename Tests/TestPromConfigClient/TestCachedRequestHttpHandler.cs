using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IMAUtils.Extension;
using Microsoft.Extensions.Logging;
using PromConfig;
using Xunit;
using PromConfigClient;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using HttpDiskCache;
using Newtonsoft.Json;
using System.Collections.Generic;
using HttpHandlersTest;
using Serilog;
using Serilog.Sinks.InMemory;
using Xunit.Abstractions;

namespace TestPromConfigClient
{
    public class TestCachedRequestHttpHandler
    {
        private IServiceProvider serviceProvider;

        private ILogger log;

        public TestCachedRequestHttpHandler( ITestOutputHelper output )
        {
            // Ajout du client prom config
            serviceProvider =
                new ServiceCollection()

                    .AddLogging( lb => lb.AddSerilog( new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.TestOutput(output).WriteTo.InMemory().CreateLogger()) )
                
                    // ajout du messages handler qui intercepte les appels 
                    .AddScoped<HttpHandlerCheck>()

                    // ajout du messages handler de cache pour la mise en cache
                    .AddTransient<CachedRequestHttpHandler>()
                    .AddTransient<ICache, FileCache>()

                    // un client http
                    .AddTransient<PromConfigHttpClient>(sp =>
                    {
                        return new PromConfigHttpClient(sp.GetService<IHttpClientFactory>(), sp.GetService<ILoggerFactory>())
                        {
                            EquipmentUri = "http://urldetest/"
                        };
                    })

                    .AddHttpClient(typeof(PromConfigHttpClient).Name)

                    // le http handler qui intercepte les appels
                    .AddHttpMessageHandler<HttpHandlerCheck>()

                    // le message handler test
                    .AddHttpMessageHandler<CachedRequestHttpHandler>()

                    .Services
                    .BuildServiceProvider();

            log = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<TestCachedRequestHttpHandler>();
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

            serviceProvider.GetService<HttpHandlerCheck>().CheckRequest = message =>
            {
                Assert.Equal("http://urldetest//api/prom/promAllocationScopes", message.RequestUri.ToString() );
            };

            serviceProvider.GetService<HttpHandlerCheck>().CheckResponse = async message =>
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
