using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IMAUtils.Extension;
using Microsoft.Extensions.Logging;
using Utils;

namespace TestPromConfigClient
{
    public class TestEquipmentRequestHandler : DelegatingHandler
    {
        private static ILogger log;

        public TestEquipmentRequestHandler(ILoggerFactory lf)
        {
            log = lf.CreateLogger(typeof(TestEquipmentRequestHandler).FullName);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (CheckRequest != null)
            {
                log.LogDebug($"Checking Request {request.Json()}");
                CheckRequest(request);
            }

            HttpResponseMessage response = null;

            if (!BlockRequestToNextHandler)
            {
                log.LogInformation($"Starting request {request.Json()}");
                log.LogDebug(request.RequestUri.Json());
                response = await base.SendAsync(request, cancellationToken);
            }

            if (CheckResponse != null)
            {
                log.LogDebug($"Checking response {response?.Json()}");
                CheckResponse(response);
            }

            return response;
        }

        public Action<HttpRequestMessage> CheckRequest { get; set; }

        public Action<HttpResponseMessage> CheckResponse { get; set; }

        public bool BlockRequestToNextHandler { get; set; }
    }
}
