using System.Net.Http;
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
        public string MessageText { get; set; }

        /// <summary>
        /// Message envoyé sous forme de structure json
        /// MessageText est ignoré si MessageJson est défini.
        /// </summary>
        public JToken MessageJson { get; set; }

        /// <summary>
        /// Liste des méthodes possibles de la requete
        /// </summary>
        public HttpMethod[] Methods  { get; set; }

    }
}
