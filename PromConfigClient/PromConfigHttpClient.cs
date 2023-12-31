﻿using System;
using System.Collections.Generic;
using System.Net.Http;
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
        private IHttpClientFactory _httpClientFactory;

        public string EquipmentClientName { get; set; }


        public string EquipmentUri { get; set; } = "http://trucmuche/"; // il en faut une par défaut pour construire HttpRequestMessage

        private string promConfigRoute = "/api/prom/promAllocationScopes";

        public PromConfigHttpClient( IHttpClientFactory hf, ILoggerFactory lf )
        {
            _httpClientFactory = hf;

            log = lf.CreateLogger<PromConfigHttpClient>();
        }

        /// <summary>
        /// Retourne la liste des configurations des proms
        /// </summary>
        /// <returns>Prom list</returns>
        public async Task<IEnumerable<PromScope>> PromConfig()
        {
            log.LogDebug("Récupération auprès du service Equipment de la configuration des Proms");

            var promConfigUrl = new Uri(new Uri(EquipmentUri ?? "http://trucmuche/"), promConfigRoute);

            // construction de la requête
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, promConfigUrl);
            HttpResponseMessage response = null;          

            //  request.SetPolicyExecutionContext(new Polly.Context() { ["Operation"] = promConfigOperation });

            // récupération de la configuration
            try
            {
                var httpClientEquipment = _httpClientFactory.CreateClient(typeof(PromConfigHttpClient).Name);
                httpClientEquipment.DefaultRequestHeaders.Add("Accept", "application/json");
                response = await httpClientEquipment.SendAsync(request);
            }
            catch ( Exception e)
            {
                throw new Exception($"Erreur lors de l'appel à l'url {promConfigUrl}", e);
            }

            IEnumerable<PromScope> promConfiguration = null;

            // parsing de la réponse
            if (null != response.Content)
            {
                var json = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    promConfiguration = JsonConvert.DeserializeObject<List<PromScope>>(json);
                    log.LogInformation($"Chargement de la configuration du service équipement:\n{promConfiguration.Json()}");
                }

            }

            return promConfiguration;
        }
    }
}