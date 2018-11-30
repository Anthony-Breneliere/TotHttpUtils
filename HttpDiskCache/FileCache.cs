using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HttpDiskCache
{
    public class FileCache : ICache
    {
        private ILogger log;

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
        /// <param name="newContent"></param>
        /// <returns></returns>
        public async Task WriteToCache(string fileName, string newContent)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            using (var textWriter = File.CreateText(fileName))
                await textWriter.WriteAsync(newContent);
        }
    }
}
