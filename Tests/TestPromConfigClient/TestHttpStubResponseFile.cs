using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using PromConfig;
using Xunit;
using PromConfigClient;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Collections.Generic;
using System.Net;
using HttpHandlersTest;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Sinks.InMemory;
using Xunit.Abstractions;

namespace TestPromConfigClient
{
    public class TestEquipmentStub
    {
        private IServiceProvider serviceProvider;

        private ILogger log;

        public TestEquipmentStub( ITestOutputHelper output )
        {
            // Ajout du client prom config
            serviceProvider =
                new ServiceCollection()

                    .AddLogging( lb => lb.AddSerilog( new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.TestOutput(output).WriteTo.InMemory().CreateLogger()) )

                    // ajout du messages handler qui intercepte les appels 
                    .AddSingleton<HttpHandlerStub>()

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
                    ResponseMessage = new HttpResponse() {ContentJson = JToken.FromObject( promScopeList ), StatusCode = HttpStatusCode.Accepted}
                }
            };

            // act
            var promConfigGet = await serviceProvider.GetService<PromConfigHttpClient>().PromConfig();

            // check
            promConfigGet.Should().BeEquivalentTo(promScopeList);

        }






    }
}
