using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttpHandlersTest
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpStatusCode _statusCode;
        private string _returnValue;
        private Exception _exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken _)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            var response = new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_returnValue ?? ""),
            };
            return Task.FromResult(response);
        }

        /// <summary>
        /// Paramètrer un retour d'une Requête Http
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="value"></param>
        public void Returns(HttpStatusCode statusCode, string value)
        {
            _statusCode = statusCode;
            _returnValue = value;
        }

        /// <summary>
        /// Paramétrer un retour d'exception lors d'une Requête Http
        /// </summary>
        /// <param name="exception"></param>
        public void Throws(Exception exception)
        {
            _exception = exception;
        }
    }
}
