using System;
using System.IO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NosData.Utils
{
    public class AzureBlobCache
    {
        private readonly BlobContainerClient _blobContainerClient;
        private readonly ILogger<AzureBlobCache> _logger;

        public AzureBlobCache(IConfiguration configuration, ILogger<AzureBlobCache> logger)
        {
            _blobContainerClient =
                new BlobContainerClient(configuration.GetValue<string>("ConnectionString"), "NosData");
            _logger = logger;
            _blobContainerClient.CreateIfNotExists();
        }

        public byte[] Load(string key)
        {
            _logger.LogInformation("Loading " + key);
            var blob = _blobContainerClient.GetBlockBlobClient(key);
            if (!blob.Exists().Value) return null;
            var createdOn = blob.GetProperties().Value.CreatedOn;
            if (DateTime.Now.Subtract(createdOn.LocalDateTime) > TimeSpan.FromDays(1)) return null;
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