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
    /// <summary>
    /// Bouchon d'accès au service équipement
    /// </summary>
    public class EquipmentHttpStub : DelegatingHandler
    {
        private static ILogger log;

        public EquipmentHttpStub(ILoggerFactory lf)
        {
            log = lf.CreateLogger(typeof(EquipmentHttpStub).FullName);
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
