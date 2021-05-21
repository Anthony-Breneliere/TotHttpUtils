using Moq;
using System;
using System.Net.Http;

namespace HttpHandlersTest
{
    /// <summary>
    /// Bouchon pour un client HTTP
    /// 
    /// Initialiser dans un premier temps une instance de <see cref="MockHttpClientFactoryBasic"/>.
    /// Sur cette instance, récupérer un mock de <see cref="IHttpClientFactory"/> prêt à l'emploi
    /// ainsi qu'une instance de <see cref="MockHttpMessageHandler"/> gérant le retour de requête Http.
    /// 
    /// Afin de paramétrer le retour de requête, appeler la méthode <see cref="MockHttpMessageHandler.Returns(System.Net.HttpStatusCode, string)"/>
    /// avec en premier paramètre le code http de retour
    /// et en second paramètre le texte/json de retour.
    /// </summary>
    /// <example>
    /// var mock = new MockHttpClientFactoryBasic();
    /// mock.HttpMessageHandler.Returns(System.Net.HttpStatusCode.OK, "{ "key": "value" }");
    /// </example>
    public class MockHttpClientFactoryBasic
    {
        public MockHttpMessageHandler HttpMessageHandler;
        
        public Mock<IHttpClientFactory> HttpClientFactory;

        public MockHttpClientFactoryBasic()
        {
            HttpMessageHandler = new MockHttpMessageHandler();

            HttpClientFactory = new Mock<IHttpClientFactory>();
            HttpClientFactory
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(new HttpClient(HttpMessageHandler)
                {
                    BaseAddress = new Uri("http://mocked")
                });
        }
    }
}
