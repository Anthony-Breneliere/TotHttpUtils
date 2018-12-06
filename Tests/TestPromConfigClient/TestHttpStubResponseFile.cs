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
using System.Net;
using HttpHandlersTest;
using Newtonsoft.Json.Linq;

namespace TestPromConfigClient
{
    public class TestEquipmentStub
    {
        private IServiceProvider serviceProvider;

        private ILogger log;

        public TestEquipmentStub()
        {
            LogManager.LoadConfiguration("nlog.config");

            // Ajout du client prom config
            serviceProvider =
                new ServiceCollection()

                    .AddLogging(lb => { lb.AddNLog().SetMinimumLevel(LogLevel.Trace); })

                    // ajout du messages handler qui intercepte les appels 
                    .AddScoped<HttpHandlerStub>()

                    .AddPromConfigHttpClient(clientEquip => { })

                    // le http handler qui intercepte les appels
                    .AddHttpMessageHandler<HttpHandlerStub>()

                    .Services
                    .BuildServiceProvider();

            log = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<TestCachedRequestHttpHandler>();
        }

        [Fact]
        public async Task TestEquipmentStubReplacedResponse()
        {
            // prepare
            var promScopeList = new[]
            {
                new PromScope {Country = "Fr", FirstProm = 1000, LastProm = 1100, Protocol = "SECOM3", Service = "PrestoPizza"},
                new PromScope {Country = "It", FirstProm = 2000, LastProm = 2100}
            };


            serviceProvider.GetService<HttpHandlerStub>().ResponseRules = new List<RequestResponseRule>()
            {
                new RequestResponseRule()
                {
                    ResponseMessage = new HttpResponse() {Content = promScopeList.JsonFlat(), StatusCode = HttpStatusCode.Accepted}
                }
            };

            // act
            var promConfigGet = await serviceProvider.GetService<PromConfigHttpClient>().PromConfig();

            // check
            promConfigGet.Should().BeEquivalentTo(promScopeList);

        }






    }
}
