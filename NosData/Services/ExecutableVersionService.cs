using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NosData.DTOs;
using NosData.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NosData.Services
{
    public class ExecutableVersionService
    {
        private readonly ILogger<TranslationsService> _logger;
        private readonly NosFileService _nosFileService;
        private readonly BlobsService _blobsService;

        public ExecutableVersionService(ILogger<TranslationsService> logger, NosFileService nosFileService, BlobsService blobsService)
        {
            _logger = logger;
            _nosFileService = nosFileService;
            _blobsService = blobsService;
        }

        public async Task<NosTaleExecutableVersion?> GetExecutableVersion()
        {
            await using var ms = new MemoryStream();
            var stream = await _blobsService.GetBlob("version", "current.json");
            if (stream == null) return null;
            await stream.CopyToAsync(ms);
            return JsonConvert.DeserializeObject<NosTaleExecutableVersion>(Encoding.UTF8.GetString(ms.ToArray()));
        }

        public async Task RefreshExecutableVersion()
        {
            var startTime = DateTime.Now;
            _logger.LogInformation($"Executable version refresh started at {startTime}.");
            using var md5 = System.Security.Cryptography.MD5.Create();

            var clientDx = _nosFileService.FetchNosTaleBinary("NostaleClientX.exe");
            var clientGl = _nosFileService.FetchNosTaleBinary("NostaleClient.exe");

            var versionIndex = ByteArrayUtils.PatternAt(clientDx,
                new byte[]
                {
                    0x46, 0x00, 0x69, 0x00, 0x6c, 0x00, 0x65, 0x00, 0x56, 0x00, 0x65, 0x00, 0x72, 0x00, 0x73, 0x00,
                    0x69, 0x00, 0x6f, 0x00, 0x6e, 0x00
                }) + 0x1A;

            var version = "";
            for (var i = 0; i < 10; i++)
            {
                version += (char)clientDx[versionIndex + i * 2];
            }

            md5.TransformFinalBlock(clientDx, 0, clientDx.Length);
            var dxHash = md5.Hash;
            md5.TransformFinalBlock(clientGl, 0, clientGl.Length);
            var glHash = md5.Hash;

            Debug.Assert(glHash != null, nameof(glHash) + " != null");
            Debug.Assert(dxHash != null, nameof(dxHash) + " != null");

            var dto = new NosTaleExecutableVersion
            {
                Md5 =
                {
                    Client = BitConverter.ToString(glHash).Replace("-", string.Empty),
                    ClientX = BitConverter.ToString(dxHash).Replace("-", string.Empty)
                },
                Version = version
            };

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dto, Formatting.Indented));
            await using var ms = new MemoryStream(bytes);
            await _blobsService.UploadBlob("version", "current.json", ms);
            _logger.LogInformation($"Executable version refresh done in {(DateTime.Now - startTime).TotalSeconds} seconds!");
        }
    }
}
