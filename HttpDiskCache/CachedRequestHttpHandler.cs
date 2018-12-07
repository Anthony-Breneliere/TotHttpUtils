
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IMAUtils.Extension;
using Microsoft.Extensions.Logging;

namespace HttpDiskCache
{
    /// <summary>
    /// Handler Http permettant de mettre en cache les messages reçus
    /// </summary>
    public class CachedRequestHttpHandler : DelegatingHandler
    {
        public static string CacheDir { get; } = "httpCache";

        private ILogger log;

        private ICache _cache;

        public CachedRequestHttpHandler(ILoggerFactory lf, ICache cache )
        {
            log = lf.CreateLogger<CachedRequestHttpHandler>();

            _cache = cache;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken )
        {

        // envoi de la requête au serveur
        Task<HttpResponseMessage> sendAsyncTask = base.SendAsync(request, cancellationToken);

            // le nom du fichier est le nom de l'opération de la requête
            var fileName = $"{CacheDir}/{request.RequestUri.AbsolutePath.Split('/').Last()}.json";

            // si le ficher existe alors on le charge
            Task<string> readFileAsyncTask = _cache.ReadFromCache(fileName);

            // attente de la réponse du web service
            HttpResponseMessage response;
            string messageContent;
            try
            {
                response =  await sendAsyncTask;
                messageContent = await response.Content.ReadAsStringAsync();
            }

            // en cas d'erreur du web service, on retourne les données en cache
            catch( Exception e )
            {
                e.logMic( log, "Erreur de récupération de la configuration de la prom");

                if (null != readFileAsyncTask)
                {
                    log.LogInformation($"Chargement du fichier cache {fileName}");
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Accepted,
                        Content = new StringContent(await readFileAsyncTask)
                    };
                }
                else
                    throw new Exception($"Il n'existe pas de fichier {fileName} en cache pour pallier à l'erreur d'appel au web service REST", e);
            }

            // on lance une tâche de mise à jour du cache dont on n'attend pas la fin
            if (!string.IsNullOrEmpty(messageContent) && null != readFileAsyncTask)
            {
                #pragma warning disable CS4014
                UpdateCacheIfRequired(fileName, messageContent, readFileAsyncTask);
            }

            return response;
        }

        private async Task UpdateCacheIfRequired(string fileName, string newContent, Task<string> readFileAsyncTask)
        {
            try
            {
                var fileContent = await readFileAsyncTask;

                // on compare le contenu du cache avec le nouveau contenu
                if (fileContent != newContent)
                {
                    // si c'est différent alors on met à jour le fichier
                    await _cache.WriteToCache(fileName, newContent);
                }
            }
            catch (Exception e)
            {
                e.logMic(log, $"Erreur lors de la mise à jour du fichier cache {fileName}");
            }
        }

    }
}