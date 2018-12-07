using System.Net;
using Newtonsoft.Json.Linq;

namespace HttpHandlersTest
{
    /// <summary>
    /// Règle d'association d'un message de réponse pour une requete et/ou un message donné
    /// </summary>
    public class HttpResponse
    {
        /// <summary>
        /// exemple: /api/route/15?age=15#tableau
        /// Si non défini alors la règle s'applique à toutes les routes
        /// </summary>
        public string Content { get; set; }


        /// <summary>
        /// exemple: /api/route/15?age=15#tableau
        /// Si non défini alors la règle s'applique à toutes les routes
        /// </summary>
        public JToken ContentJson { get; set; }

        /// <summary>
        /// Mettre vrai si RequestPathAndQuery est une regexp
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

    }
}
