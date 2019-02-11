using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IMAUtils.Extension;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using Xunit;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using System.Collections.Generic;
using System.Net;
using Divergic.Logging.Xunit;
using HttpHandlersTest;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;
using LogFactory = Divergic.Logging.Xunit.LogFactory;

namespace TestPromConfigClient
{
    public class TestHttpHandlerStub
    {
        private IServiceProvider serviceProvider;

        private ILogger log;

        private readonly List<RequestResponseRule> _regexResponseRule;

        private readonly List<RequestResponseRule> _basicResponseRule;

        private readonly List<RequestResponseRule> _justeOneRequest;

        private ITestOutputHelper _output;

        public TestHttpHandlerStub( ITestOutputHelper output )
        {
            // LogManager.LoadConfiguration("nlog.config");

            _output = output;

            // Ajout du client prom config
            serviceProvider =
                new ServiceCollection()

                    .AddLogging(lb => { lb.AddNLog().AddProvider( new TestOutputLoggerProvider(output) ).SetMinimumLevel(LogLevel.Trace); })

                    // ajout du messages handler qui intercepte les appels 
                    .AddSingleton<HttpHandlerStub>()

                    .AddHttpClient("Toto") // on s'en fiche car les appels sont bouchonnés

                    // le http handler qui intercepte les appels
                    .AddHttpMessageHandler<HttpHandlerStub>()

                    .Services
                    .BuildServiceProvider();

            log = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<TestCachedRequestHttpHandler>();


            _justeOneRequest = new List<RequestResponseRule>
            {
                new RequestResponseRule()
                {
                    RequestPathAndQuery = "quune/requete",
                    ResponseMessage = new HttpResponse { StatusCode = HttpStatusCode.OK }
                }
            };
                
            _regexResponseRule = new List<RequestResponseRule>
            {
                new RequestResponseRule
                {
                    RequestPathAndQuery = @".*/pascelui/la",
                    RequestIsRegex = true,
                    ResponseMessage = new HttpResponse {Content = "Ne faites rien", StatusCode = HttpStatusCode.Ambiguous}
                },

                new RequestResponseRule
                {
                    RequestPathAndQuery = @".*/ca/vous/en/bouche/un/couin\?rire=sansdent",
                    RequestIsRegex = true,
                    ResponseMessage = new HttpResponse {Content = "Lavez-vous la bouche merci bien", StatusCode = HttpStatusCode.Accepted}
                }
            };


            _basicResponseRule = new List<RequestResponseRule>
            {
                new RequestResponseRule
                {
                    RequestPathAndQuery = @"recherche/trouve",
                    RequestIsRegex = false,
                    ResponseMessage = new HttpResponse {Content = "Ne faites rien", StatusCode = HttpStatusCode.Accepted}
                },

                new RequestResponseRule
                {
                    RequestPathAndQuery = @"recherche/trouve/recherche/trouve",
                    RequestIsRegex = false,
                    ResponseMessage = new HttpResponse {Content = "Lavez-vous la bouche merci bien", StatusCode = HttpStatusCode.Accepted}
                },

                new RequestResponseRule
                {
                    RequestPathAndQuery = @"ca/vous/en/bouche/un/couin",
                    RequestIsRegex = false,
                    ResponseMessage = new HttpResponse {Content = "Ca sent le poisson", StatusCode = HttpStatusCode.Accepted}
                },

                new RequestResponseRule
                {
                    RequestPathAndQuery = @"chante/moi/du/json",
                    RequestIsRegex = false,
                    RequestMessage = new SimpleHttpRequest()
                    {
                        MessageJson = new JObject
                        {
                            ["Titre"] = "Ta ta yo yo",
                            ["Paroles"] = new JObject
                            {
                                ["Question"] = "Qu'est-ce qu'il y a sous ton grand chapeau"
                            }
                        }
                    },
                    ResponseMessage = new HttpResponse
                    {
                        ContentJson = new JObject
                        {
                            ["Titre"] = "Ta ta yo yo",
                            ["Paroles"] = new JObject
                            {
                                ["Reponse"] = "Dans ma tête il y a des tas d'oiseaux"
                            }
                        },
                        StatusCode = HttpStatusCode.Accepted
                    }
                }
            };
        }


        [Fact]
        public async Task TestJustRequestUri()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();
            httpStub.ResponseRules = _justeOneRequest;

            // act
            var response = await httpClientToto.GetAsync("http://lolololocalhost:654/ya/quune/requete/mon/pote");

            // assert
            response.StatusCode.Should().Be(httpStub.ResponseRules[0].ResponseMessage.StatusCode);
        }


        [Fact]
        public async Task TestBasicResponseRules()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();
            httpStub.ResponseRules = _basicResponseRule;

            // act
            var response = await httpClientToto.GetStringAsync("http://lolololocalhost:654/recherche/trouve/rien");

