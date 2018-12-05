using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace HttpHandlersTest
{
    /// <summary>
    /// Règle d'association d'un message de réponse pour une requete et/ou un message donné
    /// </summary>
    public class RequestResponseRule
    {
        /// <summary>
        /// exemple: /api/route/15?age=15#tableau
        /// Si non défini alors la règle s'applique à toutes les routes
        /// </summary>
        public string RequestPathAndQuery { get; set; }

        /// <summary>
        /// Mettre vrai si RequestPathAndQuery est une regexp
        /// </summary>
        public bool RequestIsRegex { get; set; }

        /// <summary>
        /// Message de la règle
        /// </summary>
        public string RequestMessage { get; set; }

        /// <summary>
        /// Réponse à retourner
        /// </summary>
        public HttpResponse ResponseMessage { get; set; }
    }
}
