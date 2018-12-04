using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IMAUtils.Extension;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Utils;

namespace HttpHandlersTest
{
    /// <summary>
    /// Bouchon d'accès au service équipement
    /// </summary>
    public class HttpHandlerStub : DelegatingHandler
    {
        private static ILogger log;

        public HttpHandlerStub(ILoggerFactory lf)
        {
            log = lf.CreateLogger(typeof(HttpHandlerStub).FullName);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;

            if (ReplacedResponseContent == null)
            {
                log.LogInformation($"Starting request {request.Json()}");
                log.LogDebug(request.RequestUri.Json());
                response = await base.SendAsync(request, cancellationToken);
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent( ReplacedResponseContent.ToString())
                };
            }


            return response;
        }


        public JToken ReplacedResponseContent { get; set; }
    }
}
