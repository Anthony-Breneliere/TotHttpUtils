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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace HttpHandlersTest
{
    /// <summary>
    /// Bouchon d'accès au service équipement
    /// </summary>
    public class HttpHandlerStub : DelegatingHandler
    {
        private static ILogger log;

        private FileSystemWatcher _watcher;

        public List<RequestResponseRule> ResponseRules { get; set; } = new List<RequestResponseRule>();

        public HttpHandlerStub(ILoggerFactory lf)
        {
            log = lf.CreateLogger(typeof(HttpHandlerStub).FullName);

        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;

            if (! ResponseRules.Any())
                response = await base.SendAsync(request, cancellationToken);
            
            else
            {
                RequestResponseRule foundRule = null;

                var uriPathAndQuery = request.RequestUri.PathAndQuery;
                var requestMessage = request.Content != null ? await request.Content.ReadAsStringAsync() : null;

                // recherche d'un règle correspondant à la requete
                using (var ruleEnum = ResponseRules.GetEnumerator())
                while (foundRule == null && ruleEnum.MoveNext())
                {
                    var rule = ruleEnum.Current;

                    if (string.IsNullOrEmpty(rule.RequestPathAndQuery))
                        foundRule = rule;

                    else if (rule.RequestIsRegex)
                    {
                        if (Regex.IsMatch(uriPathAndQuery, rule.RequestPathAndQuery))
                            foundRule = rule;
                    }

                    else if (uriPathAndQuery.Contains(rule.RequestPathAndQuery))
                        foundRule = rule;

                    // si un message est défini dans la règle mais que le message de la requete ne correspond pas
                    // la règle ne s'aplique pas
                    if (!string.IsNullOrEmpty(requestMessage) && !string.IsNullOrEmpty(rule.RequestMessage))
                        if (requestMessage != rule.RequestMessage)
                            foundRule = null;

                } 


                if (foundRule != null)
                {
                    response = new HttpResponseMessage()
                    {
                        Content = new StringContent(foundRule.ResponseMessage.Content),
                        StatusCode = foundRule.ResponseMessage.StatusCode
                    };

                    log.LogDebug($"Stub returning Http response message from file {_responseJsonFile}, rule {foundRule.Json()}");
                }
                else
                {
                    response = await base.SendAsync(request, cancellationToken);
                }
            }


            return response;
        }


        public string _responseJsonFile { get; set; }

        /// <summary>
        /// Le fichier de réponse est lu à chaque affectation d'un nouveau fichier de réponse
        /// </summary>
        public string ResponseJsonFile
        {
            get
            {
                return _responseJsonFile;
            }
            set
            {
                _responseJsonFile = value;

                if (!string.IsNullOrEmpty(value))
                    ReadResponseFile(value);

                UpdateFileWatcher();
            }
        }

        /// <summary>
        /// Ajout d'un file watch sur le fichier de réponse http
        /// </summary>
        private void UpdateFileWatcher()
        {
            if (_watcher != null)
                _watcher.EnableRaisingEvents = false;

            if (!string.IsNullOrEmpty(_responseJsonFile))
            {
                var fileInfo = new FileInfo(_responseJsonFile);

                // Create a new FileSystemWatcher and set its properties.
                _watcher = new FileSystemWatcher()
                {
                    Path = fileInfo.Directory.FullName,
                    Filter = fileInfo.Name,
                    NotifyFilter = NotifyFilters.LastWrite
                };

                _watcher.Changed += (sender, args) => { ReadResponseFile(_responseJsonFile); };

                _watcher.NotifyFilter = NotifyFilters.LastWrite;

                // active le watcher
                _watcher.EnableRaisingEvents = true;
            }
            else
            {
                _watcher = null;
            }

        }


        /// <summary>
        /// Lecture du fichier de réponse
        /// </summary>
        /// <param name="filePath"></param>
        private void ReadResponseFile( string filePath )
        {
            lock (_responseJsonFile)
            {
                log.LogInformation("Lecture du fichier de réponses pour les requêtes Http");

                // lecture du fichier de réponse
                var responseFile = File.ReadAllText(filePath);
                var jsonContent = JsonConvert.DeserializeObject<List<RequestResponseRule>>(responseFile);

                log.LogDebug("Fichier de réponse http:\n{jsonContent}", jsonContent);

                this.ResponseRules = jsonContent;
            }
        }

    }
}
