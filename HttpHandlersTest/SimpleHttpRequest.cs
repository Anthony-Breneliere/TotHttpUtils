using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace HttpHandlersTest
{
    /// <summary>
    /// Règle d'association d'un message de réponse pour une requete et/ou un message donné
    /// </summary>
    public class SimpleHttpRequest
    {

        /// <summary>
        /// exemple: /api/route correspond à la règle /api/route/15?age=15#tableau
        /// Si non défini alors la règle s'applique à toutes les routes
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Liste des méthodes possibles de la requete
        /// </summary>
        public HttpMethod[] Methods  { get; set; }

    }
}
