using System.Threading.Tasks;

namespace HttpDiskCache
{
    /// <summary>
    /// Interface permettant de rendre sa nature interchangeable (file, blob..)
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Lecture du cache
        /// </summary>
        /// <returns>La tâche de lecture avec le coutenu, ou null si le cache n'existe pas</returns>
        Task<string> ReadFromCache(string cacheName);

        /// <summary>
        /// Ecriture du cache
        /// </summary>
        /// <param name="cacheName">Nom unique du cache</param>
        /// <param name="newContent">Le contenu</param>
        /// <returns></returns>
        Task WriteToCache(string cacheName, string newContent);

    }

}
