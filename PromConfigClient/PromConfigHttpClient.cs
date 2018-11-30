using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IMAUtils.Extension;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PromConfig;

namespace PromConfigClient
{
    public class PromConfigHttpClient
    {
        private ILogger log;

        private HttpClient _httpClientEquipment;

        public string EquipmentClientName { get; set; }

        public string EquipmentUri { get; set; }

        private string promConfigRoute = "/api/prom/promAllocationScopes";

        public PromConfigHttpClient( IHttpClientFactory hf, ILoggerFactory lf )
        {
            _httpClientEquipment = hf.CreateClient( typeof(PromConfigHttpClient).Name );
            _httpClientEquipment.DefaultRequestHeaders.Add("Accept", "application/json");

            log = lf.CreateLogger<PromConfigHttpClient>();
        }

        /// <summary>
        /// Retourne la liste des configurations des proms
        /// </summary>
        /// <returns>Prom list</returns>
        public async Task<IEnumerable<PromScope>> PromConfig()
        {
            log.LogDebug("Récupération auprès du service Equipment de la configuration des Proms");

            var promConfigUrl = EquipmentUri + promConfigRoute;

            // construction de la requête
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, promConfigUrl);
            HttpResponseMessage response = null;

            //  request.SetPolicyExecutionContext(new Polly.Context() { ["Operation"] = promConfigOperation });

            // récupération de la configuration
            try
            {
                response = await _httpClientEquipment.SendAsync(request);
            }
            catch ( Exception e)
            {
                throw new Exception($"Erreur lors de l'appel à l'url {promConfigUrl}", e);
            }

            // parsing de la réponse
            var json = await response.Content.ReadAsStringAsync();
            var promConfiguration = JsonConvert.DeserializeObject<List<PromScope>>(json);

            log.LogInformation($"Chargement de la configuration du service équipement:\n{promConfiguration.Json()}");

            return promConfiguration;
        }
    }
}