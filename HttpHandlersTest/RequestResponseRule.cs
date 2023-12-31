﻿namespace HttpHandlersTest
{
    /// <summary>
    /// Règle d'association d'un message de réponse pour une requete et/ou un message donné
    /// </summary>
    public class RequestResponseRule
    {
        /// <summary>
        /// exemple: /api/route/15?age=15#tableau
        /// Si non défini alors la règle s'applique à toutes les routes
        /// Si RequestIsRegex est VRAI alors on considère qu'il s'agit d'une expression régulière
        /// Si RequestIsRegex est FAUX alors on considère qu'il s'agit d'une PARTIE du path/query
        /// </summary>
        public string RequestPathAndQuery { get; set; }

        /// <summary>
        /// Mettre vrai si RequestPathAndQuery est une regexp
        /// </summary>
        public bool RequestIsRegex { get; set; }

        /// <summary>
        /// Message de la règle
        /// </summary>
        public SimpleHttpRequest RequestMessage { get; set; }

        /// <summary>
        /// Réponse à retourner
        /// </summary>
        public HttpResponse ResponseMessage { get; set; }
    }
}
