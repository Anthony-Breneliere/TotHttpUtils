using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HttpLoadBalancer
{
    /// <summary>
    /// <![CDATA[
    /// Handler jouant le rôle de loadBalancer actif/passif. En cas d'échech la requête est envoyée sur le serveur de backup
    /// 
    /// Exemple d'utilisation du handler:
    /// ServiceCollection
    /// .AddHttpClient("CentraleNetPrimary", client =>
    /// {
    ///     client.BaseAddress = new Uri("http://mauvaiseadresse");
    /// })
    /// .AddHttpMessageHandler(cb => 
    ///     new HttpHandlerClientBackup("CentraleNetSecondary",
    ///     cb.GetRequiredService<IHttpClientFactory>(), cb.GetRequiredService<ILoggerFactory>())
    ///     {
    ///       SwitchOnBackupTimespan = TimeSpan.Parse("00:00:05"),
    ///       CheckHttpResponse = req => { req.EnsureSuccessStatusCode(); }
    ///     })
    /// .Services
    /// .AddHttpClient( "CentraleNetSecondary", client =>
    /// {
    ///     client.BaseAddress = new Uri("http://172.22.69.130:8443");
    /// })
    /// ]]>
    /// </summary>
    public class HttpHandlerClientBackup : DelegatingHandler
    {
        private ILogger log;

        private IHttpClientFactory _hf;

        private DateTimeOffset? SwitchedServersTime { get; set; }

        /// <summary>
        /// Temps d'attente avant retentative sur le serveur primaire
        /// </summary>
        public TimeSpan SwitchOnBackupTimespan { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Nom du client http alternatif
        /// </summary>
        private string _nextBackupHttpClientName;

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="nextBackupHttpClientName"></param>
        /// <param name="hf"></param>
        /// <param name="lf"></param>
        /// <exception cref="Exception"></exception>
        public HttpHandlerClientBackup(string nextBackupHttpClientName, IHttpClientFactory hf, ILoggerFactory lf )
        {
            if ( string.IsNullOrEmpty(nextBackupHttpClientName))
                throw new Exception("Le nom du client http de backup est obligatoire pour loe load balancing.");

            _hf = hf;
            log = lf.CreateLogger<HttpHandlerClientBackup>();
            _nextBackupHttpClientName = nextBackupHttpClientName;
            CheckHttpResponse = response => {};
        }


        /// <summary>
        /// Implémentation de SendAsync pour l'httpHandler de HttpLoadBalancer
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> firstCall, secondCall;


            //if (request.RequestUri?.IsAbsoluteUri ?? false)
            //    throw new Exception($"La requête {request.RequestUri} destination du HttpClientLoadBalanced ne peut pas être absolue " +
            //                        $"car c'est le load balancer qui détermine le serveur HTTP d'appel.");

            // gestion de l'inversion du primaire et du secondiare pendant le temps paramétré {SwitchBackTimespan}
            bool backupFirst = SwitchedServersTime != null && DateTimeOffset.Now < SwitchedServersTime + SwitchOnBackupTimespan;

            // on prépare l'ordre d'appel entre le client principal et le backup
            if (backupFirst)
            {
                firstCall = SendToBackupAsync;
                secondCall = base.SendAsync;
            }
            else
            {
                firstCall = base.SendAsync;
                secondCall = SendToBackupAsync;
                SwitchedServersTime = null;
            }

            // une copie de la requête  doit être faite avant l'envoi au premier serveur car sinon cette-ci est mise à jour par
            // les handlers du premier serveur
            var backupRequest = await CloneHttpRequestMessageAsync(request);
            var firstRequest = backupFirst ? backupRequest : request;
            var secondRequest = backupFirst ? request : backupRequest;

            try
            {
                response = await firstCall(firstRequest, cancellationToken);

                // la vérification de la réponse lance un HttpRequestException en cas d'erreur, ce qui switch sur le backup
                CheckHttpResponse(response);
            }
            catch (HttpRequestException)
            {
                log.LogError("Erreur lors de l'appel au serveur " +
                             (backupFirst ? $" de backup {firstRequest.RequestUri.Host}" : $"principal {firstRequest.RequestUri.Host}") +
                             ", tentative sur le serveur " +
                             (backupFirst ? $"principal" : $" de backup") + ".");

                response = await secondCall(secondRequest, cancellationToken);

                CheckHttpResponse(response);
            }

            return response;
        }

        /// <summary>
        /// Envoi de la requête au backup
        /// </summary>
        /// <param name="secondaryRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<HttpResponseMessage> SendToBackupAsync(HttpRequestMessage secondaryRequest, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            
            using (var secondaryHttpClient = _hf.CreateClient(_nextBackupHttpClientName))
            {
                response = await secondaryHttpClient.SendAsync(secondaryRequest).ConfigureAwait(false);

                // en cas de réussite sur le backup on switch les serveurs de bakcup à partir de maintenant pour la durée {SwitchOnBackupTimespan}:
                log.LogInformation($"Switch sur le serveur de backup {secondaryHttpClient.BaseAddress} pour une durée de {SwitchOnBackupTimespan}.");
            }

            SwitchedServersTime = DateTimeOffset.Now;
            return response;
        }


        /// <summary>
        /// Un HttpRequestMessage ne peut être envoyé deux fois, ils faut donc pouvoir le cloner
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
        {
            HttpRequestMessage clone = new HttpRequestMessage(req.Method, new Uri(req.RequestUri.PathAndQuery, UriKind.Relative));

            // Copy the request's content (via a MemoryStream) into the cloned object
            var ms = new MemoryStream();
            if (req.Content != null)
            {
                await req.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);

                // Copy the content headers
                if (req.Content.Headers != null)
                    foreach (var h in req.Content.Headers)
                        clone.Content.Headers.Add(h.Key, h.Value);
            }


            clone.Version = req.Version;

            foreach (KeyValuePair<string, object> prop in req.Properties)
                clone.Properties.Add(prop);

            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            return clone;
        }


        /// <summary>
        /// Classe d'option contentn l'identifiant du client Http
        /// </summary>
        public class Options
        {
            /// <summary>
            /// Nom de l'identifiant de l'http client
            /// </summary>
            public string HttpClientIdentifier { get; set; }
        }

        /// <summary>
        /// Action vérifiant la requête Http, une HttpRequestException doit être envoyée en cas d'échec
        /// </summary>
        public Action<HttpResponseMessage> CheckHttpResponse { get; set; }
    }
}
