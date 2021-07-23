using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NosCDN.Utils
{
    public class AzureBlobCache
    {
        private readonly BlobContainerClient _blobContainerClient;
        private readonly ILogger<AzureBlobCache> _logger;

        public AzureBlobCache(IConfiguration configuration, ILogger<AzureBlobCache> logger)
        {
            _blobContainerClient = new(configuration.GetValue<string>("ConnectionString"), "noscdn");
            _logger = logger;
            _blobContainerClient.CreateIfNotExists();
        }

        public byte[] Load(string key)
        {
            _logger.LogInformation("Loading " + key);
            var blob = _blobContainerClient.GetBlockBlobClient(key);
            if (!blob.Exists().Value) return null;
            var createdOn = blob.GetProperties().Value.CreatedOn;
            if (DateTime.Now.Subtract(createdOn.LocalDateTime) > TimeSpan.FromHours(1))
            {
                return null;
            }
            var ms = new MemoryStream();
            blob.Download().Value.Content.CopyTo(ms);
            return ms.ToArray();
        }

        public void Save(string key, byte[] data)
        {
            _logger.LogInformation("Saving " + key);
            var ms = new MemoryStream(data);
            var container = _blobContainerClient.GetBlockBlobClient(key);
            container.Upload(ms);
        }
    }
}