            // assert
            response.Should().Be(httpStub.ResponseRules[0].ResponseMessage.Content);
        }


        [Fact]
        public async Task TestBasicResponseRulesWithBadMethod()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();
            httpStub.ResponseRules = _basicResponseRule;
            // la règle n'accepte que les message PUT
            httpStub.ResponseRules[0].RequestMessage = new SimpleHttpRequest() { Methods = new[] {HttpMethod.Put} };

            // act
            Func<Task<string>> action = async () => await httpClientToto.GetStringAsync("http://lolololocalhost:654/recherche/trouve/rien");

            // assert
            await Assert.ThrowsAsync<HttpRequestException>( action );
        }

        [Fact]
        public async Task TestBasicResponseRulesWithRequest()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();
            httpStub.ResponseRules = _basicResponseRule;
            var messageRule = new JObject {["message"] = "Ce message est doit être identique dans la règle."};
            httpStub.ResponseRules[0].RequestMessage = new SimpleHttpRequest() { MessageText = messageRule.ToString() };
            log.LogDebug(httpStub.ResponseRules.Json());



            // act
            Func<Task<HttpResponseMessage>> postBad = async () => await httpClientToto.PostAsync("http://lolololocalhost:654/recherche/trouve/rien", new StringContent(""));

            // assert
            await Assert.ThrowsAsync<HttpRequestException>(postBad);

            // act => cette fois avec le bon message dans la requête post
            var postGood = httpClientToto.PostAsync("http://lolololocalhost:654/recherche/trouve/rien",
                new StringContent(messageRule.ToString()));
            var response = await (await postGood).Content.ReadAsStringAsync();

            // assert
            response.Should().Be(httpStub.ResponseRules[0].ResponseMessage.Content);

        }



        [Fact]
        public async Task TestRegexResponseRules()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();
            httpStub.ResponseRules = _regexResponseRule;

            // act
            var response = await httpClientToto.GetStringAsync("https://lolololocalhost:654/ca/vous/en/bouche/un/couin?rire=sansdent");

            // assert
            response.Should().Be(httpStub.ResponseRules[1].ResponseMessage.Content);
        }



        [Fact]
        public async Task TestBasicResponseFileWithJsonContent()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();
            httpStub.ResponseRules = _basicResponseRule;

            // act
            var response = await httpClientToto.SendAsync(new HttpRequestMessage()
            {
                Content = new StringContent(httpStub.ResponseRules[3].RequestMessage.MessageJson.JsonFlat()),
                RequestUri = new Uri("http://chocacao:10000/chante/moi/du/json", UriKind.Absolute)
            });
            var jsonResponse = JToken.Parse(await response.Content.ReadAsStringAsync());

            // assert
            jsonResponse.Should().BeEquivalentTo(httpStub.ResponseRules[3].ResponseMessage.ContentJson);
        }


        [Fact]
        public async Task TestResponseFileNotExists()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();

            // act
            Func<Task<string>> action = async () =>
            {
                httpStub.ResponseJsonFile = "FichierSansExistance.json";
                return await httpClientToto.GetStringAsync("http://lolololocalhost:654/recherche/trouve/rien");
            };

            // assert
            await Assert.ThrowsAsync<FileNotFoundException>(action);
        }

        [Fact]
        public async Task TestNoRule()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();

            // act
            Func<Task<string>> action = async () => await httpClientToto.GetStringAsync("http://lolololocalhost:654/recherche/trouve/rien");

            // assert
            await Assert.ThrowsAsync<HttpRequestException> (action);
        }


        [Fact]
        public async Task TestBasicResponseFile()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();
            File.WriteAllText("Equipementrules.json", _basicResponseRule.Json());
            httpStub.ResponseJsonFile = "Equipementrules.json";

            // act
            var response = await httpClientToto.GetStringAsync("http://lolololocalhost:654/recherche/trouve/rien");

            // assert
            response.Should().Be(httpStub.ResponseRules[0].ResponseMessage.Content);
        }


        [Fact]
        public async Task TestResponseFileWatch()
        {
            // arrange
            var httpClientToto = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");
            var httpStub = serviceProvider.GetRequiredService<HttpHandlerStub>();

            File.WriteAllText("EquipementrulesWatch.json", _regexResponseRule.Json());
            httpStub.ResponseJsonFile = "EquipementrulesWatch.json";

            var requeteUri = "https://lolololocalhost:654/ca/vous/en/bouche/un/couin?rire=sansdent";

            // act
            var response = await httpClientToto.GetStringAsync(requeteUri);

            // assert
            response.Should().Be(_regexResponseRule[1].ResponseMessage.Content);

            // arrange

            // écriture du fichier pour mettre à jour les règles
            File.WriteAllText("EquipementrulesWatch.json", _basicResponseRule.Json());

            // attente car on ne gouverne pas l'attente du file watch
            await Task.Delay( 500 );

            // act
            response = await httpClientToto.GetStringAsync(requeteUri);

            // assert
            response.Should().Be(_basicResponseRule[2].ResponseMessage.Content);
        }


        [Fact]
        public async Task TestJustRequestUriWithDedicatedHandler()
        {
            // arrange

            // Ajout du client prom config
            var serviceProviderWithDedicatedGandler =
                new ServiceCollection()

                    .AddLogging(lb => { lb.AddNLog().SetMinimumLevel(LogLevel.Trace); })

                    .AddHttpClient("Toto") // on s'en fiche car les appels sont bouchonnés

                    // le http handler qui intercepte les appels
                    .AddHttpMessageHandler( () => new HttpHandlerStub( LogFactory.Create(_output ))
                    {
                        ResponseRules = _justeOneRequest
                    })

                    .Services
                    .BuildServiceProvider();

            var httpClientToto = serviceProviderWithDedicatedGandler.GetRequiredService<IHttpClientFactory>().CreateClient("Toto");

            // act
            var response = await httpClientToto.GetAsync("http://lolololocalhost:654/ya/quune/requete/mon/pote");

            // assert
            response.StatusCode.Should().Be(_justeOneRequest[0].ResponseMessage.StatusCode);
        }
    }
}
