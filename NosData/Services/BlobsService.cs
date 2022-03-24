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

namespace NosData.Services
{
    public class BlobsService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobsService(IConfiguration configuration)
        {
            _blobServiceClient = new BlobServiceClient(configuration.GetConnectionString("Storage"));
        }

        public async Task UploadBlob(string container, string fileName, Stream data)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);

            if (!await containerClient.ExistsAsync())
            {
                await containerClient.CreateAsync();
            }

            var blobClient = containerClient.GetBlockBlobClient(fileName);

            await blobClient.UploadAsync(data);
        }

        public async Task<Stream?> GetBlob(string container, string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);

            if (!await containerClient.ExistsAsync())
            {
                await containerClient.CreateAsync();
            }

            var blobClient = containerClient.GetBlockBlobClient(fileName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }
            
            var blob = await blobClient.DownloadAsync(CancellationToken.None);
            return blob.Value.Content;
        }
    }
}
