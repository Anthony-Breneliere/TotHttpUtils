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
using System.Linq;
using System.Net;
using System.Threading;
using HttpHandlersTest;
using Newtonsoft.Json.Linq;

namespace TestPromConfigClient
{
    public class TestHttpStubResponseFile
    {
        private IServiceProvider serviceProvider;

        private ILogger log;

        public TestHttpStubResponseFile()
        {
            LogManager.LoadConfiguration("nlog.config");
            
            // Ajout du client prom config
            serviceProvider =
                new ServiceCollection()

                    .AddLogging(lb => { lb.AddNLog().SetMinimumLevel( LogLevel.Trace); })

                    // ajout du messages handler qui intercepte les appels 
                    .AddSingleton<HttpHandlerStub>()

                    .AddHttpClient("Toto") // on s'en fiche car les appels sont bouchonnés

                    // le http handler qui intercepte les appels
                    .AddHttpMessageHandler<HttpHandlerStub>()

                    .Services
                    .BuildServiceProvider();

            log = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<TestCachedRequestHttpHandler>();
        }


        private static List<RequestResponseRule> regexResponseRule = new List<RequestResponseRule>()
        {
            new RequestResponseRule()
            {
                RequestPathAndQuery = @".*/pascelui/la",
                RequestIsRegex = true,
                ResponseMessage = new HttpResponse() { Content = "Ne faites rien", StatusCode = HttpStatusCode.Ambiguous }
            },

            new RequestResponseRule()
            {
                RequestPathAndQuery = @".*/ca/vous/en/bouche/un/couin\?rire=sansdent",
                RequestIsRegex = true,
                ResponseMessage = new HttpResponse() { Content = "Lavez-vous la bouche merci bien", StatusCode = HttpStatusCode.Accepted } 
            }
        };


        private static List<RequestResponseRule> basicResponseRule = new List<RequestResponseRule>()
        {
            new RequestResponseRule()
            {
                RequestPathAndQuery = @"recherche/trouve",
                RequestIsRegex = false,
                ResponseMessage = new HttpResponse() { Content = "Ne faites rien", StatusCode = HttpStatusCode.Accepted }
            },

            new RequestResponseRule()
            {
                RequestPathAndQuery = @"recherche/trouve/recherche/trouve",
                RequestIsRegex = false,
                ResponseMessage = new HttpResponse() { Content = "Lavez-vous la bouche merci bien", StatusCode = HttpStatusCode.Accepted }
            },

            new RequestResponseRule()
            {
                RequestPathAndQuery = @"ca/vous/en/bouche/un/couin",
                RequestIsRegex = false,
                ResponseMessage = new HttpResponse() { Content = "Ca sent le poisson", StatusCode = HttpStatusCode.Accepted }
            }
        };


        [Fact]
        public async Task TestRegexResponseRules()
        {
            // prepare
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();

            httpStub.ResponseRules = regexResponseRule;
            var response = await httpClientToto.GetStringAsync("https://lolololocalhost:654/ca/vous/en/bouche/un/couin?rire=sansdent");

            response.Should().Be(httpStub.ResponseRules[1].ResponseMessage.Content);
        }



        [Fact]
        public async Task TestBasicResponseFile()
        {
            // prepare
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();

            File.WriteAllText( "Equipementrules.json", basicResponseRule.Json() );

            httpStub.ResponseJsonFile = "Equipementrules.json";

            var response = await httpClientToto.GetStringAsync("http://lolololocalhost:654/recherche/trouve/rien");

            response.Should().Be(httpStub.ResponseRules[0].ResponseMessage.Content);

        }


        [Fact]
        public async Task TestResponseFileUpdate()
        {
            // prepare
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();

            File.WriteAllText("Equipementrules.json", regexResponseRule.Json());
            httpStub.ResponseJsonFile = "Equipementrules.json";

            var requeteUri = "https://lolololocalhost:654/ca/vous/en/bouche/un/couin?rire=sansdent";

            var response = await httpClientToto.GetStringAsync(requeteUri);
            response.Should().Be(regexResponseRule[1].ResponseMessage.Content);

            // écriture du fichier pour mettre à jour les règles
            File.WriteAllText("Equipementrules.json", basicResponseRule.Json());

            // attente car on ne gouverne pas l'attente du file watch
            Thread.Sleep(500);

            response = await httpClientToto.GetStringAsync(requeteUri);
            response.Should().Be(basicResponseRule[2].ResponseMessage.Content);
        }
    }
}
