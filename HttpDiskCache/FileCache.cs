using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HttpDiskCache
{
    /// <summary>
    /// Implementation d'un cache par des fichiers
    /// </summary>
    public class FileCache : ICache
    {
        private ILogger log;

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="lf"></param>
        public FileCache( ILoggerFactory lf )
        {
            log = lf.CreateLogger<FileCache>();
        }
        
        /// <summary>
        /// Read cache from a local file
        /// </summary>
        /// <returns></returns>
        public Task<string> ReadFromCache( string cacheName)
        {
            Task<string> readFileAsyncTask = null;

            if (File.Exists(cacheName))
            {
                log.LogInformation($"Lecture du fichier {cacheName}");

                readFileAsyncTask = Task.Run(() => File.ReadAllText(cacheName));
            }

            return readFileAsyncTask;
        }

        /// <summary>
        /// Write to cache a local file
        /// </summary>
        /// <param name="fileName">Nom du fichier</param>
        /// <param name="newContent">Contenu du fichier</param>
        /// <returns></returns>
        public async Task WriteToCache(string fileName, string newContent)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            using (var textWriter = File.CreateText(fileName))
                await textWriter.WriteAsync(newContent);
        }
    }
}
